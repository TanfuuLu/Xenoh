using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

/// <summary>
/// Calls OpenAI Chat Completions (strict JSON) to produce women's-health-aware,
/// cycle-phase training and nutrition insight for Xenoh.
/// </summary>
public sealed class OpenAiCycleInsightAi(
    HttpClient httpClient,
    IOptions<OpenAiOptions> optionsAccessor
) : ICycleInsightAi
{
    private readonly OpenAiOptions _options = optionsAccessor.Value;

    public async Task<CycleInsightAiResult> GenerateAsync(
        CycleInsightAiRequest request,
        CancellationToken cancellationToken)
    {
        var languageInstruction = request.Language == "vi"
            ? $"{XenohCoachPrompt.Vietnamese}\nAll text fields must be Vietnamese."
            : XenohCoachPrompt.English;

        var systemPrompt = $$"""
{{XenohCoachPrompt.Core}}

# Feature goal
Use a female athlete's menstrual-cycle stats, recent daily logs
(flow, symptoms, mood, energy), and how their training performance aggregates by cycle phase.
Help them train and eat smarter across their cycle without treating calendar phase as destiny.

Rules:
- Lead with what to do; back it with patterns from the snapshot (phase, cycle day, symptoms, energy, training volume/RPE/completion by phase).
- Be specific and practical: name the phase, the kind of training to push or pull back, the symptom to manage, the nutrition lever (protein, iron, carbs, hydration, sodium).
- Use the per-phase training aggregation to surface real correlations (e.g. lower completion or higher RPE in the luteal phase) and give an adjustment.
- Logged symptoms, energy, and the athlete's observed performance override calendar-based defaults. State whether a recommendation comes from their own pattern or general cycle-aware guidance.
- Make each phase recommendation operational: what to emphasize, what to monitor, and how to adjust when energy or performance differs from the expected pattern.
- General fitness coaching only. NO medical diagnosis, NO treatment claims, NO contraception or fertility advice.
- The disclaimer field must clearly state this is general guidance, not medical advice, and that severe, unusual, or irregular symptoms should be discussed with a doctor.
- If data is sparse (needsData or few cycles), say what to log next to unlock better guidance and keep recommendations general.
- phaseRecommendations must contain exactly four items, one for each phase: Menstrual, Follicular, Ovulation, Luteal — each with a concrete training tip and a concrete nutrition tip.
{{languageInstruction}}

Return JSON ONLY matching this exact shape (no markdown, no commentary):
{
  "summary": "string (2-4 sentences, what to focus on across the cycle)",
  "cyclePatterns": ["string (1-3 items about cycle regularity/length/timing)"],
  "symptomPatterns": ["string (1-3 items about recurring symptoms and how to manage them)"],
  "trainingCorrelations": ["string (1-3 items linking phase to training performance with an adjustment)"],
  "phaseRecommendations": [
    { "phase": "Menstrual",  "training": "string", "nutrition": "string" },
    { "phase": "Follicular", "training": "string", "nutrition": "string" },
    { "phase": "Ovulation",  "training": "string", "nutrition": "string" },
    { "phase": "Luteal",     "training": "string", "nutrition": "string" }
  ],
  "cautions": ["string (0-3 items: when to back off or see a professional)"],
  "disclaimer": "string (general guidance, not medical advice; see a doctor for severe/irregular symptoms)"
}
""";

        var userPrompt = $"""
Cycle snapshot:
```json
{request.SnapshotJson}
```
""";

        var json = await SendJsonPromptAsync(systemPrompt, userPrompt, 0.4, cancellationToken);
        return new CycleInsightAiResult(json);
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
            ["max_completion_tokens"] = _options.MaxJsonCompletionTokens,
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
        var choice = doc.RootElement.GetProperty("choices")[0];

        if (choice.TryGetProperty("finish_reason", out var finishReason) &&
            finishReason.GetString() == "length")
        {
            throw new InvalidOperationException(
                "AI response was cut off before completing. Please try again.");
        }

        var content = choice
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("OpenAI returned empty content.");

        return content;
    }
}
