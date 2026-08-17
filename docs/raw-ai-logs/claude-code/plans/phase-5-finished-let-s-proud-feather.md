# Phase 6 — Deep Backend Review

## Status: COMPLETE (2026-08-17)

All nine stages done — Stage 6 (pagination) deliberately skipped as a documented non-goal (see its own section below), the other eight implemented, verified (build + full three-tier suite, 32 → 66 tests), and committed on branch `phase6-deepReview`, 9 commits ahead of `origin/phase6-deepReview` (not yet pushed/PR'd as of this note). `docs/WORKFLOW_LOG.md` has the full dated decision trail. Two real, live regressions were caught and fixed along the way rather than just planned around: legacy zero-price reservation rows that Stage 3's new validation-on-read surfaced, and a frontend error-message parser left stale by Stage 2's response-shape change. Not independently verified from here: GitHub Actions CI on the branch, and the docker-compose secrets path (Docker Desktop wasn't running in this session) — both worth a final look before merging to `main`.

## Context

Phase 5 (Serilog + global exception handling) is merged into `origin/main` via PR #4. The roadmap (`docs/ROADMAP.md`) deliberately deferred the full backend-review backlog to this phase, once real frontend/DevOps usage existed to inform it. Three parallel Explore passes (API layer, domain/application/infrastructure, test suite) inventoried the actual current state; a Plan pass then designed a concrete, dependency-ordered implementation sequence. Key developer decisions locked in during this planning session:

- Build all three DDD value objects (`EmailAddress`, `Money`, `DateRange`) — not a subset.
- Domain events stay explicitly out of scope.
- Secrets: rotate the JWT key, wire up .NET user-secrets (native dev) and a gitignored `.env` + `.env.example` (docker-compose), remove `Program.cs`'s silent fallback-to-hardcoded-key so missing config fails fast. Skip building a production hotel-seeding path — documented as a deliberate non-goal, not fixed, since this project never sees a real deployment.
- Execution: **staged with checkpoints** — each stage below is implemented, verified, and committed on its own before moving to the next, so there's a natural pause/redirect point between stages.

