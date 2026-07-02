# Xenoh Backend — API Endpoints

Full reference of all REST API endpoints exposed by `Xenoh.API`, including **concrete JSON response bodies** for every endpoint (not just type names) — written for consumers who don't have access to the C# source.

- **Base URL (dev):** `http://localhost:5293`
- **Base route prefix:** `/api/`
- **Architecture:** Vertical Slice + Mediator (CQRS) — each endpoint dispatches a Command/Query to a handler in `Xenoh.Application`.
- **Auth:** JWT Bearer tokens (`Authorization: Bearer <token>`) issued by `AuthController`. Refresh token is delivered via an httpOnly cookie, not in the JSON body.
- **Content type:** all requests/responses are `application/json` unless noted (file uploads use `multipart/form-data`, the share-image endpoint returns `image/png`).

> Generated 2026-07-01. Field types are read from the C# DTOs at that point in time — verify against current code before treating as a binding contract; request/response shapes evolve.

### Type notation used below

`string`, `int`, `long`, `decimal`, `bool`, `guid` (string, UUID format), `date` (`"YYYY-MM-DD"`, C# `DateOnly`), `datetime` (ISO-8601 UTC, C# `DateTime`). A trailing `?` on a field name means the value may be `null`. Enum fields show their possible string values inline as a comment.

---

## Table of Contents

1. [Authorization Policies & Rate Limits](#authorization-policies--rate-limits)
2. [Common Wrapper Types](#common-wrapper-types)
3. [Auth](#1-auth) · [Users](#2-users) · [Plans](#3-plans) · [Weekly Workouts](#4-weekly-workouts) · [Daily Workouts](#5-daily-workouts) · [Exercises](#6-exercises) · [Exercise Templates](#7-exercise-templates)
4. [Coach ↔ Client](#8-coach--client) · [Nutrition](#9-nutrition) · [Cycle Tracking](#10-cycle-tracking) · [Insights (AI)](#11-insights-ai) · [Messages](#12-messages)
5. [Community](#13-community) · [Friends](#14-friends) · [Training Day Shares](#15-training-day-shares) · [Comments](#16-comments) · [Leaderboard](#17-leaderboard)
6. [Notifications](#18-notifications) · [Blocks](#19-blocks) · [Dashboard](#20-dashboard) · [Files](#21-files) · [Subscriptions & Payments](#22-subscriptions--payments)
7. [Share (public)](#23-share-public) · [Analytics](#24-analytics) · [Bug Reports](#25-bug-reports) · [Admin](#26-admin)

---

## Authorization Policies & Rate Limits

| Policy / Attribute | Meaning |
|---|---|
| `AllowAnonymous` | No authentication required |
| `Authorize` (default) | Any authenticated user (valid JWT) |
| `RequirePro` | Authenticated + active Pro-tier subscription |
| `RequireProCoach` | Authenticated + Pro subscription + Coach role |
| `Roles = Individual / Coach` | Authenticated + specific role claim |
| `Roles = Admin` | Authenticated + Admin role |

| Rate limit policy | Applied to |
|---|---|
| `Auth` | login, register, forgot-password |
| `RefreshToken` | `/api/auth/refresh-token` |
| `ExternalAuth` | Google/Facebook OAuth endpoints |
| `Ai` | All AI-powered endpoints |
| `Webhook` | SePay payment webhook |
| `PublicShare` | Public PR share image endpoint |

**Common status codes:** `200 OK`, `204 No Content`, `400 Bad Request` (validation), `403 Forbidden` (authz), `404 Not Found`, `302 Redirect` (OAuth / share image), `503 Service Unavailable` (payment gateway down).

---

## Common Wrapper Types

Many list endpoints return this pagination wrapper instead of a bare array:

```json
{
  "items": [ /* T[] */ ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 42,
  "hasMore": true
}
```

---

## 1. Auth
**Route prefix:** `api/auth`

### POST `/api/auth/register` — AllowAnonymous *(RateLimit: Auth)*
Request:
```json
{ "email": "string", "password": "string", "firstName": "string", "lastName": "string", "role": "Individual | Coach", "gender": "Male | Female", "dateOfBirth": "date" }
```
Response `200`:
```json
{ "userId": "guid", "email": "string" }
```

### POST `/api/auth/login` — AllowAnonymous *(RateLimit: Auth)*
Request:
```json
{ "email": "string", "password": "string" }
```
Response `200` (sets refresh token as httpOnly cookie; access token in body):
```json
{
  "userId": "guid",
  "accessToken": "string",
  "email": "string",
  "fullName": "string",
  "avatarUrl": "string?",
  "roles": ["Individual"]
}
```

### POST `/api/auth/refresh-token` — AllowAnonymous *(RateLimit: RefreshToken)*
Request: none (refresh token read from cookie). Response `200`: same `AuthResponse` shape as login.

### POST `/api/auth/logout` — Authorize
Request: none. Response: `204`.

### POST `/api/auth/change-password` — Authorize
Request:
```json
{ "currentPassword": "string", "newPassword": "string" }
```
Response: `204`.

### POST `/api/auth/forgot-password/send-code` — AllowAnonymous *(RateLimit: Auth)*
Request: `{ "email": "string" }` · Response: `204`.

### POST `/api/auth/forgot-password/reset` — AllowAnonymous
Request: `{ "email": "string", "code": "string", "newPassword": "string" }` · Response: `204`.

### GET `/api/auth/external/{provider}` — AllowAnonymous *(RateLimit: ExternalAuth)*
Route: `provider` = `google` | `facebook`. Response: `302` redirect to the provider's OAuth consent screen.

### POST `/api/auth/external/exchange` — AllowAnonymous
Request: `{ "ticket": "string" }` (opaque ticket from the OAuth callback). Response `200`: `AuthResponse` (same shape as login).

### POST `/api/auth/external/complete-registration` — Authorize
Request: profile completion fields (role, gender, DOB, etc.). Response `200`: `AuthResponse`.

---

## 2. Users
**Route prefix:** `api/users` · Auth: `Authorize` unless noted

### GET `/api/users/me`
Response `200`:
```json
{
  "id": "guid",
  "email": "string",
  "firstName": "string",
  "lastName": "string",
  "avatarUrl": "string?",
  "bio": "string?",
  "height": 175.5,
  "gender": "Male | Female | null",
  "dateOfBirth": "date?",
  "developmentDirection": "string?",
  "trainingDiscipline": "string?",
  "currentStreak": 5,
  "latestBodyweight": 80.2,
  "bmi": 24.1,
  "bmiCategory": "string?",
  "dotsScore": 350.5,
  "big3Prs": { "squat": 150.0, "bench": 100.0, "deadlift": 180.0 },
  "level": 3,
  "totalXp": 4200,
  "xpToNextLevel": 800,
  "title": "string",
  "facebookUrl": "string?",
  "instagramUrl": "string?",
  "zaloUrl": "string?"
}
```

### PUT `/api/users/me`
Request (all optional, partial update):
```json
{
  "firstName": "string?", "lastName": "string?", "bio": "string?",
  "height": 175.5, "gender": "Male | Female", "dateOfBirth": "date?",
  "developmentDirection": "string?", "trainingDiscipline": "string?",
  "facebookUrl": "string?", "instagramUrl": "string?", "zaloUrl": "string?"
}
```
Response `200`: same shape as `GET /api/users/me`.

### GET `/api/users/me/preferences`
Response `200`: `{ "language": "string", "theme": "string", "weightUnit": "string" }`

### PUT `/api/users/me/preferences`
Request: `{ "language": "string?", "theme": "string?", "weightUnit": "string?" }` · Response `200`: same shape as GET.

### POST `/api/users/me/avatar`
Request: `multipart/form-data`, field `file` (image, ≤5MB). Response `200`: same shape as `GET /api/users/me`.

### POST `/api/users/me/bodyweight`
Request: `{ "weight": 80.2 }` (20–500 kg) · Response `200`:
```json
{ "id": "guid", "weight": 80.2, "date": "date" }
```

### GET `/api/users/me/bodyweight`
Response `200`: `[ { "id": "guid", "weight": 80.2, "date": "date" }, ... ]`

### DELETE `/api/users/me/bodyweight/{id:guid}`
Response: `204`.

### GET `/api/users/{userId:guid}/bodyweight`
Same array shape as `GET /api/users/me/bodyweight`. `403` if no access.

### GET `/api/users/me/exercise-prs`
Response `200`:
```json
[ { "exerciseTemplateId": "guid", "exerciseName": "string", "currentWeight": 100.0, "reps": 5, "achievedAt": "datetime" } ]
```

### GET `/api/users/me/exercise-prs/{exerciseTemplateId:guid}/history`
Response `200`:
```json
[ { "exerciseTemplateId": "guid", "weight": 95.0, "reps": 5, "achievedAt": "datetime" } ]
```

### GET `/api/users/me/training-activity?year=&month=`
Response `200`:
```json
{
  "totalDurationSeconds": 36000,
  "totalWeightTrainedKg": 15400.5,
  "accountCreatedAt": "datetime",
  "year": 2026,
  "month": 7,
  "trainedDates": ["date", "date"]
}
```

### GET `/api/users/me/volume-history?months=6`
Response `200`: `[ { "year": 2026, "month": 6, "volumeKg": 12000.5 }, ... ]`

### GET `/api/users/{userId:guid}/public`
Response `200`:
```json
{
  "id": "guid", "fullName": "string", "email": "string", "avatarUrl": "string?",
  "bio": "string?", "gender": "Male | Female | null", "height": 175.5,
  "latestBodyweight": 80.2, "bmi": 24.1, "bmiCategory": "string?",
  "currentStreak": 5, "dotsScore": 350.5
}
```

### GET `/api/users/{userId:guid}`
Response `200`: full `UserProfileResponse` (same shape as `GET /api/users/me`). `403` if no active coach-client relationship with that user.

### POST `/api/users/{userId:guid}/reports`
Request:
```json
{ "reportedUserId": "guid", "reason": "Spam | Harassment | Inappropriate | Other", "details": "string" }
```
Response `200`:
```json
{
  "id": "guid", "reporterId": "guid", "reporterName": "string",
  "reportedUserId": "guid", "reportedUserName": "string", "reportedUserEmail": "string",
  "reason": "string", "details": "string", "status": "Open | InProgress | Resolved | Dismissed",
  "adminNote": "string?", "reviewedById": "guid?", "reviewedByName": "string?",
  "reviewedAtUtc": "datetime?", "createdAt": "datetime"
}
```

---

## 3. Plans
**Route prefix:** `api/plans` · Auth: `Authorize` unless noted

**PlanResponse shape** (used by create/get/update/duplicate/activate/deactivate/AI-starter):
```json
{
  "id": "guid", "name": "string", "startDate": "date", "endDate": "date",
  "planType": "Self | Coach", "ownerId": "guid", "ownerName": "string",
  "createdByCoachId": "guid?", "coachName": "string?",
  "totalWeeks": 8, "completedWeeks": 2, "totalDays": 56, "completedDays": 14,
  "isActive": true, "createdAt": "datetime"
}
```

| Method | Route | Request | Response |
|---|---|---|---|
| GET | `/api/plans?pageNumber=&pageSize=` | — | `PagedResponse<PlanResponse>` |
| GET | `/api/plans/{planId:guid}` | — | `PlanResponse` |
| GET | `/api/plans/{planId:guid}/export` | — | XLSX file stream |
| POST | `/api/plans` | `{ "name": "string", "startDate": "date", "endDate": "date" }` | `PlanResponse` |
| POST | `/api/plans/starter-ai` *(RequirePro, RateLimit: Ai)* | AI-starter params | `PlanResponse` |
| GET | `/api/plans/coach-overview` *(RequireProCoach)* | Query `pageNumber, pageSize` | `PagedResponse<CoachPlanResponse>` (see below) |
| POST | `/api/plans/for-user` *(RequireProCoach)* | create fields + `userId` | `PlanResponse` |
| PUT | `/api/plans/{planId:guid}` | `{ "name": "string", "startDate": "date", "endDate": "date" }` | `PlanResponse` |
| DELETE | `/api/plans/{planId:guid}` | — | `204` |
| PATCH | `/api/plans/{planId:guid}/activate` | — | `PlanResponse` |
| PATCH | `/api/plans/{planId:guid}/deactivate` | — | `PlanResponse` |
| POST | `/api/plans/{planId:guid}/duplicate` | `{ "newStartDate": "date", "newEndDate": "date" }` | `PlanResponse` |

**CoachPlanResponse** (coach-overview items):
```json
{
  "id": "guid", "name": "string", "startDate": "date", "endDate": "date",
  "planType": "Self | Coach", "ownerId": "guid", "ownerName": "string",
  "ownerEmail": "string", "totalWeeks": 8, "createdAt": "datetime"
}
```

### GET `/api/plans/{planId:guid}/analytics` *(RequirePro)*
Response `200`:
```json
{
  "totalWorkoutsCompleted": 20, "totalVolume": 45000.0, "consistencyPercent": 85.5,
  "avgSessionsPerWeek": 4.2, "completedSets": 320, "avgRpe": 7.5, "highRpeSets": 12,
  "warningDays": 1, "totalDurationSeconds": 72000, "trainingScore": 82,
  "insights": [ { "type": "string", "severity": "info | warning | critical", "title": "string", "message": "string", "metricLabel": "string", "metricValue": "string" } ],
  "weeklyCompliance": [ { "weekNumber": 1, "weekName": "string", "completedDays": 5, "totalDays": 6 } ],
  "weeklyVolume": [ { "weekNumber": 1, "weekName": "string", "totalVolume": 5200.0 } ],
  "muscleGroupVolume": [ { "muscleGroup": "Chest", "completedSets": 40, "totalVolume": 5000.0, "primaryVolume": 4000.0, "secondaryVolume": 1000.0, "percentOfTotal": 15.2 } ],
  "muscleGroupHeatmap": [ { "muscleGroup": "Chest", "totalVolume": 5000.0, "weeks": [ { "weekNumber": 1, "weekName": "string", "volume": 1200.0 } ] } ],
  "muscleGroupBalance": { "frontVolume": 12000.0, "backVolume": 11000.0, "upperVolume": 15000.0, "lowerVolume": 8000.0, "otherVolume": 0.0, "maxVolume": 15000.0 },
  "powerlifting": { "squat": { "lift": "Squat", "e1Rm": [ { "weekStart": "date", "e1Rm": 150.0 } ], "prTimeline": [ { "date": "date", "weight": 150.0, "reps": 1, "e1Rm": 150.0 } ], "currentE1Rm": 150.0, "currentTrainingMax": 135.0, "isPlateau": false }, "bench": { "...": "same shape" }, "deadlift": { "...": "same shape" }, "dots": [ { "weekStart": "date", "dots": 350.5, "bodyweightKg": 80.0 } ] }
}
```

### GET `/api/plans/{planId:guid}/design-analysis` *(RequirePro)*
Response `200`:
```json
{
  "structure": { "totalWeeks": 8, "plannedTrainingDays": 40, "plannedRestDays": 16, "avgTrainingDaysPerWeek": 5.0, "longestTrainingStreak": 6 },
  "workload": { "plannedExercises": 200, "plannedSets": 800, "plannedRepVolume": 8000, "plannedTonnage": 120000.0, "avgExercisesPerTrainingDay": 5.0 },
  "muscleGroups": [ { "muscleGroup": "Chest", "weightedSets": 60.0, "primarySets": 50.0, "secondarySets": 10.0, "percentOfTotal": 15.0, "status": "balanced | undertrained | overtrained" } ],
  "balance": { "frontSets": 200.0, "backSets": 180.0, "upperSets": 250.0, "lowerSets": 130.0, "otherSets": 0.0, "maxSets": 250.0, "dominantMuscleGroups": ["Chest"], "undertrainedMajorMuscleGroups": ["Back"] },
  "movementPatterns": [ { "pattern": "Squat | Hinge | Push | Pull | Carry", "isCovered": true, "exerciseCount": 4, "plannedSets": 20 } ],
  "recoveryRisks": [ { "type": "string", "severity": "low | medium | high", "message": "string", "metric": "string" } ],
  "variety": { "uniqueExercises": 25, "repeatedExerciseCount": 3, "topRepeatedExercises": [ { "exerciseName": "string", "count": 4 } ] }
}
```

### POST `/api/plans/{planId:guid}/balance-check?lang=` *(RequirePro, RateLimit: Ai)*
Response `200`:
```json
{ "headline": "string", "severity": "info | warning | critical", "summary": "string", "warnings": ["string"], "suggestions": ["string"] }
```

---

## 4. Weekly Workouts
**Route prefix:** `api/plans/{planId}/weeks` · Auth: `Authorize`

**WeeklyWorkoutResponse:**
```json
{
  "id": "guid", "weekNumber": 1, "name": "string", "startDate": "date", "endDate": "date",
  "planId": "guid", "totalDays": 7, "completedDays": 3, "hasWarning": false,
  "isCompleted": false, "effectiveTotalDays": 7
}
```

| Method | Route | Request | Response |
|---|---|---|---|
| GET | `/api/plans/{planId:guid}/weeks?pageNumber=&pageSize=` | — | `PagedResponse<WeeklyWorkoutResponse>` |
| PATCH | `/api/plans/{planId:guid}/weeks/{weeklyWorkoutId:guid}` | `{ "name": "string" }` (1-100 chars) | `WeeklyWorkoutResponse` |

---

## 5. Daily Workouts
**Route prefix:** `api/weeks/{weeklyWorkoutId}/days`, `api/days` · Auth: `Authorize`

**DailyWorkoutResponse:**
```json
{
  "id": "guid", "date": "date", "dayOfWeek": "string", "isCompleted": false,
  "weeklyWorkoutId": "guid", "totalExercises": 5, "completedExercises": 2,
  "skippedExercises": 0, "hasWarning": false, "status": "Normal | Rest | Missed"
}
```

| Method | Route | Request | Response |
|---|---|---|---|
| GET | `/api/weeks/{weeklyWorkoutId:guid}/days?pageNumber=&pageSize=` | — | `PagedResponse<DailyWorkoutResponse>` |
| PATCH | `/api/days/{dailyWorkoutId:guid}/status` | `{ "status": "Normal \| Rest \| Missed" }` | `204` |
| PATCH | `/api/days/{dailyWorkoutId:guid}/complete-all` | — | `ExerciseResponse[]` (see §6) |
| POST | `/api/days/{sourceDailyWorkoutId:guid}/copy` | `{ "targetDailyWorkoutId": "guid" }` | `{ "targetDailyWorkoutId": "guid", "exercisesCopied": 5 }` |

---

## 6. Exercises
**Route prefix:** `api/exercises` · Auth: `Authorize`

**ExerciseResponse:**
```json
{
  "id": "guid", "exerciseTemplateId": "guid", "name": "string",
  "primaryMuscleGroup": "string", "secondaryMuscleGroups": ["string"],
  "exerciseKind": "string", "estimatedMet": 6.0,
  "plannedSets": 4, "plannedReps": 8, "plannedWeight": 60.0,
  "completedSetsCount": 2, "isCompleted": false, "isSkipped": false,
  "notes": "string?", "dailyWorkoutId": "guid", "sortOrder": 1,
  "sets": [
    { "id": "guid", "setNumber": 1, "plannedReps": 8, "plannedWeight": 60.0, "actualReps": 8, "actualWeight": 60.0, "rpe": 8.0, "isCompleted": true, "completedAt": "datetime?" }
  ],
  "personalRecordWeight": 65.0, "startedAtUtc": "datetime?", "endedAtUtc": "datetime?",
  "durationSeconds": 300, "estimatedCalories": 45.5, "calorieEstimateStatus": "string",
  "isCompetitionLift": false, "imageUrl": "string?"
}
```

| Method | Route | Request | Response |
|---|---|---|---|
| GET | `/api/exercises/by-day/{dailyWorkoutId:guid}?pageNumber=&pageSize=` | — | `PagedResponse<ExerciseResponse>` |
| GET | `/api/exercises/by-week/{weeklyWorkoutId:guid}` | — | `ExerciseResponse[]` |
| PATCH | `/api/exercises/by-day/{dailyWorkoutId:guid}/reorder` | `{ "dailyWorkoutId": "guid", "orderedExerciseIds": ["guid"] }` | `ExerciseResponse[]` |
| POST | `/api/exercises` | `{ "dailyWorkoutId": "guid", "exerciseTemplateId": "guid", "plannedSets": 4, "plannedReps": 8, "plannedWeight": 60.0, "notes": "string?" }` | `ExerciseResponse` |
| PUT | `/api/exercises/{exerciseId:guid}` | same fields as create | `ExerciseResponse` |
| DELETE | `/api/exercises/{exerciseId:guid}` | — | `204` |
| PATCH | `/api/exercises/{exerciseId:guid}/timer/start` | — | `ExerciseResponse` |
| PATCH | `/api/exercises/{exerciseId:guid}/timer/finish` | — | `ExerciseResponse` |
| PATCH | `/api/exercises/{exerciseId:guid}/timer/set-duration` | `{ "durationSeconds": 300 }` | `ExerciseResponse` |
| PATCH | `/api/exercises/{exerciseId:guid}/skip` | — | `ExerciseResponse` |
| PATCH | `/api/exercises/sets/{setId:guid}/complete` | `{ "actualReps": 8, "actualWeight": 60.0, "rpe": 8.0 }` (all optional, fall back to planned) | `ExerciseResponse` |
| PATCH | `/api/exercises/sets/{setId:guid}` | `{ "plannedReps": 8, "plannedWeight": 60.0 }` | `ExerciseResponse` |

---

## 7. Exercise Templates
**Route prefix:** `api/exercise-templates` · Auth: `Authorize` unless noted

**ExerciseTemplateResponse:**
```json
{
  "id": "guid", "name": "string", "description": "string?",
  "primaryMuscleGroup": "string", "secondaryMuscleGroups": ["string"],
  "exerciseKind": "string", "estimatedMet": 6.0, "isCustom": false,
  "ownerId": "guid?", "imageUrl": "string?"
}
```

| Method | Route | Request | Response |
|---|---|---|---|
| GET | `/api/exercise-templates?muscleGroup=` | — | `ExerciseTemplateResponse[]` |
| GET | `/api/exercise-templates/{exerciseTemplateId:guid}/last-performance?dailyWorkoutId=` | — | `{ "exerciseTemplateId": "guid", "lastActualWeight": 60.0, "lastActualReps": 8, "lastRpe": 8.0, "performedAt": "datetime?", "workoutDate": "date?" }` |
| GET | `/api/exercise-templates/for-client/{clientId:guid}` *(RequireProCoach)* | — | `ExerciseTemplateResponse[]` |
| POST | `/api/exercise-templates/custom` | `{ "name": "string", "description": "string?", "primaryMuscleGroup": "string", "secondaryMuscleGroups": ["string"], "exerciseKind": "string" }` | `ExerciseTemplateResponse` |
| PUT | `/api/exercise-templates/custom/{id:guid}` | same as create | `ExerciseTemplateResponse` |
| DELETE | `/api/exercise-templates/custom/{id:guid}` | — | `204` |
| POST | `/api/exercise-templates/custom/for-client/{clientId:guid}` *(RequireProCoach)* | create fields + `clientId` | `ExerciseTemplateResponse` |

---

## 8. Coach ↔ Client
**Route prefix:** `api/coach-client` · Auth: `Authorize` unless noted

**CoachRelationshipResponse:**
```json
{
  "id": "guid", "clientId": "guid", "clientName": "string", "clientAvatarUrl": "string?",
  "coachId": "guid", "coachName": "string",
  "status": "Pending | Active | PendingTermination | Expired | PendingRenewal",
  "createdAt": "datetime", "terminationRequestedBy": "guid?",
  "startDate": "date", "endDate": "date?",
  "renewalRequestedBy": "guid?", "proposedEndDate": "date?"
}
```

| Method | Route | Request | Response |
|---|---|---|---|
| PUT | `/api/coach-client/accept/{relationshipId:guid}` *(RequireProCoach)* | — | `CoachRelationshipResponse` |
| DELETE | `/api/coach-client/{relationshipId:guid}` | — | `204` |
| GET | `/api/coach-client/pending-requests` *(RequireProCoach)* | — | `CoachRelationshipResponse[]` |
| GET | `/api/coach-client/my-coach` *(Roles: Individual/Coach)* | — | `CoachRelationshipResponse` or `null` |
| POST | `/api/coach-client/{relationshipId:guid}/request-termination` | — | `204` |
| POST | `/api/coach-client/{relationshipId:guid}/accept-termination` | — | `204` |
| POST | `/api/coach-client/{relationshipId:guid}/reject-termination` | — | `204` |
| POST | `/api/coach-client/{relationshipId:guid}/request-renewal` | `{ "proposedEndDate": "date" }` | `204` |
| POST | `/api/coach-client/{relationshipId:guid}/accept-renewal` | — | `204` |
| POST | `/api/coach-client/{relationshipId:guid}/reject-renewal` | — | `204` |
| GET | `/api/coach-client/my-clients` *(RequireProCoach)* | — | `ClientResponse[]` (below) |
| GET | `/api/coach-client/dashboard` *(RequireProCoach)* | — | `CoachClientDashboardResponse[]` (below) |
| GET | `/api/coach-client/clients/{clientId:guid}/powerlifting` *(RequireProCoach)* | — | `ClientPowerliftingResponse` (below) |
| GET | `/api/coach-client/clients/{clientId:guid}/ai-brief?lang=` *(RequireProCoach, RateLimit: Ai)* | — | `CoachClientAiBriefResponse` (below) |
| POST | `/api/coach-client/invite-codes` *(RequireProCoach)* | `{ "coachingStartDate": "date", "coachingEndDate": "date" }` | `CoachInviteCodeDto` (below) |
| GET | `/api/coach-client/invite-codes` *(RequireProCoach)* | — | `CoachInviteCodeDto[]` |
| DELETE | `/api/coach-client/invite-codes/{id:guid}` *(RequireProCoach)* | — | `204` |
| POST | `/api/coach-client/connect-by-code` | `{ "code": "string" }` | `CoachRelationshipResponse` |

**ClientResponse:**
```json
{
  "relationshipId": "guid", "clientId": "guid", "fullName": "string", "email": "string",
  "status": "Pending | Active | PendingTermination | Expired | PendingRenewal",
  "connectedAt": "datetime", "lastWorkoutCompletedAt": "date?",
  "terminationRequestedBy": "guid?", "startDate": "date", "endDate": "date?",
  "renewalRequestedBy": "guid?", "proposedEndDate": "date?"
}
```

**CoachClientDashboardResponse** (array item):
```json
{
  "clientId": "guid", "fullName": "string", "email": "string", "avatarUrl": "string?",
  "lastWorkoutDate": "date?", "planProgressPercent": 60, "latestBodyweightKg": 80.0,
  "bigThreePRs": { "squat": 150.0, "bench": 100.0, "deadlift": 180.0 },
  "activePlanId": "guid?", "activePlanName": "string?", "activePlanStartDate": "date?",
  "activePlanEndDate": "date?", "activePlanProgressPercent": 60,
  "daysSinceLastWorkout": 2, "latestCompletedWorkoutDate": "date?", "completedWorkoutToday": false,
  "activePlanCompletedWorkoutCount": 12, "activePlanTotalWorkoutCount": 20,
  "missedWorkoutDays": 2, "completedWorkoutDays": 12, "totalWorkoutDays": 20,
  "attentionLevel": "none | low | medium | high", "attentionReasons": ["string"]
}
```

**ClientPowerliftingResponse:**
```json
{
  "clientId": "guid",
  "powerlifting": {
    "squat": { "lift": "Squat", "e1Rm": [ { "weekStart": "date", "e1Rm": 150.0 } ], "prTimeline": [ { "date": "date", "weight": 150.0, "reps": 1, "e1Rm": 150.0 } ], "currentE1Rm": 150.0, "currentTrainingMax": 135.0, "isPlateau": false },
    "bench": { "...": "same shape as squat" },
    "deadlift": { "...": "same shape as squat" },
    "dots": [ { "weekStart": "date", "dots": 350.5, "bodyweightKg": 80.0 } ]
  },
  "insights": [ { "type": "string", "severity": "info | warning | critical", "title": "string", "message": "string", "metricLabel": "string", "metricValue": "string" } ]
}
```

**CoachClientAiBriefResponse:**
```json
{
  "language": "string", "generatedAt": "datetime", "cached": false,
  "headline": "string", "attentionLevel": "none | low | medium | high",
  "progressSummary": "string", "risks": ["string"], "opportunities": ["string"], "suggestedMessage": "string"
}
```

**CoachInviteCodeDto:**
```json
{
  "id": "guid", "code": "string", "coachingStartDate": "date", "coachingEndDate": "date",
  "isUsed": false, "usedByClientId": "guid?", "usedAt": "datetime?", "createdAt": "datetime"
}
```

---

## 9. Nutrition
**Route prefix:** `api/nutrition` · Auth: `Authorize` unless noted

**NutritionSummaryResponse** (`GET /summary`):
```json
{
  "userId": "guid",
  "profile": {
    "activityLevel": "Sedentary | LightlyActive | ModeratelyActive | VeryActive | ExtremelyActive",
    "goal": "LoseFat | Maintain | GainMuscle",
    "targetWeightKg": 75.0, "customCalorieTarget": 2200,
    "proteinPerKg": 2.0, "fatPerKg": 0.8
  },
  "calculation": {
    "missingFields": ["string"], "bodyweightKg": 80.0, "age": 28,
    "bmr": 1750, "tdee": 2400, "recommendedCalories": 2200, "calorieTarget": 2200,
    "proteinG": 160.0, "carbsG": 220.0, "fatG": 64.0
  },
  "todayLog": { "date": "date", "calories": 1800, "proteinG": 140.0, "carbsG": 180.0, "fatG": 55.0, "notes": "string?" },
  "canUseAdvancedAnalysis": true
}
```

| Method | Route | Request | Response |
|---|---|---|---|
| GET | `/api/nutrition/summary` | — | `NutritionSummaryResponse` above |
| PUT | `/api/nutrition/profile` | `{ "activityLevel": "string", "goal": "string", "targetWeightKg": 75.0, "customCalorieTarget": 2200, "proteinPerKg": 2.0, "fatPerKg": 0.8 }` | `profile` object above |
| GET | `/api/nutrition/logs/{date}` | — | `{ "date": "date", "calories": 1800, "proteinG": 140.0, "carbsG": 180.0, "fatG": 55.0, "notes": "string?" }` or `null` |
| PUT | `/api/nutrition/logs/{date}` | `{ "notes": "string?" }` | same log object |
| GET | `/api/nutrition/history?from=&to=` | — | `[ { "date": "date", "calories": 1800, "proteinG": 140.0, "carbsG": 180.0, "fatG": 55.0 } ]` |
| GET | `/api/nutrition/meal-plans/{date}` | — | `MealPlanDayResponse` (below) |
| PUT | `/api/nutrition/meal-plans/{date}` | `{ "notes": "string?", "meals": [ { "name": "string", "items": [ { "foodItemId": "guid", "grams": 150.0, "servingCount": 1.0 } ] } ] }` | `MealPlanDayResponse` |
| POST | `/api/nutrition/meal-plans/items/{itemId:guid}/check` | — | `MealPlanItemResponse` (below) |
| POST | `/api/nutrition/meal-plans/items/{itemId:guid}/uncheck` | — | `MealPlanItemResponse` |
| GET | `/api/nutrition/clients/{clientId:guid}/summary` *(RequireProCoach)* | — | `NutritionSummaryResponse` |
| GET | `/api/nutrition/clients/{clientId:guid}/logs/{date}` *(RequireProCoach)* | — | daily log object or `null` |
| GET | `/api/nutrition/clients/{clientId:guid}/history?from=&to=` *(RequireProCoach)* | — | history array |
| GET | `/api/nutrition/clients/{clientId:guid}/meal-plans/{date}` *(RequireProCoach)* | — | `MealPlanDayResponse` |
| PUT | `/api/nutrition/clients/{clientId:guid}/meal-plans/{date}` *(RequireProCoach)* | same as coach's own meal-plan PUT | `MealPlanDayResponse` |
| GET | `/api/nutrition/foods/search?q=&lang=vi` | — | `FoodItemResponse[]` (below) |
| GET | `/api/nutrition/foods/resolve?name=` *(RequirePro, RateLimit: Ai)* | — | `FoodItemResponse` |
| POST | `/api/nutrition/foods` | `{ "nameVi": "string", "nameEn": "string", "caloriesPer100g": 165.0, "proteinPer100g": 31.0, "carbsPer100g": 0.0, "fatPer100g": 3.6 }` | `FoodItemResponse` |
| GET | `/api/nutrition/logs/{date}/foods` | — | `FoodLogsForDateResponse` (below) |
| POST | `/api/nutrition/logs/{date}/foods` | `{ "foodItemId": "guid", "grams": 150.0, "servingCount": 1.0 }` | `FoodLogItemResponse` (below) |
| DELETE | `/api/nutrition/logs/{date}/foods/{foodLogId:guid}` | — | `204` |

**MealPlanDayResponse:**
```json
{
  "id": "guid?", "userId": "guid", "date": "date", "notes": "string?",
  "meals": [
    {
      "id": "guid", "name": "string", "sortOrder": 0,
      "items": [
        { "id": "guid", "foodItemId": "guid", "nameVi": "string", "nameEn": "string", "sortOrder": 0,
          "grams": 150.0, "servingLabelVi": "string?", "servingLabelEn": "string?", "servingCount": 1.0,
          "plannedCalories": 250, "plannedProteinG": 30.0, "plannedCarbsG": 0.0, "plannedFatG": 5.0,
          "isChecked": false, "checkedAt": "datetime?", "foodLogId": "guid?" }
      ],
      "plannedTotals": { "calories": 250, "proteinG": 30.0, "carbsG": 0.0, "fatG": 5.0 },
      "checkedTotals": { "calories": 0, "proteinG": 0.0, "carbsG": 0.0, "fatG": 0.0 }
    }
  ],
  "plannedTotals": { "calories": 2200, "proteinG": 160.0, "carbsG": 220.0, "fatG": 64.0 },
  "checkedTotals": { "calories": 800, "proteinG": 60.0, "carbsG": 80.0, "fatG": 20.0 },
  "totalItemCount": 12, "checkedItemCount": 4
}
```
`MealPlanItemResponse` is a single item from the `items` array above.

**FoodItemResponse:**
```json
{
  "id": "guid", "nameVi": "string", "nameEn": "string",
  "caloriesPer100g": 165.0, "proteinPer100g": 31.0, "carbsPer100g": 0.0, "fatPer100g": 3.6,
  "servings": [ { "id": "guid", "labelVi": "string", "labelEn": "string?", "grams": 100.0 } ]
}
```

**FoodLogsForDateResponse:**
```json
{
  "date": "date",
  "items": [
    { "id": "guid", "foodItemId": "guid", "nameVi": "string", "nameEn": "string", "grams": 150.0,
      "servingLabelVi": "string?", "servingLabelEn": "string?", "servingCount": 1.0,
      "computedCalories": 250, "computedProteinG": 30.0, "computedCarbsG": 0.0, "computedFatG": 5.0 }
  ],
  "totals": { "totalCalories": 1800, "totalProteinG": 140.0, "totalCarbsG": 180.0, "totalFatG": 55.0 }
}
```
`FoodLogItemResponse` (from `POST logs/{date}/foods`) = a single item shape from the `items` array above.

---

## 10. Cycle Tracking
**Route prefix:** `api/cycle` · Auth: `Authorize` unless noted

### GET `/api/cycle/overview`
```json
{
  "currentPhase": "Menstrual | Follicular | Ovulation | Luteal",
  "cycleDay": 14, "daysUntilNextPeriod": 15, "daysLate": null,
  "lastPeriodStart": "date?", "nextPeriodStart": "date?", "currentPeriodPredictedEnd": "date?",
  "predictedPeriods": [ { "start": "date", "end": "date" } ],
  "ovulationDates": ["date"],
  "fertileWindows": [ { "start": "date", "end": "date" } ],
  "effectiveCycleLengthDays": 28, "effectivePeriodLengthDays": 5,
  "avgCycleLengthDays": 28, "avgPeriodLengthDays": 5,
  "isRegular": true, "cycleVariabilityDays": 2,
  "confidence": "low | medium | high", "needsData": false
}
```

### GET/PUT `/api/cycle/settings`
```json
{ "averageCycleLengthOverride": 28, "averagePeriodLengthOverride": 5, "shareWithCoach": false }
```

### GET `/api/cycle/day-markers?from=&to=`
```json
{
  "from": "date", "to": "date", "preMenstrualWindowDays": 3, "needsData": false,
  "days": [ { "date": "date", "marker": "period | fertile | ovulation | premenstrual | none" } ]
}
```

### PUT `/api/cycle/logs/{date}` / GET `/api/cycle/logs?from=&to=`
Single log:
```json
{ "date": "date", "flow": "Light | Normal | Heavy | null", "symptoms": ["string"], "mood": "Energetic | Calm | Sad | Irritable | null", "energyLevel": 3, "notes": "string?" }
```
`GET /logs` returns an array of the above; `DELETE /logs/{date}` returns `204`.

### GET `/api/cycle/insight?lang=en` *(RequirePro, RateLimit: Ai)*
```json
{
  "language": "string", "generatedAt": "datetime", "cached": false,
  "content": {
    "summary": "string", "cyclePatterns": ["string"], "symptomPatterns": ["string"],
    "trainingCorrelations": ["string"],
    "phaseRecommendations": [ { "phase": "string", "training": "string", "nutrition": "string" } ],
    "cautions": ["string"], "disclaimer": "string"
  }
}
```

### GET `/api/cycle/clients/{clientId:guid}/overview` *(RequireProCoach)*
```json
{
  "currentPhase": "string", "cycleDay": 14, "nextPeriodStart": "date?",
  "daysUntilNextPeriod": 15, "daysLate": null,
  "effectiveCycleLengthDays": 28, "effectivePeriodLengthDays": 5,
  "avgCycleLengthDays": 28, "avgPeriodLengthDays": 5, "isRegular": true, "cycleVariabilityDays": 2,
  "lastPeriodStart": "date?", "nextOvulationDate": "date?",
  "fertileWindowStart": "date?", "fertileWindowEnd": "date?",
  "frequentSymptoms": ["string"], "confidence": "low | medium | high", "needsData": false
}
```
`404` if not shared / not female / no data.

---

## 11. Insights (AI)
**Route prefix:** `api/insights` · Auth: `RequirePro` unless noted

### GET `/api/insights/me?lang=en` *(RateLimit: Ai)*
```json
{
  "language": "string", "generatedAt": "datetime", "cached": false,
  "content": {
    "trainingAdherence": { "headline": "string", "detail": "string" },
    "bodyMetrics": { "headline": "string", "detail": "string" },
    "volumeStrength": { "headline": "string", "detail": "string" },
    "muscleBalance": { "headline": "string", "detail": "string" },
    "effortGap": { "headline": "string", "detail": "string" },
    "recommendation": { "headline": "string", "actions": ["string"] },
    "planReview": { "headline": "string", "mistakes": ["string"], "suggestions": ["string"] }
  },
  "metrics": {
    "adherence": {
      "activePlanName": "string?", "planStartDate": "date?", "planEndDate": "date?",
      "totalPlanDays": 56, "completedPlanDays": 20, "planCompletionPercent": 36,
      "currentWeek": { "weekNumber": 3, "startDate": "date", "endDate": "date", "totalDays": 7, "completedDays": 4, "completionPercent": 57, "totalExercises": 25, "completedExercises": 14, "volume": 5200.0 },
      "previousWeek": { "...": "same shape as currentWeek" }
    },
    "bodyweight": { "points": [ { "date": "date", "weight": 80.0 } ], "latestWeight": 80.0, "delta": -0.5, "trend": "up | down | stable" },
    "volume": { "recentTotalVolume": 20000.0, "recentSetCount": 120, "currentWeekVolume": 5200.0, "previousWeekVolume": 4900.0, "weekVolumeDelta": 300.0 },
    "muscleBalance": [ { "muscle": "Chest", "sets": 30, "volume": 4000.0, "sharePercent": 15 } ],
    "effortGap": {
      "highRpeMisses": [ { "exercise": "string", "sets": 4, "averageRpe": 9.2, "pattern": "string" } ],
      "lowRpeWins": [ { "exercise": "string", "sets": 4, "averageRpe": 6.0, "pattern": "string" } ]
    },
    "recentPrs": [ { "exercise": "string", "weight": 150.0, "reps": 1, "achievedAt": "datetime" } ]
  }
}
```

### GET `/api/insights/plan/{planId:guid}/progress?lang=` *(RateLimit: Ai)*
```json
{ "language": "string", "planName": "string", "generatedAt": "datetime", "headline": "string", "trajectory": "improving | plateauing | declining", "summary": "string", "whatsWorking": ["string"], "focusAreas": ["string"], "nextBlock": ["string"] }
```

### GET `/api/insights/me/coach-tip?lang=` *(RateLimit: Ai)*
```json
{ "language": "string", "generatedAt": "datetime", "cached": false, "headline": "string", "category": "string", "insight": "string", "evidence": ["string"], "whyItMatters": "string", "nextAction": "string", "confidence": "low | medium | high" }
```

### Coach chat
```json
// GET /api/insights/me/coach-chat/conversations?cursor=&take=20
{
  "items": [ { "id": "guid", "title": "string", "messageCount": 12, "lastMessageAt": "datetime", "isArchived": false, "createdAt": "datetime", "updatedAt": "datetime" } ],
  "hasMore": true
}
// POST /conversations -> single conversation object above
// PATCH /conversations/{id} -> single conversation object above

// GET /conversations/{id}/messages?before=&take=30
{
  "items": [ { "id": "guid", "conversationId": "guid", "role": "user | assistant", "content": "string", "createdAt": "datetime" } ],
  "hasMore": true
}

// POST /conversations/{id}/messages  (RateLimit: Ai)
// Request: { "content": "string", "language": "string?" }
{
  "conversation": { "...": "conversation object" },
  "userMessage": { "...": "message object" },
  "assistantMessage": { "...": "message object" }
}
```

---

## 12. Messages
**Route prefix:** `api/messages` · Auth: `Authorize`

```json
// GET /api/messages/relationships/{relationshipId:guid}?pageSize=30&before=
{
  "items": [
    {
      "id": "guid", "relationshipId": "guid", "senderId": "guid", "senderName": "string",
      "content": "string", "kind": "Text | System", "isRead": false,
      "attachments": [ { "id": "guid", "fileName": "string", "contentType": "string", "sizeBytes": 102400, "isImage": true } ],
      "createdAt": "datetime"
    }
  ],
  "hasMore": true,
  "totalUnread": 3
}
```

| Method | Route | Request | Response |
|---|---|---|---|
| POST | `/api/messages/relationships/{relationshipId:guid}` | `{ "content": "string" }` | single message object above |
| POST | `/api/messages/relationships/{relationshipId:guid}/attachments` | `multipart/form-data: content?, files[]` (≤25MB total) | single message object above (with attachments) |
| GET | `/api/messages/attachments/{attachmentId:guid}/download-url?inline=false` | — | `{ "url": "string" }` |
| POST | `/api/messages/relationships/{relationshipId:guid}/read` | — | `204` |
| GET | `/api/messages/unread-counts` | — | `{ "<relationshipId:guid>": 3, "<relationshipId2:guid>": 0 }` (dictionary keyed by relationship id) |

---

## 13. Community
**Route prefix:** `api/community` · Auth: `Authorize`

| Method | Route | Response |
|---|---|---|
| GET | `/api/community/users?query=&page=1&pageSize=20` | `PagedResponse<CommunityUserSummaryResponse>` |
| GET | `/api/community/users/{userId:guid}` | `CommunityUserProfileResponse` |
| GET | `/api/community/users/{userId:guid}/training-day-shares?page=&pageSize=` | `TrainingDayShareResponse[]` (§15) |

**CommunityUserSummaryResponse:**
```json
{
  "id": "guid", "fullName": "string", "email": "string?", "avatarUrl": "string?", "bio": "string?",
  "gender": "Male | Female | null", "friendStatus": "None | Pending | Friends",
  "friendshipId": "guid?", "requestDirection": "Incoming | Outgoing | null"
}
```

**CommunityUserProfileResponse:**
```json
{
  "id": "guid", "fullName": "string", "email": "string?", "avatarUrl": "string?", "bio": "string?",
  "gender": "Male | Female | null", "developmentDirection": "string?", "trainingDiscipline": "string?",
  "friendStatus": "None | Pending | Friends", "friendshipId": "guid?", "requestDirection": "Incoming | Outgoing | null",
  "canViewStats": true, "height": 175.5, "latestBodyweight": 80.0, "bmi": 24.1, "bmiCategory": "string?",
  "currentStreak": 5, "dotsScore": 350.5, "totalTrainingDurationSeconds": 720000, "totalTrainingVolume": 250000.0,
  "big3Prs": { "squat": 150.0, "bench": 100.0, "deadlift": 180.0 }, "big3Total": 430.0, "level": 3
}
```

---

## 14. Friends
**Route prefix:** `api/friends` · Auth: `Authorize`

| Method | Route | Request | Response |
|---|---|---|---|
| GET | `/api/friends` | — | `FriendResponse[]` |
| GET | `/api/friends/requests?direction=incoming` | — | `FriendRequestResponse[]` |
| POST | `/api/friends/requests` | `{ "targetUserId": "guid" }` | `FriendRequestResponse` |
| POST | `/api/friends/requests/{id:guid}/accept` | — | `FriendRequestResponse` |
| POST | `/api/friends/requests/{id:guid}/reject` | — | `204` |
| DELETE | `/api/friends/{userId:guid}` | — | `204` |

**FriendResponse:**
```json
{ "userId": "guid", "fullName": "string", "email": "string", "avatarUrl": "string?", "bio": "string?", "friendsSince": "datetime" }
```

**FriendRequestResponse:**
```json
{ "id": "guid", "userId": "guid", "fullName": "string", "email": "string", "avatarUrl": "string?", "direction": "Incoming | Outgoing", "status": "Pending | Accepted | Rejected", "createdAt": "datetime", "respondedAt": "datetime?" }
```

---

## 15. Training Day Shares
**Route prefix:** `api/training-day-shares` · Auth: `Authorize`

**TrainingDayShareResponse:**
```json
{
  "id": "guid", "userId": "guid", "userFullName": "string", "userAvatarUrl": "string?",
  "sourceDailyWorkoutId": "guid", "workoutDate": "date", "dayOfWeek": "string",
  "dayStatus": "Normal | Rest | Missed", "exerciseCount": 5, "completedSets": 20,
  "totalVolume": 5200.0, "totalDurationSeconds": 3600, "averageRpe": 7.8,
  "hasPersonalRecord": true, "caption": "string?", "loveCount": 12, "lovedByCurrentUser": false,
  "createdAt": "datetime",
  "exercises": [
    {
      "id": "guid", "name": "string", "primaryMuscleGroup": "string", "exerciseKind": "string",
      "sortOrder": 1, "isSkipped": false, "isPersonalRecord": true, "durationSeconds": 300, "notes": "string?",
      "sets": [ { "id": "guid", "setNumber": 1, "actualReps": 8, "actualWeight": 60.0, "rpe": 8.0, "isCompleted": true } ]
    }
  ]
}
```

| Method | Route | Request | Response |
|---|---|---|---|
| POST | `/api/training-day-shares` | `{ "dailyWorkoutId": "guid", "caption": "string?" }` | `TrainingDayShareResponse` |
| GET | `/api/training-day-shares/feed?page=&pageSize=` | — | `TrainingDayShareResponse[]` |
| POST | `/api/training-day-shares/{id:guid}/love` | — | `TrainingDayShareResponse` |
| DELETE | `/api/training-day-shares/{id:guid}/love` | — | `TrainingDayShareResponse` |
| DELETE | `/api/training-day-shares/{id:guid}` | — | `204` |

---

## 16. Comments
`CommentResponse` shape used by both plan and week comments:
```json
{ "id": "guid", "content": "string", "authorId": "guid", "authorName": "string", "createdAt": "datetime" }
```

**Plan comments** — `api/plans/{planId}/comments`

| Method | Route | Request | Response |
|---|---|---|---|
| GET | `/api/plans/{planId:guid}/comments` | — | `CommentResponse[]` |
| POST | `/api/plans/{planId:guid}/comments` | `{ "content": "string" }` (1-1000 chars) | `CommentResponse` |
| DELETE | `/api/plans/{planId:guid}/comments/{commentId:guid}` | — | `204` |

**Week comments** — `api/weeks/{weekId}/comments` (identical shape, replace `planId` with `weekId`)

---

## 17. Leaderboard
**Route prefix:** `api/leaderboard` · Auth: `Authorize`

```json
// GET /api/leaderboard?type=dots&gender=
[ { "rank": 1, "userId": "guid", "fullName": "string", "score": 450.2, "bodyweightKg": 82.0 } ]

// GET /api/leaderboard/big3?gender=
[ { "rank": 1, "userId": "guid", "fullName": "string", "squatPr": 180.0, "benchPr": 120.0, "deadliftPr": 220.0, "total": 520.0, "dotsScore": 450.2, "bodyweight": 82.0 } ]
```

---

## 18. Notifications
**Route prefix:** `api/notifications` · Auth: `Authorize`

```json
// GET /api/notifications
[ { "id": "guid", "type": "string", "message": "string", "isRead": false, "relatedEntityId": "guid?", "relatedEntityType": "string?", "createdAt": "datetime" } ]
```
`PATCH /{id}/read` and `PATCH /read-all` return `204`.

---

## 19. Blocks
**Routes:** under `api/users/...` · Auth: `Authorize`

```json
// GET /api/users/me/blocks
[ { "id": "guid", "blockedUserId": "guid", "fullName": "string", "avatarUrl": "string?", "reason": "string?", "createdAt": "datetime" } ]
```
`POST /{userId}/block` request: `{ "reason": "string?" }` → `204`. `DELETE /{userId}/block` → `204`.

---

## 20. Dashboard
**Route prefix:** `api/dashboard` · Auth: `Authorize`

### GET `/api/dashboard/personal`
```json
{
  "profile": {
    "firstName": "string", "avatarUrl": "string?", "currentStreak": 5, "level": 3,
    "totalXp": 4200, "xpToNextLevel": 800, "title": "string", "latestBodyweight": 80.0,
    "bmi": 24.1, "bmiCategory": "string?", "dotsScore": 350.5
  },
  "activePlan": {
    "id": "guid", "name": "string", "startDate": "date", "endDate": "date",
    "totalDays": 56, "completedDays": 20, "progressPercent": 36,
    "currentWeek": { "id": "guid", "name": "string", "startDate": "date", "endDate": "date", "totalDays": 7, "completedDays": 4, "progressPercent": 57 }
  },
  "todayWorkout": {
    "id": "guid", "weeklyWorkoutId": "guid", "dayOfWeek": "string", "date": "date",
    "status": "Normal | Rest | Missed", "isCompleted": false,
    "totalExercises": 5, "completedExercises": 2, "totalSets": 20, "completedSets": 8,
    "plannedVolume": 5200.0, "muscleGroups": ["Chest", "Triceps"], "route": "string"
  },
  "nutritionToday": {
    "calorieTarget": 2200, "proteinTargetG": 160.0, "carbsTargetG": 220.0, "fatTargetG": 64.0,
    "loggedCalories": 1400, "loggedProteinG": 100.0, "loggedCarbsG": 140.0, "loggedFatG": 40.0,
    "remainingCalories": 800, "remainingProteinG": 60.0, "remainingCarbsG": 80.0, "remainingFatG": 24.0,
    "missingProfileFields": ["string"],
    "mealPlanToday": { "plannedCalories": 2200, "plannedProteinG": 160.0, "plannedCarbsG": 220.0, "plannedFatG": 64.0, "checkedCalories": 800, "checkedProteinG": 60.0, "checkedCarbsG": 80.0, "checkedFatG": 20.0, "totalItemCount": 12, "checkedItemCount": 4, "route": "string" }
  },
  "nextActions": [ { "type": "string", "label": "string", "description": "string", "route": "string", "priority": 1 } ],
  "proInsights": { "isUnlocked": true, "ctaLabel": "string?", "ctaRoute": "string?", "items": [ { "type": "string", "severity": "info | warning | critical", "title": "string", "message": "string" } ] }
}
```

---

## 21. Files
**Route prefix:** `api/files` · Auth: `RequirePro` (whole controller)

```json
// GET /api/files
{
  "usedBytes": 5242880, "quotaBytes": 104857600, "maxFileSizeBytes": 26214400,
  "files": [ { "id": "guid", "fileName": "string", "contentType": "string", "sizeBytes": 102400, "createdAt": "datetime", "sharedWith": [ { "shareId": "guid", "sharedWithUserId": "guid", "sharedWithName": "string" } ] } ]
}
// GET /api/files/shared-with-me
[ { "id": "guid", "fileName": "string", "contentType": "string", "sizeBytes": 102400, "createdAt": "datetime", "ownerName": "string" } ]
// GET /api/files/{id}/download-url
{ "url": "string" }
// POST /api/files/{id}/share  Request: { "clientId": "guid" }
{ "shareId": "guid", "sharedWithUserId": "guid", "sharedWithName": "string" }
```
`POST /api/files` (upload) returns the single file object shown in the `files` array above. `DELETE /{id}` and `DELETE /{id}/share/{shareId}` return `204`.

---

## 22. Subscriptions & Payments

### GET `/api/subscriptions/me` — Authorize
```json
{
  "id": "guid", "tier": "Free | ProIndividual | ProCoach", "isActive": true, "expiresAt": "datetime?", "createdAt": "datetime",
  "aiQuota": { "monthlyLimit": 100, "usedRequests": 12, "remainingRequests": 88, "periodStart": "date" }
}
```

### POST `/api/subscriptions/payment-orders` — Authorize
Request: `{ "requestedTier": "ProIndividual | ProCoach", "durationMonths": 1 }`
```json
{
  "orderId": "guid", "transferCode": "string", "amount": 199000, "durationMonths": 1,
  "requestedTier": "ProIndividual", "expiresAt": "datetime",
  "bankAccountNumber": "string", "bankAccountName": "string", "bankName": "string", "transferDescription": "string"
}
```
`503` if payment gateway is down.

### POST `/api/subscriptions/dev-activate` — Authorize
Dev-only (404 outside Development). Response: activation result object (subscription shape as above).

### POST `/api/payments/webhook` — AllowAnonymous, verified by API key header *(RateLimit: Webhook)*
Request (`SePayWebhookPayload`):
```json
{
  "id": 123456, "gateway": "string?", "transactionDate": "string?", "accountNumber": "string?",
  "subAccId": "string?", "code": "string?", "content": "string?", "transferType": "string?",
  "transferAmount": 199000, "accumulated": 0, "referenceCode": "string?", "description": "string?"
}
```
Response:
```json
{ "success": true, "message": "string" }
```

---

## 23. Share (public)
**Route prefix:** `api/share` · Auth: `AllowAnonymous` *(RateLimit: PublicShare)*

### GET `/api/share/pr/{userId:guid}/{exerciseTemplateId:guid}/image.png?copy=`
No JSON — returns a `image/png` byte stream (or `302 Redirect` to a presigned R2 URL when `?copy` is absent). `404` if the user has no PR for that exercise.

---

## 24. Analytics
**Route prefix:** `api/analytics`

Request (`TrackWebsiteActivityRequest`, used by both endpoints):
```json
{ "sessionId": "string", "path": "string", "previousPath": "string?", "referrer": "string?", "utmSource": "string?", "utmMedium": "string?", "utmCampaign": "string?", "durationSeconds": 30 }
```
| Method | Route | Auth | Response |
|---|---|---|---|
| POST | `/api/analytics/page-view` | AllowAnonymous | `204` |
| POST | `/api/analytics/usage` | Authorize | `204` |

---

## 25. Bug Reports
**Route prefix:** `api/bug-reports` · Auth: `Authorize`

### POST `/api/bug-reports`
Request: `{ "title": "string", "description": "string", "pageUrl": "string?", "browserInfo": "string?", "severity": "Low | Medium | High" }`
```json
{
  "id": "guid", "userId": "guid", "userName": "string", "userEmail": "string",
  "title": "string", "description": "string", "pageUrl": "string?", "browserInfo": "string?",
  "severity": "Low | Medium | High", "status": "Open | InProgress | Resolved | Dismissed",
  "adminNote": "string?", "reviewedById": "guid?", "reviewedByName": "string?", "reviewedAtUtc": "datetime?", "createdAt": "datetime"
}
```

---

## 26. Admin
**Route prefix:** `api/admin` · Auth: `Roles = Admin` (whole controller)

### GET `/api/admin/dashboard`
```json
{
  "totalUsers": 5200, "newUsersThisMonth": 340, "activeCoaches": 42, "activePaidSubscriptions": 890,
  "pendingReports": 3, "completedPaymentRevenueThisMonth": 45000000, "totalPlansCreated": 12000,
  "completedWorkoutDays": 82000,
  "userRegistrations": [ { "label": "string", "value": 120 } ],
  "revenue": [ { "label": "string", "value": 5000000 } ],
  "subscriptionTierDistribution": [ { "label": "Free", "value": 4300 } ],
  "planCompletionTrend": [ { "label": "string", "value": 78.5 } ]
}
```

### GET `/api/admin/insights?from=&to=&granularity=`
```json
{
  "from": "date", "to": "date", "granularity": "Month | Day",
  "totals": { "totalUsers": 5200, "newUsers": 340, "activeUsers": 2100, "activePaidSubscriptions": 890, "revenue": 45000000, "plansCreated": 900, "completedWorkoutDays": 6200, "reportsCreated": 3, "aiRequests": 1200, "communityShares": 300, "communityLoves": 900, "acceptedFriendships": 210 },
  "userRegistrations": [ { "label": "string", "value": 120 } ],
  "activeUsers": [ { "label": "string", "value": 300 } ],
  "activePaidSubscriptions": [ { "label": "string", "value": 90 } ],
  "revenue": [ { "label": "string", "value": 5000000 } ],
  "plansCreated": [ { "label": "string", "value": 120 } ],
  "completedWorkoutDays": [ { "label": "string", "value": 800 } ],
  "reportsCreated": [ { "label": "string", "value": 1 } ],
  "aiRequests": [ { "label": "string", "value": 150 } ],
  "communityActivity": [ { "label": "string", "value": 45 } ]
}
```

### GET `/api/admin/marketing?from=&to=&granularity=`
```json
{
  "from": "date", "to": "date", "granularity": "Month | Day",
  "totals": { "pageViews": 42000, "uniqueSessions": 8500, "knownUsers": 5200, "logins": 6200, "registrations": 340, "totalUsageSeconds": 1800000, "averageUsageSecondsPerSession": 211.7, "bugReportsOpen": 5 },
  "pageViews": [ { "label": "string", "value": 3200 } ],
  "logins": [ { "label": "string", "value": 500 } ],
  "registrations": [ { "label": "string", "value": 30 } ],
  "usageSeconds": [ { "label": "string", "value": 150000 } ],
  "topSources": [ { "label": "google", "value": 1200 } ],
  "topCampaigns": [ { "label": "string", "value": 300 } ],
  "topReferrers": [ { "label": "string", "value": 400 } ],
  "topEntryPages": [ { "label": "/home", "value": 900 } ],
  "topFlows": [ { "fromPath": "/home", "toPath": "/plans", "count": 300 } ]
}
```

### Reports & Bug Reports
```json
// GET /api/admin/reports?status=&reason=  -> UserReportResponse[] (see §2)
// PATCH /api/admin/reports/{reportId}  Request: { "status": "string", "adminNote": "string?" }  -> UserReportResponse
// GET /api/admin/reports/summary
{ "total": 12, "countsByStatus": [ { "status": "Open | InProgress | Resolved | Dismissed", "count": 3 } ] }

// GET /api/admin/bug-reports?status=&severity=  -> WebsiteBugReportResponse[] (see §25)
// PATCH /api/admin/bug-reports/{bugReportId}  Request: { "status": "string", "adminNote": "string?" }  -> WebsiteBugReportResponse
```

### Users
```json
// GET /api/admin/users?search=&role=&tier=&suspended=
[
  {
    "id": "guid", "email": "string", "fullName": "string", "roles": ["Individual"],
    "subscriptionTier": "Free | ProIndividual | ProCoach", "isSubscriptionActive": true,
    "isSuspended": false, "createdAt": "datetime",
    "planCount": 3, "workoutHistoryCount": 120, "reportsMadeCount": 0, "reportsReceivedCount": 0
  }
]

// GET /api/admin/users/{userId}
{
  "id": "guid", "email": "string", "fullName": "string", "roles": ["Individual"],
  "subscriptionTier": "Free | ProIndividual | ProCoach", "subscriptionExpiresAt": "datetime?", "isSubscriptionActive": true,
  "isSuspended": false, "createdAt": "datetime",
  "height": 175.5, "gender": "Male | Female | null", "dateOfBirth": "date?", "bio": "string?", "avatarUrl": "string?",
  "planCount": 3, "workoutHistoryCount": 120, "reportsMadeCount": 0, "reportsReceivedCount": 0,
  "coachRelationship": { "relationshipId": "guid", "userId": "guid", "userName": "string", "userEmail": "string", "status": "Pending | Active | Terminated" },
  "clientRelationships": [ { "...": "same shape as coachRelationship" } ]
}

// POST /api/admin/users/{userId}/suspend -> 204
// POST /api/admin/users/{userId}/unsuspend -> 204

// PATCH /api/admin/users/{userId}/subscription
// Request: { "tier": "ProIndividual", "durationMonths": 3, "reason": "string" }
{ "userId": "guid", "userName": "string", "userEmail": "string", "tier": "ProIndividual", "isActive": true, "expiresAt": "datetime?", "auditLogId": "guid" }
```

### Plans, Payments, Subscriptions
```json
// GET /api/admin/plans?planType=&ownerId=&coachId=&from=&to=&isActive=
[
  {
    "id": "guid", "name": "string", "planType": "Self | Coach", "ownerId": "guid", "ownerName": "string", "ownerEmail": "string",
    "createdByCoachId": "guid?", "coachName": "string?", "coachEmail": "string?",
    "startDate": "date", "endDate": "date", "isActive": true, "createdAt": "datetime",
    "totalWeeks": 8, "totalDays": 56, "completedDays": 20, "completionPercent": 36.0,
    "totalExercises": 200, "totalCompletedSets": 320, "totalVolume": 45000.0
  }
]

// GET /api/admin/plans/{planId}/analytics
{
  "planId": "guid", "planName": "string", "ownerId": "guid", "ownerName": "string",
  "createdByCoachId": "guid?", "coachName": "string?",
  "totalWeeks": 8, "totalDays": 56, "completedDays": 20, "completionPercent": 36.0,
  "totalExercises": 200, "totalCompletedSets": 320, "totalVolume": 45000.0
}

// GET /api/admin/payments?status=&tier=&from=&to=
[
  {
    "id": "guid", "userId": "guid", "userName": "string", "userEmail": "string",
    "requestedTier": "ProIndividual | ProCoach", "durationMonths": 1, "amount": 199000,
    "status": "Pending | Completed | Failed | Expired",
    "transferCode": "string", "sePayTransactionId": "string?", "sePayReferenceCode": "string?",
    "createdAt": "datetime", "expiresAt": "datetime", "paidAt": "datetime?"
  }
]

// GET /api/admin/payments/summary
{ "totalRevenue": 450000000, "revenueThisMonth": 45000000, "pendingAmount": 3000000, "completedOrders": 890, "activePaidSubscriptions": 890, "proIndividualSubscriptions": 700, "proCoachSubscriptions": 190 }

// GET /api/admin/subscriptions?tier=&active=
[ { "userId": "guid", "userName": "string", "userEmail": "string", "tier": "ProIndividual", "isActive": true, "expiresAt": "datetime?", "createdAt": "datetime" } ]
```

### AI Usage & Audit Logs
```json
// GET /api/admin/ai-usage/summary?periodStart=
{
  "periodStart": "date", "totalUsedRequests": 12000, "activeQuotaUsers": 400,
  "requestsByCurrentTier": [ { "label": "ProIndividual", "value": 9000 } ],
  "requestsByFeature": [ { "label": "insights", "value": 3000 } ],
  "topUsers": [ { "userId": "guid", "userName": "string", "userEmail": "string", "currentTier": "ProIndividual", "usedRequests": 88, "lastConsumedAt": "datetime?" } ]
}

// GET /api/admin/audit-logs?limit=100
[
  {
    "id": "guid", "adminUserId": "guid", "adminUserName": "string", "action": "string",
    "targetType": "string", "targetId": "guid?", "targetUserId": "guid?",
    "reason": "string", "beforeSummary": "string", "afterSummary": "string", "createdAt": "datetime"
  }
]
```

---

## Notes & Conventions

- **Pagination:** standard `pageNumber`/`pageSize` (or `page`/`pageSize`) query params; list responses are either a bare array or the `PagedResponse<T>` wrapper documented above — check the specific endpoint's table.
- **IDs:** all resource identifiers are `Guid`, serialized as lowercase-hyphenated UUID strings.
- **Dates:** `DateOnly` fields serialize as `"YYYY-MM-DD"`; `DateTime` fields serialize as ISO-8601 UTC (e.g. `"2026-07-01T14:30:00Z"`).
- **Enums:** serialize as their string name (e.g. `"Male"`, `"ProIndividual"`) unless a `[JsonConverter]` override says otherwise — verify against a live response if exact casing matters for your client.
- **File uploads:** always `multipart/form-data`, enforced size caps per endpoint (5MB avatar, 25MB documents/attachments).
- **AI endpoints** are gated behind `RequirePro` and the `Ai` rate-limit policy.
- **Coach-only actions** require both the `RequireProCoach` policy and an active `CoachClientRelationship` with the target client (checked in the handler, not just the attribute).
