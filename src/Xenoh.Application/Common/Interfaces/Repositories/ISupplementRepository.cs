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

    /// <summary>
    /// Stages removal of every regimen the coach authored for the client, for the
    /// caller to commit. Mirrors <c>IPlanRepository.DeleteCoachPlansForClientAsync</c>
    /// so a disconnect tears down all of the coach's work in one transaction.
    /// </summary>
    Task DeleteCoachRegimensForClientAsync(Guid clientId, Guid coachId, CancellationToken cancellationToken);

    /// <summary>Stages removal of a regimen along with its schedule and intake history.</summary>
    Task RemoveRegimenAsync(SupplementRegimen regimen, CancellationToken cancellationToken);

    void AddRegimen(SupplementRegimen regimen);
    void AddScheduleVersion(SupplementScheduleVersion version);
    void RemoveScheduleVersion(SupplementScheduleVersion version);
    void AddIntake(SupplementIntakeLog intake);
    void RemoveIntake(SupplementIntakeLog intake);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
