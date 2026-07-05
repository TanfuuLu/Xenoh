using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

/// <summary>
/// A document (PDF / Word) uploaded by a user and kept in R2 object storage.
/// The owner counts its <see cref="SizeBytes"/> against their per-tier storage quota.
/// </summary>
public class StoredFile : BaseEntity
{
    public Guid OwnerId { get; set; }
    public ApplicationUser Owner { get; set; } = null!;

    /// <summary>Original file name as uploaded (used for downloads).</summary>
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    /// <summary>R2 object key — internal, never exposed publicly.</summary>
    public string StorageKey { get; set; } = string.Empty;

    public ICollection<StoredFileShare> Shares { get; set; } = [];
}
