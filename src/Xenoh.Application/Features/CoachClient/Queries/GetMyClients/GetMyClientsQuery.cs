using Mediator;

namespace Xenoh.Application.Features.CoachClient.Queries.GetMyClients;

public sealed record GetMyClientsQuery : IRequest<List<ClientResponse>>;

public sealed record ClientResponse(
    Guid RelationshipId,
    Guid ClientId,
    string FullName,
    string Email,
    /// <summary>"Pending" | "Active"</summary>
    string Status,
    DateTime ConnectedAt
);
