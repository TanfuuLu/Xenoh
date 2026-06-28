using Mediator;
using Xenoh.Application.Features.Files.Dtos;

namespace Xenoh.Application.Features.Files.Commands.UploadFile;

public sealed record UploadFileCommand(
    string FileName,
    string ContentType,
    long Length,
    Stream Content
) : IRequest<StoredFileDto>;
