using Mediator;
using Xenoh.Application.Features.Files.Dtos;

namespace Xenoh.Application.Features.Files.Queries.GetFileDownloadUrl;

public sealed record GetFileDownloadUrlQuery(Guid FileId) : IRequest<FileDownloadUrlResponse>;
