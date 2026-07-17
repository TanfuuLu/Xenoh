using System.ComponentModel.DataAnnotations;
using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public sealed class FitnessChallenge : BaseEntity
{
    public Guid CreatorId { get; set; }
    public ApplicationUser Creator { get; set; } = null!;

    [MaxLength(80)]
    public string Title { get; set; } = string.Empty;
    public int TargetSessionsPerWeek { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public FitnessChallengeStatus Status { get; set; } = FitnessChallengeStatus.Upcoming;
    public DateTime? CancelledAt { get; set; }
    public DateTime? CompletionNotifiedAt { get; set; }

    public ICollection<FitnessChallengeMember> Members { get; set; } = [];
}
