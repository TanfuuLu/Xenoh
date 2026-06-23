-- ============================================================================
-- Xenoh clean seed — exactly three accounts with full website data.
--
--   admin@xenoh.app      / Admin@Xenoh123!   (Admin + Coach + Individual)
--   demo@xenoh.app       / Demo@Xenoh123!    (Individual, FEMALE, cycle data)
--   democoach@xenoh.app  / Coach@Xenoh123!   (Coach + Individual)
--
-- Demo is a client of DemoCoach. DemoCoach has personal training data AND
-- coaching data (active relationship, a coach-authored plan for Demo, chat).
-- Both have community shares and are friends.
--
-- PRECONDITIONS: schema is migrated and the app has seeded roles, system
-- ExerciseTemplates (OwnerId IS NULL) and seed FoodItems. This script only
-- creates the users and their data; it relies on those reference rows existing.
--
-- Re-runnable: it deletes the three seed emails (and their data) first.
-- Password hashes are ASP.NET Core Identity v3 (PBKDF2-HMACSHA256, 100k iters);
-- the verifier reads the parameters from the blob, so they validate as-is.
-- ============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- Enum lookup helpers (int -> string member name) for community share rows.
-- ---------------------------------------------------------------------------
CREATE TEMP TABLE mg_name(id int PRIMARY KEY, name text) ON COMMIT DROP;
INSERT INTO mg_name(id, name) VALUES
  (0,'Chest'),(1,'Back'),(2,'Shoulders'),(3,'Biceps'),(4,'Triceps'),(5,'Forearms'),
  (6,'Abs'),(7,'Quads'),(8,'Hamstrings'),(9,'Glutes'),(10,'Calves'),(11,'FullBody'),
  (12,'Cardio'),(13,'Traps'),(14,'Neck'),(15,'Adductors'),(16,'Abductors');

CREATE TEMP TABLE kind_name(id int PRIMARY KEY, name text) ON COMMIT DROP;
INSERT INTO kind_name(id, name) VALUES (0,'Strength'),(1,'Cardio');

-- ---------------------------------------------------------------------------
-- Exercise prescriptions + weekly schedule (drive the workout generator).
-- focus -> ordered exercises; base/inc are Demo (female) loads, scaled per user.
-- ---------------------------------------------------------------------------
CREATE TEMP TABLE seed_presc(
  focus text, ex_name text, sort int, sets int, reps int, base numeric, inc numeric
) ON COMMIT DROP;
INSERT INTO seed_presc VALUES
  ('Squat Day','Squat',            0,5,3,70.0,1.00),
  ('Squat Day','Bench Press',      1,4,5,42.5,0.50),
  ('Squat Day','Romanian Deadlift',2,3,6,60.0,0.75),
  ('Squat Day','Plank',            3,3,45,NULL,0.00),

  ('Bench Day','Bench Press',      0,5,5,40.0,0.50),
  ('Bench Day','Barbell Row',      1,4,8,40.0,0.50),
  ('Bench Day','Overhead Press',   2,3,6,27.5,0.25),
  ('Bench Day','Tricep Pushdown',  3,3,12,20.0,0.25),

  ('Deadlift Day','Deadlift',        0,4,3,85.0,1.25),
  ('Deadlift Day','Squat',           1,3,5,60.0,0.75),
  ('Deadlift Day','Lat Pulldown',    2,4,10,40.0,0.50),
  ('Deadlift Day','Romanian Deadlift',3,3,8,55.0,0.50),

  ('Upper Day','Bench Press',  0,4,6,37.5,0.50),
  ('Upper Day','Overhead Press',1,3,5,27.5,0.25),
  ('Upper Day','Barbell Row',  2,4,6,42.5,0.50),
  ('Upper Day','Barbell Curl', 3,3,12,17.5,0.25);

-- .NET DayOfWeek (Sun=0..Sat=6), matches Postgres EXTRACT(DOW).
CREATE TEMP TABLE seed_sched(dow int PRIMARY KEY, focus text) ON COMMIT DROP;
INSERT INTO seed_sched VALUES (1,'Squat Day'),(2,'Bench Day'),(4,'Deadlift Day'),(5,'Upper Day');

-- ---------------------------------------------------------------------------
-- Cleanup: remove the three seed accounts and everything they own.
-- ---------------------------------------------------------------------------
DO $cleanup$
DECLARE
  uids uuid[];
