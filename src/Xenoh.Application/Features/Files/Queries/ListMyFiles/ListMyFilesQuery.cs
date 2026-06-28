using Mediator;
using Xenoh.Application.Features.Files.Dtos;

namespace Xenoh.Application.Features.Files.Queries.ListMyFiles;

public sealed record ListMyFilesQuery : IRequest<MyFilesResponse>;
