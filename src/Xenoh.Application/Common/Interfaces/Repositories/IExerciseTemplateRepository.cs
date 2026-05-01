using Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IExerciseTemplateRepository
{
    Task<List<ExerciseTemplateResponse>> GetAllAsync(Guid userId, MuscleGroup? muscleGroup, CancellationToken ct = default);
    Task<ExerciseTemplate?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<ExerciseTemplate?> FindAvailableByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
}
