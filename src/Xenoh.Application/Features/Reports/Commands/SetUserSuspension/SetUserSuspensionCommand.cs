using Mediator;

namespace Xenoh.Application.Features.Reports.Commands.SetUserSuspension;

public sealed record SetUserSuspensionCommand(Guid UserId, bool Suspended) : IRequest;
