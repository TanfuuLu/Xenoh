using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

/// <summary>
/// Calls OpenAI Chat Completions with strict JSON response formats for Xenoh AI features.
/// </summary>
public sealed class OpenAiUserAnalysisAi(
    HttpClient httpClient,
    IOptions<OpenAiOptions> optionsAccessor
) : IUserAnalysisAi
{
    private readonly OpenAiOptions _options = optionsAccessor.Value;

    public async Task<UserAnalysisAiResult> GenerateAsync(
        UserAnalysisAiRequest request,
        CancellationToken cancellationToken)
    {
        var languageInstruction = request.Language == "vi"
            ? "Respond ENTIRELY in Vietnamese. All headlines, details and action items must be Vietnamese."
            : "Respond entirely in English.";

        var systemPrompt = $$"""
You are a fitness analyst for the Xenoh training app. The user is reading a dashboard "insights" page.
Given a JSON snapshot of their training and body-metric data, write a concise, encouraging, evidence-based analysis.

Rules:
- Be specific. Reference numbers from the snapshot (sets, kg, %, days).
- No medical claims, no diagnoses.
- Tone: friendly coach, direct, no fluff.
- If data is missing or sparse, acknowledge it and suggest what to log next.
- For muscleBalance, compare recent per-muscle volume and call out meaningful push/pull or upper/lower imbalance.
- For effortGap, reason from sets where actual work missed plan at high RPE, or hit plan at low RPE.
- {{languageInstruction}}

Return JSON ONLY matching this exact shape (no markdown, no commentary):
{
  "trainingAdherence":  { "headline": "string (<= 90 chars)", "detail": "string (1-3 sentences)" },
  "bodyMetrics":        { "headline": "string (<= 90 chars)", "detail": "string (1-3 sentences)" },
  "volumeStrength":     { "headline": "string (<= 90 chars)", "detail": "string (1-3 sentences)" },
  "muscleBalance":      { "headline": "string (<= 90 chars)", "detail": "string (1-3 sentences)" },
  "effortGap":          { "headline": "string (<= 90 chars)", "detail": "string (1-3 sentences)" },
  "recommendation":     { "headline": "string (<= 90 chars)", "actions": ["string", "string", "string (max 3, each <= 120 chars)"] }
}
""";

        var userPrompt = $"""
Snapshot:
```json
{request.Snapshot}
```
""";

        var json = await SendJsonPromptAsync(systemPrompt, userPrompt, 0.4, cancellationToken);
        return new UserAnalysisAiResult(json);
    }

    public async Task<StarterPlanAiResult> GenerateStarterPlanAsync(
        StarterPlanAiRequest request,
        CancellationToken cancellationToken)
    {
        var languageInstruction = request.Language == "vi"
            ? "Use Vietnamese for planName and notes."
            : "Use English for planName and notes.";

        var systemPrompt = $$"""
You are a strength and hypertrophy programming assistant for Xenoh.
Create a practical 4-week starter plan using ONLY exerciseTemplateId values from the provided catalog.

Rules:
- Do not invent exercises.
- Match the questionnaire goal, experience, daysPerWeek, and equipment as closely as the catalog allows.
- Respect the requested startDate and endDate as the plan duration; the returned weekly pattern will be repeated across that date range by the application.
- Use the optional description only as user preference context, not as permission to invent unavailable exercises.
- Never schedule 4 or more training days in a row. After 2-3 consecutive training days, there must be at least 1 rest day.
- Put compound exercises first in each workout when suitable: squat, hinge/deadlift, press, row, pull-up/pulldown, lunge, hip thrust, clean/press.
- Start most sessions with 1-3 compound movements before isolation or accessory exercises.
- Use conservative beginner/intermediate loading: plannedWeight may be null when unknown.
- Keep each workout 4-7 exercises.
- Use DayOfWeek enum names: Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday.
- {{languageInstruction}}

Return JSON ONLY matching this exact shape:
{
  "planName": "string (<= 100 chars)",
  "days": [
    {
      "dayOfWeek": "Monday",
      "focus": "string",
      "exercises": [
        {
          "exerciseTemplateId": "guid from catalog",
          "sets": 3,
          "reps": 8,
          "plannedWeight": null,
          "notes": "string or null"
        }
      ]
    }
  ]
}
""";

        var userPrompt = $"""
Questionnaire:
```json
{request.QuestionnaireJson}
```

Exercise catalog:
```json
{request.ExerciseCatalogJson}
```
""";

        var json = await SendJsonPromptAsync(systemPrompt, userPrompt, 0.35, cancellationToken);
        return new StarterPlanAiResult(json);
    }

    public async Task<PlanBalanceAiResult> ReviewPlanBalanceAsync(
        PlanBalanceAiRequest request,
        CancellationToken cancellationToken)
    {
        var languageInstruction = request.Language == "vi"
            ? "Respond entirely in Vietnamese."
            : "Respond entirely in English.";

        var systemPrompt = $$"""
You are a plan-quality reviewer for Xenoh. Review a workout plan snapshot for balance and safety.

Rules:
- Be specific about muscle group distribution, heavy day clustering, missing movement patterns, and recovery gaps.
- Do not diagnose injuries or make medical claims.
- If the plan is empty or sparse, say so and recommend what to add.
- Keep warnings non-blocking and coach-like.
- {{languageInstruction}}

Return JSON ONLY matching this exact shape:
{
  "headline": "string (<= 90 chars)",
  "severity": "Low|Medium|High",
  "summary": "string (1-3 sentences)",
  "warnings": ["string"],
  "suggestions": ["string"]
}
""";

        var userPrompt = $"""
Plan snapshot:
```json
{request.PlanSnapshotJson}
```
""";

        var json = await SendJsonPromptAsync(systemPrompt, userPrompt, 0.25, cancellationToken);
        return new PlanBalanceAiResult(json);
    }

    private async Task<string> SendJsonPromptAsync(
        string systemPrompt,
        string userPrompt,
        double temperature,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("OpenAI API key is not configured.");

        var payload = new JsonObject
        {
            ["model"] = _options.Model,
            ["temperature"] = temperature,
            ["response_format"] = new JsonObject { ["type"] = "json_object" },
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = systemPrompt },
                new JsonObject { ["role"] = "user", ["content"] = userPrompt }
            }
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = JsonContent.Create(payload)
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI request failed ({(int)response.StatusCode}): {raw}");

        using var doc = JsonDocument.Parse(raw);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("OpenAI returned empty content.");

        return content;
    }
}
