using Mediator;
using Xenoh.Application.Features.Files.Dtos;

namespace Xenoh.Application.Features.Files.Commands.ShareFileWithClient;

public sealed record ShareFileWithClientCommand(
    Guid FileId,
    Guid ClientId
) : IRequest<FileShareTargetDto>;
