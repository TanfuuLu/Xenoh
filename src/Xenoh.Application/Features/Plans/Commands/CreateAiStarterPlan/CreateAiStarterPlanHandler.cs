using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Plans.Commands.CreateAiStarterPlan;

public sealed class CreateAiStarterPlanHandler(
    IApplicationDbContext db,
    IPlanRepository planRepo,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptionService,
    IUserAnalysisAi ai
) : IRequestHandler<CreateAiStarterPlanCommand, PlanResponse>
{
    private static readonly HashSet<string> AllowedSplitPreferences = new(StringComparer.OrdinalIgnoreCase)
    {
        "full_body",
        "upper_lower",
        "push_pull_legs",
        "bro_split"
    };

    private static readonly HashSet<int> AllowedSessionLengths = [30, 45, 60, 75, 90];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async ValueTask<PlanResponse> Handle(
        CreateAiStarterPlanCommand request,
        CancellationToken cancellationToken)
    {
        if (request.EndDate <= request.StartDate)
            throw new InvalidOperationException("End date must be after start date.");
        if (!AllowedSplitPreferences.Contains(request.SplitPreference))
            throw new InvalidOperationException("Unsupported training split preference.");
        if (!AllowedSessionLengths.Contains(request.SessionLengthMinutes))
            throw new InvalidOperationException("Unsupported session length.");

        var userId = currentUser.UserId;
        var maxPlans = await subscriptionService.GetMaxPlansAsync(userId, cancellationToken);
        var planCount = await planRepo.CountByOwnerAsync(userId, cancellationToken);
        if (maxPlans != int.MaxValue && planCount >= maxPlans)
            throw new InvalidOperationException(
                $"Your current plan allows a maximum of {maxPlans} plans. Upgrade to Pro for unlimited plans.");

        var templates = await db.ExerciseTemplates
            .AsNoTracking()
            .Where(t => !t.IsArchived && (t.OwnerId == null || t.OwnerId == userId))
            .OrderBy(t => t.PrimaryMuscleGroup)
            .ThenBy(t => t.Name)
            .Select(t => new TemplateCatalogItem(
                t.Id,
                t.Name,
                t.PrimaryMuscleGroup.ToString(),
                t.SecondaryMuscleGroups.Select(m => m.ToString()).ToList(),
                t.ExerciseKind.ToString(),
                t.IsCompetitionLift,
                IsCompoundExercise(t.Name, t.PrimaryMuscleGroup.ToString(), t.SecondaryMuscleGroups.Count)))
            .ToListAsync(cancellationToken);

        if (templates.Count == 0)
            throw new InvalidOperationException("No exercise templates are available.");

        var profileContext = await db.ApplicationUsers
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                Gender = u.Gender.HasValue ? u.Gender.Value.ToString() : null,
                u.DateOfBirth,
                HeightCm = u.Height,
                DevelopmentDirection = u.DevelopmentDirection.HasValue
                    ? u.DevelopmentDirection.Value.ToString()
                    : null,
                TrainingDiscipline = u.TrainingDiscipline.HasValue
                    ? u.TrainingDiscipline.Value.ToString()
                    : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        var language = string.Equals(request.Language, "vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";
        var questionnaire = new
        {
            ProfileContext = profileContext,
            request.Goal,
            request.Experience,
            request.DaysPerWeek,
            request.SplitPreference,
            request.SessionLengthMinutes,
            request.Equipment,
            request.StartDate,
            request.EndDate,
            request.Description
        };

        var aiResult = await ai.GenerateStarterPlanAsync(
            new StarterPlanAiRequest(
                language,
                JsonSerializer.Serialize(questionnaire, JsonOptions),
                JsonSerializer.Serialize(templates, JsonOptions)),
            cancellationToken);

        var draft = JsonSerializer.Deserialize<AiStarterPlanDraft>(aiResult.Json, JsonOptions)
            ?? throw new InvalidOperationException("AI returned malformed JSON.");

        var templateById = templates.ToDictionary(t => t.Id);
        var safeTrainingDays = GetSafeTrainingDays(request.DaysPerWeek);
        var selectedDays = draft.Days
            .Where(d => Enum.TryParse<DayOfWeek>(d.DayOfWeek, ignoreCase: true, out _) && d.Exercises.Count > 0)
            .Take(request.DaysPerWeek)
            .Select((day, index) => day with { DayOfWeek = safeTrainingDays[index].ToString() })
            .ToList();

        if (selectedDays.Count == 0)
            throw new InvalidOperationException("AI did not return a usable starter plan.");

        var plan = new Plan
        {
            Name = ResolvePlanName(request, draft),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PlanType = PlanType.Self,
            OwnerId = userId,
            IsActive = planCount == 0
        };

        plan.WeeklyWorkouts = PlanWeekGenerator.Generate(plan);
        AddExercises(plan, selectedDays, templateById);

        await planRepo.AddAsync(plan, cancellationToken);
        await planRepo.SaveChangesAsync(cancellationToken);

        return await planRepo.GetByIdForUserAsync(plan.Id, userId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to reload created plan.");
    }

    private static string ResolvePlanName(CreateAiStarterPlanCommand request, AiStarterPlanDraft draft)
    {
        var name = string.IsNullOrWhiteSpace(request.Name) ? draft.PlanName : request.Name;
        if (string.IsNullOrWhiteSpace(name))
            name = $"{request.Goal} Starter Plan";

        return name.Length <= 100 ? name : name[..100];
    }

    private static void AddExercises(
        Plan plan,
        IReadOnlyList<AiStarterPlanDay> selectedDays,
        IReadOnlyDictionary<Guid, TemplateCatalogItem> templateById)
    {
        foreach (var week in plan.WeeklyWorkouts)
        {
            foreach (var draftDay in selectedDays)
            {
                if (!Enum.TryParse<DayOfWeek>(draftDay.DayOfWeek, ignoreCase: true, out var dayOfWeek))
                    continue;

                var workoutDay = week.DailyWorkouts.FirstOrDefault(d => d.DayOfWeek == dayOfWeek);
                if (workoutDay is null)
                    continue;

                var sortOrder = 0;
                foreach (var draftExercise in draftDay.Exercises
                             .OrderByDescending(e => templateById.TryGetValue(e.ExerciseTemplateId, out var template) && template.IsCompound))
                {
                    if (!templateById.TryGetValue(draftExercise.ExerciseTemplateId, out var template))
                        continue;

                    var sets = Math.Clamp(draftExercise.Sets, 1, 8);
                    var reps = Math.Clamp(draftExercise.Reps, 1, 50);
                    var exercise = new Exercise
                    {
                        ExerciseTemplateId = template.Id,
                        Name = template.Name,
                        PrimaryMuscleGroup = Enum.Parse<MuscleGroup>(template.PrimaryMuscleGroup),
                        SecondaryMuscleGroups = template.SecondaryMuscleGroups
                            .Where(m => Enum.TryParse<MuscleGroup>(m, out _))
                            .Select(Enum.Parse<MuscleGroup>)
                            .ToList(),
                        ExerciseKind = Enum.Parse<ExerciseKind>(template.ExerciseKind),
                        PlannedSets = sets,
                        PlannedReps = reps,
                        PlannedWeight = draftExercise.PlannedWeight,
                        Notes = string.IsNullOrWhiteSpace(draftExercise.Notes)
                            ? draftDay.Focus
                            : draftExercise.Notes,
                        SortOrder = sortOrder++,
                    };

                    for (var i = 1; i <= sets; i++)
                    {
                        exercise.Sets.Add(new ExerciseSet
                        {
                            SetNumber = i,
                            PlannedReps = reps,
                            PlannedWeight = draftExercise.PlannedWeight
                        });
                    }

                    workoutDay.Exercises.Add(exercise);
                }
            }
        }
    }

    private sealed record TemplateCatalogItem(
        Guid Id,
        string Name,
        string PrimaryMuscleGroup,
        IReadOnlyList<string> SecondaryMuscleGroups,
        string ExerciseKind,
        bool IsCompetitionLift,
        bool IsCompound
    );

    private static bool IsCompoundExercise(string name, string primaryMuscleGroup, int secondaryMuscleCount)
    {
        var normalized = name.ToLowerInvariant();
        var compoundNames = new[]
        {
            "squat",
            "deadlift",
            "bench",
            "press",
            "row",
            "pullup",
            "pull-up",
            "chinup",
            "chin-up",
            "pulldown",
            "lunge",
            "hip thrust",
            "clean",
            "dip",
            "leg press",
            "thruster"
        };

        return secondaryMuscleCount > 0 ||
               primaryMuscleGroup is "FullBody" ||
               compoundNames.Any(normalized.Contains);
    }

    private static IReadOnlyList<DayOfWeek> GetSafeTrainingDays(int daysPerWeek)
    {
        return daysPerWeek switch
        {
            2 => [DayOfWeek.Monday, DayOfWeek.Thursday],
            3 => [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
            4 => [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday],
            5 => [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Saturday],
            _ => throw new InvalidOperationException("AI starter plans support 2 to 5 training days per week.")
        };
    }

    private sealed record AiStarterPlanDraft(
        string PlanName,
        IReadOnlyList<AiStarterPlanDay> Days
    );

    private sealed record AiStarterPlanDay(
        string DayOfWeek,
        string Focus,
        IReadOnlyList<AiStarterPlanExercise> Exercises
    );

    private sealed record AiStarterPlanExercise(
        Guid ExerciseTemplateId,
        int Sets,
        int Reps,
        decimal? PlannedWeight,
        string? Notes
    );
}
