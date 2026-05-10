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
    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(300)]
    public string? AvatarUrl { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<PasswordResetCode> PasswordResetCodes { get; set; } = [];
    public ICollection<ExternalAuthTicket> ExternalAuthTickets { get; set; } = [];
    public ICollection<ExerciseTemplate> CustomExerciseTemplates { get; set; } = [];
    public ICollection<Plan> Plans { get; set; } = [];
    public NutritionProfile? NutritionProfile { get; set; }
    public ICollection<NutritionDailyLog> NutritionDailyLogs { get; set; } = [];

    // As client
    public CoachClientRelationship? CoachRelationship { get; set; }

    // As coach
    public ICollection<CoachClientRelationship> Clients { get; set; } = [];

    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<CoachRating> CoachRatingsReceived { get; set; } = [];
    public ICollection<CoachRating> CoachRatingsGiven { get; set; } = [];
    public CoachMarketplaceProfile? CoachMarketplaceProfile { get; set; }
    public ICollection<UserReport> ReportsMade { get; set; } = [];
    public ICollection<UserReport> ReportsReceived { get; set; } = [];
    public ICollection<UserReport> ReportsReviewed { get; set; } = [];

    public ICollection<UserBlock> BlocksMade { get; set; } = [];
    public ICollection<UserBlock> BlocksReceived { get; set; } = [];

    // XP / Leveling
    public long TotalXp { get; set; } = 0;
    public int  Level   { get; set; } = 1;

    public UserSubscription? Subscription { get; set; }
}