**Verified starting state**: local `main` is now pulled and up to date with `origin/main` (confirmed: HEAD at `8c66ea2`, the PR #4 merge commit; `backend/HotelReservation.Api/Middleware/ExceptionHandlingMiddleware.cs` present on disk). Stage 0 below is already satisfied — noted for completeness, not a pending action.

## Stage 0 — Sync and branch (prerequisite, already done)

~~`git pull` to fast-forward local `main` to `origin/main`~~ — done. Remaining action before Stage 1: branch Phase 6 work off `main` (e.g. `phase6/deep-review`, or per-stage branches if preferred — decide at execution time).

## Stage ordering and why

```
1. DTO cleanup
2. API contract hardening (exceptions, ProblemDetails, status codes, Swagger docs, DataAnnotations)
3. DDD value objects (EmailAddress, Money, DateRange)
4. Aggregate boundary cleanup
5. Repository/query efficiency
6. Pagination (skipped, see below — deliberate non-goal at this project's actual data scale)
7. REST/route consistency cleanup
8. Secrets/JWT hardening
9. Test additions (authorization/ownership/repository)
```

- **DTOs before value objects**: DTOs stay primitive regardless of entity internals. Doing them first means value objects (stage 3) only ever touch already-stable mapping call sites (`room.PricePerNight` → `room.PricePerNight.Amount`), not a moving DTO target.
- **Contract hardening before value objects**: `[ProducesResponseType]` needs the DTOs stage 1 produces (esp. the new `ReservationDto`), and stage 3's new domain-invariant exceptions need somewhere correct to land — build the exception taxonomy once, not twice.
- **Aggregate cleanup right after value objects**: both touch `OnModelCreating`/entity configuration and generate migrations — doing them back-to-back means one coherent persistence-model review instead of two overlapping ones.
- **Repo efficiency after aggregate cleanup**: removing `Room.Reservations`/`Customer.Reservations` navigation properties (stage 4) is what forces the `.Include()` rewrites (stage 5) — reversed order would mean rewriting the same lines twice.
- **Pagination after repo efficiency**: both touch the same `GetAllAsync` signatures; bundling avoids double-editing repositories/use-cases/controllers.
- **REST cleanup and secrets late**: both are largely orthogonal to the domain/DTO work, low-conflict — placing them late means fewer rebases while the riskier stages are in flight.
- **Tests last**: written against stabilized exceptions/routes/repository signatures, so assertions aren't rewritten multiple times. Note: the *existing* three-tier suite is the regression gate for every stage throughout, not just a stage-9 concern — "build + full suite green" applies after each stage.

---

## Stage 1 — DTO cleanup

- Add `ReservationDto` (`backend/HotelReservation.Application/DTOs/ReservationDto.cs`) matching the exact shape `frontend/src/app/core/models/reservation.model.ts` already expects (`Id, RoomId, CustomerId, CheckIn, CheckOut, Status, PricePerNight`). Replace the three anonymous-object returns in `Reservations/GetReservationById.cs`, `GetReservations.cs`, `GetMyReservations.cs` (currently `Task<object?>`/`Task<IEnumerable<object>>`).
- `CreateRoomRequest`/`UpdateRoomRequest` are byte-for-byte identical — collapse to one `RoomRequest` type, with a comment explaining why create/update share it.
- `UpdateCustomerRequest` vs `CustomerDto`: **leave separate**, document why — request/response shapes serve different purposes even when currently identical; collapsing risks over-posting-style coupling later.
- Auth DTOs/interfaces in `Authentication/`: **corrected during implementation** — the actual established convention is the opposite of what was first assumed: `Interfaces/` and `DTOs/` already centralize every other feature's interfaces and DTOs, only use-case classes live per-feature. `Authentication/` was the real outlier. Moved `IAuthService`/`IJwtTokenService` → `Interfaces/`, `LoginRequest`/`RegisterRequest`/`AuthenticationResponse` → `DTOs/`, folder removed, all references updated.
- **Verify**: full solution build + test suite; manually diff `GET /api/reservations/mine` JSON shape against the frontend's `Reservation` model field-for-field.

## Stage 2 — API contract hardening

- New exception taxonomy in `backend/HotelReservation.Application/Common/Exceptions/`: abstract `AppException`, plus `NotFoundException`, `ConflictException`, `ForbiddenException`, `ValidationException`, `UnauthenticatedException`. Domain entity constructors keep throwing plain `ArgumentException` for invariants (decoupled from HTTP concerns) — the middleware maps that to 400 too.
- Migrate every existing `throw new InvalidOperationException(...)` site across `Reservations/`, `Customers/`, `Rooms/`, `Hotel/`, `Common/ImageValidation.cs`, `AuthService.cs` to the right new type (not-found → `NotFoundException`, double-booking/delete-with-reservations → `ConflictException`, cancel-someone-else's-reservation → `ForbiddenException`, image validation → `ValidationException`, the handful of "unauthenticated" defensive checks → `UnauthenticatedException`). `AuthService.LoginAsync`'s "Invalid credentials" and `JwtTokenService`'s key-length guard stay as plain `InvalidOperationException` (login keeps its explicit controller-level 401 catch; the JWT guard becomes unreachable once stage 8 fails fast at startup, kept as defense-in-depth).
- Rewrite `ExceptionHandlingMiddleware` (`backend/HotelReservation.Api/Middleware/`, arrives via stage 0's pull) to build real `ProblemDetails` responses, `switch`-mapping each exception type to its status code, keeping the existing `HasStarted` guard and the no-`Response.Clear()` CORS fix from Phase 5. 500s keep `Detail = null` so internal messages never leak.
- Remove now-redundant per-controller `catch (InvalidOperationException)` blocks (Rooms, Customers, Account) — the middleware handles it centrally now. Fix status codes: updates/deletes/cancel → `204 NoContent` instead of `200 Ok()`; `Account.Register` → `201 Created` (creates a Customer + IdentityUser).
- Add `[ProducesResponseType]` to every action referencing the now-stable DTOs; wire `IncludeXmlComments` in `Program.cs`'s `AddSwaggerGen` + `GenerateDocumentationFile` in the csproj.
- Add `DataAnnotations` (`[Required]`, `[EmailAddress]`, `[Range]`, `[MaxLength]`) to every `*Request` DTO. `[ApiController]`'s automatic `ValidationProblemDetails` on invalid `ModelState` needs no extra wiring — already default behavior. Cross-field checks (check-out after check-in) stay as the domain-level invariant, not a DataAnnotation.
- Update existing test assertions broken by the exception-type migration (`CancelReservationTests.cs` and others currently assert `ThrowsAsync<InvalidOperationException>()` — update per the new types).
- **Frontend note (flag, don't fix here)**: `frontend/src/app/core/utils/http-error.ts` parses `{error: "message"}` today (confirmed) — the `ProblemDetails` shape change requires a small coordinated frontend update (`.detail`/`.title` instead of `.error`) or its error dialogs silently fall back to a generic message for every 4xx after this stage. Decide up front whether to pair this with a small frontend commit per stage, or batch all frontend-facing breaks (this one, stage 6's, and any stage-7 route renames) into one follow-up pass after stage 9.
- **Verify**: build + full suite green; Swagger UI shows response docs; manually trigger a 404/409/403/400 and confirm `application/problem+json` with correct status.

## Stage 3 — DDD value objects

- `EmailAddress`, `Money` (`Amount` + `Currency`), `DateRange` (`CheckIn`/`CheckOut` + `Overlaps()`) as immutable `record`s in `backend/HotelReservation.Domain/ValueObjects/`, validating in the constructor (mirroring the pattern `Room`/`Reservation` already use for their own invariants).
- Mapping strategy, deliberately not uniform:
  - `EmailAddress` → EF `ValueConverter<EmailAddress, string>` — no schema change, maps onto the existing column.
  - `Money` → EF owned entity (`OwnsOne`) — genuinely needs a schema change (new `Currency` column on `Rooms`/`Reservations`, backfilled e.g. `"EUR"` for existing rows) since it's two properties, not one.
  - `DateRange` → EF owned entity mapped onto the existing `CheckIn`/`CheckOut` column names — no schema change, just a shape change.
- Entity changes: `Customer.Email` → `EmailAddress`, `Room.PricePerNight`/`Reservation.PricePerNight` → `Money`, `Reservation.CheckIn`/`CheckOut` → single `Stay: DateRange` property. Constructors keep taking primitives (`string`/`decimal`/`DateTime`) and wrap internally — minimizes churn at call sites.
- This is where stage 2 pays off directly: `CreateReservation.cs`'s manual `checkOut <= checkIn` check gets deleted entirely — `new DateRange(...)`'s constructor enforces it once, and its `ArgumentException` is already mapped to 400 by the stage-2 middleware.
- **Honest limitation, not full dedup**: `DateRange.Overlaps()` centralizes the *concept* (unit-testable once), but `ReservationRepository.HasOverlappingReservationAsync` and `RoomRepository`'s availability filter must stay as raw LINQ property comparisons — EF can't translate an arbitrary C# instance method to SQL without pulling the whole table into memory. The expression itself stays duplicated across those two query sites by EF's own constraints; only the definition is centralized.
- First-ever `OnModelCreating` in the project (confirmed empty today) — use `IEntityTypeConfiguration<T>` classes in a new `backend/HotelReservation.Infrastructure/Persistence/Configurations/` folder, wired via `ApplyConfigurationsFromAssembly`.
- **Verify**: `dotnet ef migrations add` and review the generated `Up()` by eye — expect a real `Currency` column addition (the one genuinely risky step, review the backfill against real dev-DB data before applying) and near-no-op changes for `EmailAddress`/`DateRange`; full suite green; confirm `dotnet ef database update` applies cleanly against the existing dev database.

## Stage 4 — Aggregate boundary cleanup

- Remove `Hotel.AddRoom()` — confirmed dead code (`CreateRoom.cs` never calls it).
- Remove the `Room.Reservations`/`Customer.Reservations` collection navigation properties entirely, formalizing what's already true in practice: `Room`, `Customer`, `Reservation` are independent aggregate roots referencing each other only by `Guid` id (matching how `Reservation` already references `Room`/`Customer`).
- **This has a real, non-cosmetic consequence**: EF's convention-based FK inference (confirmed today: cascading `FK_Reservations_Rooms_RoomId`/`FK_Reservations_Customers_CustomerId`) relied entirely on those navigation properties. Removing them silently drops the FKs unless explicitly reconfigured in the new `ReservationConfiguration.cs` — reconfigure explicitly, and switch `OnDelete` from `Cascade` to `Restrict` at the same time (the application layer already guards against deleting a room/customer with reservations; `Restrict` makes the database enforce the same rule as defense-in-depth, rather than silently cascading away reservation history if that guard is ever bypassed). Call this out explicitly in the commit — it's a deliberate behavior change, not just a refactor.
- `Hotel.Rooms` navigation is a different, load-bearing relationship (real FK, not id-only-by-convention like Reservation↔Room/Customer) — leave it as-is, don't over-apply the pattern.
- **Verify**: build + full suite; `dotnet ef migrations add` should touch only the FK `OnDelete` behavior — small, easy to review by eye.

## Stage 5 — Repository/query efficiency

- Removing the navigation properties in stage 4 forces `RoomRepository.GetByIdAsync`/`GetAllAsync` and `CustomerRepository.GetByIdAsync`/`GetAllAsync`/`GetByIdentityUserIdAsync`'s `.Include(...Reservations)` calls to be rewritten — they no longer compile as-is. Drop the Includes (confirmed unused by `RoomDto`/`CustomerDto`); rewrite `RoomRepository`'s availability filter (currently relies on the `Room.Reservations` navigation) as an explicit query — recommend adding a new `IReservationRepository.GetOverlappingRoomIdsAsync(DateRange)` method rather than having `RoomRepository` reach directly into the `Reservations` table (that reach-across was already a layering smell).
- Drop `HotelRepository.GetAsync`'s `.Include(h => h.Rooms)` — unused by `HotelDto`.
- The previously-flagged "redundant double-fetch" in `DeleteRoom`/`DeleteCustomer` and the "wasted Include" in `CreateReservation`/`GetMyReservations` are resolved as a side effect of removing the Includes above — no separate change needed there; note this explicitly since the original inventory's framing assumed the Include was still present.
- Remove dead code: `IReservationRepository.RoomExistsAsync`/`CustomerExistsAsync` (confirmed unused anywhere).
- **Verify**: build + full suite; manually exercise `GET /api/rooms?checkIn=...&checkOut=...` against a dev DB with a known overlapping reservation to confirm correct filtering (a proper regression test for this lands in stage 9).

## Stage 6 — Pagination (skipped, deliberate non-goal)

**Decided not to implement.** Re-examined once the actual dev data was in view (2 rooms, a handful of reservations/customers — nowhere near a scale pagination would help) and weighed against its real cost: unlike every other stage, this one requires a coordinated frontend change (every list response wraps from a bare array into `{items, page, pageSize, totalCount, totalPages}`, which every list-consuming Angular service/component would need to follow). That's real engineering cost for a performance/UX problem this project doesn't have and isn't likely to reach. Same treatment as the production hotel-seeding path skipped in Stage 8: a documented, reasoned non-goal rather than a silently dropped backlog item.

Original plan, kept for reference in case the data volume ever changes: `PagedResult<T>` (`Items`/`Page`/`PageSize`/`TotalCount`/`TotalPages`) wrapping `GET /api/rooms`, `/api/customers`, `/api/reservations` (not `/api/reservations/mine`, scoped to one customer already), with repository `GetAllAsync` methods gaining skip/take.

## Stage 7 — REST/route consistency cleanup

Scoped small — align, don't redesign:

- `HotelController` stays singular (`/api/hotel`) — the system is genuinely single-hotel, a plural route would be less honest, not more consistent. Document inline.
- `me` vs `mine`: standardize on **`mine`** (`Customers/me` → `Customers/mine`) since `Reservations` established it first. Frontend-breaking (one call site) — pair with the frontend fix.
- `AccountController` → rename route to `api/auth` (or rename the controller) since register/login are unambiguously auth, not account-management. Check `frontend/src/app/core/auth/*.ts` for the hardcoded path first; if not worth the coordinated frontend change right now, explicitly defer and document as accepted debt — lowest-value item on this list.
- No `POST` on `CustomersController`: **leave as-is** — customers are deliberately only created via `AccountController.Register` (atomically with the linked `IdentityUser`); a bare create endpoint would allow orphaned, login-less customer rows.
- `[Authorize(Roles = "Admin")]` short-form vs fully-qualified inline: cosmetic-only, standardize on the short form with a `using` statement everywhere.
- **Verify**: build + full suite; grep the frontend for any renamed literal route before merging, to confirm nothing is left pointing at a 404.

## Stage 8 — Secrets/JWT hardening

- Rotate the JWT signing key (generate a new random 256+-bit value) — the old one (`ChangeThisDevKey12345678901234567890`, currently committed in `appsettings.json` and `docker-compose.yml`) is retired everywhere, including local dev.
- Remove `Program.cs`'s silent `?? "<hardcoded-key>"` fallback — replace with a throw at startup if `Jwt:Key` is missing, so misconfiguration fails fast and loud instead of silently running with a weak key. Same treatment for `Jwt:Issuer`/`Jwt:Audience` for consistency, though those aren't secrets.
- Delete `Jwt.Key` from `appsettings.json` entirely. Native dev path: `dotnet user-secrets init` + `dotnet user-secrets set "Jwt:Key" "..."` for `backend/HotelReservation.Api`.
- Docker-compose path (**revised**): commit an actual, working `.env` directly — **not** gitignored, no `.env.example` copy-and-rename step. Switch `docker-compose.yml`'s `backend`/`db` `environment:` blocks to `${VAR}` substitution reading from it. The file opens with a comment block explaining these values are for this demo project only, and that a real development setup must gitignore `.env` to avoid leaking secrets — i.e. the file documents the correct practice it's deliberately not following, rather than silently modeling bad practice. Chosen over the `.env.example` approach specifically for this portfolio project: a reviewer cloning the repo should get `docker-compose up` working immediately with zero setup steps, which matters more here than secret hygiene for values that are already fake/dev-only and were already committed in `docker-compose.yml` before this stage.
- `JwtTokenService`'s existing throw-on-missing-key behavior is already correct — no change needed there, the inconsistency being fixed is specifically `Program.cs`'s leniency.
- Explicitly **not** doing: a production hotel-seeding path. `SeedDevAdminAsync` stays exactly as `IsDevelopment()`-gated as today; only the *values* it reads move from committed-plaintext in `appsettings.json`/`docker-compose.yml` to user-secrets (native) / the committed `.env` (docker-compose). Document the scope boundary near the seed method.
- **Verify**: confirm `dotnet run` fails fast with a clear message if the user-secret isn't set (deliberately test this once), then confirm it starts once set; confirm `docker-compose up` works end-to-end straight from the committed `.env` with no extra setup step; `git diff`/`grep` confirms the old hardcoded key/passwords are gone from `appsettings.json` and `docker-compose.yml` (moved to `.env`/user-secrets, not just duplicated).

## Stage 9 — Test additions (authorization / ownership / repository)

Scoped exactly to the roadmap's wording — not full use-case coverage — following existing conventions (xunit.v3 + FluentAssertions, Moq for Application-layer, `MethodUnderTest_Scenario_ExpectedResult` naming, private static `MakeX()` factories, `IClassFixture<CustomWebApplicationFactory>` for integration).

- **Application-layer ownership**: extend the existing `CancelReservationTests.cs` pattern (already covers own/other's/admin-bypass/not-found well) to `GetMyReservationsTests.cs` (new — asserts scoping to the caller's own customer id, asserts unauthenticated throws). **Not** `DeleteReservation`/`GetReservationById` — both are confirmed Admin-only today with no ownership logic in the use case to test; inventing that would test behavior that doesn't exist. If ownership should extend there, that's a stage-7-adjacent routing decision, out of this stage's scope.
- **Integration-layer authorization** (new `Tests.Integration/Authorization/AuthorizationIntegrationTests.cs`, extending `AuthenticationIntegrationTests.cs`'s pattern): anonymous → 401 on a protected endpoint; Customer-role token → 403 on an Admin-only endpoint; Customer A's token cancelling Customer B's reservation → 403 (the concrete gap explicitly missing today). Admin-role tests need direct `UserManager`/`RoleManager` seeding via the factory's service provider, since `Register` always assigns `Customer`.
- **Repository/infrastructure tests** (new `Tests.Integration/Repositories/`, reusing `CustomWebApplicationFactory`'s SQLite fixture — no new test project): `RoomRepository`'s date-filtered `GetAllAsync` (the query rewritten in stage 5 — highest-value new test here), `ReservationRepository.HasOverlappingReservationAsync` true/false cases, deleting a room/customer with reservations now throwing `DbUpdateException` (proves stage 4's `Restrict` FK change), and a round-trip save/load proving the `Money`/`EmailAddress`/`DateRange` EF mappings from stage 3 actually work against a real provider.
- **Domain-layer**: small `DateRangeTests`/`MoneyTests`/`EmailAddressTests` in `Tests.Domain` for constructor validation + `Overlaps()`, following the existing folder-per-entity convention.
- **Verify**: full three-tier suite green; confirm CI (GitHub Actions) also passes on the branch before merging back to main.

---

## Risks to keep in view across stages

1. Two backend-driven frontend breaks are baked into this plan (stage 2's `ProblemDetails` shape, stage 6's `PagedResult<T>` wrapper), plus optional smaller ones in stage 7 if the route renames are taken. None are fixed by this plan (backend-only) — decide up front whether to pair each with a small frontend commit as it lands, or batch them into one frontend follow-up pass after stage 9.
2. Stage 3's migration is the single highest-risk step — a real schema change (new `Currency` column with a backfill) against tables that may hold real dev data. Review the generated SQL by eye; test against a throwaway DB copy first if the dev data matters.
3. Stage 4's `Cascade` → `Restrict` change is deliberate, not incidental — call it out in the commit message.
4. Existing tests will fail mid-stage-2 until the exception-type assertions are updated in that same stage — expected, budgeted churn.

## Verification (end to end)

Per-stage: `dotnet build` on the full solution, `dotnet test` across all three test projects (Domain/Application/Integration) green, plus the stage-specific manual check noted above. After stage 9: full suite green, GitHub Actions CI green on the branch, then merge back to `main` with the developer's explicit go-ahead per `CLAUDE.md`'s commit conventions. `docs/WORKFLOW_LOG.md` gets a dated entry per stage as work happens, per the standing instruction — not deferred to the end.
