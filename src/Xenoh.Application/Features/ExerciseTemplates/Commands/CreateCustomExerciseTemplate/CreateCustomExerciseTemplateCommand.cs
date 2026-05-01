using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.ExerciseTemplates.Commands.CreateCustomExerciseTemplate;

public sealed record CreateCustomExerciseTemplateCommand : IRequest<ExerciseTemplateResponse>
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    [Required]
    [EnumDataType(typeof(MuscleGroup))]
    public required MuscleGroup PrimaryMuscleGroup { get; init; }

    public List<MuscleGroup> SecondaryMuscleGroups { get; init; } = [];

    [Required]
    [EnumDataType(typeof(ExerciseKind))]
    public required ExerciseKind ExerciseKind { get; init; }
}
