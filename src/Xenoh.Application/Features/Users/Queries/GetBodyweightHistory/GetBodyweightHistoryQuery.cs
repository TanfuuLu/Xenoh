using Mediator;
using Xenoh.Application.Features.Users.Commands.LogBodyweight;

namespace Xenoh.Application.Features.Users.Queries.GetBodyweightHistory;

public sealed record GetBodyweightHistoryQuery(Guid? UserId = null) : IRequest<List<BodyweightLogResponse>>;
