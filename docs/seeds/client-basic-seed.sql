-- Basic development seed for client@gmail.com.
-- Run manually against the development PostgreSQL database when needed.
-- This file intentionally uses existing baseline ExerciseTemplates only.

DO $$
DECLARE
    v_user_id uuid;
    v_plan_id uuid;
    v_week_id uuid;
    v_day1_id uuid;
    v_day2_id uuid;
    v_exercise_id uuid;
    v_template_id uuid;
BEGIN
    SELECT "Id"
    INTO v_user_id
    FROM "AspNetUsers"
    WHERE "NormalizedEmail" = 'CLIENT@GMAIL.COM'
    LIMIT 1;

    IF v_user_id IS NULL THEN
        RAISE EXCEPTION 'User client@gmail.com does not exist. Create the account first, then run this seed.';
    END IF;

    UPDATE "AspNetUsers"
    SET
        "FirstName" = COALESCE(NULLIF("FirstName", ''), 'Client'),
        "LastName" = COALESCE(NULLIF("LastName", ''), 'Nguyen'),
        "Height" = 175.5,
        "Gender" = 0,
        "PreferredLanguage" = 'vi',
        "PreferredTheme" = 'dark',
        "PreferredWeightUnit" = 'kg',
        "TotalXp" = GREATEST("TotalXp", 1200),
        "Level" = GREATEST("Level", 4)
    WHERE "Id" = v_user_id;

    INSERT INTO "UserSubscriptions" ("Id", "UserId", "Tier", "ExpiresAt", "CreatedAt", "UpdatedAt")
    VALUES (gen_random_uuid(), v_user_id, 'ProIndividual', '2026-12-31 23:59:59+00', now(), now())
    ON CONFLICT ("UserId") DO UPDATE
    SET "Tier" = 'ProIndividual',
        "ExpiresAt" = EXCLUDED."ExpiresAt",
        "UpdatedAt" = now();

    INSERT INTO "NutritionProfiles" (
        "Id", "UserId", "ActivityLevel", "Goal", "TargetWeightKg",
        "CustomCalorieTarget", "ProteinPerKg", "FatPerKg", "CreatedAt", "UpdatedAt")
    VALUES (gen_random_uuid(), v_user_id, 3, 1, 78.0, 2850, 2.0, 0.85, now(), now())
    ON CONFLICT ("UserId") DO UPDATE
    SET "ActivityLevel" = 3,
        "Goal" = 1,
        "TargetWeightKg" = 78.0,
        "CustomCalorieTarget" = 2850,
        "ProteinPerKg" = 2.0,
        "FatPerKg" = 0.85,
        "UpdatedAt" = now();

    INSERT INTO "BodyweightLogs" ("Id", "UserId", "Weight", "Date", "CreatedAt", "UpdatedAt")
    VALUES
        (gen_random_uuid(), v_user_id, 79.2, DATE '2026-05-15', now(), now()),
        (gen_random_uuid(), v_user_id, 79.0, DATE '2026-05-22', now(), now()),
        (gen_random_uuid(), v_user_id, 78.8, DATE '2026-05-29', now(), now()),
        (gen_random_uuid(), v_user_id, 78.7, DATE '2026-06-05', now(), now())
    ON CONFLICT ("UserId", "Date") DO UPDATE
    SET "Weight" = EXCLUDED."Weight",
        "UpdatedAt" = now();

    INSERT INTO "NutritionDailyLogs" ("Id", "UserId", "Date", "Calories", "ProteinG", "CarbsG", "FatG", "Notes", "CreatedAt", "UpdatedAt")
    VALUES
        (gen_random_uuid(), v_user_id, DATE '2026-05-30', 2780, 156, 320, 78, 'Basic seed training day.', now(), now()),
        (gen_random_uuid(), v_user_id, DATE '2026-05-31', 2660, 150, 275, 82, 'Basic seed rest day.', now(), now()),
        (gen_random_uuid(), v_user_id, DATE '2026-06-01', 2860, 162, 345, 76, 'Basic seed training day.', now(), now()),
        (gen_random_uuid(), v_user_id, DATE '2026-06-02', 2840, 160, 338, 77, 'Basic seed training day.', now(), now()),
        (gen_random_uuid(), v_user_id, DATE '2026-06-03', 2680, 154, 282, 80, 'Basic seed rest day.', now(), now()),
        (gen_random_uuid(), v_user_id, DATE '2026-06-04', 2910, 166, 355, 78, 'Basic seed heavy session day.', now(), now()),
        (gen_random_uuid(), v_user_id, DATE '2026-06-05', 2760, 158, 310, 79, 'Basic seed check-in day.', now(), now())
    ON CONFLICT ("UserId", "Date") DO UPDATE
    SET "Calories" = EXCLUDED."Calories",
        "ProteinG" = EXCLUDED."ProteinG",
        "CarbsG" = EXCLUDED."CarbsG",
        "FatG" = EXCLUDED."FatG",
        "Notes" = EXCLUDED."Notes",
        "UpdatedAt" = now();

    IF NOT EXISTS (
        SELECT 1 FROM "Plans"
        WHERE "OwnerId" = v_user_id AND "Name" = 'Client Basic Strength Seed'
    ) THEN
        UPDATE "Plans"
        SET "IsActive" = false,
            "UpdatedAt" = now()
        WHERE "OwnerId" = v_user_id AND "IsActive" = true;

        v_plan_id := gen_random_uuid();
        INSERT INTO "Plans" ("Id", "Name", "StartDate", "EndDate", "PlanType", "OwnerId", "CreatedByCoachId", "IsActive", "CreatedAt", "UpdatedAt")
        VALUES (v_plan_id, 'Client Basic Strength Seed', DATE '2026-06-01', DATE '2026-06-28', 0, v_user_id, NULL, true, now(), now());

        v_week_id := gen_random_uuid();
        INSERT INTO "WeeklyWorkouts" ("Id", "WeekNumber", "Name", "StartDate", "EndDate", "IsCompleted", "PlanId", "CreatedAt", "UpdatedAt")
        VALUES (v_week_id, 1, 'Week 1', DATE '2026-06-01', DATE '2026-06-07', false, v_plan_id, now(), now());

        v_day1_id := gen_random_uuid();
        INSERT INTO "DailyWorkouts" ("Id", "Date", "DayOfWeek", "IsCompleted", "Status", "WeeklyWorkoutId", "CreatedAt", "UpdatedAt")
        VALUES (v_day1_id, DATE '2026-06-01', 1, true, 0, v_week_id, now(), now());

        v_day2_id := gen_random_uuid();
        INSERT INTO "DailyWorkouts" ("Id", "Date", "DayOfWeek", "IsCompleted", "Status", "WeeklyWorkoutId", "CreatedAt", "UpdatedAt")
        VALUES (v_day2_id, DATE '2026-06-04', 4, true, 0, v_week_id, now(), now());

        SELECT "Id" INTO v_template_id FROM "ExerciseTemplates" WHERE "Name" = 'Squat' AND "OwnerId" IS NULL LIMIT 1;
        v_exercise_id := gen_random_uuid();
        INSERT INTO "Exercises" ("Id", "Name", "PrimaryMuscleGroup", "SecondaryMuscleGroups", "ExerciseKind", "EstimatedMet", "ExerciseTemplateId", "PlannedSets", "PlannedReps", "PlannedWeight", "SortOrder", "IsCompleted", "XpAwarded", "StartedAtUtc", "EndedAtUtc", "DurationSeconds", "Notes", "DailyWorkoutId", "CreatedAt", "UpdatedAt")
        VALUES (v_exercise_id, 'Squat', 7, '[9, 8]'::jsonb, 0, 5.0, v_template_id, 3, 5, 125, 1, true, true, '2026-06-01 11:00:00+00', '2026-06-01 11:35:00+00', 2100, NULL, v_day1_id, now(), now());
        INSERT INTO "ExerciseSets" ("Id", "SetNumber", "PlannedReps", "PlannedWeight", "ActualReps", "ActualWeight", "Rpe", "IsCompleted", "CompletedAt", "ExerciseId", "CreatedAt", "UpdatedAt")
        VALUES
            (gen_random_uuid(), 1, 5, 125, 5, 125, 7.5, true, '2026-06-01 11:08:00+00', v_exercise_id, now(), now()),
            (gen_random_uuid(), 2, 5, 125, 5, 125, 8.0, true, '2026-06-01 11:16:00+00', v_exercise_id, now(), now()),
            (gen_random_uuid(), 3, 5, 125, 5, 127.5, 8.5, true, '2026-06-01 11:24:00+00', v_exercise_id, now(), now());

        SELECT "Id" INTO v_template_id FROM "ExerciseTemplates" WHERE "Name" = 'Bench Press' AND "OwnerId" IS NULL LIMIT 1;
        v_exercise_id := gen_random_uuid();
        INSERT INTO "Exercises" ("Id", "Name", "PrimaryMuscleGroup", "SecondaryMuscleGroups", "ExerciseKind", "EstimatedMet", "ExerciseTemplateId", "PlannedSets", "PlannedReps", "PlannedWeight", "SortOrder", "IsCompleted", "XpAwarded", "StartedAtUtc", "EndedAtUtc", "DurationSeconds", "Notes", "DailyWorkoutId", "CreatedAt", "UpdatedAt")
        VALUES (v_exercise_id, 'Bench Press', 0, '[4, 2]'::jsonb, 0, 5.0, v_template_id, 3, 5, 92.5, 2, true, true, '2026-06-04 11:00:00+00', '2026-06-04 11:35:00+00', 2100, 'Last set was slow; useful for AI insight testing.', v_day2_id, now(), now());
        INSERT INTO "ExerciseSets" ("Id", "SetNumber", "PlannedReps", "PlannedWeight", "ActualReps", "ActualWeight", "Rpe", "IsCompleted", "CompletedAt", "ExerciseId", "CreatedAt", "UpdatedAt")
        VALUES
            (gen_random_uuid(), 1, 5, 92.5, 5, 92.5, 8.0, true, '2026-06-04 11:08:00+00', v_exercise_id, now(), now()),
            (gen_random_uuid(), 2, 5, 92.5, 5, 92.5, 8.5, true, '2026-06-04 11:16:00+00', v_exercise_id, now(), now()),
            (gen_random_uuid(), 3, 5, 92.5, 4, 95.0, 9.5, true, '2026-06-04 11:24:00+00', v_exercise_id, now(), now());

        INSERT INTO "WorkoutHistories" ("Id", "UserId", "Date", "CreatedAt", "UpdatedAt")
        VALUES
            (gen_random_uuid(), v_user_id, DATE '2026-06-01', now(), now()),
            (gen_random_uuid(), v_user_id, DATE '2026-06-04', now(), now())
        ON CONFLICT ("UserId", "Date") DO NOTHING;
    END IF;

    DELETE FROM "AiFeatureCaches"
    WHERE "UserId" = v_user_id OR "SubjectUserId" = v_user_id;

    DELETE FROM "UserAnalyses"
    WHERE "UserId" = v_user_id;
END $$;
