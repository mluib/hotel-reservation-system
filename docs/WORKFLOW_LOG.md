# AI-assisted Development — Workflow Log

Running log of notable AI-agent interactions during development: what was generated, what was wrong or needed correction, and why.

Kept lightweight — a couple of bullet points per entry, added as things happen. Used as raw material for a polished writeup at the end of the project.

Format:

- `[what was asked] -> [what the agent produced] -> [correction/decision made, and why]`

This is the living version of the log — reconstructed and dated from the raw ChatGPT/Copilot exports in `docs/raw-ai-logs/` plus the git history, and maintained going forward per the standing instruction in [`CLAUDE.md`](../CLAUDE.md). `docs/raw-ai-logs/` remains untouched as the frozen original transcripts this was built from.

---

## 2026-07-21 — Project planning and initial architecture

- Discussed project goals and approach with ChatGPT -> explored ideas for a hotel reservation system -> decided on ASP.NET Core Web API with Clean Architecture (`Domain`/`Application`/`Infrastructure`/`Api` as separate projects, dependencies pointing inward).
- Asked Copilot (agent mode) to create the `Domain` project structure (`Entities/`, `Enums/`) -> Copilot scaffolded `Hotel`, `Room`, `Reservation`, `Customer`, `RoomType` as plain data classes -> accepted as a starting skeleton, flagged for a follow-up review pass.
- Asked Copilot to review the entities for missing relationships/business rules -> it flagged anemic-model issues: no overlap prevention, no nights/price calculation, no navigation properties, no `ReservationStatus`, no value objects (`Email`/`Money`) -> decided to add overlap prevention, status, and nights/price now, and explicitly **defer** value objects/aggregates/domain events to keep v1 simple (documented in `decisions.md`).
- Asked Copilot to review the updated domain model as a "first version, avoid advanced DDD patterns" check -> confirmed suitable for v1, recommended keeping `IsAvailable`/`CreateReservation`-style domain methods and relying on DB constraints instead of optimistic concurrency for now -> accepted.
- Reviewed the `CreateReservation` use case and `IReservationRepository` for dependency direction -> confirmed Application → Domain only, Infrastructure implements Application interfaces -> pattern locked in as the template for all later features.

## 2026-07-22 — Persistence, first API, validation, Customer CRUD

- Added `HotelDbContext`, reservation repository, EF Core migration, the first reservation API endpoint, Swagger UI, and DI registration for `CreateReservation` (mostly direct Copilot agent-mode output, low friction, accepted as-is).
- Asked Copilot to implement reservation validation (no overlaps, check-out after check-in, customer/room must exist) -> it added `RoomExistsAsync`/`CustomerExistsAsync` to the repository and wired the checks into `CreateReservation` -> accepted; noted as a v1-acceptable race condition (check-then-insert) rather than an atomic DB operation.
- Asked Copilot to implement Customer CRUD following the existing pattern -> it generated the full stack (interfaces, DTOs, use cases, repository, controller) but used **reflection to set the `Id`** in `UpdateCustomer` -> rejected: asked for a proper domain `Update()` method instead, since reflection bypasses entity invariants. Copilot replaced it with `customer.Update(firstName, lastName, email)`.
- Also corrected: `UpdateCustomerRequest` originally carried an `Id` field that had to match the route id (extra `BadRequest` check) -> removed the `Id` from the request DTO entirely, made the route id authoritative.
- Asked for Rooms/Hotel/Reservation CRUD endpoints -> Copilot hit its per-turn tool-call limit mid-implementation, then finished on request; also hit a Visual Studio Edit-and-Continue lock (`ENC0023`) requiring the debugger to be stopped before the build could add new interface members -> resolved by stopping debugging and rebuilding.
- Asked to change POST responses to return `CreatedAtAction` with the full DTO instead of a bare 200/Ok -> applied across Rooms/Customers/Reservations controllers, accepted.

## 2026-07-29 — Documentation pass, Identity/JWT authentication, authorization