BEGIN
  SELECT array_agg("Id") INTO uids FROM "AspNetUsers"
   WHERE "Email" IN ('admin@xenoh.app','demo@xenoh.app','democoach@xenoh.app');
  IF uids IS NULL THEN RETURN; END IF;

  DELETE FROM "TrainingDayShareSets" s USING "TrainingDayShareExercises" e, "TrainingDayShares" sh
    WHERE s."TrainingDayShareExerciseId"=e."Id" AND e."TrainingDayShareId"=sh."Id" AND sh."UserId"=ANY(uids);
  DELETE FROM "TrainingDayShareExercises" e USING "TrainingDayShares" sh
    WHERE e."TrainingDayShareId"=sh."Id" AND sh."UserId"=ANY(uids);
  DELETE FROM "TrainingDayShareLoves" l USING "TrainingDayShares" sh
    WHERE l."TrainingDayShareId"=sh."Id" AND (sh."UserId"=ANY(uids) OR l."UserId"=ANY(uids));
  DELETE FROM "TrainingDayShares" WHERE "UserId"=ANY(uids);

  DELETE FROM "Messages" m USING "CoachClientRelationships" r
    WHERE m."RelationshipId"=r."Id" AND (r."ClientId"=ANY(uids) OR r."CoachId"=ANY(uids));
  DELETE FROM "CoachClientRelationships" WHERE "ClientId"=ANY(uids) OR "CoachId"=ANY(uids);
  DELETE FROM "Friendships" WHERE "UserAId"=ANY(uids) OR "UserBId"=ANY(uids);

  DELETE FROM "ExerciseSets" s USING "Exercises" e, "DailyWorkouts" d, "WeeklyWorkouts" w, "Plans" p
    WHERE s."ExerciseId"=e."Id" AND e."DailyWorkoutId"=d."Id" AND d."WeeklyWorkoutId"=w."Id"
      AND w."PlanId"=p."Id" AND p."OwnerId"=ANY(uids);
  DELETE FROM "Exercises" e USING "DailyWorkouts" d, "WeeklyWorkouts" w, "Plans" p
    WHERE e."DailyWorkoutId"=d."Id" AND d."WeeklyWorkoutId"=w."Id" AND w."PlanId"=p."Id" AND p."OwnerId"=ANY(uids);
  DELETE FROM "DailyWorkouts" d USING "WeeklyWorkouts" w, "Plans" p
    WHERE d."WeeklyWorkoutId"=w."Id" AND w."PlanId"=p."Id" AND p."OwnerId"=ANY(uids);
  DELETE FROM "WeeklyWorkouts" w USING "Plans" p WHERE w."PlanId"=p."Id" AND p."OwnerId"=ANY(uids);
  DELETE FROM "Plans" WHERE "OwnerId"=ANY(uids);

  DELETE FROM "BodyweightLogs" WHERE "UserId"=ANY(uids);
  DELETE FROM "WorkoutHistories" WHERE "UserId"=ANY(uids);
  DELETE FROM "UserExercisePRs" WHERE "UserId"=ANY(uids);
  DELETE FROM "UserExercisePRHistories" WHERE "UserId"=ANY(uids);
  DELETE FROM "NutritionDailyLogs" WHERE "UserId"=ANY(uids);
  DELETE FROM "FoodLogs" WHERE "UserId"=ANY(uids);
  DELETE FROM "CycleDailyLogs" WHERE "UserId"=ANY(uids);
  DELETE FROM "CycleSettings" WHERE "UserId"=ANY(uids);
  DELETE FROM "NutritionProfiles" WHERE "UserId"=ANY(uids);
  DELETE FROM "UserSubscriptions" WHERE "UserId"=ANY(uids);
  DELETE FROM "AspNetUserRoles" WHERE "UserId"=ANY(uids);
  DELETE FROM "AspNetUsers" WHERE "Id"=ANY(uids);
END
$cleanup$;

