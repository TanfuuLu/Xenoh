using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Seeders;

public static class ExerciseTemplateSeeder
{
    public static List<ExerciseTemplate> GetTemplates() =>
    [
        // ── CHEST ──────────────────────────────────────────────
        new() { Name = "Bench Press",           Description = "Barbell flat bench press",             PrimaryMuscleGroup = MuscleGroup.Chest,       SecondaryMuscleGroups = [MuscleGroup.Triceps, MuscleGroup.Shoulders],   IsCompetitionLift = true, CompetitionLiftType = CompetitionLiftType.Bench },
        new() { Name = "Incline Bench Press",   Description = "Barbell incline bench press",          PrimaryMuscleGroup = MuscleGroup.Chest,       SecondaryMuscleGroups = [MuscleGroup.Shoulders, MuscleGroup.Triceps] },
        new() { Name = "Dumbbell Fly",          Description = "Dumbbell chest fly on flat bench",     PrimaryMuscleGroup = MuscleGroup.Chest,       SecondaryMuscleGroups = [MuscleGroup.Shoulders] },
        new() { Name = "Push-Up",               Description = "Bodyweight push-up",                  PrimaryMuscleGroup = MuscleGroup.Chest,       SecondaryMuscleGroups = [MuscleGroup.Triceps, MuscleGroup.Shoulders, MuscleGroup.Core] },
        new() { Name = "Cable Crossover",       Description = "Cable chest fly crossover",           PrimaryMuscleGroup = MuscleGroup.Chest,       SecondaryMuscleGroups = [MuscleGroup.Shoulders] },

        // ── BACK ───────────────────────────────────────────────
        new() { Name = "Deadlift",              Description = "Conventional barbell deadlift",        PrimaryMuscleGroup = MuscleGroup.Back,        SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Hamstrings, MuscleGroup.Quadriceps, MuscleGroup.Core], IsCompetitionLift = true, CompetitionLiftType = CompetitionLiftType.Deadlift },
        new() { Name = "Pull-Up",               Description = "Bodyweight pull-up",                  PrimaryMuscleGroup = MuscleGroup.Back,        SecondaryMuscleGroups = [MuscleGroup.Biceps, MuscleGroup.Forearms] },
        new() { Name = "Barbell Row",           Description = "Bent-over barbell row",               PrimaryMuscleGroup = MuscleGroup.Back,        SecondaryMuscleGroups = [MuscleGroup.Biceps, MuscleGroup.Forearms, MuscleGroup.Core] },
        new() { Name = "Lat Pulldown",          Description = "Cable lat pulldown",                  PrimaryMuscleGroup = MuscleGroup.Back,        SecondaryMuscleGroups = [MuscleGroup.Biceps, MuscleGroup.Forearms] },
        new() { Name = "Seated Cable Row",      Description = "Seated cable row machine",            PrimaryMuscleGroup = MuscleGroup.Back,        SecondaryMuscleGroups = [MuscleGroup.Biceps, MuscleGroup.Forearms] },
        new() { Name = "T-Bar Row",             Description = "T-bar rowing machine",                PrimaryMuscleGroup = MuscleGroup.Back,        SecondaryMuscleGroups = [MuscleGroup.Biceps, MuscleGroup.Core] },

        // ── SHOULDERS ──────────────────────────────────────────
        new() { Name = "Overhead Press",        Description = "Barbell overhead shoulder press",     PrimaryMuscleGroup = MuscleGroup.Shoulders,   SecondaryMuscleGroups = [MuscleGroup.Triceps, MuscleGroup.Core] },
        new() { Name = "Dumbbell Lateral Raise",Description = "Dumbbell lateral raise",              PrimaryMuscleGroup = MuscleGroup.Shoulders,   SecondaryMuscleGroups = [] },
        new() { Name = "Front Raise",           Description = "Dumbbell front raise",                PrimaryMuscleGroup = MuscleGroup.Shoulders,   SecondaryMuscleGroups = [MuscleGroup.Chest] },
        new() { Name = "Reverse Fly",           Description = "Rear delt dumbbell fly",              PrimaryMuscleGroup = MuscleGroup.Shoulders,   SecondaryMuscleGroups = [MuscleGroup.Back] },
        new() { Name = "Arnold Press",          Description = "Arnold dumbbell shoulder press",      PrimaryMuscleGroup = MuscleGroup.Shoulders,   SecondaryMuscleGroups = [MuscleGroup.Triceps] },

