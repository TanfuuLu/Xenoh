using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class CoachRating : BaseEntity
{
    public Guid CoachId { get; set; }
    public ApplicationUser Coach { get; set; } = null!;

    public Guid ClientId { get; set; }
    public ApplicationUser Client { get; set; } = null!;

    public int Rating { get; set; }
    public string? Comment { get; set; }
}