-- ---------------------------------------------------------------------------
-- Workout-plan generator: weekly/daily/exercises/sets + workout history.
-- Past scheduled days are completed (a few deliberately missed); future days
-- are planned only. Loads scale by p_mult and deload 8% every 4th week.
-- ---------------------------------------------------------------------------
CREATE FUNCTION pg_temp.gen_plan(
  p_owner uuid, p_name text, p_weeks int, p_start date, p_mult numeric,
  p_plantype int, p_created_by_coach uuid, p_is_active boolean, p_today date
) RETURNS void AS $gen$
DECLARE
  v_cutoff date := p_today - 1;
  plan_id uuid := gen_random_uuid();
  weekly_id uuid; day_id uuid; ex_id uuid;
  w int; off int; v_date date; v_dow int; v_focus text;
  presc record; tmpl record;
  v_weight numeric; v_raw numeric; v_started timestamptz; v_ended timestamptz;
  v_is_past boolean; v_is_missed boolean; v_status int; s int; v_rpe numeric; v_areps int;
BEGIN
  INSERT INTO "Plans"("Id","Name","StartDate","EndDate","PlanType","OwnerId","CreatedByCoachId","IsActive","CreatedAt","UpdatedAt")
  VALUES (plan_id, p_name, p_start, p_start + (p_weeks*7-1), p_plantype, p_owner, p_created_by_coach, p_is_active, now(), now());

  FOR w IN 1..p_weeks LOOP
    weekly_id := gen_random_uuid();
    INSERT INTO "WeeklyWorkouts"("Id","WeekNumber","Name","StartDate","EndDate","IsCompleted","PlanId","CreatedAt","UpdatedAt")
    VALUES (weekly_id, w, 'Week '||w, p_start+(w-1)*7, p_start+(w-1)*7+6, (p_start+(w-1)*7+6) <= v_cutoff, plan_id, now(), now());

    FOR off IN 0..6 LOOP
      v_date := p_start + (w-1)*7 + off;
      v_dow := EXTRACT(DOW FROM v_date)::int;
      SELECT focus INTO v_focus FROM seed_sched WHERE dow = v_dow;

      IF v_focus IS NULL THEN
        INSERT INTO "DailyWorkouts"("Id","Date","DayOfWeek","IsCompleted","Status","WeeklyWorkoutId","CreatedAt","UpdatedAt")
        VALUES (gen_random_uuid(), v_date, v_dow, false, 1, weekly_id, now(), now());  -- Rest
        CONTINUE;
      END IF;

      v_is_past := v_date <= v_cutoff;
      v_is_missed := v_is_past AND ((w=3 AND v_dow=2) OR (w=6 AND v_dow=4) OR (w=9 AND v_dow=1));
      v_status := CASE WHEN v_is_missed THEN 2 ELSE 0 END;  -- Missed / Normal
      day_id := gen_random_uuid();
      INSERT INTO "DailyWorkouts"("Id","Date","DayOfWeek","IsCompleted","Status","WeeklyWorkoutId","CreatedAt","UpdatedAt")
      VALUES (day_id, v_date, v_dow, (v_is_past AND NOT v_is_missed), v_status, weekly_id, now(), now());

      IF v_is_past AND NOT v_is_missed THEN
        INSERT INTO "WorkoutHistories"("Id","UserId","Date","CreatedAt","UpdatedAt")
        VALUES (gen_random_uuid(), p_owner, v_date, now(), now());
      END IF;

      FOR presc IN SELECT * FROM seed_presc WHERE focus = v_focus ORDER BY sort LOOP
        SELECT "Id","PrimaryMuscleGroup","SecondaryMuscleGroups","ExerciseKind","EstimatedMet"
          INTO tmpl FROM "ExerciseTemplates" WHERE "Name"=presc.ex_name AND "OwnerId" IS NULL LIMIT 1;
        IF tmpl."Id" IS NULL THEN CONTINUE; END IF;

        IF presc.base IS NULL THEN
          v_weight := NULL;
        ELSE
          v_raw := presc.base*p_mult + presc.inc*p_mult*(w-1);
          IF w % 4 = 0 THEN v_raw := v_raw*0.92; END IF;
          v_weight := round(v_raw*2)/2;
        END IF;

        ex_id := gen_random_uuid();
        IF v_is_past AND NOT v_is_missed THEN
          v_started := (v_date + interval '18 hours 15 minutes' + (presc.sort * interval '17 minutes'));
          v_ended   := v_started + (CASE WHEN presc.ex_name IN ('Squat','Bench Press','Deadlift') THEN interval '16 minutes' ELSE interval '12 minutes' END);
          INSERT INTO "Exercises"("Id","Name","PrimaryMuscleGroup","SecondaryMuscleGroups","ExerciseKind","EstimatedMet",
            "ExerciseTemplateId","PlannedSets","PlannedReps","PlannedWeight","SortOrder","IsCompleted","StartedAtUtc","EndedAtUtc",
            "DurationSeconds","Notes","DailyWorkoutId","CreatedAt","UpdatedAt","XpAwarded","IsSkipped")
          VALUES (ex_id, presc.ex_name, tmpl."PrimaryMuscleGroup", tmpl."SecondaryMuscleGroups", tmpl."ExerciseKind", tmpl."EstimatedMet",
            tmpl."Id", presc.sets, presc.reps, v_weight, presc.sort, true, v_started, v_ended,
            EXTRACT(EPOCH FROM (v_ended - v_started))::int, CASE WHEN presc.sort=0 THEN v_focus||' main work' ELSE NULL END,
            day_id, now(), now(), true, false);

          FOR s IN 1..presc.sets LOOP
            v_rpe := LEAST(9.0, round((6.4 + (s-1)*0.3 + (CASE WHEN presc.ex_name IN ('Squat','Bench Press','Deadlift') THEN 0.3 ELSE 0 END))::numeric, 1));
            v_areps := CASE WHEN s=presc.sets AND presc.ex_name='Deadlift' AND v_date >= p_today-14 THEN GREATEST(1, presc.reps-1) ELSE presc.reps END;
            INSERT INTO "ExerciseSets"("Id","SetNumber","PlannedReps","PlannedWeight","ActualReps","ActualWeight","Rpe","IsCompleted","CompletedAt","ExerciseId","CreatedAt","UpdatedAt")
            VALUES (gen_random_uuid(), s, presc.reps, v_weight, v_areps, v_weight, v_rpe, true, v_started + (s*interval '3 minutes'), ex_id, now(), now());
          END LOOP;
        ELSE
          INSERT INTO "Exercises"("Id","Name","PrimaryMuscleGroup","SecondaryMuscleGroups","ExerciseKind","EstimatedMet",
            "ExerciseTemplateId","PlannedSets","PlannedReps","PlannedWeight","SortOrder","IsCompleted","DailyWorkoutId","CreatedAt","UpdatedAt","XpAwarded","IsSkipped")
          VALUES (ex_id, presc.ex_name, tmpl."PrimaryMuscleGroup", tmpl."SecondaryMuscleGroups", tmpl."ExerciseKind", tmpl."EstimatedMet",
            tmpl."Id", presc.sets, presc.reps, v_weight, presc.sort, false, day_id, now(), now(), false, false);

          FOR s IN 1..presc.sets LOOP
            INSERT INTO "ExerciseSets"("Id","SetNumber","PlannedReps","PlannedWeight","IsCompleted","ExerciseId","CreatedAt","UpdatedAt")
            VALUES (gen_random_uuid(), s, presc.reps, v_weight, false, ex_id, now(), now());
          END LOOP;
        END IF;
      END LOOP;
    END LOOP;
  END LOOP;
