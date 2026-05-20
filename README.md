# Xenoh

Training journal for lifters. Coaching workspace for coaches. AI-assisted insight for both.

Xenoh is a web app where athletes follow training plans, log workouts, track progress, manage nutrition, and work with coaches in one shared system. The product is built around the real training workflow shown in the app screens: open the dashboard, continue today's workout, review the plan, log every set, check progress, and let coaches manage clients from the same data.

![Xenoh home page](docs/images/real/home.png)

## What You Can See In The App

The 9 product screens show these core experiences:

| Screen | What it shows |
| --- | --- |
| Athlete dashboard | Daily training summary, streak, bodyweight, BMI, DOTS, XP, active plan, nutrition, and next actions |
| Plan timeline | Current plan, week cards, completion status, warnings, balance check, and coach-client comments |
| Workout session | Exercise-by-exercise logging with sets, reps, weight, RPE, volume, time, PR badges, AI suggestions, and completion controls |
| Progress analytics | Training score, total sessions, total volume, compliance, recommendations, weekly muscle heatmaps, and body heatmap |
| Coach profile | Coach bio, pricing, contract status, reviews, connected clients, report/block actions, and rating form |
| Coach client roster | Active clients, inactive clients, missing plans, attention flags, schedule timeline, and client performance cards |
| Client detail | Client body stats, plan progress, custom exercises, nutrition access, and coach-side plan creation |
| AI client insight | Client summary, progress notes, risks, opportunities, and refreshable AI analysis |
| AI plan analysis | Plan balance review, mistakes to fix, suggestions, total weeks, completion, muscle groups, total time, and average RPE |

## Core Product Flow

1. An athlete signs in and lands on a dashboard with today's training state.
2. The athlete opens the active plan and sees weekly progress, current week, warnings, and comments.
3. The athlete logs a workout set by set, including reps, weight, RPE, time, volume, and completion.
4. Progress pages turn the logged data into compliance, volume, training score, recommendations, and heatmaps.
5. A coach can connect with clients, track their training, assign plans, and inspect client details.
6. AI features help review workouts, analyze plan balance, and summarize client risks or opportunities.

## MVP Scope

| Area | Features |
| --- | --- |
| Public website | Home, about, pricing, FAQ, Vietnamese/English language switch, login/register |
| Auth | Register, login, JWT access token, refresh token cookie |
| Athlete dashboard | Daily summary, streak, bodyweight, BMI, DOTS, XP, active plan, nutrition, next actions |
| Plans | Active plan, weekly timeline, current week marker, completion percent, warnings, plan comments |
| Workout logging | Exercise list, set completion, reps, weight, RPE, volume, timers, PR tracking, mark workout done |
| Progress | Training score, session count, total volume, compliance, recommendations, weekly heatmaps |
| Nutrition | Daily calories, nutrition target, food logging, coach access to client nutrition |
| Coach profile | Bio, rating, pricing, contract, connected status, report, block, review submission |
| Coach workspace | Client roster, schedule timeline, active/inactive flags, missing-plan flags, client performance |
| Client management | Client stats, plan progress, custom exercises, nutrition, AI analysis, create plan |
| AI | Workout suggestions, balance check, client insight, plan analysis, risks and opportunities |
| Billing | Subscription and coach billing support |

## User Roles

### Individual

Individuals can train with their own plan or a coach-authored plan. They can continue today's workout, log completed sets, watch plan progress move forward, review heatmaps, and track nutrition from the same sidebar.

### Coach

Coaches can manage client rosters, see who needs attention, inspect each client's stats, create or assign plans, write custom exercises, review nutrition, and use AI insight to identify risks and opportunities.

## Feature Summary

### Athlete Dashboard

The dashboard is the athlete's daily command center. It shows the current date, a welcome message, streak, bodyweight, BMI, DOTS score, XP progress, today's workout progress, active plan progress, nutrition, and next recommended actions.

### Plan Management

The plan page shows the full training block as weekly cards. Each week includes completion progress, current-week status, warnings when training is below target, completed weeks, comments, and a balance check action for reviewing plan quality.

### Workout Logging

The workout page focuses on execution. Each exercise contains prescribed work, target load, muscle group, time, calories, completed sets, editable set inputs, RPE, PR context, total volume, total time, and a mark-done flow.

### Progress Analytics

Progress screens turn training history into readable analytics: training score, number of sessions, total volume, compliance percentage, weekly muscle-group heatmap, body heatmap, and practical recommendations when volume drops or compliance is uneven.

### Coach Profile And Relationship

The coach profile shows coach identity, connection status, bio, experience, monthly and per-session pricing, contract details, connected client count, rating, review submission, report, block, and disconnect actions.

### Coach Client Management

The coach workspace shows active clients, clients needing attention, clients without plans, inactive clients, a schedule timeline, and client cards with contract dates, plan progress, last workout, bodyweight, and Big 3 PRs.

### Client Detail For Coaches

The client detail page gives coaches a client-level view: streak, bodyweight, BMI, DOTS, height, gender, birthday, plan progress, assigned plans, custom exercises, nutrition access, and AI analysis.

### AI Insights

AI pages summarize client progress, risks, and opportunities. Plan analysis reviews structure, completion, total weeks, muscle groups, total time, average RPE, mistakes to fix, and suggestions for better balance.

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
