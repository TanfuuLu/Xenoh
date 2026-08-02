using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.AspNetCore.Identity;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    ChangeTracker ChangeTracker { get; }
    DbSet<ApplicationUser> ApplicationUsers { get; }
    DbSet<Plan> Plans { get; }
    DbSet<WeeklyWorkout> WeeklyWorkouts { get; }
    DbSet<DailyWorkout> DailyWorkouts { get; }
    DbSet<Exercise> Exercises { get; }
    DbSet<ExerciseSet> ExerciseSets { get; }
    DbSet<CoachClientRelationship> CoachClientRelationships { get; }
    DbSet<CoachInviteCode> CoachInviteCodes { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetCode> PasswordResetCodes { get; }
    DbSet<ExternalAuthTicket> ExternalAuthTickets { get; }
    DbSet<ExerciseTemplate> ExerciseTemplates { get; }
    DbSet<UserExercisePR> UserExercisePRs { get; }
    DbSet<UserExercisePRHistory> UserExercisePRHistories { get; }
    DbSet<WorkoutHistory> WorkoutHistories { get; }
    DbSet<BodyweightLog> BodyweightLogs { get; }
    DbSet<PlanComment> PlanComments { get; }
    DbSet<WeeklyWorkoutComment> WeeklyWorkoutComments { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<UserSubscription> UserSubscriptions { get; }
    DbSet<PaymentOrder> PaymentOrders { get; }
    DbSet<LegalAcceptance> LegalAcceptances { get; }
    DbSet<UserReport> UserReports { get; }
    DbSet<UserBlock> UserBlocks { get; }
    DbSet<Friendship> Friendships { get; }
    DbSet<TrainingDayShare> TrainingDayShares { get; }
    DbSet<TrainingDayShareExercise> TrainingDayShareExercises { get; }
    DbSet<TrainingDayShareSet> TrainingDayShareSets { get; }
    DbSet<TrainingDayShareLove> TrainingDayShareLoves { get; }
    DbSet<CommunitySettings> CommunitySettings { get; }
    DbSet<FitnessChallenge> FitnessChallenges { get; }
    DbSet<FitnessChallengeMember> FitnessChallengeMembers { get; }
    DbSet<FitnessChallengeCheckIn> FitnessChallengeCheckIns { get; }
    DbSet<OrganizerProfile> OrganizerProfiles { get; }
    DbSet<CompetitionEvent> CompetitionEvents { get; }
    DbSet<CompetitionEventStaff> CompetitionEventStaff { get; }
    DbSet<CompetitionCategory> CompetitionCategories { get; }
    DbSet<CompetitionRegistration> CompetitionRegistrations { get; }
    DbSet<CompetitionPaymentReceipt> CompetitionPaymentReceipts { get; }
    DbSet<PowerliftingCompetitionResult> PowerliftingCompetitionResults { get; }
    DbSet<BodybuildingCompetitionResult> BodybuildingCompetitionResults { get; }
    DbSet<CompetitionAuditLog> CompetitionAuditLogs { get; }
    DbSet<IdentityRole<Guid>> Roles { get; }
    DbSet<IdentityUserRole<Guid>> UserRoles { get; }
    DbSet<NutritionProfile> NutritionProfiles { get; }
    DbSet<NutritionDailyLog> NutritionDailyLogs { get; }
    DbSet<FoodItem> FoodItems { get; }
    DbSet<FoodServing> FoodServings { get; }
    DbSet<FoodLog> FoodLogs { get; }
    DbSet<MealPlanDay> MealPlanDays { get; }
    DbSet<MealPlanMeal> MealPlanMeals { get; }
    DbSet<MealPlanItem> MealPlanItems { get; }
    DbSet<UserAnalysis> UserAnalyses { get; }
    DbSet<AiFeatureCache> AiFeatureCaches { get; }
    DbSet<AiFeatureUsage> AiFeatureUsages { get; }
    DbSet<AiUsageQuota> AiUsageQuotas { get; }
    DbSet<AiChatConversation> AiChatConversations { get; }
    DbSet<AiChatMessage> AiChatMessages { get; }
    DbSet<RevokedToken> RevokedTokens { get; }
    DbSet<Message> Messages { get; }
    DbSet<ChatMessageAttachment> ChatMessageAttachments { get; }
    DbSet<CycleDailyLog> CycleDailyLogs { get; }
    DbSet<CycleSettings> CycleSettings { get; }
    DbSet<AdminAuditLog> AdminAuditLogs { get; }
    DbSet<WebsiteActivityEvent> WebsiteActivityEvents { get; }
    DbSet<WebsiteBugReport> WebsiteBugReports { get; }
    DbSet<StoredFile> StoredFiles { get; }
    DbSet<StoredFileShare> StoredFileShares { get; }
    DbSet<PromotionCode> PromotionCodes { get; }
    DbSet<AccountDeletionRequest> AccountDeletionRequests { get; }
    DbSet<AccountDeletionAuditLog> AccountDeletionAuditLogs { get; }
    DbSet<SupplementRegimen> SupplementRegimens { get; }
    DbSet<SupplementScheduleVersion> SupplementScheduleVersions { get; }
    DbSet<SupplementDoseSlot> SupplementDoseSlots { get; }
    DbSet<SupplementIntakeLog> SupplementIntakeLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
