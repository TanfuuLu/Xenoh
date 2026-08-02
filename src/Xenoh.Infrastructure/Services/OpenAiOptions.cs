namespace Xenoh.Infrastructure.Services;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gpt-5.5";
    public string BaseUrl { get; init; } = "https://api.openai.com/v1";
    public int TimeoutSeconds { get; init; } = 60;
    public int MaxChatCompletionTokens { get; init; } = 450;
    public int MaxJsonCompletionTokens { get; init; } = 1800;
    public int MaxAnalysisCompletionTokens { get; init; } = 3200;

    /// <summary>
    /// Larger budget for the AI starter-plan call, whose JSON (multiple days × exercises × notes)
    /// is far bigger than the other JSON features and overflows <see cref="MaxJsonCompletionTokens"/>.
    /// </summary>
    public int MaxStarterPlanCompletionTokens { get; init; } = 4000;
}
