using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Leaderboard.Queries.GetBig3Leaderboard;
using Xenoh.Application.Features.Leaderboard.Queries.GetLeaderboard;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class LeaderboardRepository(ApplicationDbContext db) : ILeaderboardRepository
{
    private const int MaxEntries = 50;

    public async Task<List<LeaderboardEntryResponse>> GetSingleLiftAsync(
        CompetitionLiftType lift, Gender? gender, int max, CancellationToken ct)
    {
        var query =
            from pr in db.UserExercisePRs.AsNoTracking()
            join t in db.ExerciseTemplates.AsNoTracking()
                on pr.ExerciseTemplateId equals t.Id
            join u in db.ApplicationUsers.AsNoTracking()
                on pr.UserId equals u.Id
            where t.IsCompetitionLift && t.CompetitionLiftType == lift
            select new { pr.UserId, FullName = u.FirstName + " " + u.LastName, pr.Weight, u.Gender };

        if (gender.HasValue)
            query = query.Where(x => x.Gender == gender.Value);

        var rows = await query
            .OrderByDescending(x => x.Weight)
            .Take(max > 0 ? max : MaxEntries)
            .ToListAsync(ct);

        return rows.Select((r, i) => new LeaderboardEntryResponse(
            i + 1, r.UserId, r.FullName, r.Weight, null
        )).ToList();
    }

    public async Task<List<LeaderboardEntryResponse>> GetDotsAsync(Gender? gender, int max, CancellationToken ct)
    {
        // Load all users that have at least 1 competition lift PR
        var usersWithPrs = await (
            from pr in db.UserExercisePRs.AsNoTracking()
            join t in db.ExerciseTemplates.AsNoTracking()
                on pr.ExerciseTemplateId equals t.Id
            join u in db.ApplicationUsers.AsNoTracking()
                on pr.UserId equals u.Id
            where t.IsCompetitionLift
            select new { pr.UserId, FullName = u.FirstName + " " + u.LastName, u.Gender, t.CompetitionLiftType, pr.Weight }
        ).ToListAsync(ct);

        // Latest bodyweight per user
        var userIds = usersWithPrs.Select(x => x.UserId).Distinct().ToList();
        var bodyweights = await db.BodyweightLogs
            .AsNoTracking()
            .Where(b => userIds.Contains(b.UserId))
            .GroupBy(b => b.UserId)
            .Select(g => new { UserId = g.Key, Weight = g.OrderByDescending(b => b.Date).First().Weight })
            .ToDictionaryAsync(x => x.UserId, x => (decimal?)x.Weight, ct);

        // Group PRs per user
        var prsByUser = usersWithPrs
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.CompetitionLiftType!.Value, x => (decimal?)x.Weight));

        var userMeta = usersWithPrs
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        var limit = max > 0 ? max : MaxEntries;

        var entries = userMeta.Values
            .Where(u => !gender.HasValue || u.Gender == gender.Value)
            .Select(u =>
            {
                var bw = bodyweights.GetValueOrDefault(u.UserId);
                var prs = prsByUser.GetValueOrDefault(u.UserId, []);
                var dots = GetMyProfileHandler.CalculateDots(u.Gender, bw, prs);
                return new { u.UserId, u.FullName, Dots = dots, BodyweightKg = bw };
            })
            .Where(x => x.Dots.HasValue)
            .OrderByDescending(x => x.Dots!.Value)
            .Take(limit)
            .ToList();

        return entries.Select((e, i) => new LeaderboardEntryResponse(
            i + 1, e.UserId, e.FullName, e.Dots!.Value, e.BodyweightKg
        )).ToList();
    }

    public async Task<List<Big3LeaderboardEntryResponse>> GetBig3Async(Gender? gender, CancellationToken ct)
    {
        // Load all competition lift PRs joined with user info
        var rows = await (
            from pr in db.UserExercisePRs.AsNoTracking()
            join t  in db.ExerciseTemplates.AsNoTracking() on pr.ExerciseTemplateId equals t.Id
            join u  in db.ApplicationUsers.AsNoTracking()  on pr.UserId             equals u.Id
            where t.IsCompetitionLift && t.CompetitionLiftType != null
            select new
            {
                pr.UserId,
                FullName = u.FirstName + " " + u.LastName,
                u.Gender,
                LiftType = t.CompetitionLiftType!.Value,
                pr.Weight,
            }
        ).ToListAsync(ct);

        // Latest bodyweight per user (for DOTS)
        var userIds     = rows.Select(r => r.UserId).Distinct().ToList();
        var bodyweights = await db.BodyweightLogs
            .AsNoTracking()
            .Where(b => userIds.Contains(b.UserId))
            .GroupBy(b => b.UserId)
            .Select(g => new { UserId = g.Key, Weight = g.OrderByDescending(b => b.Date).First().Weight })
            .ToDictionaryAsync(x => x.UserId, x => (decimal?)x.Weight, ct);

        // Build per-user entries
        var entries = rows
            .GroupBy(r => r.UserId)
            .Select(g =>
            {
                var first    = g.First();
                var prsByLift = g.ToDictionary(x => x.LiftType, x => (decimal?)x.Weight);

                prsByLift.TryGetValue(CompetitionLiftType.Squat,    out var squat);
                prsByLift.TryGetValue(CompetitionLiftType.Bench,    out var bench);
                prsByLift.TryGetValue(CompetitionLiftType.Deadlift, out var deadlift);

                var total = (squat ?? 0) + (bench ?? 0) + (deadlift ?? 0);
                var bw    = bodyweights.GetValueOrDefault(g.Key);
                var dots  = GetMyProfileHandler.CalculateDots(first.Gender, bw, prsByLift);

                return new
                {
                    first.UserId,
                    first.FullName,
                    first.Gender,
                    SquatPr    = squat,
                    BenchPr    = bench,
                    DeadliftPr = deadlift,
                    Total      = total,
                    DotsScore  = dots,
                };
            })
            .Where(e => !gender.HasValue || e.Gender == gender.Value)
            .Where(e => e.Total > 0)
            .OrderByDescending(e => e.Total)
            .Take(MaxEntries)
            .ToList();

        return entries.Select((e, i) => new Big3LeaderboardEntryResponse(
            i + 1,
            e.UserId,
            e.FullName,
            e.SquatPr,
            e.BenchPr,
            e.DeadliftPr,
            e.Total,
            e.DotsScore
        )).ToList();
    }
}
