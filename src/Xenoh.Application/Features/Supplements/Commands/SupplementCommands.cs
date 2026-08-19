using Mediator;

namespace Xenoh.Application.Features.Supplements.Commands;

public sealed record CreateSupplementRegimenCommand(
    CreateSupplementRegimenRequest Request,
    Guid? UserId = null) : IRequest<SupplementRegimenResponse>;

public sealed record UpdateSupplementRegimenCommand(
    Guid RegimenId,
    UpdateSupplementRegimenRequest Request,
    Guid? UserId = null) : IRequest<SupplementRegimenResponse>;

public sealed record ArchiveSupplementRegimenCommand(
    Guid RegimenId,
    Guid? UserId = null) : IRequest<Unit>;

/// <summary>
/// Permanently removes a regimen and its schedule/intake history. Distinct from
/// <see cref="ArchiveSupplementRegimenCommand"/>, which keeps the history and only
/// stops future doses.
/// </summary>
public sealed record DeleteSupplementRegimenCommand(
    Guid RegimenId,
    Guid? UserId = null) : IRequest<Unit>;

public sealed record RecordSupplementDoseCommand(
    Guid DoseSlotId,
    DateOnly Date,
    RecordSupplementDoseRequest Request) : IRequest<SupplementDailyDoseResponse>;

public sealed record ResetSupplementDoseCommand(
    Guid DoseSlotId,
    DateOnly Date) : IRequest<Unit>;
