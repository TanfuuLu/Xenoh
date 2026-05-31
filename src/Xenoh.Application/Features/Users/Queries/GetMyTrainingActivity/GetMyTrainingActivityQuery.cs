using Mediator;

namespace Xenoh.Application.Features.Users.Queries.GetMyTrainingActivity;

public sealed record GetMyTrainingActivityQuery(int Year, int Month)
    : IRequest<TrainingActivityResponse>;
