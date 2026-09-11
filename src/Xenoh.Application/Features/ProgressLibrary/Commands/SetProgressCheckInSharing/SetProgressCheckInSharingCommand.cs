using Mediator;
using Xenoh.Application.Features.ProgressLibrary.Dtos;

namespace Xenoh.Application.Features.ProgressLibrary.Commands.SetProgressCheckInSharing;

public sealed record SetProgressCheckInSharingCommand(Guid CheckInId, bool IsSharedWithCoach) : IRequest<ProgressCheckInDto>;
