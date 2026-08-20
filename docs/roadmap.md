# Roadmap

Phase order for the remaining work, decided 2026-08-03 (see [`workflow-log.md`](workflow-log.md) for the reasoning behind the order). The guiding principle: the frontend's actual needs should drive backend work, not the other way around — so design comes before contract changes, and the deep backend review is deliberately last, once real usage patterns exist.

*Before Phase 0:* the initial backend (ASP.NET Core Web API, Clean Architecture — Domain/Application/Infrastructure/Api — EF Core, Identity + JWT auth, and the three-tier test suite) already existed going into this roadmap, built with ChatGPT for planning/architecture discussion and GitHub Copilot generating parts of the implementation, guided and corrected throughout by the developer. See [`docs/raw-ai-logs/`](raw-ai-logs/) for that history and [`workflow-log.md`](workflow-log.md)'s earliest entries — Phase 0 below is where repo restructuring and this AI-agent-collaboration workflow began, not where the project itself started.

- **Phase 0 (done)** — Repo restructured into [`backend/`](../backend/) + [`frontend/`](../frontend/), this roadmap, the living [workflow log](workflow-log.md), and [`CLAUDE.md`](../CLAUDE.md) standing instructions.

- **Phase 1 (done) — UX/design pass**
  Use Claude ([`claude.ai/design`](https://claude.ai/design) or artifacts) to sketch key screens/flows — auth, browse rooms, book/manage a reservation, admin views — *before* touching the API contract, since the screens determine what endpoints/fields are actually needed.

- **Phase 2 (done) — Backend contract pass**
  Informed by Phase 1: adjust/extend the DTOs and endpoints the frontend will actually consume (naming, response shapes, any missing fields or endpoints the design surfaced). This is *not* the full review backlog below — deep optimization (DDD value objects, pagination, validation hardening, perf) is deliberately deferred to Phase 6, once real frontend usage shows what actually matters.

- **Phase 3 (done) — Angular scaffold + implementation**
  Build against the stabilized API contract; small backend contract tweaks feed back as they surface.

- **Phase 4 (done) — DevOps**
  [`backend/Dockerfile`](../backend/Dockerfile), [`frontend/Dockerfile`](../frontend/Dockerfile), root [`docker-compose.yml`](../docker-compose.yml) for local full-stack runs, GitHub Actions [CI](../.github/workflows/ci.yml) (build+test backend, build+lint frontend). CD is optional/stretch.

- **Phase 5 (done) — Application/runtime logging**
  Add structured *operational* logging to the running backend (e.g. Serilog — request logs, error logs; already flagged below as "Add proper logging"). The deliverable is code — the Serilog setup in [`Program.cs`](../backend/HotelReservation.Api/Program.cs) and `ILogger` calls at the right points — not files: the log output itself is transient console/stdout data while the app runs, never committed. This is separate from [`workflow-log.md`](workflow-log.md), which is the meta-record of *how the project was built with AI*, is an actual committed doc, and isn't a phase — it's already running continuously since Phase 0.

- **Phase 6 (done) — Deep backend review**
  The full backlog, done last, informed by real frontend/DevOps needs:
  - Consistent REST style
  - Proper Swagger response documentation (`ProducesResponseType`)
  - DTO review
  - DDD improvements (value objects like `EmailAddress`/`Money`, aggregate review, domain services/events — *deliberately not built*, see [`docs/decisions.md`](decisions.md))
  - API response improvements (status codes)
  - Validation improvements (required/stringlength/emailaddress, ModelState handling, `ProblemDetails`, global exception handling) — *DataAnnotations chosen over FluentValidation*, sufficient for this project's validation needs without an extra dependency; see [`docs/decisions.md`](decisions.md)
  - Review unnecessary database calls
  - Repository review (e.g. `Include`s)
  - Pagination for larger datasets — *skipped as a deliberate non-goal* (tiny dev dataset, no real performance problem to solve; would have forced a coordinated frontend change for no payoff — see [`workflow-log.md`](workflow-log.md))
  - Proper logging
  - JWT: role/user seeding, JWT key cleanup
  - Tests: authorization integration tests, application ownership tests, repository/infrastructure tests (save/load reservations)

- **Phase 7 (done) — Final documentation pass**
  Polished architecture write-up and README, using [`workflow-log.md`](workflow-log.md) as raw material — the CV deliverable.
