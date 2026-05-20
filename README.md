# Xenoh Backend API

Xenoh Backend is the ASP.NET Core API for the Xenoh training platform. It handles authentication, user profiles, workout plans, coach-client relationships, exercise data, progress tracking, payments, notifications, and other server-side workflows used by the frontend application.

The project follows Clean Architecture. Business rules stay in the inner layers, infrastructure concerns stay outside, and the API layer remains thin.

## Tech Stack

- .NET Core / ASP.NET Core
- PostgreSQL
- Entity Framework Core
- ASP.NET Core Identity
- JWT authentication
- Mediator/CQRS request handling
- Mapster object mapping
- Redis when caching or distributed coordination is justified
- SignalR when real-time updates are required

## Architecture

The backend is organized into four main layers:

- `Xenoh.Domain` - pure domain entities, enums, value objects, and core business concepts.
- `Xenoh.Application` - application business logic, commands, queries, DTOs, validators, interfaces, and use cases.
- `Xenoh.Infrastructure` - database access, repositories, Identity persistence, external service integrations, and implementation details.
- `Xenoh.API` - HTTP controllers, middleware, authentication setup, dependency injection, and API entry point.

Dependency direction:

```text
API -> Application -> Domain
API -> Infrastructure -> Application -> Domain
```

The domain layer must not depend on Entity Framework, ASP.NET Core, Identity, or any external framework.

## Repository Structure

```text
Xenoh_be/
  src/
    Xenoh.API/              ASP.NET Core API host
    Xenoh.Application/      CQRS handlers, DTOs, application services
    Xenoh.Domain/           Domain model and business concepts
    Xenoh.Infrastructure/   EF Core, repositories, external integrations
  tests/
    Xenoh.Application.Tests/
  Xenoh.slnx
```

## Prerequisites

- .NET SDK compatible with the solution target framework
- PostgreSQL
- EF Core CLI tools
- Git

Install EF Core tools if they are not already available:

```powershell
dotnet tool install --global dotnet-ef
```

## Configuration

The public configuration file contains safe placeholders only:

```text
src/Xenoh.API/appsettings.json
```

For local development, create a private development config from the example file:

```powershell
Copy-Item src/Xenoh.API/appsettings.Development.example.json src/Xenoh.API/appsettings.Development.json
```

Then update `appsettings.Development.json` with local values:

- PostgreSQL connection string
- JWT issuer, audience, and signing key
- SMTP credentials
- OAuth client settings
- Payment provider settings
- AI provider settings, if used

Never commit real secrets. `appsettings.Development.json`, local environment files, logs, build output, and local artifacts are ignored by Git.

For production, use environment variables or the deployment platform's secret manager. Do not store production credentials in source control.

## Database Setup

Create a PostgreSQL database for local development, then apply EF Core migrations:

```powershell
dotnet restore
dotnet ef database update --project src/Xenoh.Infrastructure --startup-project src/Xenoh.API
```

If the API cannot connect to the database, confirm:

- PostgreSQL is running.
- The configured database exists.
- The connection string in `appsettings.Development.json` is correct.
- The database user has permission to create and update schema objects.

## Run Locally

From the backend repository root:

```powershell
dotnet restore
dotnet build Xenoh.slnx
dotnet run --project src/Xenoh.API
```

Default local API URLs are configured by the API launch settings and may include:

```text
http://localhost:5293
https://localhost:7017
```

Use the HTTPS URL when connecting from the frontend if local certificates are configured.

## Tests

Run all backend tests:

```powershell
dotnet test
```

Run a specific test project:

```powershell
dotnet test tests/Xenoh.Application.Tests
```

## API Development Rules

- Keep controllers thin.
- Put business logic in the Application layer.
- Use commands and queries for use cases.
- Use Mapster for object mapping where practical.
- Use repositories and infrastructure services through Application interfaces.
- Use `async` and `await` for I/O operations.
- Use `AsNoTracking()` for read-only EF Core queries.
- Avoid N+1 queries by shaping queries intentionally.
- Do not introduce new infrastructure dependencies unless they are justified by the requirement.

## Authentication

The API uses ASP.NET Core Identity with JWT authentication.

Typical authentication flow:

1. User registers or signs in.
2. API validates credentials through Identity.
3. API issues JWT access credentials.
4. Frontend sends the token with protected API requests.
5. API validates authorization through roles, policies, or user ownership rules.

Keep token signing keys private and rotate them if they are ever exposed.

## Security Checklist Before Publishing

Before making the repository public or deploying:

- Confirm no real secrets are tracked by Git.
- Confirm local config files are ignored.
- Rotate any credential that was ever committed, shared, logged, or pasted into a tool.
- Keep API keys server-side only.
- Use environment variables or a secret manager in production.
- Review CORS origins before deployment.
- Review authentication and authorization policies for protected endpoints.
- Do not expose internal exception details in production.

Useful checks:

```powershell
git status --short
git grep -n "sk-" -- .
git grep -n "password" -- .
git grep -n "secret" -- .
```

Manual review is still required. Automated text search can miss encoded, renamed, or indirect secrets.

## Deployment Notes

For production deployments:

- Build from a clean checkout.
- Supply production configuration through environment variables or secret storage.
- Run database migrations intentionally as part of the release process.
- Enable HTTPS.
- Configure CORS for the production frontend origin.
- Store logs outside the repository.
- Monitor authentication failures, payment callbacks, and background job errors.

## License

No license has been specified yet. Add a license before publishing if you want others to know how they may use the code.
