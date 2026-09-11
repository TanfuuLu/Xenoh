using Mediator;
using Xenoh.Application.Features.ProgressLibrary.Dtos;

namespace Xenoh.Application.Features.ProgressLibrary.Commands.CreateProgressCheckIn;

public sealed record CreateProgressCheckInCommand(
    DateOnly CheckInDate,
    string? Title,
    string? Notes,
    decimal? BodyweightKg,
    bool IsMilestone,
    IReadOnlyList<ProgressPhotoUpload> Photos) : IRequest<ProgressCheckInDto>;