- Asked ChatGPT to generate project documentation and architecture-decision docs -> produced `docs/raw-ai-logs/ChatGPT/{tech-stack,decisions,architecture-overview,ai-assisted-development}.md` -> accepted as the project's early reference docs (now superseded as the *living* doc by this file, per this session's restructuring).
- Asked Copilot to add ASP.NET Core Identity + JWT Bearer auth with register/login and role-based authorization, keeping auth out of the Domain layer -> first pass put JWT logic partly in `AccountController`/`Infrastructure` directly -> corrected twice: (1) moved the `IJwtTokenService` contract and DTOs into `Application/Authentication` so Application doesn't depend on Identity types, (2) moved the actual auth workflow (`AuthService`) out of the controller entirely so the controller only delegates to `IAuthService`.
- Decided the JWT `sub` claim should be the immutable `userId` (GUID), not username/email, per OAuth/OIDC convention -> Copilot implemented and explained the reasoning; kept `ClaimTypes.NameIdentifier`/`Name` alongside for convenience.
- Asked to enable JWT auth in Swagger -> first attempt used `Microsoft.OpenApi.Models` types that didn't compile against the installed package version; several rounds of guess-and-fail followed (build errors, wrong reference types) -> called out directly ("you're so bad, `Microsoft.OpenApi.Models` doesn't exist here") -> Copilot corrected to the actual available namespace and shipped a reduced config (security *definition* only, no `AddSecurityRequirement`, since the reference type it needed wasn't present in this package version). Full resolution deferred (see 2026-07-30).
- Decided registration should never accept a client-supplied role -> asked Copilot to force role to `"Customer"` unconditionally in `AuthService.RegisterAsync`, removed `Role` from `RegisterRequest` -> reasoning: letting clients pick their own role is a privilege-escalation bug.
- Defined authorization rules per controller (Customers: admin-only; Rooms: public GET, admin-only writes; Hotel: public GET, admin-only PUT; Reservations: customer-create/own-view, admin-manage) -> applied as controller/action-level `[Authorize(Roles=...)]`, register/login left anonymous.
- Asked for reservation *ownership* authorization (customers see only their own reservations) -> Copilot added `ICurrentUserService`/`ForbiddenException`, enforced ownership inside `GetReservationById`/`CreateReservation` -> then **refined the design**: removed `CustomerId` from `CreateReservationRequest` entirely (always inferred from the JWT) and added `GET /api/reservations/mine`; then further corrected so `GetById` is **admin-only** (no in-handler ownership branching) while `mine` serves all authenticated roles — simpler split than mixed ownership checks in one endpoint.
- Caught a modeling issue: Copilot was parsing the JWT user id directly into `Customer.Id` (Guid) -> corrected: `Customer` needed a separate `IdentityUserId` (string) distinct from its own `Id`, resolved via `GetByIdentityUserIdAsync` — the auth user and the business customer are different identities that happen to be linked, not the same key.
- Decided registration should auto-create the linked domain `Customer` (no separate create-customer call) -> `AuthService.RegisterAsync` now creates the `IdentityUser`, assigns the role, creates the `Customer` with `IdentityUserId` set, using the same email; the standalone `POST /api/customers` create endpoint was then removed since it was now a redundant/inconsistent path.

## 2026-07-30 — Swagger JWT resolved, signing-key hardening

- Hit a runtime `IDX10720` exception: the dev JWT signing key was too short for HS256 (208 bits, needs ≥256) -> fixed by Base64-decoding the key and validating length (≥32 bytes) at token-generation time with a clear failure message instead of a silent/obscure crypto error.
- Continued the Swagger-JWT saga from 07-29: tried re-adding `AddSecurityRequirement`, hit repeated build failures as package versions were swapped (Swashbuckle 10.2.3 ↔ 6.5.0, `Microsoft.OpenApi` add/remove) -> eventually settled on Swashbuckle 6.5.0 with the classic `OpenApiSecurityScheme`/`OpenApiReference` shape, which compiled and worked.
- Diagnosed a confusing 401 (`invalid_token`): pasting `"Bearer <token>"` into Swagger's HTTP-bearer Authorize dialog produced a doubled `Authorization: Bearer Bearer <token>` header -> explained and fixed by documenting "paste the raw token only, no `Bearer ` prefix" rather than switching scheme types.
- Question raised: does auth even need `AddSecurityRequirement` to work in Swagger? -> clarified it only affects OpenAPI *metadata* (lock icons, codegen), not whether the Authorize dialog actually attaches the header -> decided to drop it for simplicity, since accurate per-operation security metadata wasn't a priority for this stage.

