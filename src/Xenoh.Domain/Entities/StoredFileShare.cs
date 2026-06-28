using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

/// <summary>
/// A coach sharing one of their <see cref="StoredFile"/>s with one connected client.
/// The recipient gets read/download access only; the file still belongs to (and counts
/// against the quota of) the owner.
/// </summary>
public class StoredFileShare : BaseEntity
{
    public Guid FileId { get; set; }
    public StoredFile File { get; set; } = null!;

    public Guid SharedByUserId { get; set; }

    public Guid SharedWithUserId { get; set; }
    public ApplicationUser SharedWithUser { get; set; } = null!;
}
