namespace Xenoh.Infrastructure.Services;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gpt-5.5";
    public string BaseUrl { get; init; } = "https://api.openai.com/v1";
    public int TimeoutSeconds { get; init; } = 60;
}
