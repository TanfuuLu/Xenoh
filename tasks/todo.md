# Task Backlog: Review and Security Remediation

## Task 1: Reject undefined challenge enum values

**Status:** Complete (automated verification passed).

**Description:** Harden the API and Application boundaries so numeric or otherwise undefined values for metric type, access type, and selected lifts cannot enter challenge business logic.

**Acceptance criteria:**

- [ ] JSON enum handling rejects integer enum tokens for HTTP requests.
- [ ] `FitnessChallengeRules.ValidateInput` rejects every undefined metric, access, and lift value, including values supplied by non-HTTP callers.
- [ ] Focused tests cover invalid numeric metric/access values, invalid selected lifts, and valid string enum payloads.

**Verification:**

- [ ] Tests pass: `dotnet test Xenoh.slnx --no-restore --filter "FullyQualifiedName~FitnessChallengeTests|FullyQualifiedName~SecurityHardeningTests"`
- [ ] Build succeeds: `dotnet build Xenoh.slnx --no-restore`
- [ ] Manual check: create/update requests with integer enum tokens return a validation response and do not persist a challenge.

**Dependencies:** None

**Files likely touched:**

- `src/Xenoh.API/Program.cs`
- `src/Xenoh.Application/Features/FitnessChallenges/FitnessChallengeRules.cs`
- `tests/Xenoh.Application.Tests/FitnessChallengeTests.cs`
- `tests/Xenoh.Application.Tests/Infrastructure/SecurityHardeningTests.cs`

**Estimated scope:** Medium

## Task 2: Make challenge schedule submission timezone-correct

**Status:** Complete (automated verification passed; cross-browser manual check pending).

**Description:** Replace browser-local UTC conversion with a contract that submits timezone-local wall-clock values and lets the backend perform authoritative conversion using the selected IANA timezone.

**Acceptance criteria:**

- [ ] Create/update input accepts `startsAtLocal`, `endsAtLocal`, and `timeZoneId`; responses continue returning `startsAtUtc` and `endsAtUtc`.
- [ ] Backend conversion produces the same UTC instants regardless of the browser/system timezone and rejects unknown, invalid, or ambiguous local times.
- [ ] Frontend create/edit flows preserve and display the selected timezone without calling `new Date(localString).toISOString()` for request construction.

**Verification:**

- [ ] Tests pass: `dotnet test Xenoh.slnx --no-restore --filter "FullyQualifiedName~FitnessChallengeTests"`
- [ ] Frontend lint passes: `npm run lint`
- [ ] Frontend build succeeds: `npm run build`
- [ ] Manual check: submit the same Bangkok wall-clock schedule from a Bangkok browser and a Los Angeles browser; stored UTC values match.

**Dependencies:** None

**Files likely touched:**

- `src/Xenoh.Application/Features/FitnessChallenges/FitnessChallengeContracts.cs`
- `src/Xenoh.Application/Features/FitnessChallenges/FitnessChallengeRules.cs`
- `tests/Xenoh.Application.Tests/FitnessChallengeTests.cs`
- `../Xenoh_fe/src/features/community/types.ts`
- `../Xenoh_fe/src/features/community/pages/CommunityChallengesPage.tsx`

**Estimated scope:** Medium

## Task 3: Correct week boundaries and partial-week targets

**Status:** Complete (automated verification passed).

**Description:** Make challenge progress use a half-open local-date range and prorate the final partial week so an end boundary does not create an extra full-target bucket.

**Acceptance criteria:**

- [ ] An exact 14-local-day challenge produces exactly two seven-day buckets.
- [ ] End-local-date activity is excluded from scoring, while the preceding local date remains included.
- [ ] A partial final bucket uses `ceiling(target × coveredDays / 7)` and tests cover one-day, multi-day, exact-week, and timezone-boundary cases.

**Verification:**

