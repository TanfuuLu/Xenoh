using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Cycle.Common;

namespace Xenoh.Application.Features.Insights.Common;

public static class CoachChatSupport
{
    public const int MaxMessageLength = 4000;
    public const int MaxReplyLength = 1600;

    private static readonly string[] FitnessTerms =
    [
        "training", "workout", "exercise", "nutrition", "protein", "calorie", "recovery", "sleep",
        "gym", "lift", "squat", "bench", "deadlift", "cardio", "running", "hypertrophy", "powerlifting",
        "tap", "luyen", "bai tap", "dinh duong", "phuc hoi", "ngu", "chay", "suc manh", "giam mo", "tang co"
    ];

    private static readonly string[] OffTopicTerms =
    [
        "code", "programming", "javascript", "c#", "python", "sql", "crypto", "bitcoin", "stock",
        "weather", "politics", "election", "movie", "song", "lyrics", "joke", "recipe",
        "lap trinh", "viet code", "tien ao", "chung khoan", "thoi tiet", "chinh tri",
        "bau cu", "phim", "bai hat", "loi bai hat", "truyen cuoi", "nau an"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static string NormalizeLanguage(string? language) =>
        string.Equals(language, "vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";

    public static bool IsClearlyOffTopic(string message)
    {
        var normalized = RemoveVietnameseDiacritics(message.ToLowerInvariant());
        if (FitnessTerms.Any(normalized.Contains))
            return false;

        return OffTopicTerms.Any(normalized.Contains);
    }

    public static string OffTopicReply(string language) =>
        language == "vi"
            ? "Mình chỉ hỗ trợ các câu hỏi liên quan đến luyện tập, dinh dưỡng, phục hồi và tiến độ trong Xenoh. Bạn muốn mình xem phần nào trong kế hoạch tập của bạn?"
            : "I can only help with training, nutrition, recovery, and progress inside Xenoh. Which part of your training plan do you want to review?";

    public static string TrimMessage(string content)
    {
        var trimmed = content.Trim();
        return trimmed.Length <= MaxMessageLength ? trimmed : trimmed[..MaxMessageLength];
    }

    public static string TrimReply(string reply)
    {
        var trimmed = reply.Trim();
        return trimmed.Length <= MaxReplyLength ? trimmed : $"{trimmed[..MaxReplyLength].TrimEnd()}...";
    }

    public static async Task<string> BuildSnapshotJsonAsync(
        IApplicationDbContext db,
        Guid userId,
        CancellationToken ct)
    {
        var snapshot = await BuildContextAsync(db, userId, ct);
        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    private static async Task<object> BuildContextAsync(
        IApplicationDbContext db,
        Guid userId,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var since = today.AddDays(-28);

        var profile = await db.ApplicationUsers
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                Gender = u.Gender.HasValue ? u.Gender.Value.ToString() : null,
                u.DateOfBirth,
                HeightCm = u.Height,
                DevelopmentDirection = u.DevelopmentDirection.HasValue ? u.DevelopmentDirection.Value.ToString() : null,
                TrainingDiscipline = u.TrainingDiscipline.HasValue ? u.TrainingDiscipline.Value.ToString() : null,
            })
            .FirstOrDefaultAsync(ct);

        var activePlan = await db.Plans
            .AsNoTracking()
            .Where(p => p.OwnerId == userId && p.IsActive)
            .Select(p => new
            {
                p.Name,
                p.StartDate,
                p.EndDate,
                TotalDays = p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).Count(d => d.Exercises.Any()),
                CompletedDays = p.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts)
                    .Count(d => d.Exercises.Any() && d.Exercises.All(e => e.IsCompleted)),
            })
            .FirstOrDefaultAsync(ct);

        var recentSets = await db.ExerciseSets
            .AsNoTracking()
            .Where(s => s.IsCompleted &&
                        s.Exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId == userId &&
                        s.Exercise.DailyWorkout.Date >= since)
            .Select(s => new
            {
                s.Exercise.PrimaryMuscleGroup,
                s.PlannedReps,
                s.PlannedWeight,
                s.ActualReps,
                s.ActualWeight,
                s.Rpe,
            })
            .ToListAsync(ct);

        var topMuscles = recentSets
            .GroupBy(s => s.PrimaryMuscleGroup.ToString())
            .Select(g => new { Muscle = g.Key, Sets = g.Count() })
            .OrderByDescending(m => m.Sets)
            .Take(6)
            .ToList();

