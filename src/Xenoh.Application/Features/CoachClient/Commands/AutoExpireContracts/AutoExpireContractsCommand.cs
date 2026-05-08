using Mediator;

namespace Xenoh.Application.Features.CoachClient.Commands.AutoExpireContracts;

public sealed record AutoExpireContractsCommand : IRequest<int>;
