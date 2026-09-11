using Mediator;
using Xenoh.Application.Features.ProgressLibrary.Dtos;

namespace Xenoh.Application.Features.ProgressLibrary.Queries.ListMyProgressCheckIns;

public sealed record ListMyProgressCheckInsQuery : IRequest<IReadOnlyList<ProgressCheckInDto>>;
