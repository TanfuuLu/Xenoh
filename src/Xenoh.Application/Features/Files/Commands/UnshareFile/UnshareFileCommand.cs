using Mediator;

namespace Xenoh.Application.Features.Files.Commands.UnshareFile;

public sealed record UnshareFileCommand(
    Guid FileId,
    Guid ShareId
) : IRequest;
