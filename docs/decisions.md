# Architecture Decisions

Lightweight ADR-style record: what was decided, why, and what was rejected instead. These are real decisions made across the project's history (see [`workflow-log.md`](workflow-log.md) for the dated trail each one came from) — this file collects and organizes them by topic rather than by date.

## Clean Architecture (Domain/Application/Infrastructure/Api)

- **Decision:** four projects, dependencies point inward only; Domain has zero external references.
- **Reason:** business rules stay testable and framework-independent; swapping EF Core or the web framework later would touch Infrastructure/Api, not Domain/Application.
- **Rejected:** a single-project/layered-folders approach — faster to start, but nothing stops a controller from reaching straight into EF Core, which is exactly the coupling this avoids.

## Repository pattern + interfaces owned by Application

- **Decision:** `IReservationRepository` etc. are defined in Application, implemented in Infrastructure.
- **Reason:** Dependency Inversion — Application depends on an abstraction it owns, not on EF Core; use cases stay unit-testable with a mock.

## DTOs at the API boundary, never raw entities

- **Decision:** controllers accept/return DTOs (`CreateReservationRequest`, `ReservationDto`, …), never Domain entities directly.
- **Reason:** decouples the wire contract from internal modeling — an entity can gain a field without breaking clients, and entities never need JSON-serialization concerns.

## `IdentityUser` and `Customer` are separate rows

- **Decision:** ASP.NET Core Identity's `IdentityUser` (login/password/roles) and the domain `Customer` (name/email/business data) are linked via `Customer.IdentityUserId`, not merged into one type.
- **Reason:** auth identity and business entity are different concerns with different lifecycles; keeps Identity's framework-specific shape out of the Domain layer entirely.
- **Caught mid-implementation:** an early pass parsed the JWT user id directly into the customer's own id — corrected once the two were recognized as genuinely different concepts, not the same id in two places.

## Ownership enforced in the use case layer, not just role

- **Decision:** authorization checks that depend on *whose* record it is (e.g. cancelling a reservation) live in the use case, reading identity via `ICurrentUserService`, never a client-supplied id.
- **Reason:** `[Authorize(Roles = "Customer")]` alone would let any customer act on any other customer's data; a client-supplied customer id on create/cancel would be directly spoofable.
- **Rejected:** taking a `customerId` parameter from the request body — removed once identified as a spoofing risk; inferred from the JWT instead.

## DDD value objects added deliberately later, not up front

- **Decision:** `EmailAddress`, `Money`, `DateRange` were introduced in a dedicated later pass (Phase 6), not the original domain model.
- **Reason:** the first pass deliberately kept the model simple (raw `decimal`/`DateTime`, "avoid advanced DDD patterns" per an early explicit review) until real usage showed which primitives actually needed guarding — adding structure speculatively, before it earns its cost, was rejected early on.

## Domain events and domain services — deliberately not built

- **Decision:** no domain event infrastructure (event base type, dispatch mechanism) and no domain service layer were added during the Phase 6 DDD pass, despite being on the original review backlog alongside the value objects.
- **Reason:** nothing in the app currently needs either — no side effects to decouple (no emails, no other bounded contexts, no read-model projections) and no cross-entity logic that doesn't already fit naturally in a use case. Same reasoning as skipping pagination: adding the machinery speculatively, with nothing to consume it, isn't worth the cost.

## Independent aggregates — no shared navigation properties

- **Decision:** `Room`, `Customer`, `Reservation` removed their `Reservations` navigation collections; each is its own aggregate root.
- **Reason:** nothing in the app ever needed to load "a room's reservations" as a graph; the shared navigation was unused coupling. Deleting a `Room`/`Customer` that still has reservations is rejected explicitly (any status — a cancelled reservation is still a historical record), enforced at the application layer *and* a DB-level restrict-delete FK as defense-in-depth.

## Double-booking race: serializable transaction, no deadlock-retry loop

