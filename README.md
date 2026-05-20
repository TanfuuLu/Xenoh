# Xenoh

Training journal for lifters. Coaching workspace for coaches.

Xenoh is a web app where athletes follow training plans, log workouts, track progress, manage nutrition, and work with coaches in one shared system.

![Xenoh home page](docs/images/real/home.png)

## Product Highlights

- Personal dashboard with today's workout, active plan, nutrition, next actions, streak, bodyweight, BMI, DOTS, and XP.
- Plan timeline with weekly progress, compliance status, comments, and coach-client discussion.
- Workout logger for sets, reps, weight, RPE, volume, rest time, PRs, AI suggestions, and completion tracking.
- Progress analytics with training score, sessions, total volume, compliance, recommendations, and muscle-group heatmaps.
- Coach profile with pricing, contract state, rating, report/block actions, and connection status.
- Coach client dashboard with active clients, inactive clients, attention flags, missing plans, schedule timeline, and client performance cards.
- Client detail page for coaches with body stats, plan progress, custom exercises, nutrition, and AI analysis.
- AI insights for workout suggestions, plan balance checks, client risk/opportunity summaries, and plan analysis.

## MVP Scope

| Area | Features |
| --- | --- |
| Public website | Home, about, pricing, FAQ, Vietnamese/English language switch |
| Auth | Register, login, JWT access token, refresh token cookie |
| Athlete dashboard | Daily summary, training metrics, active plan, nutrition, next actions |
| Plans | Active plan, weekly timeline, plan comments, balance check |
| Workout logging | Exercise list, set completion, reps, weight, RPE, volume, timers, PR tracking |
| Progress | Compliance, training score, total volume, heatmaps, recommendations |
| Nutrition | Daily nutrition overview and food logging |
| Coach marketplace | Coach profile, pricing, contracts, reviews, report/block |
| Coach workspace | Client roster, schedule, client status flags, client performance overview |
| AI | Workout suggestions, client insight, plan balance, plan analysis |
| Billing | Subscription and coach billing support |

## User Roles

### Individual

Individuals can train with their own plan, receive a coach-authored plan, complete daily workouts, record performance, review progress, and manage nutrition.

### Coach

Coaches can manage clients, assign plans, monitor progress, review client stats, write custom exercises, inspect nutrition, and use AI insight to spot risks or opportunities.

## Public Pages

### About Page

![Xenoh about page](docs/images/real/about.png)

### Login

![Xenoh login page](docs/images/real/login.png)

### Register

![Xenoh register page](docs/images/real/register.png)

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