END
$gen$ LANGUAGE plpgsql;

-- ---------------------------------------------------------------------------
-- Community share generator: snapshot the N most recent completed days.
-- ---------------------------------------------------------------------------
CREATE FUNCTION pg_temp.gen_shares(p_user uuid, p_lover uuid, p_limit int) RETURNS void AS $sh$
DECLARE
  d record; share_id uuid;
BEGIN
  FOR d IN
    SELECT dw."Id" AS day_id, dw."Date" AS dt, dw."DayOfWeek" AS dow
      FROM "DailyWorkouts" dw
      JOIN "WeeklyWorkouts" ww ON ww."Id"=dw."WeeklyWorkoutId"
      JOIN "Plans" p ON p."Id"=ww."PlanId"
     WHERE p."OwnerId"=p_user AND dw."IsCompleted"=true
     ORDER BY dw."Date" DESC LIMIT p_limit
  LOOP
    share_id := gen_random_uuid();
    INSERT INTO "TrainingDayShares"("Id","UserId","SourceDailyWorkoutId","WorkoutDate","DayOfWeek","DayStatus",
      "ExerciseCount","CompletedSets","TotalVolume","TotalDurationSeconds","AverageRpe","HasPersonalRecord","Caption","CreatedAt","UpdatedAt")
    SELECT share_id, p_user, d.day_id, d.dt, d.dow, 'Normal',
           count(DISTINCT e."Id"),
           count(s.*) FILTER (WHERE s."IsCompleted"),
           COALESCE(sum(s."ActualReps"*s."ActualWeight"),0),
           COALESCE(max(ed.dur),0),
           round(avg(s."Rpe"),1), false, 'Solid session today 💪', now(), now()
      FROM "Exercises" e
      LEFT JOIN "ExerciseSets" s ON s."ExerciseId"=e."Id"
      LEFT JOIN (SELECT "DailyWorkoutId", sum("DurationSeconds") dur FROM "Exercises" GROUP BY "DailyWorkoutId") ed ON ed."DailyWorkoutId"=e."DailyWorkoutId"
     WHERE e."DailyWorkoutId"=d.day_id;

    INSERT INTO "TrainingDayShareExercises"("Id","TrainingDayShareId","Name","PrimaryMuscleGroup","ExerciseKind","SortOrder","IsSkipped","DurationSeconds","Notes","IsPersonalRecord","CreatedAt","UpdatedAt")
    SELECT gen_random_uuid(), share_id, e."Name", mg.name, kn.name, e."SortOrder", e."IsSkipped", e."DurationSeconds", e."Notes", false, now(), now()
      FROM "Exercises" e
      JOIN mg_name mg ON mg.id=e."PrimaryMuscleGroup"
      JOIN kind_name kn ON kn.id=e."ExerciseKind"
     WHERE e."DailyWorkoutId"=d.day_id;

    INSERT INTO "TrainingDayShareSets"("Id","TrainingDayShareExerciseId","SetNumber","ActualReps","ActualWeight","Rpe","IsCompleted","CreatedAt","UpdatedAt")
    SELECT gen_random_uuid(), tse."Id", s."SetNumber", s."ActualReps", s."ActualWeight", s."Rpe", s."IsCompleted", now(), now()
      FROM "TrainingDayShareExercises" tse
      JOIN "Exercises" e ON e."DailyWorkoutId"=d.day_id AND e."Name"=tse."Name" AND e."SortOrder"=tse."SortOrder"
      JOIN "ExerciseSets" s ON s."ExerciseId"=e."Id"
     WHERE tse."TrainingDayShareId"=share_id;

    IF p_lover IS NOT NULL THEN
      INSERT INTO "TrainingDayShareLoves"("Id","TrainingDayShareId","UserId","CreatedAt","UpdatedAt")
      VALUES (gen_random_uuid(), share_id, p_lover, now(), now());
    END IF;
  END LOOP;
