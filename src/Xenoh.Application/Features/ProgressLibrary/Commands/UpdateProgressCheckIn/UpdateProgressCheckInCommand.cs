using Mediator;
using Xenoh.Application.Features.ProgressLibrary.Dtos;

namespace Xenoh.Application.Features.ProgressLibrary.Commands.UpdateProgressCheckIn;

public sealed record UpdateProgressCheckInCommand(
    Guid CheckInId,
    DateOnly CheckInDate,
    string? Title,
    string? Notes,
    decimal? BodyweightKg,
    bool IsMilestone) : IRequest<ProgressCheckInDto>;
