using Mediator;

namespace Xenoh.Application.Features.Files.Commands.DeleteFile;

public sealed record DeleteFileCommand(Guid FileId) : IRequest;
