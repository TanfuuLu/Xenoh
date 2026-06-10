using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Cycle.Common;

/// <summary>
/// Cycle tracking is only available to profiles whose gender is Female.
/// Throws <see cref="InvalidOperationException"/> (surfaced as 400) otherwise.
/// </summary>
public static class CycleGuard
{
    public const string NotFemaleMessage =
        "Cycle tracking is only available for profiles with gender set to Female.";

    public static async Task EnsureFemaleAsync(IApplicationDbContext db, Guid userId, CancellationToken ct)
    {
        var gender = await db.ApplicationUsers
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Gender)
            .FirstOrDefaultAsync(ct);

        if (gender != Gender.Female)
            throw new InvalidOperationException(NotFemaleMessage);
    }
}
