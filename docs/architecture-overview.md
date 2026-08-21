# Architecture Overview

What the system is made of and how the pieces connect. For *why* a given choice was made, see [`decisions.md`](decisions.md).

## Backend — Clean Architecture

Four projects, dependencies point inward only:

```
HotelReservation.Api            (controllers, DI wiring, middleware, authentication — thin)
        ↓ depends on
HotelReservation.Infrastructure (EF Core, repositories, Identity, JWT, persistence)
        ↓ implements interfaces from
HotelReservation.Application    (use cases, DTOs, repository interfaces)
        ↓ depends on
HotelReservation.Domain         (entities, invariants, value objects, enums — no external deps)
```

- **Domain** — `Hotel`, `Room`, `Reservation`, `Customer` entities; `EmailAddress`/`Money`/`DateRange` value objects; no EF, no ASP.NET Core, no framework references at all.
- **Application** — one class per use case (`CreateReservation`, `CancelReservation`, `GetRooms`, …), each with a single `ExecuteAsync`; repository *interfaces* (`IReservationRepository`); DTOs; the shared exception taxonomy.
- **Infrastructure** — EF Core `HotelDbContext` + Fluent API configurations, repository *implementations*, `AuthService`, `JwtTokenService`, disk-based `ImageStorageService`.
- **Api** — controllers delegate straight to a use case class; no business logic in a controller body; DI registration and the HTTP pipeline live in [`Program.cs`](../backend/HotelReservation.Api/Program.cs).

Enforced in practice, not just on paper: Application only references Domain; Infrastructure implements Application's interfaces; nothing in Domain or Application ever references Infrastructure or Api.

## Request flow (example: creating a reservation)

1. `POST /api/reservations` → `ReservationsController` (`[Authorize(Roles = "Customer")]`)
2. Controller reads the JWT-derived customer id via `ICurrentUserService`, calls `CreateReservation.ExecuteAsync(...)`
3. Use case: loads the room, checks availability (no overlapping non-cancelled reservation for the date range), constructs a `Reservation` (which itself constructs `DateRange`/`Money`, enforcing invariants at the boundary)
4. Repository persists via EF Core; DB constraints (foreign keys, restrict-on-delete) back up the same rules as defense-in-depth
5. Any rejection anywhere in that chain throws a typed exception, caught once in `ExceptionHandlingMiddleware`, turned into a `ProblemDetails` (RFC 7807) response — no controller-level try/catch

## Authentication & authorization

- **ASP.NET Core Identity** (`IdentityUser`) for login/passwords/roles, **JWT Bearer** for stateless API auth — issued at login, validated on every request (issuer, audience, lifetime, signing key).
- **Two roles**: `Customer`, `Admin`. Registration always assigns `Customer` server-side — a client can never request a role (privilege-escalation prevention). A dev-only startup seed creates one `Admin` login, since there's no promotion path otherwise.
- **`IdentityUser` (auth identity) and `Customer` (business entity) are deliberately separate rows**, linked by `Customer.IdentityUserId` — see [`decisions.md`](decisions.md) for why.
- **Ownership, not just role, is enforced in the use case layer** — e.g. `CancelReservation` lets an `Admin` cancel anything, but a `Customer` only their own reservation (`reservation.CustomerId != customer.Id` → `ForbiddenException`), resolved via `ICurrentUserService` reading the JWT's subject claim, never a client-supplied id.
- Endpoint-level authorization by controller: `Hotel`/`Rooms` — public read, admin-only write; `Customers` — admin-only; `Reservations` — customer-create/customer-"mine", admin-manage-all; `Account` (register/login) — anonymous.

## Domain model

- **Value objects** (`EmailAddress`, `Money`, `DateRange`) — immutable, validate on construction, equality by value. Added deliberately later (Phase 6) rather than in the original pass, once real usage showed which primitives needed guarding.
- **Independent aggregates** — `Room`, `Customer`, `Reservation` each stand alone; no shared navigation properties between them (removed deliberately — see [`decisions.md`](decisions.md)). Deleting a `Room`/`Customer` with existing reservations is rejected at the application layer *and* backed by a DB-level restrict-delete foreign key.
- **Reservation price snapshot** — `PricePerNight` is copied onto the reservation at booking time, so a later room-price change never rewrites history.

## Error handling & logging

- A single exception taxonomy (`NotFoundException`, `ConflictException`, `ForbiddenException`, `ValidationException`, `UnauthenticatedException`) thrown from Application; `ExceptionHandlingMiddleware` maps each to its HTTP status + a `ProblemDetails` body. Everything else falls through to a generic, detail-free 500.
- **Serilog**: one structured line per request, plus explicit `ILogger` calls at business-significant events (reservation created/cancelled, rejections). Rejections log at `Warning`, genuine 500s at `Error`.

## Testing

Three tiers, ~66 tests total:

- **`Tests.Domain`** (xUnit) — entity invariants, value-object validation, boundary conditions (e.g. `DateRange.Overlaps`)
- **`Tests.Application`** (xUnit + Moq) — use-case logic and ownership rules, dependencies mocked
- **`Tests.Integration`** (`WebApplicationFactory` + SQLite in-memory) — real HTTP calls through the full pipeline: auth, authorization (anonymous/wrong-role/cross-customer-ownership), repository behavior against a real (if swapped) database provider

## Frontend

Angular (standalone components, signals, no NgRx) — implemented by Claude Code against a Claude Design mockup (itself built on an earlier Claude Code wireframe), under the developer's decisions throughout (see [`ai-assisted-development.md`](ai-assisted-development.md)). Talks to the same JWT-secured API: an HTTP interceptor attaches the token, route guards check role, and DTO-mirroring models keep the two sides in sync.

- **Structure**: `core/` (auth — JWT decode/storage, guards, interceptor; models mirroring the backend's DTOs; HTTP services wrapping each controller) · `shared/` (generic dialogs) · `layout/` (role-aware nav bar) · `features/` (one folder per screen — `home`, `auth`, `rooms`, `booking`, `reservations`, `admin` — routed via `app.routes.ts`)
- **State**: no state-management library — a handful of injectable services hold shared state (hotel record, logged-in user) as signals; everything else is local component state
- **Backend reachability**: a startup health check, plus an HTTP interceptor watching every request for connectivity failures, gates the whole app behind a dedicated "backend unavailable" screen (manual + automatic retry) instead of rendering against absent data

## DevOps

Docker (multi-stage builds — SDK/Node build stage, minimal runtime stage), docker-compose (full stack incl. SQL Server), GitHub Actions CI (build+test backend, build+lint frontend). See [`running.md`](running.md) for every way to run the stack.