        var latestBodyweight = await db.BodyweightLogs
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.Date)
            .Select(b => (decimal?)b.Weight)
            .FirstOrDefaultAsync(ct);

        var prs = await db.UserExercisePRs
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Join(db.ExerciseTemplates.AsNoTracking(),
                p => p.ExerciseTemplateId, t => t.Id,
                (p, t) => new { Exercise = t.Name, p.Weight, p.Reps, p.AchievedAt })
            .OrderByDescending(p => p.AchievedAt)
            .Take(5)
            .ToListAsync(ct);

        var nutrition = await db.NutritionDailyLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.Date >= since)
            .ToListAsync(ct);

        var cycleContext = await CycleContextBuilder.TryBuildAsync(
            db, userId, today, today.AddDays(21), ct);

        return new
        {
            AsOf = today,
            Profile = profile,
            CycleContext = cycleContext,
            LatestBodyweightKg = latestBodyweight,
            ActivePlan = activePlan is null ? null : new
            {
                activePlan.Name,
                activePlan.StartDate,
                activePlan.EndDate,
                activePlan.TotalDays,
                activePlan.CompletedDays,
                CompletionPercent = activePlan.TotalDays == 0
                    ? 0
                    : (int)Math.Round(activePlan.CompletedDays * 100.0 / activePlan.TotalDays),
            },
            Recent28Days = new
            {
                CompletedSets = recentSets.Count,
                AverageRpe = recentSets.Where(s => s.Rpe.HasValue).Select(s => s.Rpe!.Value).DefaultIfEmpty().Average(),
                HighRpeSets = recentSets.Count(s => s.Rpe >= 8.5m),
                MissedTargetSets = recentSets.Count(s =>
                    (s.ActualReps is not null && s.ActualReps < s.PlannedReps) ||
                    (s.ActualWeight is not null && s.PlannedWeight is not null && s.ActualWeight < s.PlannedWeight)),
                TopMuscles = topMuscles,
            },
            Nutrition = nutrition.Count == 0 ? null : new
            {
                LoggedDays = nutrition.Count,
                AverageCalories = (int)Math.Round(nutrition.Average(l => l.Calories)),
                AverageProteinG = Math.Round(nutrition.Average(l => l.ProteinG), 1),
            },
            RecentPrs = prs,
        };
    }

    private static string RemoveVietnameseDiacritics(string value)
    {
        var result = value
            .Replace('á', 'a').Replace('à', 'a').Replace('ả', 'a').Replace('ã', 'a').Replace('ạ', 'a')
            .Replace('ă', 'a').Replace('ắ', 'a').Replace('ằ', 'a').Replace('ẳ', 'a').Replace('ẵ', 'a').Replace('ặ', 'a')
            .Replace('â', 'a').Replace('ấ', 'a').Replace('ầ', 'a').Replace('ẩ', 'a').Replace('ẫ', 'a').Replace('ậ', 'a')
            .Replace('é', 'e').Replace('è', 'e').Replace('ẻ', 'e').Replace('ẽ', 'e').Replace('ẹ', 'e')
            .Replace('ê', 'e').Replace('ế', 'e').Replace('ề', 'e').Replace('ể', 'e').Replace('ễ', 'e').Replace('ệ', 'e')
            .Replace('í', 'i').Replace('ì', 'i').Replace('ỉ', 'i').Replace('ĩ', 'i').Replace('ị', 'i')
            .Replace('ó', 'o').Replace('ò', 'o').Replace('ỏ', 'o').Replace('õ', 'o').Replace('ọ', 'o')
            .Replace('ô', 'o').Replace('ố', 'o').Replace('ồ', 'o').Replace('ổ', 'o').Replace('ỗ', 'o').Replace('ộ', 'o')
            .Replace('ơ', 'o').Replace('ớ', 'o').Replace('ờ', 'o').Replace('ở', 'o').Replace('ỡ', 'o').Replace('ợ', 'o')
            .Replace('ú', 'u').Replace('ù', 'u').Replace('ủ', 'u').Replace('ũ', 'u').Replace('ụ', 'u')
            .Replace('ư', 'u').Replace('ứ', 'u').Replace('ừ', 'u').Replace('ử', 'u').Replace('ữ', 'u').Replace('ự', 'u')
            .Replace('ý', 'y').Replace('ỳ', 'y').Replace('ỷ', 'y').Replace('ỹ', 'y').Replace('ỵ', 'y')
            .Replace('đ', 'd');

        return result;
    }
}