- **Decision:** `CreateReservation` wraps its overlap check and the reservation insert in a single `Serializable`-isolation transaction (via a new `ITransactionRunner` abstraction, so the Application layer still doesn't depend on EF Core directly). A database-level failure to complete that transaction is converted to the same `ConflictException` an application-level "already booked" rejection throws, rather than surfacing as an unhandled 500 or a silent double-booking; it isn't caught and retried automatically.
- **Reason:** this closes the actual double-booking race — two concurrent requests for the same room/dates can no longer both succeed — with an idiomatic EF Core mechanism and no schema change. Automatic retry-on-deadlock is what a production system under real concurrent load would want, but isn't proportionate to this project's traffic; a concurrency integration test (firing two concurrent create-requests at the same room/dates) proves the property that actually matters: exactly one succeeds, the other gets a clean 409.
- **Verified, not assumed:** the concurrency test runs against `Tests.Integration`'s SQLite-backed host, which shares one physical connection across every request (see `CustomWebApplicationFactory`) — a different mechanism from SQL Server's row-range locking. Confirmed empirically: a second concurrent transaction fails outright *opening* (not committing) — `Microsoft.Data.Sqlite.SqliteException`, error 1 — because SQLite rejects a nested `BEGIN` on a connection already mid-transaction. `TransactionRunner` treats a failure to even start the transaction the same as a failure to commit it, which is what makes the same code correctly handle both SQLite's connection-level conflict and SQL Server's real serialization-failure/deadlock errors.
- **Rejected:** a per-room advisory lock (`sp_getapplock`) — ties correctness to raw SQL for no real gain here; a normalized one-row-per-booked-night table with a unique constraint — the strongest guarantee available, but requires a new table plus touching `CancelReservation`/`DeleteReservation` too, disproportionate to this project's scale.

## Global exception handling + `ProblemDetails`, not per-controller try/catch

- **Decision:** one exception taxonomy, one middleware, RFC 7807 `ProblemDetails` responses.
- **Reason:** consistent error shape for every endpoint, no duplicated try/catch, and a single place to decide what's safe to expose (taxonomy messages are user-facing by design; unclassified 500s never leak `ex.Message`).

## Request validation: DataAnnotations, not FluentValidation

- **Decision:** request DTOs (`RegisterRequest`, `CreateReservationRequest`, …) use built-in attributes (`[Required]`, `[EmailAddress]`, …), wired automatically into `ModelState` → a `ProblemDetails` 400 on failure.
- **Reason:** the project's actual validation needs are simple required/format/range checks with no cross-field or conditional rules — exactly what attributes cover. FluentValidation's extra expressiveness (separately-testable validator classes, conditional rules) wasn't earning its cost as a third dependency here.
- **Note:** `FluentValidation` was still named in the original Phase 6 backlog ([`roadmap.md`](roadmap.md)) when that list was first drafted, before this alternative was chosen during implementation — since corrected there to point back at this decision, rather than left looking like a dropped item.

## Secrets: fail-fast, no hardcoded fallback

- **Decision:** `Jwt:Key` has no default — a missing value throws at startup instead of silently signing tokens with a guessable placeholder.
- **Reason:** a working-but-insecure default is worse than a loud failure; found and fixed a real problem this way (a previously-committed hardcoded key).
- **Trade-off, stated explicitly:** the repo-root `.env` *is* committed (normally it should be gitignored) — a deliberate, documented exception so `docker compose up` works with zero setup for anyone cloning this portfolio repo. Not how a real production project should handle it.

## Registration reveals a duplicate email; login never reveals account existence

- **Decision:** `POST /api/account/register` returns a distinct 409 Conflict (with Identity's own message) when the email is already registered, while `POST /api/account/login` returns the identical "Invalid credentials." for both an unknown email and a wrong password.
- **Reason:** the two endpoints' failure modes aren't symmetric. Login's two hidden causes lead to the same corrective action either way (recheck credentials, or go register), so hiding which one it was closes a real enumeration vector for free. Registration's two causes — duplicate email vs. weak password — need genuinely different fixes ("log in instead" vs. "pick a stronger password"), and the enumeration signal here isn't really in the message text anyway: `ConflictException` and `ValidationException` already differ by HTTP status (409 vs. 400) regardless of wording.
- **Rejected:** collapsing both into one generic response to mirror login's enumeration-avoidance — would remove the weak-password signal a legitimate user needs to fix their submission, for a security benefit that's marginal on a project with no real user base to protect.

## JWT stored in browser `localStorage`, no refresh token

- **Decision:** the Angular frontend stores the JWT in `localStorage` (`auth.service.ts`); there is no refresh-token flow or server-side revocation, and logout only discards the token client-side.
- **Reason:** standard, widely-used SPA practice, proportionate to this project's scope (no real user data, no production traffic).
- **Trade-off, stated explicitly:** a future XSS bug would also be a token-theft bug, and a stolen token stays valid for its full lifetime (`Jwt:ExpireMinutes = 60`) with no way to revoke it early. Accepted as-is rather than moving to httpOnly cookies + CSRF or a refresh/revocation store, which would be a real auth-model change (Angular interceptor, CORS/CSRF handling), not a hardening tweak.

## Security response headers added; a real CSP is a deliberate non-goal

- **Decision:** `X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options`, and HSTS (outside Development) are set via a small hand-rolled middleware; the Kestrel `Server` header is suppressed. No Content-Security-Policy is defined.
- **Reason:** the first set is a handful of static header assignments — cheap and unambiguous, no new dependency needed. A real CSP is different: getting it right requires enumerating the Angular build's actual script/style/font origins, and the payoff is low for a same-origin SPA behind nginx with no third-party scripts.
- **Rejected:** a security-headers NuGet package (e.g. `NetEscapades.AspNetCore.SecurityHeaders`) — more thorough, but a new dependency for something this small, inconsistent with choosing DataAnnotations over FluentValidation elsewhere.

## Image upload: magic-byte signature check, not a re-encode or a new dependency

- **Decision:** `ImageValidation` checks the first bytes of an uploaded file against known JPEG/PNG/WebP signatures, in addition to the existing Content-Type/size checks — the Content-Type header alone is client-supplied and trivially spoofable.
- **Reason:** the existing 5MB cap makes buffering the whole upload into memory cheap, so a hand-rolled signature check needs no new dependency. Real-world impact of the previous gap was already low (server-generated filenames, static-file serving, no executable extension reachable), but it's a known anti-pattern worth closing regardless.
- **Rejected:** a file-type-sniffing package (e.g. `MimeDetective`) — a dependency for three known, stable signatures trivial to hand-check; and a full re-encode via `SixLabors.ImageSharp` — the strongest guarantee available, but a heavier dependency with real CPU cost that changes the stored file's fidelity, disproportionate to the actual risk here.

## Pagination — deliberately skipped

- **Decision:** not implemented, despite being on the original review backlog.
- **Reason:** the dataset is intentionally tiny (a handful of rooms/reservations) with no real performance problem to solve, and it's the one item that would force a coordinated frontend change for zero actual benefit here. Recorded as a considered-and-rejected non-goal, not an oversight.

## Angular: standalone components, signals, no NgRx

- **Decision:** no state-management library; a handful of injectable services hold shared state as signals.
- **Reason:** matches Angular's current default direction (standalone components/functional guards are already the framework default); the app's actual shared-state surface (hotel record, logged-in user, room-browse filters) is small enough that a full store would be unused ceremony.

## Monorepo, `backend/` + `frontend/`

- **Decision:** one repository, two top-level folders, one CI pipeline — not separate repos.
- **Reason:** this is a single coherent CV artifact with one workflow-log narrative; splitting repos would fragment that story for no benefit at this scale.

## AI-assisted development — the developer stays responsible

- **Decision:** AI tools generate implementations, review code, and draft documentation. They also surface tradeoffs and ask clarifying questions when a choice is genuinely open — but the developer makes the actual call, whether the question originated from the developer or was raised by the AI. What doesn't happen: an AI-made choice shipping without the developer engaging with it at all.
- **The developer remains responsible for:**
  - every architecture decision on this page — genuinely decided, not rubber-stamped, regardless of who raised the question
  - reviewing generated code before it's committed (nothing is committed without explicit approval — see [`CLAUDE.md`](../CLAUDE.md))
  - directing scope: prompts specify what should change and why, not "make it better"
  - correcting the agent when it drifts, misattributes a decision, or gets a fact wrong — logged, not edited away (see [`ai-assisted-development.md`](ai-assisted-development.md) for concrete examples)
- **DevOps, hands-on rather than hands-off:** for Docker/docker-compose/CI, the developer asked for a teaching-oriented plan rather than a done-for-you one, made every concrete technical decision (auto-migrate on startup, ESLint via schematic, …), and personally ran every command, tested every result, and diagnosed the real problems that came up (a Docker Desktop virtualization setting, SQL Server networking). Claude Code generated the file content itself, against that plan and those decisions.
- **Exception, stated plainly:** the Angular frontend's code was written by Claude Code end-to-end, agentically — but every screen, flow, and behavior it implements follows the developer's own decisions and the Claude Design mockup approved beforehand, not the coding agent's own judgment calls.
