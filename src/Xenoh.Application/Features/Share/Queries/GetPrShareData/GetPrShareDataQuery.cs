using Mediator;

namespace Xenoh.Application.Features.Share.Queries.GetPrShareData;

public sealed record GetPrShareDataQuery(Guid UserId, Guid ExerciseTemplateId) : IRequest<PrShareData?>;
