namespace Xenoh.Domain.Enums;

public enum CommunityStatsVisibility
{
    Friends = 0,
    OnlyMe = 1
}

public enum FitnessChallengeStatus
{
    Upcoming = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3
}

public enum FitnessChallengeMemberStatus
{
    Invited = 0,
    Accepted = 1,
    Declined = 2,
    Left = 3,
    Removed = 4
}

public enum FitnessChallengeMetricType
{
    TrainingSessions = 0,
    TrainingStreak = 1,
    SbdImprovement = 2,
    CustomCheckIns = 3
}

public enum FitnessChallengeAccessType
{
    InviteOnly = 0,
    Connections = 1,
    Community = 2
}
