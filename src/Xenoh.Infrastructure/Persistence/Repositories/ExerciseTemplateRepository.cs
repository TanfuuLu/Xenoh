using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.Pagination;
using Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class ExerciseTemplateRepository(ApplicationDbContext db) : IExerciseTemplateRepository
{
    public Task<PagedResponse<ExerciseTemplateResponse>> GetAllAsync(
        Guid userId,
        MuscleGroup? muscleGroup,
        int pageNumber,
        int pageSize,
        CancellationToken ct) =>
        GetAvailableForUserAsync(userId, muscleGroup, pageNumber, pageSize, ct);

    public async Task<PagedResponse<ExerciseTemplateResponse>> GetAvailableForUserAsync(
        Guid userId,
        MuscleGroup? muscleGroup,
        int pageNumber,
        int pageSize,
        CancellationToken ct)
    {
        var query = db.ExerciseTemplates
            .AsNoTracking()
            .Where(t => !t.IsArchived && (t.OwnerId == null || t.OwnerId == userId));

        if (muscleGroup is not null)
            query = query.Where(t => t.PrimaryMuscleGroup == muscleGroup);

        var orderedQuery = query
            .OrderBy(t => t.OwnerId == null ? 0 : 1)
            .ThenBy(t => t.PrimaryMuscleGroup)
            .ThenBy(t => t.Name);

        var totalCount = await orderedQuery.CountAsync(ct);
        var items = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new ExerciseTemplateResponse(
                t.Id,
                t.Name,
                t.Description,
                t.PrimaryMuscleGroup.ToString(),
                t.SecondaryMuscleGroups.Select(m => m.ToString()).ToList(),
                t.ExerciseKind.ToString(),
                t.EstimatedMet,
                t.OwnerId != null,
                t.OwnerId,
                t.ImageUrl))
            .ToListAsync(ct);

        return new PagedResponse<ExerciseTemplateResponse>(
            items,
            pageNumber,
            pageSize,
            totalCount,
            pageNumber * pageSize < totalCount);
    }

    public Task<ExerciseTemplate?> FindByIdAsync(Guid id, CancellationToken ct) =>
        db.ExerciseTemplates
          .AsNoTracking()
          .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<ExerciseTemplate?> FindAvailableByIdAsync(Guid id, Guid userId, CancellationToken ct) =>
        db.ExerciseTemplates
          .FirstOrDefaultAsync(
            t => t.Id == id && !t.IsArchived && (t.OwnerId == null || t.OwnerId == userId),
            ct);
}