## 2026-08-01 — Full test suite (Domain/Application/Integration)

- Asked Copilot to create a three-tier test suite (xUnit + FluentAssertions + Moq + `WebApplicationFactory`/SQLite in-memory), explicitly *no* controller unit tests, organized by feature -> generated `Tests.Domain`, `Tests.Application`, `Tests.Integration` with tests for Customer/Reservation invariants, `CreateReservation` use-case scenarios, and an end-to-end register→login→call-protected-endpoint integration test.
- Build broke on incorrect `ProjectReference` paths and an unavailable Moq version -> fixed paths and pinned Moq to 4.20.72.
- Hit `System.InvalidOperationException: Only a single database provider can be registered` in `CustomWebApplicationFactory` (production SQL Server + test SQLite both registered) -> fixed properly on the *third* attempt: `services.RemoveAll<T>()` for the DbContext descriptors plus an isolated `UseInternalServiceProvider` scoped to SQLite-only services, so the two providers never coexist in the same service collection.
- Integration test `Register_Login_And_Access_Mine` failed silently (deserialized the token as `dynamic` and read `.token`, but the API returns `Token` with a capital T) -> fixed by deserializing into the actual `AuthenticationResponse` type instead of `dynamic`.
- Asked Copilot to just run and fix the failing test itself rather than proposing fixes -> it ran the suite (7/8 passing), applied the `ConfigureWebHost`-only fix above, reran, all 8 passing -> decision: prefer "run it and show me green" over "here's what might work" going forward for test debugging.

## 2026-08-02 — Docs archive, README status (Claude Code)

- Asked Claude Code to archive the raw ChatGPT/Copilot chat exports into `docs/raw-ai-logs/` and update the README status line -> done, reflecting the implemented backend (Clean Architecture, JWT auth, three-tier tests) with Angular/Docker/CI still marked in progress.

## 2026-08-03 — Repo restructure and living workflow log (Claude Code)

- Asked Claude Code to plan overall project structure (monorepo vs. multi-repo, folder layout, frontend framework, sequencing of review/frontend/devops/logging) before doing any deep backend review -> agreed: single monorepo, restructure into `backend/` + `frontend/` (backend moved via `git mv`, verified with `dotnet build`/`dotnet test` — 8/8 passing), Angular confirmed as the frontend framework (reconsidered explicitly against Vue and React first).
- Asked Claude Code to make the AI-workflow log self-maintaining instead of manual -> this file was reconstructed by Claude Code from the ChatGPT summary *and* the full Copilot chat logs (which had more concrete detail — package/build struggles, the SQLite provider conflict, the Swagger back-and-forth — than the ChatGPT summary alone), dated using the git commit history as the anchor, and promoted from `docs/raw-ai-logs/` into a living `docs/WORKFLOW_LOG.md`; a standing instruction was added to `CLAUDE.md` so future Claude Code sessions keep appending to it without being asked.
- Asked Claude Code to lay out the roadmap -> wrote `docs/ROADMAP.md` fixing the phase order going forward: UX/design pass before the backend contract pass (so the frontend's actual needs drive the API, not the reverse), Angular implementation, DevOps, application/runtime logging, then the deferred deep backend review, then a final documentation pass.
- Visual Studio flagged vulnerable packages plus a deprecated `xunit` -> investigated with `dotnet list package --vulnerable --include-transitive`/`--outdated` plus web research, confirming two fixable root causes: `xunit` 2.9.3 deprecated in favor of `xunit.v3`, and `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 high-severity transitive vuln (GHSA-2m69-gcr7-jv3q) via `Microsoft.EntityFrameworkCore.Sqlite` in `Tests.Integration`, fixable by pinning a patched version directly -> applied `xunit.v3 3.2.2` across all three test projects (drop-in, `dotnet test` green on all 8 tests) and pinned `SQLitePCLRaw.bundle_e_sqlite3` to `3.0.5` in `Tests.Integration` (confirmed clean via `dotnet list package --vulnerable` afterward).
