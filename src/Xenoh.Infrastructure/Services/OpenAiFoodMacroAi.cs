using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Infrastructure.Services;

public sealed class OpenAiFoodMacroAi(
    HttpClient httpClient,
    IOptions<OpenAiOptions> optionsAccessor,
    ILogger<OpenAiFoodMacroAi> logger
) : IFoodMacroAi
{
    private readonly OpenAiOptions _options = optionsAccessor.Value;

    public async Task<FoodMacroAiResult> ResolveAsync(FoodMacroAiRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("OpenAI API key is not configured.");

        var systemPrompt = """
You are a precise nutrition database assistant.
Given a food name in Vietnamese or English, return the per-100g macronutrients for a typical home-cooked or standard preparation.

Rules:
- Calories must satisfy: calories ≈ (protein * 4) + (carbs * 4) + (fat * 9), within 5%.
- All values must be non-negative decimals with up to 2 decimal places.
- If the food name is ambiguous, assume the most common preparation (e.g. "gà" = boiled chicken).
- defaultServingLabel and defaultServingGrams are optional; provide them only if there is an obvious single serving (e.g. "quả" for avocado, "lát" for bread).
- No medical claims, no disclaimers.

Return JSON ONLY matching this exact shape (no markdown, no commentary):
{
  "nameVi": "string — Vietnamese name",
  "nameEn": "string — English name",
  "caloriesPer100g": number,
  "proteinPer100g": number,
  "carbsPer100g": number,
  "fatPer100g": number,
  "defaultServingLabel": "string or null",
  "defaultServingGrams": number or null
}
""";

        var userPrompt = $"Food name: \"{request.FoodName}\"";

        var payload = new JsonObject
        {
            ["model"] = _options.Model,
            ["temperature"] = 0.1,
            ["max_completion_tokens"] = _options.MaxJsonCompletionTokens,
            ["response_format"] = new JsonObject { ["type"] = "json_object" },
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = systemPrompt },
                new JsonObject { ["role"] = "user",   ["content"] = userPrompt }
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
        {
            // The upstream body carries org ids and rate-limit state, and controllers surface
            // InvalidOperationException messages straight to the client. Keep it in the log.
            logger.LogError("OpenAI request failed ({StatusCode}): {Body}", (int)response.StatusCode, raw);
            throw new InvalidOperationException("The AI service is temporarily unavailable. Please try again.");
        }

        using var doc = JsonDocument.Parse(raw);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("OpenAI returned empty content.");

        using var resultDoc = JsonDocument.Parse(content);
        var r = resultDoc.RootElement;

        return new FoodMacroAiResult(
            NameVi: r.GetProperty("nameVi").GetString() ?? request.FoodName,
            NameEn: r.GetProperty("nameEn").GetString() ?? request.FoodName,
            CaloriesPer100g: r.GetProperty("caloriesPer100g").GetDecimal(),
            ProteinPer100g: r.GetProperty("proteinPer100g").GetDecimal(),
            CarbsPer100g: r.GetProperty("carbsPer100g").GetDecimal(),
            FatPer100g: r.GetProperty("fatPer100g").GetDecimal(),
            DefaultServingLabel: r.TryGetProperty("defaultServingLabel", out var lbl) && lbl.ValueKind != JsonValueKind.Null
                ? lbl.GetString()
                : null,
            DefaultServingGrams: r.TryGetProperty("defaultServingGrams", out var grm) && grm.ValueKind != JsonValueKind.Null
                ? grm.GetDecimal()
                : null
        );
    }
}
