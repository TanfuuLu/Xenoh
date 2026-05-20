# Xenoh

Training journal for lifters. Client management for coaches.

Xenoh helps users plan workouts, log sessions, track progress, and share training plans between coaches and clients. The screenshots below are captured from the real running website.

![Xenoh home page](docs/images/real/home.png)

## Preview

### About Page

![Xenoh about page](docs/images/real/about.png)

### Login

![Xenoh login page](docs/images/real/login.png)

### Register

![Xenoh register page](docs/images/real/register.png)

## What Xenoh Does

- Build training plans.
- Log sets, reps, weight, and completed work.
- Track volume, streaks, progress, and personal records.
- Let individuals train without a coach.
- Let coaches assign plans and monitor clients.
- Keep coach-client training history in one shared place.
- Use AI to review plans, suggest training changes, and summarize client progress.

## MVP Features

| Area | MVP |
| --- | --- |
| Public site | Landing page, pricing, FAQ, about page |
| Auth | Register, login, JWT-protected API |
| Dashboard | Daily overview, streak, bodyweight, BMI, DOTS, XP, active plan |
| Training | Plans, weekly timeline, workout logging, RPE, volume, PR tracking |
| Progress | Training score, sessions, total volume, compliance, heatmaps |
| Nutrition | Daily nutrition overview and food logging |
| Coach tools | Client roster, schedule view, client profiles, plan assignment |
| AI | AI suggestions, balance check, client insight, plan analysis |
| Payments | Coach billing support |
| Backend | Clean Architecture ASP.NET Core API |

## App Features

### Individual Training

- Personal dashboard with streak, bodyweight, BMI, DOTS, XP, today's workout, nutrition, and next actions.
- Active plan overview with week-by-week progress and completion status.
- Workout session screen for logging sets, reps, weight, RPE, rest time, calories, and volume.
- Exercise library and custom exercise support.
- Progress analytics with training score, compliance, total volume, muscle-group heatmaps, and body-part distribution.

### Coach And Client

- Coach profile with pricing, contract status, reviews, reports, and block controls.
- Coach client dashboard with active clients, attention flags, missing plans, inactive clients, and schedule timeline.
- Client profile for coaches with body stats, plan progress, custom exercises, nutrition, and AI analysis.
- Shared plan comments so coach and client can communicate inside the training context.

### AI Features

- AI workout suggestions from the dashboard and workout screens.
- Plan balance check for weekly programming quality.
- AI client insight with risks, opportunities, and progress summary.
- AI plan analysis with mistakes to fix, suggested changes, completion metrics, workload, time, and RPE.

## Backend Stack

- ASP.NET Core
- PostgreSQL
- Entity Framework Core
- ASP.NET Core Identity + JWT
- CQRS with Mediator
- Mapster
- Clean Architecture

## Project Structure

```text
src/
  Xenoh.API
  Xenoh.Application
  Xenoh.Domain
  Xenoh.Infrastructure
tests/
  Xenoh.Application.Tests
```

<details>
<summary>Run the backend locally</summary>

### Prerequisites

- .NET SDK compatible with the solution target framework
- PostgreSQL
- EF Core CLI

```powershell
dotnet tool install --global dotnet-ef
```

### Configure

Create a private local config:

```powershell
Copy-Item src/Xenoh.API/appsettings.Development.example.json src/Xenoh.API/appsettings.Development.json
```

Update:

- PostgreSQL connection string
- JWT settings
- SMTP settings
- OAuth settings
- Payment settings
- AI provider settings, if used

Do not commit real secrets.

### Database

```powershell
dotnet restore
dotnet ef database update --project src/Xenoh.Infrastructure --startup-project src/Xenoh.API
```

### Start API

```powershell
dotnet run --project src/Xenoh.API
```

Local URLs may include:

```text
http://localhost:5293
https://localhost:7017
```

</details>

<details>
<summary>Development notes</summary>

- Keep controllers thin.
- Put business logic in `Xenoh.Application`.
- Keep `Xenoh.Domain` pure.
- Use repositories through Application interfaces.
- Use Mapster for mapping.
- Use async I/O.
- Use `AsNoTracking()` for read-only EF queries.
- Keep secrets in local config or deployment secret storage.

Run tests:

```powershell
dotnet test
```

</details>

## Security

Public config files contain placeholders only. Local secrets are ignored by Git.

Before publishing or deploying:

```powershell
git status --short
git grep -n "password" -- .
git grep -n "secret" -- .
git grep -n "sk-" -- .
```

Rotate any credential that was ever exposed.

## License

No license has been specified yet.
