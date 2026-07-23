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

public sealed record RecordSupplementDoseCommand(
    Guid DoseSlotId,
    DateOnly Date,
    RecordSupplementDoseRequest Request) : IRequest<SupplementDailyDoseResponse>;

public sealed record ResetSupplementDoseCommand(
    Guid DoseSlotId,
    DateOnly Date) : IRequest<Unit>;
