using Mediator;
using Xenoh.Application.Features.CoachClient.Commands.GenerateInviteCode;

namespace Xenoh.Application.Features.CoachClient.Queries.GetMyInviteCodes;

public sealed record GetMyInviteCodesQuery : IRequest<List<CoachInviteCodeDto>>;
