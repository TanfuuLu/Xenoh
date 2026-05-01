using Mediator;

namespace Xenoh.Application.Features.ExerciseTemplates.Commands.DeleteCustomExerciseTemplate;

public sealed record DeleteCustomExerciseTemplateCommand(Guid Id) : IRequest;
