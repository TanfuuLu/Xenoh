namespace Xenoh.Application.Common.Interfaces;

public sealed record CycleInsightAiRequest(string Language, string SnapshotJson);

public sealed record CycleInsightAiResult(string Json);

public interface ICycleInsightAi
{
    Task<CycleInsightAiResult> GenerateAsync(CycleInsightAiRequest request, CancellationToken cancellationToken);
}
