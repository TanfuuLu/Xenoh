using System.Linq.Expressions;
using Xenoh.Application.Features.CoachClient.Commands.RequestCoach;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.CoachClient;

public static class CoachRelationshipMapper
{
    public static readonly Expression<Func<CoachClientRelationship, CoachRelationshipResponse>> ResponseProjection = relationship =>
        new CoachRelationshipResponse(
            relationship.Id,
            relationship.ClientId,
            relationship.Client.FirstName + " " + relationship.Client.LastName,
            relationship.Client.AvatarUrl,
            relationship.CoachId,
            relationship.Coach.FirstName + " " + relationship.Coach.LastName,
            relationship.Status.ToString(),
            relationship.CreatedAt,
            relationship.TerminationRequestedBy,
            relationship.StartDate,
            relationship.EndDate,
            relationship.RenewalRequestedBy,
            relationship.ProposedEndDate,
            relationship.SelectedCoachingType.HasValue ? relationship.SelectedCoachingType.Value.ToString() : null,
            relationship.SelectedQuantity,
            relationship.SelectedPriceAmount,
            relationship.SelectedCurrency);

    public static CoachRelationshipResponse ToResponse(CoachClientRelationship relationship) =>
        ToResponse(relationship, relationship.Client, relationship.Coach);

    public static CoachRelationshipResponse ToResponse(
        CoachClientRelationship relationship,
        ApplicationUser client,
        ApplicationUser coach) =>
        new(
            relationship.Id,
            relationship.ClientId,
            $"{client.FirstName} {client.LastName}",
            client.AvatarUrl,
            relationship.CoachId,
            $"{coach.FirstName} {coach.LastName}",
            relationship.Status.ToString(),
            relationship.CreatedAt,
            relationship.TerminationRequestedBy,
            relationship.StartDate,
            relationship.EndDate,
            relationship.RenewalRequestedBy,
            relationship.ProposedEndDate,
            relationship.SelectedCoachingType?.ToString(),
            relationship.SelectedQuantity,
            relationship.SelectedPriceAmount,
            relationship.SelectedCurrency);
}
