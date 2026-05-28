using Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;
using Xenoh.Application.Common.Pagination;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IExerciseTemplateRepository
{
    Task<PagedResponse<ExerciseTemplateResponse>> GetAllAsync(Guid userId, MuscleGroup? muscleGroup, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<ExerciseTemplateResponse>> GetAvailableForUserAsync(Guid userId, MuscleGroup? muscleGroup, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<ExerciseTemplate?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<ExerciseTemplate?> FindAvailableByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
}
