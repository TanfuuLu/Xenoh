using Xenoh.Application.Features.Share.Queries.GetPrShareData;

namespace Xenoh.Application.Common.Interfaces;

/// <summary>
/// Stores rendered PR share cards in the public R2 share-images folder, keyed by
/// user + exercise + PR version. Generation is lazy: the card is only rendered and
/// uploaded the first time a given PR version is requested.
/// </summary>
public interface IPrShareImageStorage
{
    /// <summary>
    /// True when the R2 share bucket is configured. When false the caller should
    /// fall back to serving the freshly rendered bytes directly.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Ensures the share card for this PR exists in R2 (rendering + uploading it on a
    /// cache miss) and returns its public URL.
    /// </summary>
    Task<string> EnsureAsync(
        Guid userId,
        Guid exerciseTemplateId,
        PrShareData data,
        Func<CancellationToken, Task<byte[]>> render,
        CancellationToken ct = default);

    /// <summary>
    /// Reads the stored share-card bytes for this PR, or null if they are not in R2.
    /// </summary>
    Task<byte[]?> TryGetAsync(
        Guid userId,
        Guid exerciseTemplateId,
        PrShareData data,
        CancellationToken ct = default);
}
