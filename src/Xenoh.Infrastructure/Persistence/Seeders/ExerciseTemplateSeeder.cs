using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Seeders;

public static class ExerciseTemplateSeeder
{
    public static List<ExerciseTemplate> GetTemplates() =>
    [
        // ── CHEST ──────────────────────────────────────────────────────────────
        new() { Name = "Bench Press",              Description = "Barbell flat bench press",                   PrimaryMuscleGroup = MuscleGroup.Chest,      SecondaryMuscleGroups = [MuscleGroup.Triceps, MuscleGroup.Shoulders],          IsCompetitionLift = true,  CompetitionLiftType = CompetitionLiftType.Bench },
        new() { Name = "Incline Bench Press",      Description = "Barbell incline bench press",                PrimaryMuscleGroup = MuscleGroup.Chest,      SecondaryMuscleGroups = [MuscleGroup.Shoulders, MuscleGroup.Triceps] },
        new() { Name = "Decline Bench Press",      Description = "Barbell decline bench press",               PrimaryMuscleGroup = MuscleGroup.Chest,      SecondaryMuscleGroups = [MuscleGroup.Triceps] },
        new() { Name = "Dumbbell Bench Press",     Description = "Flat dumbbell bench press",                  PrimaryMuscleGroup = MuscleGroup.Chest,      SecondaryMuscleGroups = [MuscleGroup.Triceps, MuscleGroup.Shoulders] },
        new() { Name = "Incline Dumbbell Press",   Description = "Incline dumbbell bench press",               PrimaryMuscleGroup = MuscleGroup.Chest,      SecondaryMuscleGroups = [MuscleGroup.Shoulders, MuscleGroup.Triceps] },
        new() { Name = "Dumbbell Fly",             Description = "Dumbbell chest fly on flat bench",           PrimaryMuscleGroup = MuscleGroup.Chest,      SecondaryMuscleGroups = [MuscleGroup.Shoulders] },
        new() { Name = "Cable Crossover",          Description = "Cable chest fly crossover",                  PrimaryMuscleGroup = MuscleGroup.Chest,      SecondaryMuscleGroups = [MuscleGroup.Shoulders] },
        new() { Name = "Chest Dip",                Description = "Parallel bar dip leaning forward",          PrimaryMuscleGroup = MuscleGroup.Chest,      SecondaryMuscleGroups = [MuscleGroup.Triceps, MuscleGroup.Shoulders] },
        new() { Name = "Pec Deck",                 Description = "Machine pec deck / chest fly",               PrimaryMuscleGroup = MuscleGroup.Chest,      SecondaryMuscleGroups = [] },
        new() { Name = "Push-Up",                  Description = "Bodyweight push-up",                         PrimaryMuscleGroup = MuscleGroup.Chest,      SecondaryMuscleGroups = [MuscleGroup.Triceps, MuscleGroup.Shoulders, MuscleGroup.Abs] },

        // ── BACK ───────────────────────────────────────────────────────────────
        new() { Name = "Deadlift",                 Description = "Conventional barbell deadlift",               PrimaryMuscleGroup = MuscleGroup.Back,       SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Hamstrings, MuscleGroup.Quads, MuscleGroup.Abs], IsCompetitionLift = true, CompetitionLiftType = CompetitionLiftType.Deadlift },
        new() { Name = "Pull-Up",                  Description = "Bodyweight pull-up (pronated grip)",          PrimaryMuscleGroup = MuscleGroup.Back,       SecondaryMuscleGroups = [MuscleGroup.Biceps, MuscleGroup.Forearms] },
        new() { Name = "Chin-Up",                  Description = "Bodyweight chin-up (supinated grip)",         PrimaryMuscleGroup = MuscleGroup.Back,       SecondaryMuscleGroups = [MuscleGroup.Biceps] },
        new() { Name = "Barbell Row",              Description = "Bent-over barbell row",                       PrimaryMuscleGroup = MuscleGroup.Back,       SecondaryMuscleGroups = [MuscleGroup.Biceps, MuscleGroup.Forearms, MuscleGroup.Abs] },
        new() { Name = "Pendlay Row",              Description = "Strict barbell row from floor",               PrimaryMuscleGroup = MuscleGroup.Back,       SecondaryMuscleGroups = [MuscleGroup.Biceps, MuscleGroup.Abs] },
        new() { Name = "Single-Arm Dumbbell Row",  Description = "One-arm dumbbell row on bench",               PrimaryMuscleGroup = MuscleGroup.Back,       SecondaryMuscleGroups = [MuscleGroup.Biceps, MuscleGroup.Forearms] },
        new() { Name = "Lat Pulldown",             Description = "Cable lat pulldown",                          PrimaryMuscleGroup = MuscleGroup.Back,       SecondaryMuscleGroups = [MuscleGroup.Biceps, MuscleGroup.Forearms] },
        new() { Name = "Seated Cable Row",         Description = "Seated cable row machine",                    PrimaryMuscleGroup = MuscleGroup.Back,       SecondaryMuscleGroups = [MuscleGroup.Biceps, MuscleGroup.Forearms] },
        new() { Name = "T-Bar Row",                Description = "T-bar rowing machine",                        PrimaryMuscleGroup = MuscleGroup.Back,       SecondaryMuscleGroups = [MuscleGroup.Biceps, MuscleGroup.Abs] },
        new() { Name = "Face Pull",                Description = "Cable face pull for rear delts and back",     PrimaryMuscleGroup = MuscleGroup.Back,       SecondaryMuscleGroups = [MuscleGroup.Shoulders] },
        new() { Name = "Rack Pull",                Description = "Partial deadlift from knee height",           PrimaryMuscleGroup = MuscleGroup.Back,       SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Hamstrings] },

        // ── SHOULDERS ──────────────────────────────────────────────────────────
        new() { Name = "Overhead Press",           Description = "Barbell overhead shoulder press",             PrimaryMuscleGroup = MuscleGroup.Shoulders,  SecondaryMuscleGroups = [MuscleGroup.Triceps, MuscleGroup.Abs] },
        new() { Name = "Dumbbell Shoulder Press",  Description = "Seated or standing dumbbell press",           PrimaryMuscleGroup = MuscleGroup.Shoulders,  SecondaryMuscleGroups = [MuscleGroup.Triceps] },
        new() { Name = "Arnold Press",             Description = "Arnold dumbbell shoulder press",              PrimaryMuscleGroup = MuscleGroup.Shoulders,  SecondaryMuscleGroups = [MuscleGroup.Triceps] },
        new() { Name = "Dumbbell Lateral Raise",   Description = "Dumbbell lateral raise",                      PrimaryMuscleGroup = MuscleGroup.Shoulders,  SecondaryMuscleGroups = [] },
        new() { Name = "Cable Lateral Raise",      Description = "Single-arm cable lateral raise",              PrimaryMuscleGroup = MuscleGroup.Shoulders,  SecondaryMuscleGroups = [] },
        new() { Name = "Front Raise",              Description = "Dumbbell front raise",                        PrimaryMuscleGroup = MuscleGroup.Shoulders,  SecondaryMuscleGroups = [MuscleGroup.Chest] },
        new() { Name = "Reverse Fly",              Description = "Rear delt dumbbell fly",                      PrimaryMuscleGroup = MuscleGroup.Shoulders,  SecondaryMuscleGroups = [MuscleGroup.Back] },
        new() { Name = "Upright Row",              Description = "Barbell or cable upright row",                PrimaryMuscleGroup = MuscleGroup.Shoulders,  SecondaryMuscleGroups = [MuscleGroup.Biceps, MuscleGroup.Forearms] },
        new() { Name = "Machine Shoulder Press",   Description = "Plate-loaded or selectorized shoulder press", PrimaryMuscleGroup = MuscleGroup.Shoulders,  SecondaryMuscleGroups = [MuscleGroup.Triceps] },

        // ── BICEPS ─────────────────────────────────────────────────────────────
        new() { Name = "Barbell Curl",             Description = "Standing barbell bicep curl",                 PrimaryMuscleGroup = MuscleGroup.Biceps,     SecondaryMuscleGroups = [MuscleGroup.Forearms] },
        new() { Name = "Dumbbell Curl",            Description = "Alternating dumbbell curl",                   PrimaryMuscleGroup = MuscleGroup.Biceps,     SecondaryMuscleGroups = [MuscleGroup.Forearms] },
        new() { Name = "Hammer Curl",              Description = "Neutral grip dumbbell curl",                  PrimaryMuscleGroup = MuscleGroup.Biceps,     SecondaryMuscleGroups = [MuscleGroup.Forearms] },
        new() { Name = "Preacher Curl",            Description = "EZ-bar preacher curl",                        PrimaryMuscleGroup = MuscleGroup.Biceps,     SecondaryMuscleGroups = [] },
        new() { Name = "Cable Curl",               Description = "Low-cable bicep curl",                        PrimaryMuscleGroup = MuscleGroup.Biceps,     SecondaryMuscleGroups = [MuscleGroup.Forearms] },
        new() { Name = "Concentration Curl",       Description = "Single-arm concentration curl",               PrimaryMuscleGroup = MuscleGroup.Biceps,     SecondaryMuscleGroups = [] },
        new() { Name = "Incline Dumbbell Curl",    Description = "Incline bench dumbbell curl for peak",        PrimaryMuscleGroup = MuscleGroup.Biceps,     SecondaryMuscleGroups = [] },

        // ── TRICEPS ────────────────────────────────────────────────────────────
        new() { Name = "Tricep Pushdown",          Description = "Cable tricep pushdown",                       PrimaryMuscleGroup = MuscleGroup.Triceps,    SecondaryMuscleGroups = [] },
        new() { Name = "Skull Crusher",            Description = "EZ-bar lying tricep extension",               PrimaryMuscleGroup = MuscleGroup.Triceps,    SecondaryMuscleGroups = [] },
        new() { Name = "Overhead Tricep Extension",Description = "Dumbbell or cable overhead tricep ext",       PrimaryMuscleGroup = MuscleGroup.Triceps,    SecondaryMuscleGroups = [] },
        new() { Name = "Tricep Dip",               Description = "Parallel bar dips (upright torso)",           PrimaryMuscleGroup = MuscleGroup.Triceps,    SecondaryMuscleGroups = [MuscleGroup.Chest, MuscleGroup.Shoulders] },
        new() { Name = "Close-Grip Bench Press",   Description = "Narrow grip barbell bench press",             PrimaryMuscleGroup = MuscleGroup.Triceps,    SecondaryMuscleGroups = [MuscleGroup.Chest, MuscleGroup.Shoulders] },
        new() { Name = "Tricep Kickback",          Description = "Dumbbell tricep kickback",                    PrimaryMuscleGroup = MuscleGroup.Triceps,    SecondaryMuscleGroups = [] },
        new() { Name = "Diamond Push-Up",          Description = "Close-grip push-up targeting triceps",        PrimaryMuscleGroup = MuscleGroup.Triceps,    SecondaryMuscleGroups = [MuscleGroup.Chest] },

        // ── QUADS ──────────────────────────────────────────────────────────────
        new() { Name = "Squat",                    Description = "Barbell back squat",                          PrimaryMuscleGroup = MuscleGroup.Quads,      SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Hamstrings, MuscleGroup.Abs], IsCompetitionLift = true, CompetitionLiftType = CompetitionLiftType.Squat },
        new() { Name = "Front Squat",              Description = "Barbell front squat",                         PrimaryMuscleGroup = MuscleGroup.Quads,      SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Abs] },
        new() { Name = "Hack Squat",               Description = "Machine hack squat",                          PrimaryMuscleGroup = MuscleGroup.Quads,      SecondaryMuscleGroups = [MuscleGroup.Glutes] },
        new() { Name = "Leg Press",                Description = "Machine leg press",                           PrimaryMuscleGroup = MuscleGroup.Quads,      SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Hamstrings] },
        new() { Name = "Leg Extension",            Description = "Machine leg extension",                       PrimaryMuscleGroup = MuscleGroup.Quads,      SecondaryMuscleGroups = [] },
        new() { Name = "Lunge",                    Description = "Barbell or dumbbell lunge",                   PrimaryMuscleGroup = MuscleGroup.Quads,      SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Hamstrings, MuscleGroup.Calves] },
        new() { Name = "Bulgarian Split Squat",    Description = "Rear-foot-elevated split squat",              PrimaryMuscleGroup = MuscleGroup.Quads,      SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Hamstrings] },
        new() { Name = "Goblet Squat",             Description = "Dumbbell or kettlebell goblet squat",         PrimaryMuscleGroup = MuscleGroup.Quads,      SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Abs] },
        new() { Name = "Step-Up",                  Description = "Dumbbell step-up onto box",                   PrimaryMuscleGroup = MuscleGroup.Quads,      SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Calves] },

        // ── HAMSTRINGS ─────────────────────────────────────────────────────────
        new() { Name = "Romanian Deadlift",        Description = "RDL with barbell",                            PrimaryMuscleGroup = MuscleGroup.Hamstrings, SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Back] },
        new() { Name = "Sumo Deadlift",            Description = "Wide-stance sumo deadlift",                   PrimaryMuscleGroup = MuscleGroup.Hamstrings, SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Back, MuscleGroup.Quads] },
        new() { Name = "Leg Curl",                 Description = "Machine lying or seated leg curl",             PrimaryMuscleGroup = MuscleGroup.Hamstrings, SecondaryMuscleGroups = [] },
        new() { Name = "Nordic Curl",              Description = "Partner-assisted nordic hamstring curl",       PrimaryMuscleGroup = MuscleGroup.Hamstrings, SecondaryMuscleGroups = [MuscleGroup.Glutes] },
        new() { Name = "Good Morning",             Description = "Barbell good morning",                        PrimaryMuscleGroup = MuscleGroup.Hamstrings, SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Back] },

        // ── GLUTES ─────────────────────────────────────────────────────────────
        new() { Name = "Hip Thrust",               Description = "Barbell hip thrust",                          PrimaryMuscleGroup = MuscleGroup.Glutes,     SecondaryMuscleGroups = [MuscleGroup.Hamstrings, MuscleGroup.Abs] },
        new() { Name = "Glute Bridge",             Description = "Bodyweight or barbell glute bridge",          PrimaryMuscleGroup = MuscleGroup.Glutes,     SecondaryMuscleGroups = [MuscleGroup.Hamstrings, MuscleGroup.Abs] },
        new() { Name = "Cable Kickback",           Description = "Cable glute kickback",                        PrimaryMuscleGroup = MuscleGroup.Glutes,     SecondaryMuscleGroups = [MuscleGroup.Hamstrings] },
        new() { Name = "Donkey Kick",              Description = "Bodyweight or cable donkey kick",             PrimaryMuscleGroup = MuscleGroup.Glutes,     SecondaryMuscleGroups = [MuscleGroup.Hamstrings] },
        new() { Name = "Sumo Squat",               Description = "Wide-stance bodyweight or goblet squat",      PrimaryMuscleGroup = MuscleGroup.Glutes,     SecondaryMuscleGroups = [MuscleGroup.Quads, MuscleGroup.Hamstrings] },

        // ── CALVES ─────────────────────────────────────────────────────────────
        new() { Name = "Standing Calf Raise",      Description = "Machine or barbell standing calf raise",      PrimaryMuscleGroup = MuscleGroup.Calves,     SecondaryMuscleGroups = [] },
        new() { Name = "Seated Calf Raise",        Description = "Machine seated calf raise",                   PrimaryMuscleGroup = MuscleGroup.Calves,     SecondaryMuscleGroups = [] },
        new() { Name = "Donkey Calf Raise",        Description = "Donkey calf raise on machine or with belt",   PrimaryMuscleGroup = MuscleGroup.Calves,     SecondaryMuscleGroups = [] },
        new() { Name = "Single-Leg Calf Raise",    Description = "Bodyweight single-leg calf raise",            PrimaryMuscleGroup = MuscleGroup.Calves,     SecondaryMuscleGroups = [] },

        // ── ABS ────────────────────────────────────────────────────────────────
        new() { Name = "Plank",                    Description = "Forearm plank hold",                          PrimaryMuscleGroup = MuscleGroup.Abs,        SecondaryMuscleGroups = [MuscleGroup.Shoulders] },
        new() { Name = "Side Plank",               Description = "Lateral forearm plank hold",                  PrimaryMuscleGroup = MuscleGroup.Abs,        SecondaryMuscleGroups = [MuscleGroup.Shoulders] },
        new() { Name = "Crunch",                   Description = "Bodyweight abdominal crunch",                 PrimaryMuscleGroup = MuscleGroup.Abs,        SecondaryMuscleGroups = [] },
        new() { Name = "Decline Crunch",           Description = "Crunch on a decline bench",                   PrimaryMuscleGroup = MuscleGroup.Abs,        SecondaryMuscleGroups = [] },
        new() { Name = "Hanging Leg Raise",        Description = "Hanging bar leg raise",                       PrimaryMuscleGroup = MuscleGroup.Abs,        SecondaryMuscleGroups = [MuscleGroup.Forearms] },
        new() { Name = "Cable Crunch",             Description = "Kneeling cable crunch",                       PrimaryMuscleGroup = MuscleGroup.Abs,        SecondaryMuscleGroups = [] },
        new() { Name = "Ab Wheel Rollout",         Description = "Ab wheel roll-out",                           PrimaryMuscleGroup = MuscleGroup.Abs,        SecondaryMuscleGroups = [MuscleGroup.Shoulders, MuscleGroup.Back] },
        new() { Name = "Russian Twist",            Description = "Seated rotational core exercise",             PrimaryMuscleGroup = MuscleGroup.Abs,        SecondaryMuscleGroups = [] },
        new() { Name = "Pallof Press",             Description = "Anti-rotation cable Pallof press",            PrimaryMuscleGroup = MuscleGroup.Abs,        SecondaryMuscleGroups = [MuscleGroup.Shoulders] },
        new() { Name = "Dragon Flag",              Description = "Bruce Lee dragon flag",                       PrimaryMuscleGroup = MuscleGroup.Abs,        SecondaryMuscleGroups = [] },

        // ── FOREARMS ───────────────────────────────────────────────────────────
        new() { Name = "Wrist Curl",               Description = "Barbell wrist curl",                          PrimaryMuscleGroup = MuscleGroup.Forearms,   SecondaryMuscleGroups = [] },
        new() { Name = "Reverse Wrist Curl",       Description = "Barbell reverse wrist curl",                  PrimaryMuscleGroup = MuscleGroup.Forearms,   SecondaryMuscleGroups = [] },
        new() { Name = "Farmer's Walk",            Description = "Heavy dumbbell carry",                        PrimaryMuscleGroup = MuscleGroup.Forearms,   SecondaryMuscleGroups = [MuscleGroup.Abs, MuscleGroup.Shoulders] },
        new() { Name = "Plate Pinch",              Description = "Plate pinch hold for grip strength",          PrimaryMuscleGroup = MuscleGroup.Forearms,   SecondaryMuscleGroups = [] },

        // ── TRAPS ──────────────────────────────────────────────────────────────
        new() { Name = "Barbell Shrug",            Description = "Standing barbell shrug",                      PrimaryMuscleGroup = MuscleGroup.Traps,      SecondaryMuscleGroups = [MuscleGroup.Forearms] },
        new() { Name = "Dumbbell Shrug",           Description = "Standing dumbbell shrug",                     PrimaryMuscleGroup = MuscleGroup.Traps,      SecondaryMuscleGroups = [MuscleGroup.Forearms] },
        new() { Name = "Smith Machine Shrug",      Description = "Smith machine trap shrug",                    PrimaryMuscleGroup = MuscleGroup.Traps,      SecondaryMuscleGroups = [MuscleGroup.Forearms] },
        new() { Name = "Trap Bar Shrug",           Description = "Trap bar shrug",                              PrimaryMuscleGroup = MuscleGroup.Traps,      SecondaryMuscleGroups = [MuscleGroup.Forearms] },
        new() { Name = "High Pull",                Description = "Barbell high pull",                           PrimaryMuscleGroup = MuscleGroup.Traps,      SecondaryMuscleGroups = [MuscleGroup.Shoulders, MuscleGroup.Back] },

        // ── NECK ───────────────────────────────────────────────────────────────
        new() { Name = "Neck Flexion",             Description = "Neck flexion with harness or plate",          PrimaryMuscleGroup = MuscleGroup.Neck,       SecondaryMuscleGroups = [] },
        new() { Name = "Neck Extension",           Description = "Neck extension with harness or plate",        PrimaryMuscleGroup = MuscleGroup.Neck,       SecondaryMuscleGroups = [MuscleGroup.Traps] },
        new() { Name = "Lateral Neck Flexion",     Description = "Side neck flexion",                           PrimaryMuscleGroup = MuscleGroup.Neck,       SecondaryMuscleGroups = [] },
        new() { Name = "Neck Harness Extension",   Description = "Weighted neck harness extension",             PrimaryMuscleGroup = MuscleGroup.Neck,       SecondaryMuscleGroups = [MuscleGroup.Traps] },

        // ── ADDUCTORS ──────────────────────────────────────────────────────────
        new() { Name = "Hip Adduction Machine",    Description = "Seated machine hip adduction",               PrimaryMuscleGroup = MuscleGroup.Adductors,  SecondaryMuscleGroups = [] },
        new() { Name = "Copenhagen Plank",         Description = "Side plank variation for adductors",          PrimaryMuscleGroup = MuscleGroup.Adductors,  SecondaryMuscleGroups = [MuscleGroup.Abs] },
        new() { Name = "Cable Hip Adduction",      Description = "Standing cable hip adduction",                PrimaryMuscleGroup = MuscleGroup.Adductors,  SecondaryMuscleGroups = [] },
        new() { Name = "Sumo Leg Press",           Description = "Wide-stance leg press",                       PrimaryMuscleGroup = MuscleGroup.Adductors,  SecondaryMuscleGroups = [MuscleGroup.Quads, MuscleGroup.Glutes] },
        new() { Name = "Side Lunge",               Description = "Lateral lunge",                               PrimaryMuscleGroup = MuscleGroup.Adductors,  SecondaryMuscleGroups = [MuscleGroup.Quads, MuscleGroup.Glutes] },

        // ── ABDUCTORS ──────────────────────────────────────────────────────────
        new() { Name = "Hip Abduction Machine",    Description = "Seated machine hip abduction",               PrimaryMuscleGroup = MuscleGroup.Abductors,  SecondaryMuscleGroups = [MuscleGroup.Glutes] },
        new() { Name = "Cable Hip Abduction",      Description = "Standing cable hip abduction",                PrimaryMuscleGroup = MuscleGroup.Abductors,  SecondaryMuscleGroups = [MuscleGroup.Glutes] },
        new() { Name = "Banded Lateral Walk",      Description = "Mini-band lateral walk",                      PrimaryMuscleGroup = MuscleGroup.Abductors,  SecondaryMuscleGroups = [MuscleGroup.Glutes] },
        new() { Name = "Clamshell",                Description = "Side-lying clamshell",                        PrimaryMuscleGroup = MuscleGroup.Abductors,  SecondaryMuscleGroups = [MuscleGroup.Glutes] },
        new() { Name = "Side-Lying Leg Raise",     Description = "Side-lying hip abduction raise",              PrimaryMuscleGroup = MuscleGroup.Abductors,  SecondaryMuscleGroups = [MuscleGroup.Glutes] },

        // ── FULL BODY ──────────────────────────────────────────────────────────
        new() { Name = "Burpee",                   Description = "Full body burpee",                            PrimaryMuscleGroup = MuscleGroup.FullBody,   SecondaryMuscleGroups = [MuscleGroup.Chest, MuscleGroup.Abs, MuscleGroup.Quads] },
        new() { Name = "Clean and Press",          Description = "Barbell power clean and press",               PrimaryMuscleGroup = MuscleGroup.FullBody,   SecondaryMuscleGroups = [MuscleGroup.Shoulders, MuscleGroup.Back, MuscleGroup.Quads] },
        new() { Name = "Kettlebell Swing",         Description = "Two-hand kettlebell swing",                   PrimaryMuscleGroup = MuscleGroup.FullBody,   SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Hamstrings, MuscleGroup.Abs] },
        new() { Name = "Thruster",                 Description = "Front squat to overhead press",               PrimaryMuscleGroup = MuscleGroup.FullBody,   SecondaryMuscleGroups = [MuscleGroup.Quads, MuscleGroup.Shoulders, MuscleGroup.Abs] },
        new() { Name = "Turkish Get-Up",           Description = "Kettlebell Turkish get-up",                   PrimaryMuscleGroup = MuscleGroup.FullBody,   SecondaryMuscleGroups = [MuscleGroup.Shoulders, MuscleGroup.Abs, MuscleGroup.Glutes] },
        new() { Name = "Man Maker",                Description = "Dumbbell push-up, row, clean and press",      PrimaryMuscleGroup = MuscleGroup.FullBody,   SecondaryMuscleGroups = [MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders] },
        new() { Name = "Box Jump",                 Description = "Plyometric jump onto a box",                  PrimaryMuscleGroup = MuscleGroup.FullBody,   SecondaryMuscleGroups = [MuscleGroup.Quads, MuscleGroup.Glutes, MuscleGroup.Calves] },

        // ── CARDIO ─────────────────────────────────────────────────────────────
        new() { Name = "Running",                  Description = "Treadmill or outdoor running",                 PrimaryMuscleGroup = MuscleGroup.Cardio,     SecondaryMuscleGroups = [MuscleGroup.Quads, MuscleGroup.Calves, MuscleGroup.Glutes], ExerciseKind = ExerciseKind.Cardio, EstimatedMet = 9.8m },
        new() { Name = "Cycling",                  Description = "Stationary bike or outdoor cycling",          PrimaryMuscleGroup = MuscleGroup.Cardio,     SecondaryMuscleGroups = [MuscleGroup.Quads, MuscleGroup.Glutes, MuscleGroup.Calves], ExerciseKind = ExerciseKind.Cardio, EstimatedMet = 7.5m },
        new() { Name = "Rowing Machine",           Description = "Ergometer rowing machine",                    PrimaryMuscleGroup = MuscleGroup.Cardio,     SecondaryMuscleGroups = [MuscleGroup.Back, MuscleGroup.Shoulders, MuscleGroup.Quads], ExerciseKind = ExerciseKind.Cardio, EstimatedMet = 7.0m },
        new() { Name = "Jump Rope",                Description = "Skipping rope cardio",                        PrimaryMuscleGroup = MuscleGroup.Cardio,     SecondaryMuscleGroups = [MuscleGroup.Calves, MuscleGroup.Shoulders], ExerciseKind = ExerciseKind.Cardio, EstimatedMet = 10.0m },
        new() { Name = "Assault Bike",             Description = "Fan bike full-body cardio",                   PrimaryMuscleGroup = MuscleGroup.Cardio,     SecondaryMuscleGroups = [MuscleGroup.Shoulders, MuscleGroup.Quads], ExerciseKind = ExerciseKind.Cardio, EstimatedMet = 9.0m },
        new() { Name = "Elliptical",               Description = "Elliptical cross-trainer",                    PrimaryMuscleGroup = MuscleGroup.Cardio,     SecondaryMuscleGroups = [MuscleGroup.Quads, MuscleGroup.Glutes], ExerciseKind = ExerciseKind.Cardio, EstimatedMet = 5.5m },
        new() { Name = "Stair Climber",            Description = "Stair climber machine",                       PrimaryMuscleGroup = MuscleGroup.Cardio,     SecondaryMuscleGroups = [MuscleGroup.Glutes, MuscleGroup.Quads, MuscleGroup.Calves], ExerciseKind = ExerciseKind.Cardio, EstimatedMet = 8.8m },
        new() { Name = "Mountain Climber",         Description = "Bodyweight mountain climber",                 PrimaryMuscleGroup = MuscleGroup.Cardio,     SecondaryMuscleGroups = [MuscleGroup.Abs, MuscleGroup.Shoulders], ExerciseKind = ExerciseKind.Cardio, EstimatedMet = 8.0m },
    ];
}
