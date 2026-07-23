using Microsoft.EntityFrameworkCore;
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
        CancellationToken cancellationToken) =>
        db.SupplementDoseSlots
            .AsNoTracking()
            .Include(x => x.ScheduleVersion)
                .ThenInclude(x => x.Regimen)
            .SingleOrDefaultAsync(
                x => x.Id == doseSlotId && x.ScheduleVersion.Regimen.UserId == userId,
                cancellationToken);

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
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        await db.SupplementScheduleVersions
            .AsNoTracking()
            .Where(x =>
                x.Regimen.UserId == userId &&
                x.EffectiveFrom <= to &&
                (x.EffectiveTo == null || x.EffectiveTo >= from))
            .Include(x => x.Regimen)
                .ThenInclude(x => x.CreatedByUser)
            .Include(x => x.DoseSlots)
            .OrderBy(x => x.EffectiveFrom)
            .ToListAsync(cancellationToken);

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
