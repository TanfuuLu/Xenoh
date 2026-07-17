using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // User Stats (Wave 2)
    public decimal? Height { get; set; }        // cm
    public Gender? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public DevelopmentDirection? DevelopmentDirection { get; set; }
    public TrainingDiscipline? TrainingDiscipline { get; set; }
    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(300)]
    public string? AvatarUrl { get; set; }

    [MaxLength(300)]
    public string? FacebookUrl { get; set; }

    [MaxLength(300)]
    public string? InstagramUrl { get; set; }

    [MaxLength(300)]
    public string? ZaloUrl { get; set; }

    [MaxLength(2)]
    public string PreferredLanguage { get; set; } = "en";

    [MaxLength(5)]
    public string PreferredTheme { get; set; } = "light";

    [MaxLength(2)]
    public string PreferredWeightUnit { get; set; } = "kg";

    public bool TrackRpe { get; set; } = true;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<PasswordResetCode> PasswordResetCodes { get; set; } = [];
    public ICollection<ExternalAuthTicket> ExternalAuthTickets { get; set; } = [];
    public ICollection<ExerciseTemplate> CustomExerciseTemplates { get; set; } = [];
    public ICollection<Plan> Plans { get; set; } = [];
    public NutritionProfile? NutritionProfile { get; set; }
    public ICollection<NutritionDailyLog> NutritionDailyLogs { get; set; } = [];
    public ICollection<FoodLog> FoodLogs { get; set; } = [];
    public ICollection<MealPlanDay> MealPlanDays { get; set; } = [];

    // Menstrual cycle tracking
    public CycleSettings? CycleSettings { get; set; }
    public ICollection<CycleDailyLog> CycleDailyLogs { get; set; } = [];

    // As client
    public CoachClientRelationship? CoachRelationship { get; set; }

    // As coach
    public ICollection<CoachClientRelationship> Clients { get; set; } = [];

    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<UserReport> ReportsMade { get; set; } = [];
    public ICollection<UserReport> ReportsReceived { get; set; } = [];
    public ICollection<UserReport> ReportsReviewed { get; set; } = [];

    public ICollection<UserBlock> BlocksMade { get; set; } = [];
    public ICollection<UserBlock> BlocksReceived { get; set; } = [];
    public ICollection<Friendship> FriendshipsAsUserA { get; set; } = [];
    public ICollection<Friendship> FriendshipsAsUserB { get; set; } = [];
    public ICollection<Friendship> FriendRequestsMade { get; set; } = [];
    public ICollection<Friendship> FriendRequestsReceived { get; set; } = [];
    public ICollection<TrainingDayShare> TrainingDayShares { get; set; } = [];
    public ICollection<TrainingDayShareLove> TrainingDayShareLoves { get; set; } = [];
    public CommunitySettings? CommunitySettings { get; set; }
    public ICollection<FitnessChallenge> CreatedFitnessChallenges { get; set; } = [];
    public ICollection<FitnessChallengeMember> FitnessChallengeMemberships { get; set; } = [];

    // XP / Leveling
    public long TotalXp { get; set; } = 0;
    public int Level { get; set; } = 1;

    public UserSubscription? Subscription { get; set; }
}
