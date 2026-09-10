# Coaching agreements

Implemented 2026-09-08; not deployed or migrated against a live database.

## Delivered behavior

- Coaches publish a terms snapshot through a three-step editor. A single-use code opens a read-only client preview; acceptance requires that exact agreement ID and explicit acknowledgment.
- Both participants can revisit `/relationships/:id`, including after ending or subscription expiry. Details include accepted versions, timestamps, responsibilities and lifecycle activity.
- Dates use Asia/Bangkok. The end date is inclusive. Future starts reserve the relationship without granting early access.
- Notice defaults to 7 days, configurable from 1 to 30 before acceptance. A client can independently start notice or end immediately without a reason, evidence, or coach approval. A coach can only request an ending; coaching continues until the client approves, then the agreed notice period begins. The client may decline the request.
- Renewal extends the existing scope through a separately accepted version; it does not change services or notice terms. Pending renewal never extends expired access. Ending rejects outstanding proposals.
- Existing connections remain explicitly legacy, without invented acceptance or new notice requirements. Coaches can propose terms for mutual acceptance. Old unused codes without terms cannot activate coaching.
- Plans and actual sets are retained with original authorship as read-only client history. Coach supplement prescriptions are archived, retaining intake records. Coach meal prescriptions and file shares are removed at finalization, with independent food logs retained. Effective access checks deny coach access and new prescription logging at the deadline even before finalization.
- Conversation text remains available to original participants, read-only after ending. New cross-participant attachment/file downloads are denied. Already issued presigned links and downloaded copies cannot be recalled.
- Lifecycle notifications are committed with the agreement event, delivered through SignalR and retried by the worker. The client deduplicates by notification ID, refreshes on reconnect, and links relationship events/messages to the exact agreement/conversation.

No new library or architecture replacement was introduced. The existing Mediator implementation is retained. Authenticated in-app acceptance is not represented as verified electronic signing. Coaching payments, penalties, automatic renewal, notice cancellation, editing published offers and new block/report workflows are not part of this release. Create a new offer for different terms; revoke an unused old invitation through the existing invitation management flow. Policy `coaching-v1` must remain immutable if later policy versions are introduced.

## API

`RelationshipsController` delegates to Application handlers. All routes require authentication; reads and mutations validate participation.

- `GET /api/relationships?page=1` and `GET /api/relationships/{id}`
- `POST /api/relationships/invitation-preview` with `code`
- `GET /api/relationships/{id}/ending-preview?immediate=false`
- `POST /api/relationships/{id}/ending` with `immediate`, `expectedRevision`, and ordinary preview timestamps `requestedAtUtc` / `effectiveAtUtc`
- `POST /api/relationships/{id}/ending-response` lets the client approve or decline a coach's ending request
- `POST /api/relationships/{id}/proposals` and `POST /api/relationships/{id}/proposals/{agreementId}/response`

Existing invite creation/redemption endpoints now require terms/acknowledgment and exact reviewed agreement ID respectively. Old direct acceptance/renewal controller routes return 410 rather than bypassing versioned consent. Stale revisions and known reservation conflicts return 409. Ordinary ending previews expire after five minutes.

## Database and rollout

Migration: `20260907155759_CoachingAgreements`. It adds agreement/event tables, concurrency metadata, notification delivery metadata and plan archival/provenance fields. The backfill associates old coach plans with their most relevant existing relationship and archives plans without a live relationship. It cannot recover records deleted before this change.

Back up and test the migration on a staging PostgreSQL copy before production. Deploy the coordinated frontend/backend release; old clients cannot satisfy the new consent contract. The existing API startup initializer can apply migrations, so do not point a test instance at production.

From the backend directory, using the intended environment configuration:

```powershell
dotnet ef database update --project src/Xenoh.Infrastructure --startup-project src/Xenoh.API --configuration Release
```

The finalization worker runs every 30 seconds after its startup delay. Access checks are independent of worker timing. Notification delivery is at-least-once; monitor `Agreement notification delivery deferred for retry` warnings. Offline clients recover persisted notifications through REST.

Do not use the migration's Down operation as a routine production rollback: it removes agreement audit data, and earlier application versions do not enforce the retention/access contract. Prefer a forward fix or an explicitly planned backup restore.

## Verification

- Backend Release test suite: 520 passed, 3 existing skips, 0 failures. Coverage includes coach ending requests, client approval/decline, client-only immediate ending, and pending connection declines.
- Frontend notification routing/reconciliation tests: 6 passed.
- Frontend production build passed; lint has 0 errors and 5 existing hook warnings. Existing bundle-size/SignalR annotation warnings remain.
- EF reports no pending model changes. Access-query SQL translation is tested using the PostgreSQL provider without connecting to a database.
- Both repositories pass `git diff --check` (line-ending warnings only).
- Browser checks use real React components with an isolated mock API adapter in frontend `tests/agreements-browser.tsx`. Verified client preview/acknowledgment/navigation, ordinary ending, safety confirmation, ended read-only state, and Vietnamese layout at 390px with no horizontal overflow or browser console errors. This is not live backend or multi-user SignalR verification. The full create wizard was not completed through browser automation because native date entry was not supported reliably by the browser-control adapter.

Still required before production: apply migration on staging PostgreSQL, verify simultaneous redemptions using independent database connections, and exercise two authenticated sessions for live notifications, reconnect recovery, expiry, retained history and revoked downloads. No live database mutation or deployment was performed during implementation.
