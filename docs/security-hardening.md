# Security Hardening Notes

## Identity Boundary Debt

`Xenoh.Domain.Entities.ApplicationUser` currently inherits from `IdentityUser<Guid>`. That keeps the current Identity integration stable, but it means the Domain project still has an ASP.NET Core Identity dependency.

Do not broaden this dependency. A future architecture cleanup should move the Identity-specific user type to Infrastructure and keep a pure domain user/profile model in Domain. That refactor should be handled separately because many entities currently navigate directly to `ApplicationUser`.

## SQL Access Rule

Application code should continue using EF Core LINQ and repository methods. Raw SQL APIs such as `FromSqlRaw`, `ExecuteSqlRaw`, `SqlQueryRaw`, `NpgsqlCommand`, `DbCommand`, and direct `CommandText` are blocked by tests outside EF migrations unless a deliberate allowlist is added with review.
