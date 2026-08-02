using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.FitnessChallenges;

public sealed record ChallengeWeekProgressResponse(
    DateOnly StartsOn,
    DateOnly EndsOn,
    int CompletedSessions,
    int TargetSessions);

public sealed record ChallengeMemberResponse(
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    string Status,
    bool IsCreator,
    decimal? Score,
    int? Rank,
    string ScoreUnit,
    bool BaselineReady,
    bool CheckedInToday,
    int CompletedSessions,
    int TargetSessions,
    IReadOnlyList<ChallengeWeekProgressResponse> Weeks);

public sealed record FitnessChallengeResponse(
    Guid Id,
    string Title,
    string Description,
    Guid CreatorId,
    string CreatorName,
    FitnessChallengeMetricType MetricType,
    FitnessChallengeAccessType AccessType,
    int TargetSessionsPerWeek,
    IReadOnlyList<CompetitionLiftType> SelectedLifts,
    string? CheckInPrompt,
    int Capacity,
    int AcceptedCount,
    int ReservedCount,
    string TimeZoneId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string Status,
    bool CanManage,
    bool CanJoin,
    bool JoinClosed,
    IReadOnlyList<ChallengeMemberResponse> Members);

public sealed record FitnessChallengeSummaryResponse(
    Guid Id,
    string Title,
    string Description,
    Guid CreatorId,
    string CreatorName,
    string? CreatorAvatarUrl,
    FitnessChallengeMetricType MetricType,
    FitnessChallengeAccessType AccessType,
    int Capacity,
    int AcceptedCount,
    int ReservedCount,
    string TimeZoneId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string Status,
    bool CanJoin);

public sealed record ChallengeInviteeResponse(
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    string Relationship);

public sealed record FitnessChallengeInput
{
    [Required, MinLength(3), MaxLength(80)]
    public string Title { get; init; } = string.Empty;
    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;
    public FitnessChallengeMetricType MetricType { get; init; }
    public FitnessChallengeAccessType AccessType { get; init; }
    [Range(0, 7)]
    public int TargetSessionsPerWeek { get; init; }
    public IReadOnlyList<CompetitionLiftType> SelectedLifts { get; init; } = [];
    [MaxLength(160)]
    public string? CheckInPrompt { get; init; }
    [Range(2, 25)]
    public int Capacity { get; init; }
    [Required, MaxLength(80)]
    public string TimeZoneId { get; init; } = "Asia/Ho_Chi_Minh";
    [MaxLength(19)]
    public string? StartsAtLocal { get; init; }
    [MaxLength(19)]
    public string? EndsAtLocal { get; init; }
    public DateTime StartsAtUtc { get; init; }
    public DateTime EndsAtUtc { get; init; }
    public IReadOnlyList<Guid> InviteeUserIds { get; init; } = [];
}

public sealed record CreateFitnessChallengeCommand(FitnessChallengeInput Input) : IRequest<FitnessChallengeResponse>;
public sealed record UpdateFitnessChallengeCommand(Guid ChallengeId, FitnessChallengeInput Input) : IRequest<FitnessChallengeResponse>;
public sealed record GetFitnessChallengesQuery(string? Status = null) : IRequest<IReadOnlyList<FitnessChallengeResponse>>;
public sealed record GetDiscoverableFitnessChallengesQuery : IRequest<IReadOnlyList<FitnessChallengeSummaryResponse>>;
public sealed record GetFitnessChallengeQuery(Guid ChallengeId) : IRequest<FitnessChallengeResponse>;
public sealed record GetChallengeInviteesQuery : IRequest<IReadOnlyList<ChallengeInviteeResponse>>;
public sealed record AcceptFitnessChallengeCommand(Guid ChallengeId) : IRequest<FitnessChallengeResponse>;
public sealed record DeclineFitnessChallengeCommand(Guid ChallengeId) : IRequest;
public sealed record JoinFitnessChallengeCommand(Guid ChallengeId) : IRequest<FitnessChallengeResponse>;
public sealed record LeaveFitnessChallengeCommand(Guid ChallengeId) : IRequest;
public sealed record CancelFitnessChallengeCommand(Guid ChallengeId) : IRequest;
public sealed record InviteFitnessChallengeMembersCommand(Guid ChallengeId, IReadOnlyList<Guid> UserIds) : IRequest<FitnessChallengeResponse>;
public sealed record RemoveFitnessChallengeMemberCommand(Guid ChallengeId, Guid UserId) : IRequest;
public sealed record CheckInFitnessChallengeCommand(Guid ChallengeId, string? Note) : IRequest<FitnessChallengeResponse>;
public sealed record UndoFitnessChallengeCheckInCommand(Guid ChallengeId) : IRequest<FitnessChallengeResponse>;
