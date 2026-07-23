using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface ISupplementRepository
{
    Task<IReadOnlyList<SupplementRegimen>> GetRegimensAsync(
        Guid userId,
        bool includeArchived,
        CancellationToken cancellationToken);

    Task<SupplementRegimen?> GetRegimenForUpdateAsync(
        Guid regimenId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<SupplementDoseSlot?> GetDoseSlotAsync(
        Guid doseSlotId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<SupplementIntakeLog?> GetIntakeForUpdateAsync(
        Guid doseSlotId,
        DateOnly date,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SupplementScheduleVersion>> GetScheduleVersionsAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SupplementIntakeLog>> GetIntakesAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    void AddRegimen(SupplementRegimen regimen);
    void AddScheduleVersion(SupplementScheduleVersion version);
    void RemoveScheduleVersion(SupplementScheduleVersion version);
    void AddIntake(SupplementIntakeLog intake);
    void RemoveIntake(SupplementIntakeLog intake);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