- [ ] Tests pass: `dotnet test Xenoh.slnx --no-restore --filter "FullyQualifiedName~FitnessChallengeTests"`
- [ ] Build succeeds: `dotnet build Xenoh.slnx --no-restore`
- [ ] Manual check: progress responses show correct week counts and targets for 7-, 10-, and 14-day schedules.

**Dependencies:** Task 2

**Files likely touched:**

- `src/Xenoh.Application/Features/FitnessChallenges/FitnessChallengeRules.cs`
- `src/Xenoh.Application/Features/FitnessChallenges/FitnessChallengeMapping.cs`
- `tests/Xenoh.Application.Tests/FitnessChallengeTests.cs`

**Estimated scope:** Medium

## Task 4: Narrow the raw-SQL security-test exception

**Status:** Complete (RED/GREEN guardrail verification passed).

**Description:** Replace the file-wide `DatabaseInitializer.cs` allowance with a narrowly scoped, reviewable exception for the existing trusted seed operation.

**Acceptance criteria:**

- [ ] The source scan fails if a second raw-SQL call is added anywhere in `DatabaseInitializer.cs`.
- [ ] The currently reviewed seed execution remains allowed by an exact marker, method, or isolated trusted executor.
- [ ] The test message explains how to review and intentionally update the exception.

**Verification:**

- [ ] Tests pass: `dotnet test Xenoh.slnx --no-restore --filter "FullyQualifiedName~SecurityHardeningTests"`
- [ ] Manual check: temporarily introducing a second raw-SQL token makes the focused test fail; remove the temporary change afterward.

**Dependencies:** None

**Files likely touched:**

- `tests/Xenoh.Application.Tests/Infrastructure/SecurityHardeningTests.cs`
- `src/Xenoh.Infrastructure/Persistence/Seeders/DatabaseInitializer.cs`

**Estimated scope:** Small

## Task 5: Aggregate admin activity trends in PostgreSQL

**Status:** Complete (PostgreSQL SQL-shape test confirms server-side grouping).

**Description:** Change workout and website activity trends to return monthly aggregate rows from the database rather than materializing six months of event dates in application memory.

**Acceptance criteria:**

- [ ] Workout and website trend queries execute server-side grouping and return at most six aggregate rows per series.
- [ ] Missing months are filled with zero without changing labels, ordering, or response contracts.
- [ ] Relational tests cover sparse months, year boundaries, and empty data.

**Verification:**

- [ ] Tests pass: `dotnet test Xenoh.slnx --no-restore --filter "FullyQualifiedName~Admin"`
- [ ] Build succeeds: `dotnet build Xenoh.slnx --no-restore`
- [ ] Manual check: inspect generated SQL or EF logging and confirm grouping/counting occurs in PostgreSQL.

**Dependencies:** None

**Files likely touched:**

- `src/Xenoh.Application/Features/Admin/AdminQueries.cs`
- `tests/Xenoh.Application.Tests/Features/Admin/AdminQueryTests.cs`

**Estimated scope:** Small

## Task 6: Upgrade the vulnerable backend OpenAPI dependency path

**Status:** Complete (backend vulnerability scan reports no vulnerable packages).

**Description:** Move the transitive `Microsoft.OpenApi` dependency to a patched compatible version while preserving development-only API-document generation.

**Acceptance criteria:**

- [ ] `Microsoft.OpenApi` resolves to a patched version compatible with the current .NET 10 OpenAPI stack.
- [ ] Development OpenAPI document generation still succeeds.
- [ ] The backend vulnerability scan no longer reports the high-severity advisory, or a time-bounded exception documents why no compatible resolution exists.

**Verification:**

- [ ] Audit passes: `dotnet list Xenoh.slnx package --vulnerable --include-transitive`
- [ ] Tests pass: `dotnet test Xenoh.slnx --no-restore`
- [ ] Build succeeds: `dotnet build Xenoh.slnx --no-restore`
- [ ] Manual check: run the API in Development and load the generated OpenAPI document.

