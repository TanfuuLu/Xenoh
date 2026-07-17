using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public sealed class FitnessChallengeMember : BaseEntity
{
    public Guid ChallengeId { get; set; }
    public FitnessChallenge Challenge { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public FitnessChallengeMemberStatus Status { get; set; } = FitnessChallengeMemberStatus.Invited;
    public DateTime? RespondedAt { get; set; }
    public DateOnly? LastBehindReminderWeekStart { get; set; }
    public DateOnly? LastCompletionNotificationWeekStart { get; set; }
}