END
$sh$ LANGUAGE plpgsql;

-- ===========================================================================
-- Main seed
-- ===========================================================================
DO $main$
DECLARE
  today date := CURRENT_DATE;
  monday date := date_trunc('week', CURRENT_DATE)::date;  -- ISO Monday
  role_admin uuid; role_coach uuid; role_indiv uuid;
  admin_id uuid := gen_random_uuid();
  demo_id  uuid := gen_random_uuid();
  coach_id uuid := gen_random_uuid();
  rel_id uuid := gen_random_uuid();
  i int; d date; ps date; v_flow int; v_sym int; v_mood int; v_en int;
BEGIN
  SELECT "Id" INTO role_admin FROM "AspNetRoles" WHERE "Name"='Admin';
  SELECT "Id" INTO role_coach FROM "AspNetRoles" WHERE "Name"='Coach';
  SELECT "Id" INTO role_indiv FROM "AspNetRoles" WHERE "Name"='Individual';
  IF role_admin IS NULL OR role_coach IS NULL OR role_indiv IS NULL THEN
    RAISE EXCEPTION 'Roles not seeded — run the API once to seed roles/templates/foods first.';
  END IF;

  -- ----- Users -----
  INSERT INTO "AspNetUsers"("Id","FirstName","LastName","CreatedAt","Height","Gender","DateOfBirth","Bio","TotalXp","Level",
    "UserName","NormalizedUserName","Email","NormalizedEmail","EmailConfirmed","PasswordHash","SecurityStamp","ConcurrencyStamp",
    "PhoneNumberConfirmed","TwoFactorEnabled","LockoutEnabled","AccessFailedCount","PreferredLanguage","PreferredTheme","PreferredWeightUnit",
    "DevelopmentDirection","TrainingDiscipline")
  VALUES
    (admin_id,'Admin','Xenoh', now()-interval '200 days', 180, 0, DATE '1990-01-01', 'Platform administrator.', 0, 1,
     'admin@xenoh.app','ADMIN@XENOH.APP','admin@xenoh.app','ADMIN@XENOH.APP', true,
     'AQAAAAEAAYagAAAAEMQY77YM/lSNEiFl/5inQAd6ES4jA2OmpF42NLn0XDnG1z1DIs2+nS809xuoeHUwzA==',
     gen_random_uuid()::text, gen_random_uuid()::text, false,false,true,0,'en','dark','kg', 5, 6),

    (demo_id,'Demo','Athlete', now()-interval '180 days', 165, 1, DATE '1998-07-22', 'Female powerlifter chasing a 1.5x bodyweight squat.', 9200, 11,
     'demo@xenoh.app','DEMO@XENOH.APP','demo@xenoh.app','DEMO@XENOH.APP', true,
     'AQAAAAEAAYagAAAAEKyfXo5Tyza0170lK7wMG/u0ewa/Y5NDTi/S6EiXbhaRUVi4dkxz5HH61q4UkPeYUQ==',
     gen_random_uuid()::text, gen_random_uuid()::text, false,false,true,0,'en','dark','kg', 0, 0),

    (coach_id,'Demo','Coach', now()-interval '220 days', 178, 0, DATE '1992-03-10', 'Strength coach. Powerlifting + general strength programming.', 22000, 22,
     'democoach@xenoh.app','DEMOCOACH@XENOH.APP','democoach@xenoh.app','DEMOCOACH@XENOH.APP', true,
     'AQAAAAEAAYagAAAAEE46oWvYcVePA2s36LZ3WUdB/+trKMXG8SPROy64kXQUhBgYWwV8M1Mu3eMEjjAevA==',
     gen_random_uuid()::text, gen_random_uuid()::text, false,false,true,0,'en','dark','kg', 0, 0);

  INSERT INTO "AspNetUserRoles"("UserId","RoleId") VALUES
    (admin_id, role_admin),(admin_id, role_coach),(admin_id, role_indiv),
    (demo_id, role_indiv),
    (coach_id, role_coach),(coach_id, role_indiv);

  -- ----- Subscriptions -----
  INSERT INTO "UserSubscriptions"("Id","UserId","Tier","ExpiresAt","CreatedAt","UpdatedAt") VALUES
    (gen_random_uuid(), admin_id,'ProCoach', TIMESTAMPTZ '9999-12-31 23:59:59+00', now(), now()),
    (gen_random_uuid(), demo_id, 'ProIndividual', now()+interval '1 year', now(), now()),
    (gen_random_uuid(), coach_id,'ProCoach', now()+interval '1 year', now(), now());

  -- ----- Plans / workouts -----
  PERFORM pg_temp.gen_plan(demo_id, '16-Week Strength Block', 16, monday - 70, 1.00, 0, NULL, true, today);     -- Demo self (active)
  PERFORM pg_temp.gen_plan(coach_id,'12-Week Peak Block',     12, monday - 49, 1.55, 0, NULL, true, today);     -- Coach self (active)
  PERFORM pg_temp.gen_plan(demo_id, 'Coach Plan — Squat Focus', 8, monday + 7, 1.05, 1, coach_id, false, today); -- Coach-authored for Demo (newly assigned, future)

  -- ----- Bodyweight (weekly) -----
  i := 0; d := monday - 70;
  WHILE d <= today LOOP
    INSERT INTO "BodyweightLogs"("Id","UserId","Weight","Date","CreatedAt","UpdatedAt")
    VALUES (gen_random_uuid(), demo_id, round((63.5 - i*0.04 + (CASE WHEN i%3=0 THEN 0.2 ELSE 0 END))::numeric,1), d, now(), now());
    INSERT INTO "BodyweightLogs"("Id","UserId","Weight","Date","CreatedAt","UpdatedAt")
    VALUES (gen_random_uuid(), coach_id, round((84.0 + i*0.03 + (CASE WHEN i%2=0 THEN 0.15 ELSE 0 END))::numeric,1), d, now(), now());
    d := d + 7; i := i + 1;
  END LOOP;

  -- ----- Personal records (current + history) -----
  INSERT INTO "UserExercisePRHistories"("Id","UserId","ExerciseTemplateId","Weight","Reps","AchievedAt","CreatedAt","UpdatedAt")
  SELECT gen_random_uuid(), v.uid, t."Id", v.wt, v.reps, v.at, now(), now()
  FROM (VALUES
    (demo_id,'Squat',82.5,3, now()-interval '70 days'),(demo_id,'Squat',90.0,3, now()-interval '20 days'),
    (demo_id,'Bench Press',47.5,3, now()-interval '60 days'),(demo_id,'Bench Press',55.0,2, now()-interval '15 days'),
    (demo_id,'Deadlift',100.0,3, now()-interval '55 days'),(demo_id,'Deadlift',112.5,2, now()-interval '10 days'),
    (demo_id,'Overhead Press',35.0,3, now()-interval '25 days'),
    (coach_id,'Squat',170.0,2, now()-interval '60 days'),(coach_id,'Squat',182.5,1, now()-interval '12 days'),
    (coach_id,'Bench Press',122.5,2, now()-interval '40 days'),(coach_id,'Bench Press',132.5,1, now()-interval '9 days'),
    (coach_id,'Deadlift',210.0,1, now()-interval '45 days'),(coach_id,'Deadlift',222.5,1, now()-interval '7 days'),
    (coach_id,'Overhead Press',80.0,2, now()-interval '20 days')
  ) AS v(uid, name, wt, reps, at)
  JOIN "ExerciseTemplates" t ON t."Name"=v.name AND t."OwnerId" IS NULL;

  INSERT INTO "UserExercisePRs"("Id","UserId","ExerciseTemplateId","Weight","Reps","AchievedAt","CreatedAt","UpdatedAt")
  SELECT gen_random_uuid(), h."UserId", h."ExerciseTemplateId", h."Weight", h."Reps", h."AchievedAt", now(), now()
  FROM "UserExercisePRHistories" h
  JOIN (SELECT "UserId","ExerciseTemplateId", max("AchievedAt") mx FROM "UserExercisePRHistories"
        WHERE "UserId" IN (demo_id, coach_id) GROUP BY "UserId","ExerciseTemplateId") last
    ON last."UserId"=h."UserId" AND last."ExerciseTemplateId"=h."ExerciseTemplateId" AND last.mx=h."AchievedAt"
  WHERE h."UserId" IN (demo_id, coach_id);

  -- ----- Nutrition -----
  INSERT INTO "NutritionProfiles"("Id","UserId","ActivityLevel","Goal","TargetWeightKg","CustomCalorieTarget","ProteinPerKg","FatPerKg","CreatedAt","UpdatedAt") VALUES
    (gen_random_uuid(), demo_id, 4, 1, 62.0, 2250, 1.9, 0.9, now(), now()),
    (gen_random_uuid(), coach_id,4, 2, 86.0, 3200, 2.0, 0.9, now(), now());

  FOR i IN 0..83 LOOP
    d := today - 83 + i;
    INSERT INTO "NutritionDailyLogs"("Id","UserId","Date","Calories","ProteinG","CarbsG","FatG","Notes","CreatedAt","UpdatedAt")
    VALUES (gen_random_uuid(), demo_id, d, 2230 + (i%5-2)*40, 120 + (i%4)*3, 235 + (i%6)*6, round(((2230 + (i%5-2)*40) - (120+(i%4)*3)*4 - (235+(i%6)*6)*4)/9.0,1),
            CASE WHEN EXTRACT(DOW FROM d)::int IN (1,2,4,5) THEN 'Training day' ELSE 'Rest day' END, now(), now());
    INSERT INTO "NutritionDailyLogs"("Id","UserId","Date","Calories","ProteinG","CarbsG","FatG","Notes","CreatedAt","UpdatedAt")
    VALUES (gen_random_uuid(), coach_id, d, 3180 + (i%5-2)*55, 170 + (i%4)*4, 360 + (i%6)*8, round(((3180 + (i%5-2)*55) - (170+(i%4)*4)*4 - (360+(i%6)*8)*4)/9.0,1),
            CASE WHEN EXTRACT(DOW FROM d)::int IN (1,2,4,5) THEN 'Training day' ELSE 'Rest day' END, now(), now());
  END LOOP;

  -- ----- Cycle tracking (Demo, female) -----
  INSERT INTO "CycleSettings"("Id","UserId","AverageCycleLengthOverride","AveragePeriodLengthOverride","ShareWithCoach","CreatedAt","UpdatedAt")
  VALUES (gen_random_uuid(), demo_id, NULL, NULL, true, now(), now());

  -- three ~28-day cycles, latest period starting 4 days ago
  FOREACH ps IN ARRAY ARRAY[today-4, today-32, today-60] LOOP
    -- two premenstrual days before period
    FOR i IN 1..2 LOOP
      d := ps - i;
      INSERT INTO "CycleDailyLogs"("Id","UserId","Date","Flow","Symptoms","Mood","EnergyLevel","Notes","CreatedAt","UpdatedAt")
      VALUES (gen_random_uuid(), demo_id, d, NULL, 1284, 5, 3, 'PMS', now(), now());  -- Cramps? no: MoodSwings|Cravings|Bloating
    END LOOP;
    -- five period days
    FOR i IN 0..4 LOOP
      d := ps + i;
      IF d > today THEN CONTINUE; END IF;
      v_flow := CASE WHEN i<2 THEN 4 WHEN i=2 THEN 3 ELSE 2 END;             -- Heavy/Medium/Light
      v_sym  := CASE WHEN i<2 THEN 49 WHEN i=2 THEN 20 ELSE 16 END;          -- Cramps|Fatigue|BackPain / Bloating|Fatigue / Fatigue
      v_mood := CASE WHEN i<2 THEN 4 WHEN i=2 THEN 3 ELSE 2 END;             -- Low/Neutral/Good
      v_en   := CASE WHEN i<2 THEN 2 WHEN i=2 THEN 3 ELSE 4 END;
      INSERT INTO "CycleDailyLogs"("Id","UserId","Date","Flow","Symptoms","Mood","EnergyLevel","Notes","CreatedAt","UpdatedAt")
      VALUES (gen_random_uuid(), demo_id, d, v_flow, v_sym, v_mood, v_en, CASE WHEN i=0 THEN 'Period start' ELSE NULL END, now(), now());
    END LOOP;
  END LOOP;

  -- ----- Coach <-> client relationship + chat -----
  INSERT INTO "CoachClientRelationships"("Id","ClientId","CoachId","Status","StartDate","EndDate","CreatedAt","UpdatedAt")
  VALUES (rel_id, demo_id, coach_id, 1, today-30, NULL, now(), now());  -- Active

  INSERT INTO "Messages"("Id","RelationshipId","SenderId","Content","IsRead","Kind","CreatedAt","UpdatedAt") VALUES
    (gen_random_uuid(), rel_id, coach_id, 'Welcome aboard! I''ve set up your squat-focus block — let''s build that base.', true, 'User', now()-interval '30 days', now()-interval '30 days'),
    (gen_random_uuid(), rel_id, demo_id,  'Thank you! Excited to start. My knees felt great this week.', true, 'User', now()-interval '29 days', now()-interval '29 days'),
    (gen_random_uuid(), rel_id, coach_id, 'Great. Keep RPE at 7-8 on the top sets and log everything.', true, 'User', now()-interval '21 days', now()-interval '21 days'),
    (gen_random_uuid(), rel_id, demo_id,  'Hit a 90kg squat triple today 🎉', false, 'User', now()-interval '2 days', now()-interval '2 days'),
    (gen_random_uuid(), rel_id, coach_id, 'Huge! New PR. Deload next week then we test a single.', false, 'User', now()-interval '1 day', now()-interval '1 day');

  -- ----- Friendship -----
  INSERT INTO "Friendships"("Id","UserAId","UserBId","RequesterId","AddresseeId","Status","RespondedAt","CreatedAt","UpdatedAt")
  SELECT gen_random_uuid(),
         LEAST(demo_id, coach_id), GREATEST(demo_id, coach_id),
         coach_id, demo_id, 'Accepted', now()-interval '25 days', now()-interval '26 days', now();

  -- ----- Community shares -----
  PERFORM pg_temp.gen_shares(demo_id, coach_id, 3);
  PERFORM pg_temp.gen_shares(coach_id, demo_id, 2);
END
$main$;

COMMIT;

-- Quick verification (run separately if desired):
--   SELECT "Email", (SELECT string_agg(r."Name",', ') FROM "AspNetUserRoles" ur JOIN "AspNetRoles" r ON r."Id"=ur."RoleId" WHERE ur."UserId"=u."Id")
--   FROM "AspNetUsers" u ORDER BY "Email";
