using Mediator;
using Xenoh.Application.Features.Files.Dtos;

namespace Xenoh.Application.Features.Files.Queries.ListSharedWithMe;

public sealed record ListSharedWithMeQuery : IRequest<List<SharedFileDto>>;
