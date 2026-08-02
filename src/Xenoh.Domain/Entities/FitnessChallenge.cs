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
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    public FitnessChallengeMetricType MetricType { get; set; } = FitnessChallengeMetricType.TrainingSessions;
    public FitnessChallengeAccessType AccessType { get; set; } = FitnessChallengeAccessType.InviteOnly;
    public int TargetSessionsPerWeek { get; set; }
    public List<CompetitionLiftType> SelectedLifts { get; set; } = [];
    [MaxLength(160)]
    public string? CheckInPrompt { get; set; }
    public int Capacity { get; set; } = 10;
    [MaxLength(80)]
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public FitnessChallengeStatus Status { get; set; } = FitnessChallengeStatus.Upcoming;
    public DateTime? CancelledAt { get; set; }
    public DateTime? StartNotifiedAt { get; set; }
    public DateTime? CompletionNotifiedAt { get; set; }
    public uint Version { get; set; }

    public ICollection<FitnessChallengeMember> Members { get; set; } = [];
    public ICollection<FitnessChallengeCheckIn> CheckIns { get; set; } = [];
}