**Dependencies:** None

**Files likely touched:**

- `src/Xenoh.API/Xenoh.API.csproj`

**Estimated scope:** Small

## Task 7: Apply compatible frontend dependency fixes

**Status:** Complete with documented exception. The lockfile and dry-run install are verified; replacing the installed dependency tree is deferred while the user's Vite server holds package files open.

**Description:** Update direct and transitive frontend packages to compatible patched releases, then document any advisory that remains because it is unreachable or requires a breaking major upgrade.

**Acceptance criteria:**

- [ ] Compatible fixes are applied for Cloudflare tooling, `brace-expansion`, `miniflare`, `postcss`, `sharp`, `undici`, `wrangler`, and related transitive packages.
- [ ] Any remaining React Router advisory includes reachability analysis, owner, target version, and review date.
- [ ] No new runtime dependency or replacement library is introduced.

**Verification:**

- [ ] Audit reviewed: `npm audit --json`
- [ ] Frontend lint passes: `npm run lint`
- [ ] Frontend build succeeds: `npm run build`
- [ ] Manual check: `npm run preview` starts and primary routes load without console errors.

**Dependencies:** None

**Files likely touched:**

- `../Xenoh_fe/package.json`
- `../Xenoh_fe/package-lock.json`
- `../Xenoh_fe/SECURITY.md`

**Estimated scope:** Medium

## Task 8: Run full regression and security verification

**Status:** Automated verification complete. Browser-level manual verification is pending because no browser-control tool is available.

**Description:** Verify the combined remediation across both repositories and record any residual risk without modifying unrelated work.

**Acceptance criteria:**

- [ ] All backend tests pass with the existing expected skips only.
- [ ] Frontend lint/build and backend migration SQL generation succeed.
- [ ] Dependency scans, secret checks, and diff checks have no untriaged high-severity result.

**Verification:**

- [ ] Backend: `dotnet test Xenoh.slnx --no-restore`
- [ ] Frontend: `npm run lint`
- [ ] Frontend: `npm run build`
- [ ] Migration: `dotnet ef migrations script --project src/Xenoh.Infrastructure --startup-project src/Xenoh.API --idempotent --no-build`
- [ ] Diff hygiene: `git diff --check` in both repositories.
- [ ] Manual check: create, edit, join, score, and view a challenge using a selected timezone different from the browser timezone.

**Dependencies:** Tasks 1-7

**Files likely touched:**

- `tasks/plan.md`
- `tasks/todo.md`

**Estimated scope:** Small

## Task 9: Rotate credentials and clean sensitive artifacts

**Status:** Pending explicit operational authorization; no credentials, sessions, artifacts, or keys were changed.

**Description:** After explicit authorization and coordination with credential owners, rotate exposed or potentially exposed credentials, revoke affected sessions, remove approved generated copies and token-bearing logs, and replace or relocate the workspace private key.

**Acceptance criteria:**

- [ ] Credential and session rotation is confirmed before local copies are removed.
- [ ] Every deletion target is resolved to an explicit path under the approved workspace/artifact scope and recoverability is documented.
- [ ] A final scan finds no development secrets, access-token query strings, or private-key material in the agreed scope.

**Verification:**

- [ ] Manual check: credential owners confirm new credentials work and old credentials/tokens fail.
- [ ] Manual check: approved `artifacts` copies and runtime logs no longer contain the identified values.
- [ ] Manual check: `xenoh-be.pem` is replaced or relocated to an approved protected location.
- [ ] Documentation: update `E:\Xenoh\SECURITY-ROTATION.md` with completion evidence that does not include secret values.

**Dependencies:** Explicit operational approval; independent of code Tasks 1-8

**Files likely touched:**

- `E:\Xenoh\SECURITY-ROTATION.md`
- Explicitly approved generated artifact paths only
- Explicitly approved private-key location only

**Estimated scope:** Medium
