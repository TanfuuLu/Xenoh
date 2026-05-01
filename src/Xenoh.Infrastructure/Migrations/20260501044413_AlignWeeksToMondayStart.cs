using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignWeeksToMondayStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    plan_row RECORD;
                    week_index integer;
                    weeks_needed integer;
                    calendar_start date;
                    calendar_end date;
                    week_start date;
                    week_end date;
                    week_id uuid;
                    hash text;
                    day_date date;
                    day_hash text;
                BEGIN
                    FOR plan_row IN SELECT "Id", "StartDate", "EndDate" FROM "Plans" LOOP
                        calendar_start := plan_row."StartDate" - (((EXTRACT(DOW FROM plan_row."StartDate")::integer + 6) % 7));
                        calendar_end := (plan_row."EndDate" - (((EXTRACT(DOW FROM plan_row."EndDate")::integer + 6) % 7))) + 6;
                        weeks_needed := ((calendar_end - calendar_start) / 7) + 1;

                        FOR week_index IN 0..(weeks_needed - 1) LOOP
                            week_start := calendar_start + (week_index * 7);
                            week_end := week_start + 6;

                            SELECT "Id"
                            INTO week_id
                            FROM "WeeklyWorkouts"
                            WHERE "PlanId" = plan_row."Id"
                            ORDER BY "WeekNumber", "StartDate", "CreatedAt"
                            OFFSET week_index
                            LIMIT 1;

                            IF week_id IS NULL THEN
                                hash := md5(plan_row."Id"::text || ':' || week_start::text);
                                week_id := (
                                    substr(hash, 1, 8) || '-' ||
                                    substr(hash, 9, 4) || '-' ||
                                    substr(hash, 13, 4) || '-' ||
                                    substr(hash, 17, 4) || '-' ||
                                    substr(hash, 21, 12)
                                )::uuid;

                                INSERT INTO "WeeklyWorkouts" (
                                    "Id", "WeekNumber", "Name", "StartDate", "EndDate", "PlanId", "CreatedAt", "UpdatedAt"
                                )
                                VALUES (
                                    week_id,
                                    week_index + 1,
                                    'Week ' || (week_index + 1) || ' (' || to_char(week_start, 'DD/MM') || ' - ' || to_char(week_end, 'DD/MM') || ')',
                                    week_start,
                                    week_end,
                                    plan_row."Id",
                                    NOW(),
                                    NOW()
                                );
                            ELSE
                                UPDATE "WeeklyWorkouts"
                                SET
                                    "WeekNumber" = week_index + 1,
                                    "Name" = 'Week ' || (week_index + 1) || ' (' || to_char(week_start, 'DD/MM') || ' - ' || to_char(week_end, 'DD/MM') || ')',
                                    "StartDate" = week_start,
                                    "EndDate" = week_end,
                                    "UpdatedAt" = NOW()
                                WHERE "Id" = week_id;
                            END IF;

                            UPDATE "DailyWorkouts"
                            SET "WeeklyWorkoutId" = week_id,
                                "UpdatedAt" = NOW()
                            WHERE "Id" IN (
                                SELECT d."Id"
                                FROM "DailyWorkouts" d
                                INNER JOIN "WeeklyWorkouts" w ON w."Id" = d."WeeklyWorkoutId"
                                WHERE w."PlanId" = plan_row."Id"
                                  AND d."Date" BETWEEN week_start AND week_end
                            );

                            day_date := week_start;
                            WHILE day_date <= week_end LOOP
                                IF NOT EXISTS (
                                    SELECT 1
                                    FROM "DailyWorkouts" d
                                    INNER JOIN "WeeklyWorkouts" w ON w."Id" = d."WeeklyWorkoutId"
                                    WHERE w."PlanId" = plan_row."Id"
                                      AND d."Date" = day_date
                                ) THEN
                                    day_hash := md5(plan_row."Id"::text || ':day:' || day_date::text);
                                    INSERT INTO "DailyWorkouts" (
                                        "Id", "Date", "DayOfWeek", "IsCompleted", "WeeklyWorkoutId", "CreatedAt", "UpdatedAt", "Status"
                                    )
                                    VALUES (
                                        (
                                            substr(day_hash, 1, 8) || '-' ||
                                            substr(day_hash, 9, 4) || '-' ||
                                            substr(day_hash, 13, 4) || '-' ||
                                            substr(day_hash, 17, 4) || '-' ||
                                            substr(day_hash, 21, 12)
                                        )::uuid,
                                        day_date,
                                        EXTRACT(DOW FROM day_date)::integer,
                                        FALSE,
                                        week_id,
                                        NOW(),
                                        NOW(),
                                        0
                                    );
                                END IF;

                                day_date := day_date + 1;
                            END LOOP;
                        END LOOP;
                    END LOOP;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
