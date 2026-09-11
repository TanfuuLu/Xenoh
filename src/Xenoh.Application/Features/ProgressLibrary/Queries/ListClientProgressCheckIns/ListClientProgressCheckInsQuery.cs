using Mediator;
using Xenoh.Application.Features.ProgressLibrary.Dtos;

namespace Xenoh.Application.Features.ProgressLibrary.Queries.ListClientProgressCheckIns;

public sealed record ListClientProgressCheckInsQuery(Guid ClientId) : IRequest<IReadOnlyList<ProgressCheckInDto>>;
