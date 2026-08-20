# Phase 5 Plan — Application/Runtime Logging

*(Drafted in parallel while Phase 4 DevOps work is still in progress on `phase4-devops`. Not to be started until Phase 4 is done and this plan is reviewed.)*

## Context

Per [`docs/ROADMAP.md`](docs/ROADMAP.md), Phase 5 is "Application/runtime logging": add structured *operational* logging (Serilog) to the running backend — request logs, error logs, `ILogger` calls at the right points. This is distinct from `WORKFLOW_LOG.md` (the meta-record of how the project was built), and its deliverable is code (Serilog wiring + `ILogger` calls), not committed log output.

Investigation (via Explore agent) found the backend is a **genuinely greenfield case for this**: no logging package, no `ILogger` usage, and no logging config beyond the untouched ASP.NET Core template defaults exist anywhere in `backend/`. It also surfaced a real gap that blocks meaningful error logging: **no exception handling exists at all** — most controller actions (e.g. `ReservationsController.Create`) have no try/catch, so unhandled exceptions (domain validation failures, double-booking conflicts, DB errors) currently become raw, unlogged 500s. A few actions (deletes, uploads, auth) do local `try/catch (InvalidOperationException)` returning a plain anonymous-object body, not `ProblemDetails`.

The roadmap files full "global exception handling / `ProblemDetails`" under Phase 6. Asked about this tension, the developer chose: **add a minimal global exception-handling middleware in Phase 5** (log the exception, return a generic 500) so error logging actually has something to log end-to-end, while leaving full `ProblemDetails` standardization for Phase 6.

Docker/CI context: the backend container already runs on stdout/stderr with no special log driver config — a Serilog Console sink fits directly with no Dockerfile/compose changes needed. `.github/workflows/ci.yml` now exists (added since the exploration above) — it builds+tests the backend (`dotnet test`) and builds+lints the frontend, so Phase 5's `dotnet test` verification step will also run in CI automatically. `WORKFLOW_LOG.md` already has its Phase 4 entry (2026-08-08) — Phase 4 is close to wrapped up.

## Scope

1. **Serilog wiring in `Program.cs`** (`backend/HotelReservation.Api/Program.cs`)
   - Add `Serilog.AspNetCore` and `Serilog.Sinks.Console` package references to `HotelReservation.Api.csproj`.
   - Bootstrap logger pattern: a minimal `Log.Logger` set up before `CreateBuilder` so startup failures (e.g. bad config, migration failure) are also logged, wrapped in try/catch/finally with `Log.CloseAndFlush()`.
   - `builder.Host.UseSerilog((context, services, config) => config.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext())` — config-driven so levels differ per environment via appsettings.
   - Add `app.UseSerilogRequestLogging()` as the **first** middleware in the pipeline (before Swagger/HTTPS/static files/CORS/auth) so it wraps the entire request including the new exception-handling middleware — gives accurate status code and duration even on unhandled exceptions.

2. **Minimal global exception-handling middleware**
   - New small middleware (inline `app.Use(...)` or a dedicated `ExceptionHandlingMiddleware` class in `HotelReservation.Api`) registered right after request logging, before everything else.
   - Catches unhandled exceptions, logs via `ILogger` at Error level with method/path context, returns a generic 500 with a small JSON error body (not `ProblemDetails` — that's explicitly Phase 6).
   - Leaves the existing local `try/catch (InvalidOperationException) → BadRequest` patterns in controllers untouched (out of scope; those already produce a response, just don't log — see item 3).

3. **`ILogger<T>` calls at meaningful points** — not blanket coverage of every action (request logging middleware already covers the HTTP-level record for every call). Focus on business-significant events and existing catch sites:
   - Reservation creation/cancellation (success and domain-rule failures, e.g. double-booking).
   - Auth: login success/failure, registration (existing `AccountController` catch blocks gain a `LogWarning` before returning `BadRequest`/`Unauthorized`).
   - Existing `InvalidOperationException` catch sites in `CustomersController`/`RoomsController` (delete, image upload) gain a `LogWarning`/`LogError` call before returning their response.
   - Inject `ILogger<T>` via constructor in the relevant Application-layer use case classes and/or controllers, following the existing per-use-case DI pattern already in `Program.cs`.

4. **Config** (`appsettings.json`, `appsettings.Development.json`)
   - Replace the untouched default `"Logging"` block with a `"Serilog"` section: `MinimumLevel` (Information default, `Microsoft.AspNetCore`/`System` overridden to Warning to cut framework noise), `WriteTo: [Console]` with a structured/readable output template. (The existing `"Logging"` block currently governs the ASP.NET Core template's built-in console/debug providers — it's live today, just untuned. `UseSerilog` replaces those providers outright and reads `"Serilog"` instead, so the old block becomes dead config once the switch happens.)
   - `appsettings.Development.json` override: more verbose minimum level for the app's own namespace (Debug) to make local dev useful.
   - No `appsettings.Production.json` exists yet and docker-compose currently runs with `ASPNETCORE_ENVIRONMENT=Development` (a Phase 4 concern) — Phase 5 won't add a Production file unless it turns out to be needed for a meaningfully different log level; flag but don't block on it.

## Files touched (representative, not exhaustive)

- `backend/HotelReservation.Api/HotelReservation.Api.csproj` — add Serilog packages.
- `backend/HotelReservation.Api/Program.cs` — bootstrap logger, `UseSerilog`, request-logging + exception-handling middleware registration.
- `backend/HotelReservation.Api/appsettings.json`, `appsettings.Development.json` — Serilog config sections.
- `backend/HotelReservation.Api/Controllers/ReservationsController.cs`, `AccountController.cs`, `CustomersController.cs`, `RoomsController.cs` — `ILogger` injection + calls at the points described above.
- Relevant `Application` use-case classes (e.g. reservation create/cancel) if logging belongs closer to the business logic than the controller.
- Possibly a new `backend/HotelReservation.Api/Middleware/ExceptionHandlingMiddleware.cs` (or inline in `Program.cs` if kept small).

## Verification

- No existing test convention covers logging/middleware behavior (`Tests.Integration`'s `CustomWebApplicationFactory` doesn't touch logging), and standing up log-assertion infrastructure (e.g. `Serilog.Sinks.TestCorrelator`) is more than this phase calls for — verify manually instead:
  1. Run the backend locally (`dotnet run`) and via `docker-compose up`, confirm structured request-log lines appear on startup/for each request.
  2. Exercise a normal flow (login, create a reservation) and confirm the expected `LogInformation` business-event lines appear.
  3. Deliberately trigger a failure (double-book a room, bad login) and confirm both the existing `BadRequest`/`Unauthorized` response *and* a corresponding log line at Warning/Error appear.
  4. Trigger the already-existing unhandled-exception path: `ReservationsController.Create` has no try/catch today, and `CreateReservation.ExecuteAsync` throws `InvalidOperationException` on a double-booking conflict — booking overlapping dates for the same room currently produces a raw, unlogged 500. Confirm the new middleware instead logs it and returns the generic error response.
- Existing `Tests.Integration`/`Tests.Application` suites should still pass unchanged (`dotnet test`) — logging additions shouldn't break behavior.

## Open items to revisit at execution time

- Exact set of use cases that get `ILogger` calls — the list above is representative; finalize while implementing based on what's actually interesting to log.
- Whether the exception-handling middleware is inline or a dedicated class — decide based on how large it ends up being.
