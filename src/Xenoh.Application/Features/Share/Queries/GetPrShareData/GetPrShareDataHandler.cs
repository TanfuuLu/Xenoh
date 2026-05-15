using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.Share.Queries.GetPrShareData;

public sealed class GetPrShareDataHandler(IApplicationDbContext db)
    : IRequestHandler<GetPrShareDataQuery, PrShareData?>
{
    public async ValueTask<PrShareData?> Handle(GetPrShareDataQuery request, CancellationToken cancellationToken)
    {
        var pr = await db.UserExercisePRs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.UserId == request.UserId && p.ExerciseTemplateId == request.ExerciseTemplateId,
                cancellationToken);

        if (pr is null)
            return null;

        var template = await db.ExerciseTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.ExerciseTemplateId, cancellationToken);

        if (template is null)
            return null;

        var user = await db.ApplicationUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return null;

        var userName = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrEmpty(userName))
            userName = user.Email ?? "Athlete";

        return new PrShareData(
            UserName: userName,
            ExerciseName: template.Name,
            WeightKg: pr.Weight,
            Reps: pr.Reps,
            AchievedAt: pr.AchievedAt,
            Level: user.Level,
            IsCompetitionLift: template.IsCompetitionLift
        );
    }
}
