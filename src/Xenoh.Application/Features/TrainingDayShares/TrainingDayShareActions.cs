using System.ComponentModel.DataAnnotations;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.TrainingDayShares;

public sealed record GetTrainingDayShareQuery(Guid ShareId) : IRequest<TrainingDayShareResponse>;
public sealed record UpdateTrainingDayShareCommand(Guid ShareId, [property: MaxLength(500)] string? Caption, bool? IsReusable = null)
    : IRequest<TrainingDayShareResponse>;
public sealed record CopyReusableTrainingShareCommand(Guid ShareId, Guid TargetDailyWorkoutId) : IRequest<int>;
public sealed record ReportTrainingDayShareCommand(Guid ShareId, ReportReason Reason, [property: MaxLength(2000)] string Details)
    : IRequest<Guid>;

internal static class TrainingDayShareAccess
{
    public static async Task<bool> CanViewAsync(IApplicationDbContext db, Guid ownerId, Guid viewerId, CancellationToken ct)
    {
        if (ownerId == viewerId) return true;
        if (await db.UserBlocks.AsNoTracking().AnyAsync(b =>
            (b.BlockerId == viewerId && b.BlockedId == ownerId) ||
            (b.BlockerId == ownerId && b.BlockedId == viewerId), ct)) return false;
        return await db.Friendships.AsNoTracking().AnyAsync(f => f.Status == FriendshipStatus.Accepted &&
            ((f.UserAId == viewerId && f.UserBId == ownerId) ||
             (f.UserAId == ownerId && f.UserBId == viewerId)), ct);
    }
}

public sealed class GetTrainingDayShareHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetTrainingDayShareQuery, TrainingDayShareResponse>
{
    public async ValueTask<TrainingDayShareResponse> Handle(GetTrainingDayShareQuery request, CancellationToken ct)
    {
        var share = await db.TrainingDayShares.AsNoTracking().Include(x => x.User).Include(x => x.Loves)
            .Include(x => x.Exercises).ThenInclude(x => x.Sets)
            .FirstOrDefaultAsync(x => x.Id == request.ShareId, ct)
            ?? throw new InvalidOperationException("Training day share not found.");
        if (!await TrainingDayShareAccess.CanViewAsync(db, share.UserId, currentUser.UserId, ct))
            throw new UnauthorizedAccessException();
        return TrainingDayShareMapping.ToResponse(share, currentUser.UserId);
    }
}

public sealed class UpdateTrainingDayShareHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateTrainingDayShareCommand, TrainingDayShareResponse>
{
    public async ValueTask<TrainingDayShareResponse> Handle(UpdateTrainingDayShareCommand request, CancellationToken ct)
    {
        var share = await db.TrainingDayShares.Include(x => x.User).Include(x => x.Loves)
            .Include(x => x.Exercises).ThenInclude(x => x.Sets)
            .FirstOrDefaultAsync(x => x.Id == request.ShareId, ct)
            ?? throw new InvalidOperationException("Training day share not found.");
        if (share.UserId != currentUser.UserId) throw new UnauthorizedAccessException();
        var caption = string.IsNullOrWhiteSpace(request.Caption) ? null : request.Caption.Trim();
        if (caption?.Length > 500) throw new InvalidOperationException("Caption cannot exceed 500 characters.");
        share.Caption = caption;
        if (request.IsReusable.HasValue)
        {
            if (request.IsReusable.Value && share.Exercises.Any(x => x.ExerciseTemplateId == Guid.Empty || x.PlannedSets <= 0))
                throw new InvalidOperationException("This older share cannot be reused because it has no workout template snapshot.");
            share.IsReusable = request.IsReusable.Value;
        }
        share.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return TrainingDayShareMapping.ToResponse(share, currentUser.UserId);
    }
}

public sealed class CopyReusableTrainingShareHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CopyReusableTrainingShareCommand, int>
{
    public async ValueTask<int> Handle(CopyReusableTrainingShareCommand request, CancellationToken ct)
    {
        var share = await db.TrainingDayShares.AsNoTracking().Include(x => x.Exercises).ThenInclude(x => x.Sets)
            .FirstOrDefaultAsync(x => x.Id == request.ShareId, ct) ?? throw new InvalidOperationException("Training day share not found.");
        if (!share.IsReusable) throw new InvalidOperationException("This workout is not available for reuse.");
        if (!await TrainingDayShareAccess.CanViewAsync(db, share.UserId, currentUser.UserId, ct)) throw new UnauthorizedAccessException();
        var target = await db.DailyWorkouts.Include(x => x.Exercises).ThenInclude(x => x.Sets)
            .Include(x => x.WeeklyWorkout).ThenInclude(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == request.TargetDailyWorkoutId, ct) ?? throw new InvalidOperationException("Target workout not found.");
        var plan = target.WeeklyWorkout.Plan;
        var canEdit = plan.PlanType == PlanType.Coach ? plan.CreatedByCoachId == currentUser.UserId : plan.OwnerId == currentUser.UserId;
        if (!canEdit) throw new UnauthorizedAccessException();
        db.Exercises.RemoveRange(target.Exercises);
        var exercises = share.Exercises.OrderBy(x => x.SortOrder).Select((x, index) => new Exercise
        {
            DailyWorkoutId = target.Id, ExerciseTemplateId = x.ExerciseTemplateId, Name = x.Name,
            PrimaryMuscleGroup = x.PrimaryMuscleGroup, SecondaryMuscleGroups = [.. x.SecondaryMuscleGroups],
            ExerciseKind = x.ExerciseKind, EstimatedMet = x.EstimatedMet, PlannedSets = x.PlannedSets,
            PlannedReps = x.PlannedReps, PlannedWeight = x.PlannedWeight, Notes = x.Notes, SortOrder = index,
            Sets = x.Sets.OrderBy(s => s.SetNumber).Select(s => new ExerciseSet
            {
                SetNumber = s.SetNumber, PlannedReps = s.PlannedReps, PlannedWeight = s.PlannedWeight
            }).ToList()
        }).ToList();
        db.Exercises.AddRange(exercises); target.IsCompleted = false; target.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return exercises.Count;
    }
}

public sealed class ReportTrainingDayShareHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ReportTrainingDayShareCommand, Guid>
{
    public async ValueTask<Guid> Handle(ReportTrainingDayShareCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Details)) throw new InvalidOperationException("Report details are required.");
        var share = await db.TrainingDayShares.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ShareId, ct)
            ?? throw new InvalidOperationException("Training day share not found.");
        if (share.UserId == currentUser.UserId) throw new InvalidOperationException("You cannot report your own share.");
        if (!await TrainingDayShareAccess.CanViewAsync(db, share.UserId, currentUser.UserId, ct))
            throw new UnauthorizedAccessException();
        var duplicate = await db.UserReports.AsNoTracking().AnyAsync(x => x.ReporterId == currentUser.UserId &&
            x.RelatedEntityId == share.Id && x.Status == ReportStatus.Pending, ct);
        if (duplicate) throw new InvalidOperationException("You already reported this share.");
        var report = new UserReport
        {
            ReporterId = currentUser.UserId,
            ReportedUserId = share.UserId,
            Reason = request.Reason,
            Details = request.Details.Trim(),
            RelatedEntityId = share.Id,
            RelatedEntityType = "TrainingDayShare"
        };
        db.UserReports.Add(report);
        await db.SaveChangesAsync(ct);
        return report.Id;
    }
}
