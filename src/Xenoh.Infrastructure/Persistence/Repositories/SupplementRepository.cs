using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Features.CoachClient;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class SupplementRepository(ApplicationDbContext db) : ISupplementRepository
{
    public async Task<IReadOnlyList<SupplementRegimen>> GetRegimensAsync(
        Guid userId,
        bool includeArchived,
        CancellationToken cancellationToken) =>
        await db.SupplementRegimens
            .AsNoTracking()
            .Where(x => x.UserId == userId && (includeArchived || !x.IsArchived))
            .Include(x => x.CreatedByUser)
            .Include(x => x.ScheduleVersions)
                .ThenInclude(x => x.DoseSlots)
            .OrderBy(x => x.IsArchived)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<SupplementRegimen?> GetRegimenForUpdateAsync(
        Guid regimenId,
        Guid userId,
        CancellationToken cancellationToken) =>
        db.SupplementRegimens
            .Include(x => x.CreatedByUser)
            .Include(x => x.ScheduleVersions)
                .ThenInclude(x => x.DoseSlots)
            .SingleOrDefaultAsync(x => x.Id == regimenId && x.UserId == userId, cancellationToken);

    public Task<SupplementDoseSlot?> GetDoseSlotAsync(
        Guid doseSlotId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var activeRelationships = db.CoachClientRelationships.EffectiveAt(DateTime.UtcNow);
        return db.SupplementDoseSlots
            .AsNoTracking()
            .Include(x => x.ScheduleVersion)
                .ThenInclude(x => x.Regimen)
            .SingleOrDefaultAsync(
                x => x.Id == doseSlotId && x.ScheduleVersion.Regimen.UserId == userId
                    && (x.ScheduleVersion.Regimen.CreatedByUserId == null || x.ScheduleVersion.Regimen.CreatedByUserId == userId ||
                        (!x.ScheduleVersion.Regimen.IsArchived && activeRelationships.Any(r =>
                            r.ClientId == userId && r.CoachId == x.ScheduleVersion.Regimen.CreatedByUserId))),
                cancellationToken);
    }

    public Task<SupplementIntakeLog?> GetIntakeForUpdateAsync(
        Guid doseSlotId,
        DateOnly date,
        Guid userId,
        CancellationToken cancellationToken) =>
        db.SupplementIntakeLogs.SingleOrDefaultAsync(
            x => x.DoseSlotId == doseSlotId &&
                 x.ScheduledDate == date &&
                 x.UserId == userId,
            cancellationToken);

    public async Task<IReadOnlyList<SupplementScheduleVersion>> GetScheduleVersionsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var versions = await db.SupplementScheduleVersions.AsNoTracking()
            .Where(x => x.Regimen.UserId == userId && x.EffectiveFrom <= to &&
                (x.EffectiveTo == null || x.EffectiveTo >= from))
            .Include(x => x.Regimen).ThenInclude(x => x.CreatedByUser)
            .Include(x => x.DoseSlots).OrderBy(x => x.EffectiveFrom).ToListAsync(cancellationToken);
        var activeCoachIds = await db.CoachClientRelationships.EffectiveAt(DateTime.UtcNow).AsNoTracking()
            .Where(r => r.ClientId == userId).Select(r => r.CoachId).ToListAsync(cancellationToken);
        // Read-only response state: don't schedule doses after the deadline while waiting for the worker.
        foreach (var version in versions)
            if (version.Regimen.CreatedByUserId is { } author && author != userId && !activeCoachIds.Contains(author))
                version.Regimen.IsArchived = true;
        return versions;
    }

    public async Task<IReadOnlyList<SupplementIntakeLog>> GetIntakesAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        await db.SupplementIntakeLogs
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.ScheduledDate >= from &&
                x.ScheduledDate <= to)
            .ToListAsync(cancellationToken);

    public async Task ArchiveCoachRegimensForClientAsync(
        Guid clientId,
        Guid coachId,
        CancellationToken cancellationToken)
    {
        var regimens = await db.SupplementRegimens
            .Where(x => x.UserId == clientId && x.CreatedByUserId == coachId)
            .Include(x => x.ScheduleVersions)
                .ThenInclude(x => x.DoseSlots)
            .ToListAsync(cancellationToken);

        var today = Xenoh.Domain.Rules.CoachingPolicy.LocalDate(DateTime.UtcNow);
        foreach (var regimen in regimens)
        {
            regimen.IsArchived = true;
            regimen.UpdatedAt = DateTime.UtcNow;
            foreach (var version in regimen.ScheduleVersions)
                if (version.EffectiveTo == null || version.EffectiveTo >= today)
                    version.EffectiveTo = today;
        }
    }

    public async Task RemoveRegimenAsync(
        SupplementRegimen regimen,
        CancellationToken cancellationToken)
    {
        await RemoveIntakeLogsAsync([regimen], cancellationToken);
        db.SupplementRegimens.Remove(regimen);
    }

    /// <summary>
    /// Intake logs hang off dose slots rather than the regimen, and are never loaded with
    /// it, so they are removed explicitly instead of leaning on the database cascade.
    /// </summary>
    private async Task RemoveIntakeLogsAsync(
        IReadOnlyCollection<SupplementRegimen> regimens,
        CancellationToken cancellationToken)
    {
        var slotIds = regimens
            .SelectMany(x => x.ScheduleVersions)
            .SelectMany(x => x.DoseSlots)
            .Select(x => x.Id)
            .ToList();
        if (slotIds.Count == 0)
            return;

        var logs = await db.SupplementIntakeLogs
            .Where(x => slotIds.Contains(x.DoseSlotId))
            .ToListAsync(cancellationToken);

        db.SupplementIntakeLogs.RemoveRange(logs);
    }

    public void AddRegimen(SupplementRegimen regimen) => db.SupplementRegimens.Add(regimen);
    public void AddScheduleVersion(SupplementScheduleVersion version) =>
        db.SupplementScheduleVersions.Add(version);
    public void RemoveScheduleVersion(SupplementScheduleVersion version) =>
        db.SupplementScheduleVersions.Remove(version);
    public void AddIntake(SupplementIntakeLog intake) => db.SupplementIntakeLogs.Add(intake);
    public void RemoveIntake(SupplementIntakeLog intake) => db.SupplementIntakeLogs.Remove(intake);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
