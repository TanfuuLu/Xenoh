# Implementation Plan: Review and Security Remediation

## Status

Code remediation implemented and automated verification complete on 2026-07-29. Browser-level manual checks remain pending because no browser-control tool is available, and the active frontend development server prevented replacing the installed `node_modules` tree. Task 9 remains unapproved and was not performed.

## Overview

Remediate the confirmed correctness, security, performance, and dependency findings from the current flexible community-challenge changes across `Xenoh_be` and `Xenoh_fe`. The work preserves Clean Architecture, MediatR CQRS, EF Core, PostgreSQL, Mapster, and the existing React/TanStack Query stack. Operational credential rotation and workspace cleanup remain a separately authorized activity because they affect external credentials, sessions, and local artifacts.

## Scope

Included:

- Reject undefined challenge enum values at the API/application boundary.
- Make selected-timezone scheduling deterministic across browsers and daylight-saving transitions.
- Correct challenge week construction and partial-week targets.
- Aggregate six-month admin activity trends in PostgreSQL instead of materializing every event.
- Narrow the raw-SQL security-test exception.
- Remediate currently reported backend and frontend dependency advisories with compatible versions.
- Re-run focused and full quality/security verification.

Separately authorized:

- Rotate exposed or potentially exposed credentials.
- Revoke affected sessions/tokens.
- Remove generated artifact copies containing development configuration or access tokens.
- Relocate or replace the workspace private key.

Excluded:

- New product features or UI redesign.
- Architecture or technology-stack changes.
- Unrelated lint warnings and unrelated modified files.
- Destructive cleanup or external credential changes without explicit approval.

## Architecture Decisions

- Keep challenge responses in UTC, but accept schedule input as timezone-local wall-clock values plus an IANA `timeZoneId`. Convert and validate in the Application layer with `TimeZoneInfo`; reject invalid or ambiguous local times rather than silently choosing an offset.
- Treat the scoring date range as half-open: `[local start date, local end date)`. Lifecycle status continues to use exact UTC instants.
- Permit a final partial week. Its target is prorated as `ceiling(weekly target × covered days / 7)`, preventing a one-day tail from receiving a full weekly target.
- Validate enums explicitly with `Enum.IsDefined` in application rules even if JSON serializer settings are tightened. This protects non-HTTP callers and keeps business invariants in the Application layer.
- Use provider-translatable EF Core `GroupBy` projections for admin monthly counts. Fill missing months in memory only after the database returns at most six grouped rows.
- Keep raw SQL isolated and trusted. The source-security test may allow only the specific reviewed seed execution site, not the entire initializer file.
- Apply compatible dependency updates only; do not add replacement libraries or adopt breaking major versions as part of this remediation.

## Dependency Graph

```text
Task 1: Enum boundary hardening

Task 2: Timezone-local schedule contract
    |
    +--> Task 3: Half-open week and partial-target semantics

Task 4: Raw-SQL test guardrail

Task 5: Database-side admin trend aggregation

Task 6: Backend dependency remediation
Task 7: Frontend dependency remediation

Tasks 1-7
    |
    +--> Task 8: Full regression and security verification

Explicit operational approval
    |
    +--> Task 9: Credential rotation and artifact cleanup
```

## Task List

### Phase 1: Fail-Fast Correctness and Security Boundaries

- [x] Task 1: Reject undefined challenge enum values.
- [x] Task 2: Make challenge schedule submission timezone-correct.
- [x] Task 3: Correct week boundaries and partial-week targets.

### Checkpoint: Challenge Boundary

- [x] Focused challenge tests pass.
- [x] Backend build succeeds.
- [x] Frontend lint and build succeed.
- [ ] Manual scheduling checks cover browser timezone different from selected timezone.
- [ ] Human review confirms schedule and partial-week semantics.

### Phase 2: Query and Guardrail Hardening

- [x] Task 4: Narrow the raw-SQL security-test exception.
- [x] Task 5: Aggregate admin trends in PostgreSQL.

### Checkpoint: Backend Hardening

- [x] Focused security and admin tests pass.
- [x] Generated SQL confirms server-side aggregation.
- [x] Backend solution tests pass.

### Phase 3: Dependency Remediation

- [x] Task 6: Upgrade the vulnerable backend OpenAPI dependency path.
- [x] Task 7: Apply compatible frontend dependency fixes.

### Checkpoint: Dependency Health

- [x] Backend vulnerability scan has no unresolved reachable high-severity advisory.
- [x] Frontend audit findings are fixed or documented with reachability and upgrade constraints.
- [x] Backend tests and frontend lint/build pass after lockfile changes.

### Phase 4: Release Verification

- [x] Task 8: Run complete automated regression, migration, and diff checks.

### Checkpoint: Code Complete

- [x] All automated task acceptance criteria are met.
- [x] No new compiler, linter, or test failures exist.
- [x] Migration SQL generation succeeds.
- [x] Changed code receives a final review.

### Phase 5: Separately Authorized Operations

- [ ] Task 9: Rotate credentials, revoke affected sessions, and clean sensitive artifacts.

### Checkpoint: Operational Closure

- [ ] Credential owners confirm rotation/revocation.
- [ ] Sensitive generated copies and runtime-token logs are removed from approved targets.
- [ ] Private-key replacement or relocation is confirmed.
- [ ] A final secret scan finds no remaining exposed values in the agreed scope.

## Definition of Done

Every code task is complete only when:

- Its acceptance criteria and focused verification pass.
- The relevant repository builds successfully.
- New or changed behavior has regression coverage at the closest practical layer.
- Authentication, authorization, input validation, logging, and data exposure are reviewed where applicable.
- No unrelated user changes are overwritten.
- No new library is introduced without explicit rationale.
- The final diff contains no whitespace errors, generated secrets, access tokens, or private keys.
- Documentation and dependency-audit exceptions state owners and follow-up dates.

## Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Changing schedule input breaks existing clients | High | Change backend and frontend contracts in one vertical slice; retain UTC response fields and test create/update round trips. |
| DST gaps or overlaps produce silent time shifts | High | Reject invalid and ambiguous local wall-clock values with a clear validation error. |
| Partial-week semantics differ from product intent | Medium | Treat the stated prorating rule as an approval gate before implementation. |
| EF Core cannot translate the desired month grouping | Medium | Add a relational integration test and inspect generated SQL; use a PostgreSQL-specific expression only within Infrastructure if required. |
| Dependency fixes introduce breaking behavior | Medium | Use compatible upgrades first, review lockfile deltas, and run full build/test checks after each ecosystem update. |
| Cleanup destroys evidence or valid local configuration | High | Rotate first, preserve an approved audit record, resolve exact paths, and obtain explicit authorization before deletion. |
| Large dirty worktrees cause accidental overlap | High | Touch only listed files, inspect diffs before each change, and never reset or discard unrelated modifications. |

## Remaining Approval

- Confirm whether Task 9 is authorized or should remain a handoff checklist for credential owners.

## Planning Verification

- [x] Every task has acceptance criteria and verification steps in `tasks/todo.md`.
- [x] Dependencies are identified and ordered.
- [x] Tasks are sized XS, S, or M.
- [x] Checkpoints exist after each major phase.
- [ ] Human has reviewed the completed remediation and approved any remaining manual/operational work.
