using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;

namespace Xenoh.Infrastructure.Services;

public sealed class AccountDeletionService(
    ApplicationDbContext db,
    ITokenBlacklist tokenBlacklist,
    IDocumentStorageService documentStorage,
    ILogger<AccountDeletionService> logger) : IAccountDeletionService
{
    public async Task DeleteAccountAsync(
        Guid userId,
        AccountDeletionRequest? deletionRequest,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var user = await db.ApplicationUsers.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        deletionRequest ??= CreateDirectDeletionRequest(user, now);

        var deletedEmail = $"deleted-{user.Id:N}@deleted.xenoh.invalid";
        var storedObjectKeys = await CollectStoredObjectKeysAsync(userId, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await DeleteProductDataAsync(userId, cancellationToken);
            await RevokeAuthStateAsync(userId, user, deletedEmail, cancellationToken);

            await db.AccountDeletionRequests
                .Where(r => r.UserId == userId && r.Id != deletionRequest.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Email, deletedEmail), cancellationToken);

            AnonymizeUser(user, deletedEmail);
            AnonymizeRetainedRows(userId, deletedEmail, deletionRequest, now);

            if (!string.IsNullOrWhiteSpace(accessToken))
                await tokenBlacklist.RevokeTokenAsync(accessToken);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            await MarkFailedAsync(deletionRequest.Id, ex, cancellationToken);
            throw;
        }

        await DeleteStoredObjectsBestEffortAsync(storedObjectKeys, cancellationToken);
    }

    private AccountDeletionRequest CreateDirectDeletionRequest(ApplicationUser user, DateTime now)
    {
        var request = new AccountDeletionRequest
        {
            Email = (user.Email ?? user.UserName ?? $"user-{user.Id:N}@unknown.invalid").Trim().ToLowerInvariant(),
            UserId = user.Id,
            VerificationTokenHash = $"DIRECT-{Guid.NewGuid():N}",
            ExpiresAt = now,
            VerifiedAt = now,
            RetainUntil = now.AddYears(7),
            Status = AccountDeletionStatus.Verified
        };

        db.AccountDeletionRequests.Add(request);
        db.AccountDeletionAuditLogs.Add(new AccountDeletionAuditLog
        {
            AccountDeletionRequest = request,
            EventType = "Requested",
            Detail = "Authenticated user requested direct account deletion."
        });
        db.AccountDeletionAuditLogs.Add(new AccountDeletionAuditLog
        {
            AccountDeletionRequest = request,
            EventType = "Verified",
            Detail = "Authenticated request verified by JWT."
        });

        return request;
    }

    private async Task<List<string>> CollectStoredObjectKeysAsync(Guid userId, CancellationToken ct)
    {
        var relationshipIds = await db.CoachClientRelationships
            .Where(r => r.ClientId == userId || r.CoachId == userId)
            .Select(r => r.Id)
            .ToListAsync(ct);

        var messageIds = await db.Messages
            .Where(m => m.SenderId == userId || relationshipIds.Contains(m.RelationshipId))
            .Select(m => m.Id)
            .ToListAsync(ct);

        var keys = await db.StoredFiles
            .Where(f => f.OwnerId == userId)
            .Select(f => f.StorageKey)
            .ToListAsync(ct);

        keys.AddRange(await db.ChatMessageAttachments
            .Where(a => messageIds.Contains(a.MessageId))
            .Select(a => a.StorageKey)
            .ToListAsync(ct));

        return keys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.Ordinal).ToList();
    }

    private async Task DeleteProductDataAsync(Guid userId, CancellationToken ct)
    {
        var planIds = await db.Plans
            .Where(p => p.OwnerId == userId)
            .Select(p => p.Id)
            .ToListAsync(ct);
        var weeklyWorkoutIds = await db.WeeklyWorkouts
            .Where(w => planIds.Contains(w.PlanId))
            .Select(w => w.Id)
            .ToListAsync(ct);
        var dailyWorkoutIds = await db.DailyWorkouts
            .Where(d => weeklyWorkoutIds.Contains(d.WeeklyWorkoutId))
            .Select(d => d.Id)
            .ToListAsync(ct);
        var exerciseIds = await db.Exercises
            .Where(e => dailyWorkoutIds.Contains(e.DailyWorkoutId))
            .Select(e => e.Id)
            .ToListAsync(ct);
        var shareIds = await db.TrainingDayShares
            .Where(s => s.UserId == userId || dailyWorkoutIds.Contains(s.SourceDailyWorkoutId))
            .Select(s => s.Id)
            .ToListAsync(ct);
        var shareExerciseIds = await db.TrainingDayShareExercises
            .Where(e => shareIds.Contains(e.TrainingDayShareId))
            .Select(e => e.Id)
            .ToListAsync(ct);
        var relationshipIds = await db.CoachClientRelationships
            .Where(r => r.ClientId == userId || r.CoachId == userId)
            .Select(r => r.Id)
            .ToListAsync(ct);
        var messageIds = await db.Messages
            .Where(m => m.SenderId == userId || relationshipIds.Contains(m.RelationshipId))
            .Select(m => m.Id)
            .ToListAsync(ct);
        var fileIds = await db.StoredFiles
            .Where(f => f.OwnerId == userId)
            .Select(f => f.Id)
            .ToListAsync(ct);
        var mealPlanDayIds = await db.MealPlanDays
            .Where(d => d.UserId == userId)
            .Select(d => d.Id)
            .ToListAsync(ct);
        var mealPlanMealIds = await db.MealPlanMeals
            .Where(m => mealPlanDayIds.Contains(m.MealPlanDayId))
            .Select(m => m.Id)
            .ToListAsync(ct);
        var aiConversationIds = await db.AiChatConversations
            .Where(c => c.UserId == userId)
            .Select(c => c.Id)
            .ToListAsync(ct);
        var customFoodItemIds = await db.FoodItems
            .Where(f => f.CreatedByUserId == userId)
            .Select(f => f.Id)
            .ToListAsync(ct);

        await db.ChatMessageAttachments.Where(a => messageIds.Contains(a.MessageId)).ExecuteDeleteAsync(ct);
        await db.Messages.Where(m => messageIds.Contains(m.Id)).ExecuteDeleteAsync(ct);
        await db.CoachClientRelationships.Where(r => relationshipIds.Contains(r.Id)).ExecuteDeleteAsync(ct);
        await db.CoachInviteCodes.Where(c => c.CoachId == userId).ExecuteDeleteAsync(ct);
        await db.CoachInviteCodes
            .Where(c => c.UsedByClientId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.UsedByClientId, (Guid?)null)
                .SetProperty(c => c.IsUsed, false)
                .SetProperty(c => c.UsedAt, (DateTime?)null), ct);

        await db.StoredFileShares
            .Where(s => fileIds.Contains(s.FileId) || s.SharedByUserId == userId || s.SharedWithUserId == userId)
            .ExecuteDeleteAsync(ct);
        await db.StoredFiles.Where(f => fileIds.Contains(f.Id)).ExecuteDeleteAsync(ct);

        await db.TrainingDayShareSets.Where(s => shareExerciseIds.Contains(s.TrainingDayShareExerciseId)).ExecuteDeleteAsync(ct);
        await db.TrainingDayShareExercises.Where(e => shareIds.Contains(e.TrainingDayShareId)).ExecuteDeleteAsync(ct);
        await db.TrainingDayShareLoves.Where(l => shareIds.Contains(l.TrainingDayShareId) || l.UserId == userId).ExecuteDeleteAsync(ct);
        await db.TrainingDayShares.Where(s => shareIds.Contains(s.Id)).ExecuteDeleteAsync(ct);

        await db.MealPlanItems
            .Where(i => mealPlanMealIds.Contains(i.MealPlanMealId) || i.CheckedByUserId == userId)
            .ExecuteDeleteAsync(ct);
        await db.MealPlanMeals.Where(m => mealPlanMealIds.Contains(m.Id)).ExecuteDeleteAsync(ct);
        await db.MealPlanDays.Where(d => mealPlanDayIds.Contains(d.Id)).ExecuteDeleteAsync(ct);
        await db.FoodLogs.Where(l => l.UserId == userId).ExecuteDeleteAsync(ct);
        await db.NutritionDailyLogs.Where(l => l.UserId == userId).ExecuteDeleteAsync(ct);
        await db.NutritionProfiles.Where(p => p.UserId == userId).ExecuteDeleteAsync(ct);
        await db.FoodServings.Where(s => customFoodItemIds.Contains(s.FoodItemId)).ExecuteDeleteAsync(ct);
        await db.FoodItems.Where(f => customFoodItemIds.Contains(f.Id)).ExecuteDeleteAsync(ct);

        await db.ExerciseSets.Where(s => exerciseIds.Contains(s.ExerciseId)).ExecuteDeleteAsync(ct);
        await db.Exercises.Where(e => exerciseIds.Contains(e.Id)).ExecuteDeleteAsync(ct);
        await db.DailyWorkouts.Where(d => dailyWorkoutIds.Contains(d.Id)).ExecuteDeleteAsync(ct);
        await db.WeeklyWorkoutComments.Where(c => weeklyWorkoutIds.Contains(c.WeeklyWorkoutId) || c.AuthorId == userId).ExecuteDeleteAsync(ct);
        await db.WeeklyWorkouts.Where(w => weeklyWorkoutIds.Contains(w.Id)).ExecuteDeleteAsync(ct);
        await db.PlanComments.Where(c => planIds.Contains(c.PlanId) || c.AuthorId == userId).ExecuteDeleteAsync(ct);
        await db.Plans.Where(p => planIds.Contains(p.Id)).ExecuteDeleteAsync(ct);
        await db.Plans
            .Where(p => p.CreatedByCoachId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CreatedByCoachId, (Guid?)null), ct);

        await db.WorkoutHistories.Where(h => h.UserId == userId).ExecuteDeleteAsync(ct);
        await db.BodyweightLogs.Where(l => l.UserId == userId).ExecuteDeleteAsync(ct);
        await db.UserExercisePRHistories.Where(h => h.UserId == userId).ExecuteDeleteAsync(ct);
        await db.UserExercisePRs.Where(p => p.UserId == userId).ExecuteDeleteAsync(ct);
        await db.ExerciseTemplates
            .Where(t => t.OwnerId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.OwnerId, (Guid?)null)
                .SetProperty(t => t.IsArchived, true)
                .SetProperty(t => t.Description, (string?)null)
                .SetProperty(t => t.ImageUrl, (string?)null), ct);

        await db.AiChatMessages.Where(m => aiConversationIds.Contains(m.ConversationId)).ExecuteDeleteAsync(ct);
        await db.AiChatConversations.Where(c => aiConversationIds.Contains(c.Id)).ExecuteDeleteAsync(ct);
        await db.UserAnalyses.Where(a => a.UserId == userId).ExecuteDeleteAsync(ct);
        await db.AiFeatureCaches.Where(c => c.UserId == userId || c.SubjectUserId == userId).ExecuteDeleteAsync(ct);
        await db.AiFeatureUsages.Where(u => u.UserId == userId).ExecuteDeleteAsync(ct);
        await db.AiUsageQuotas.Where(q => q.UserId == userId).ExecuteDeleteAsync(ct);

        await db.CycleDailyLogs.Where(l => l.UserId == userId).ExecuteDeleteAsync(ct);
        await db.CycleSettings.Where(s => s.UserId == userId).ExecuteDeleteAsync(ct);
        await db.Notifications.Where(n => n.RecipientId == userId).ExecuteDeleteAsync(ct);
        await db.UserBlocks.Where(b => b.BlockerId == userId || b.BlockedId == userId).ExecuteDeleteAsync(ct);
        await db.Friendships
            .Where(f => f.UserAId == userId || f.UserBId == userId || f.RequesterId == userId || f.AddresseeId == userId)
            .ExecuteDeleteAsync(ct);
        await db.UserReports
            .Where(r => r.ReporterId == userId || r.ReportedUserId == userId || r.ReviewedById == userId)
            .ExecuteDeleteAsync(ct);
        await db.WebsiteBugReports
            .Where(r => r.UserId == userId || r.ReviewedById == userId)
            .ExecuteDeleteAsync(ct);
        await db.WebsiteActivityEvents
            .Where(e => e.UserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.UserId, (Guid?)null)
                .SetProperty(e => e.SessionId, "deleted-account")
                .SetProperty(e => e.UserAgent, (string?)null), ct);
    }

    private async Task RevokeAuthStateAsync(
        Guid userId,
        ApplicationUser user,
        string deletedEmail,
        CancellationToken ct)
    {
        await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true), ct);
        await db.PasswordResetCodes.Where(c => c.UserId == userId).ExecuteDeleteAsync(ct);
        await db.ExternalAuthTickets.Where(t => t.UserId == userId).ExecuteDeleteAsync(ct);
        await db.UserRoles.Where(r => r.UserId == userId).ExecuteDeleteAsync(ct);
        await db.UserClaims.Where(c => c.UserId == userId).ExecuteDeleteAsync(ct);
        await db.UserLogins.Where(l => l.UserId == userId).ExecuteDeleteAsync(ct);
        await db.UserTokens.Where(t => t.UserId == userId).ExecuteDeleteAsync(ct);

        user.Email = deletedEmail;
        user.NormalizedEmail = deletedEmail.ToUpperInvariant();
        user.UserName = deletedEmail;
        user.NormalizedUserName = deletedEmail.ToUpperInvariant();
    }

    private static void AnonymizeUser(ApplicationUser user, string deletedEmail)
    {
        user.Email = deletedEmail;
        user.NormalizedEmail = deletedEmail.ToUpperInvariant();
        user.UserName = deletedEmail;
        user.NormalizedUserName = deletedEmail.ToUpperInvariant();
        user.FirstName = "Deleted";
        user.LastName = "User";
        user.PhoneNumber = null;
        user.PhoneNumberConfirmed = false;
        user.PasswordHash = null;
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        user.Height = null;
        user.Gender = null;
        user.DateOfBirth = null;
        user.DevelopmentDirection = null;
        user.TrainingDiscipline = null;
        user.Bio = null;
        user.AvatarUrl = null;
        user.FacebookUrl = null;
        user.InstagramUrl = null;
        user.ZaloUrl = null;
        user.PreferredLanguage = "en";
        user.PreferredTheme = "light";
        user.PreferredWeightUnit = "kg";
        user.TotalXp = 0;
        user.Level = 1;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;
    }

    private void AnonymizeRetainedRows(
        Guid userId,
        string deletedEmail,
        AccountDeletionRequest deletionRequest,
        DateTime now)
    {
        foreach (var request in db.AccountDeletionRequests.Local.Where(r => r.UserId == userId))
            request.Email = deletedEmail;

        deletionRequest.Email = deletedEmail;
        deletionRequest.Status = AccountDeletionStatus.Completed;
        deletionRequest.CompletedAt = now;
        if (deletionRequest.RetainUntil == default)
            deletionRequest.RetainUntil = now.AddYears(7);
        deletionRequest.FailureReason = null;

        db.AccountDeletionAuditLogs.Add(new AccountDeletionAuditLog
        {
            AccountDeletionRequest = deletionRequest,
            EventType = "Completed",
            Detail = "Account deleted and retained records anonymized."
        });
    }

    private async Task MarkFailedAsync(Guid requestId, Exception exception, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        var request = await db.AccountDeletionRequests.SingleOrDefaultAsync(r => r.Id == requestId, ct);
        if (request is null)
            return;

        request.Status = AccountDeletionStatus.Failed;
        request.FailureReason = exception.Message.Length > 1000
            ? exception.Message[..1000]
            : exception.Message;
        db.AccountDeletionAuditLogs.Add(new AccountDeletionAuditLog
        {
            AccountDeletionRequestId = request.Id,
            EventType = "Failed",
            Detail = request.FailureReason
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task DeleteStoredObjectsBestEffortAsync(IEnumerable<string> storageKeys, CancellationToken ct)
    {
        foreach (var key in storageKeys)
        {
            try
            {
                await documentStorage.DeleteAsync(key, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete stored object {StorageKey} during account deletion.", key);
            }
        }
    }
}
