using Mediator;

namespace Xenoh.Application.Features.ProgressLibrary.Commands.DeleteProgressPhoto;

public sealed record DeleteProgressPhotoCommand(Guid CheckInId, Guid PhotoId) : IRequest;