        // ── BICEPS ─────────────────────────────────────────────
        new() { Name = "Barbell Curl",          Description = "Standing barbell bicep curl",         PrimaryMuscleGroup = MuscleGroup.Biceps,      SecondaryMuscleGroups = [MuscleGroup.Forearms] },
        new() { Name = "Dumbbell Curl",         Description = "Alternating dumbbell curl",           PrimaryMuscleGroup = MuscleGroup.Biceps,      SecondaryMuscleGroups = [MuscleGroup.Forearms] },
        new() { Name = "Hammer Curl",           Description = "Neutral grip dumbbell curl",          PrimaryMuscleGroup = MuscleGroup.Biceps,      SecondaryMuscleGroups = [MuscleGroup.Forearms] },
        new() { Name = "Preacher Curl",         Description = "EZ-bar preacher curl",               PrimaryMuscleGroup = MuscleGroup.Biceps,      SecondaryMuscleGroups = [] },

        // ── TRICEPS ────────────────────────────────────────────
        new() { Name = "Tricep Pushdown",       Description = "Cable tricep pushdown",               PrimaryMuscleGroup = MuscleGroup.Triceps,     SecondaryMuscleGroups = [] },
        new() { Name = "Skull Crusher",         Description = "EZ-bar lying tricep extension",       PrimaryMuscleGroup = MuscleGroup.Triceps,     SecondaryMuscleGroups = [] },
        new() { Name = "Tricep Dip",            Description = "Parallel bar dips",                  PrimaryMuscleGroup = MuscleGroup.Triceps,     SecondaryMuscleGroups = [MuscleGroup.Chest, MuscleGroup.Shoulders] },
        new() { Name = "Close-Grip Bench Press",Description = "Narrow grip barbell bench press",    PrimaryMuscleGroup = MuscleGroup.Triceps,     SecondaryMuscleGroups = [MuscleGroup.Chest, MuscleGroup.Shoulders] },

