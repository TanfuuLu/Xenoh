using Mediator;

namespace Xenoh.Application.Features.ProgressLibrary.Commands.DeleteProgressCheckIn;

public sealed record DeleteProgressCheckInCommand(Guid CheckInId) : IRequest;
