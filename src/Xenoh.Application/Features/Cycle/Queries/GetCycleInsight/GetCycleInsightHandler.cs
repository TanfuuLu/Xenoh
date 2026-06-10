using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Cycle.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Services;

namespace Xenoh.Application.Features.Cycle.Queries.GetCycleInsight;

public sealed class GetCycleInsightHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ICycleInsightAi ai
) : IRequestHandler<GetCycleInsightQuery, CycleInsightResponse>
{
    private const int PromptVersion = 1;
    private const string Feature = "cycle-insight";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async ValueTask<CycleInsightResponse> Handle(
        GetCycleInsightQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        await CycleGuard.EnsureFemaleAsync(db, userId, cancellationToken);

        var language = string.Equals(request.Language, "vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";

        var snapshot = await BuildSnapshotAsync(userId, cancellationToken);
        var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);
        var fingerprint = ComputeFingerprint(snapshotJson, language);

        var existing = await db.AiFeatureCaches
            .FirstOrDefaultAsync(
                c => c.UserId == userId && c.Feature == Feature && c.Language == language,
                cancellationToken);

        if (existing is not null && existing.DataFingerprint == fingerprint)
        {
            var cachedContent = JsonSerializer.Deserialize<CycleInsightContent>(existing.ContentJson, JsonOptions)
                ?? throw new InvalidOperationException("Cached cycle insight is corrupted.");
            return new CycleInsightResponse(language, existing.GeneratedAt, Cached: true, cachedContent);
        }

        var aiResult = await ai.GenerateAsync(new CycleInsightAiRequest(language, snapshotJson), cancellationToken);

        var content = JsonSerializer.Deserialize<CycleInsightContent>(aiResult.Json, JsonOptions)
            ?? throw new InvalidOperationException("AI returned malformed JSON.");

        var contentJson = JsonSerializer.Serialize(content, JsonOptions);
        var now = DateTime.UtcNow;

        if (existing is null)
        {
            db.AiFeatureCaches.Add(new AiFeatureCache
            {
                Feature = Feature,
                Language = language,
                UserId = userId,
                DataFingerprint = fingerprint,
                ContentJson = contentJson,
                GeneratedAt = now,
            });
        }
        else
        {
            existing.DataFingerprint = fingerprint;
            existing.ContentJson = contentJson;
            existing.GeneratedAt = now;
            existing.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CycleInsightResponse(language, now, Cached: false, content);
    }

    private async Task<CycleSnapshot> BuildSnapshotAsync(Guid userId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var profile = await db.ApplicationUsers
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.DateOfBirth, u.DevelopmentDirection, u.TrainingDiscipline })
            .FirstOrDefaultAsync(ct);

        int? age = profile?.DateOfBirth is { } dob
            ? Math.Max(0, today.Year - dob.Year - (today.DayOfYear < dob.DayOfYear ? 1 : 0))
            : null;

        // All flow days (for phase mapping) + recent daily logs (for symptom/energy detail).
        var since400 = today.AddDays(-400);
        var flowDays = await db.CycleDailyLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.Flow != null && l.Date >= since400 && l.Date <= today)
            .OrderBy(l => l.Date)
            .Select(l => new { l.Date, l.Flow })
            .ToListAsync(ct);

        var settings = await db.CycleSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        var flowDayList = flowDays.Select(x => new CycleFlowDay(x.Date, x.Flow!.Value)).ToList();
        var prediction = CyclePhaseCalculator.Calculate(
            flowDayList, settings?.AverageCycleLengthOverride, settings?.AveragePeriodLengthOverride, today);

        // Phase memoization for arbitrary dates.
        var phaseCache = new Dictionary<DateOnly, CyclePhase>();
        CyclePhase PhaseFor(DateOnly date)
        {
            if (phaseCache.TryGetValue(date, out var cached)) return cached;
            var p = CyclePhaseCalculator.Calculate(
                flowDayList.Where(f => f.Date <= date),
                settings?.AverageCycleLengthOverride,
                settings?.AveragePeriodLengthOverride,
                date).Phase;
            phaseCache[date] = p;
            return p;
        }

        // Recent daily logs (last 60 days), condensed.
        var since60 = today.AddDays(-60);
        var recentLogs = await db.CycleDailyLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.Date >= since60 && l.Date <= today)
            .OrderBy(l => l.Date)
            .Select(l => new { l.Date, l.Flow, l.Symptoms, l.Mood, l.EnergyLevel })
            .ToListAsync(ct);

        var condensedLogs = recentLogs.Select(l => new CycleLogPoint(
            l.Date,
            PhaseFor(l.Date).ToString(),
            l.Flow?.ToString(),
            CycleMapper.SymptomsToList(l.Symptoms),
            l.Mood?.ToString(),
            l.EnergyLevel
        )).ToList();

        // Training sets last 90 days, aggregated per phase.
        var since90 = today.AddDays(-90);
        var sets = await db.ExerciseSets
            .AsNoTracking()
            .Where(s => s.Exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId == userId &&
                        s.Exercise.DailyWorkout.Date >= since90 &&
                        s.Exercise.DailyWorkout.Date <= today)
            .Select(s => new
            {
                Day = s.Exercise.DailyWorkout.Date,
                s.IsCompleted,
                s.ActualReps,
                s.PlannedReps,
                s.ActualWeight,
                s.PlannedWeight,
                s.Rpe
            })
            .ToListAsync(ct);

        var phaseAgg = sets
            .GroupBy(s => PhaseFor(s.Day))
            .Where(g => g.Key != CyclePhase.Unknown)
            .Select(g =>
            {
                var completed = g.Where(s => s.IsCompleted).ToList();
                var volume = completed.Sum(s => (s.ActualReps ?? s.PlannedReps) * (s.ActualWeight ?? s.PlannedWeight ?? 0m));
                var rpes = completed.Where(s => s.Rpe.HasValue).Select(s => s.Rpe!.Value).ToList();
                return new PhaseTraining(
                    g.Key.ToString(),
                    completed.Count,
                    Math.Round(volume, 1),
                    rpes.Count > 0 ? Math.Round(rpes.Average(), 1) : (decimal?)null,
                    g.Count() > 0 ? (int)Math.Round(completed.Count / (double)g.Count() * 100) : 0
                );
            })
            .OrderBy(p => p.Phase)
            .ToList();

        var stats = new CycleStats(
            prediction.Phase.ToString(),
            prediction.CycleDay,
            prediction.AverageCycleLengthDays,
            prediction.AveragePeriodLengthDays,
            prediction.CycleVariabilityDays,
            prediction.IsRegular,
            prediction.NeedsData);

        return new CycleSnapshot(
            today,
            new CycleProfileContext(age, profile?.DevelopmentDirection?.ToString(), profile?.TrainingDiscipline?.ToString()),
            stats,
            condensedLogs,
            phaseAgg);
    }

    private static string ComputeFingerprint(string snapshotJson, string language)
    {
        var canonical = $"{PromptVersion}|{language}|{snapshotJson}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash);
    }

    // ─── Snapshot records (private to handler) ──────────────────────────────

    private sealed record CycleSnapshot(
        DateOnly AsOf,
        CycleProfileContext ProfileContext,
        CycleStats Stats,
        IReadOnlyList<CycleLogPoint> RecentLogs,
        IReadOnlyList<PhaseTraining> TrainingByPhase);

    private sealed record CycleProfileContext(int? Age, string? DevelopmentDirection, string? TrainingDiscipline);

    private sealed record CycleStats(
        string CurrentPhase,
        int? CycleDay,
        int? AvgCycleLengthDays,
        int? AvgPeriodLengthDays,
        int? VariabilityDays,
        bool IsRegular,
        bool NeedsData);

    private sealed record CycleLogPoint(
        DateOnly Date,
        string Phase,
        string? Flow,
        IReadOnlyList<string> Symptoms,
        string? Mood,
        int? Energy);

    private sealed record PhaseTraining(
        string Phase,
        int Sets,
        decimal Volume,
        decimal? AvgRpe,
        int CompletionRate);
}