        // ── QUADRICEPS ─────────────────────────────────────────
        new() { Name = "Squat",                 Description = "Barbell back squat",                  PrimaryMuscleGroup = MuscleGroup.Quadriceps,  SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Hamstrings, MuscleGroup.Core],  IsCompetitionLift = true, CompetitionLiftType = CompetitionLiftType.Squat },
        new() { Name = "Front Squat",           Description = "Barbell front squat",                 PrimaryMuscleGroup = MuscleGroup.Quadriceps,  SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Core] },
        new() { Name = "Leg Press",             Description = "Machine leg press",                   PrimaryMuscleGroup = MuscleGroup.Quadriceps,  SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Hamstrings] },
        new() { Name = "Leg Extension",         Description = "Machine leg extension",               PrimaryMuscleGroup = MuscleGroup.Quadriceps,  SecondaryMuscleGroups = [] },
        new() { Name = "Lunge",                 Description = "Barbell or dumbbell lunge",           PrimaryMuscleGroup = MuscleGroup.Quadriceps,  SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Hamstrings, MuscleGroup.Calves] },

        // ── HAMSTRINGS ─────────────────────────────────────────
        new() { Name = "Romanian Deadlift",     Description = "RDL with barbell",                   PrimaryMuscleGroup = MuscleGroup.Hamstrings,  SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Back] },
        new() { Name = "Leg Curl",              Description = "Machine lying or seated leg curl",    PrimaryMuscleGroup = MuscleGroup.Hamstrings,  SecondaryMuscleGroups = [] },
        new() { Name = "Good Morning",          Description = "Barbell good morning",               PrimaryMuscleGroup = MuscleGroup.Hamstrings,  SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Back] },

        // ── GLUTES ─────────────────────────────────────────────
        new() { Name = "Hip Thrust",            Description = "Barbell hip thrust",                  PrimaryMuscleGroup = MuscleGroup.Glutes,      SecondaryMuscleGroups = [MuscleGroup.Hamstrings, MuscleGroup.Core] },
        new() { Name = "Glute Bridge",          Description = "Bodyweight or barbell glute bridge",  PrimaryMuscleGroup = MuscleGroup.Glutes,      SecondaryMuscleGroups = [MuscleGroup.Hamstrings, MuscleGroup.Core] },
        new() { Name = "Cable Kickback",        Description = "Cable glute kickback",               PrimaryMuscleGroup = MuscleGroup.Glutes,      SecondaryMuscleGroups = [MuscleGroup.Hamstrings] },

        // ── CALVES ─────────────────────────────────────────────
        new() { Name = "Standing Calf Raise",   Description = "Machine or barbell standing calf raise", PrimaryMuscleGroup = MuscleGroup.Calves,  SecondaryMuscleGroups = [] },
        new() { Name = "Seated Calf Raise",     Description = "Machine seated calf raise",           PrimaryMuscleGroup = MuscleGroup.Calves,      SecondaryMuscleGroups = [] },

        // ── CORE ───────────────────────────────────────────────
        new() { Name = "Plank",                 Description = "Forearm plank hold",                  PrimaryMuscleGroup = MuscleGroup.Core,        SecondaryMuscleGroups = [MuscleGroup.Shoulders] },
        new() { Name = "Crunch",                Description = "Bodyweight abdominal crunch",         PrimaryMuscleGroup = MuscleGroup.Core,        SecondaryMuscleGroups = [] },
        new() { Name = "Hanging Leg Raise",     Description = "Hanging bar leg raise",               PrimaryMuscleGroup = MuscleGroup.Core,        SecondaryMuscleGroups = [MuscleGroup.Forearms] },
        new() { Name = "Cable Crunch",          Description = "Kneeling cable crunch",               PrimaryMuscleGroup = MuscleGroup.Core,        SecondaryMuscleGroups = [] },
        new() { Name = "Russian Twist",         Description = "Seated rotational core exercise",     PrimaryMuscleGroup = MuscleGroup.Core,        SecondaryMuscleGroups = [] },

        // ── FOREARMS ───────────────────────────────────────────
        new() { Name = "Wrist Curl",            Description = "Barbell wrist curl",                  PrimaryMuscleGroup = MuscleGroup.Forearms,    SecondaryMuscleGroups = [] },
        new() { Name = "Farmer's Walk",         Description = "Heavy dumbbell carry",                PrimaryMuscleGroup = MuscleGroup.Forearms,    SecondaryMuscleGroups = [MuscleGroup.Core, MuscleGroup.Shoulders] },

        // ── FULL BODY ──────────────────────────────────────────
        new() { Name = "Burpee",                Description = "Full body burpee",                    PrimaryMuscleGroup = MuscleGroup.FullBody,    SecondaryMuscleGroups = [MuscleGroup.Chest, MuscleGroup.Core, MuscleGroup.Quadriceps] },
        new() { Name = "Clean and Press",       Description = "Barbell power clean and press",       PrimaryMuscleGroup = MuscleGroup.FullBody,    SecondaryMuscleGroups = [MuscleGroup.Shoulders, MuscleGroup.Back, MuscleGroup.Quadriceps] },
        new() { Name = "Kettlebell Swing",      Description = "Two-hand kettlebell swing",           PrimaryMuscleGroup = MuscleGroup.FullBody,    SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Hamstrings, MuscleGroup.Core] },
    ];
}
