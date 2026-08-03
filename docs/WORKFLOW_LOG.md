# AI-assisted Development — Workflow Log

Running log of notable AI-agent interactions during development: what was generated, what was wrong or needed correction, and why.

Kept lightweight — a couple of bullet points per entry, added as things happen. Used as raw material for a polished writeup at the end of the project.

Format:

- `[what was asked] -> [what was produced]` — exactly one `->` per bullet.
- A follow-up correction, refinement, or decision is its **own bullet** in the same form, not a clause tacked onto the original one — it's a separate ask, even if it happened moments later.
- Describe outcomes and reasoning, not specific classes/methods/files — that detail already lives in the diffs and commits; the log's job is to stay legible to a reader who isn't going to open the code.
- The agent never refers to itself as "I"/"me"/"mine" — write "Claude Code"/"Copilot"/"the agent", or phrase it passively (e.g. "rewrote the file" rather than "I rewrote the file").

This is the living version of the log — reconstructed and dated from the raw ChatGPT/Copilot exports in `docs/raw-ai-logs/` plus the git history, and maintained going forward per the standing instruction in [`CLAUDE.md`](../CLAUDE.md). `docs/raw-ai-logs/` remains untouched as the frozen original transcripts this was built from.

---

## 2026-07-21 — Project planning and initial architecture

- Discussed project goals and asked ChatGPT to recommend an architecture -> settled on an ASP.NET Core Web API with Clean Architecture (Domain/Application/Infrastructure/Api, dependencies pointing inward).
- Asked Copilot to scaffold the Domain project structure -> produced plain data classes for the core entities (Hotel, Room, Reservation, Customer, RoomType), accepted as a first-pass skeleton pending a business-rules review.
- Asked Copilot to review those entities for missing relationships and business rules -> flagged anemic-model issues: no overlap prevention, no status, no nights/price calculation, no value objects.
- Decided which of those gaps to close immediately vs. defer -> added overlap prevention, reservation status, and nights/price calculation now; explicitly deferred value objects, aggregates, and domain events to keep v1 simple.
- Asked Copilot to sanity-check the updated domain model as a "first version, avoid advanced DDD patterns" pass -> confirmed it was suitable for v1 and recommended relying on database constraints instead of optimistic concurrency for now.
- Reviewed the reservation use case and its repository interface for correct dependency direction -> confirmed Application depends only on Domain, and Infrastructure implements Application's interfaces; locked in as the template for all later features.

## 2026-07-22 — Persistence, first API, validation, Customer CRUD

- Asked Copilot to add persistence and the first API endpoint -> produced the database context, reservation repository, first migration, first reservation endpoint, and Swagger UI; low-friction, accepted as-is.
- Asked Copilot to implement reservation validation (no overlaps, check-out after check-in, room/customer must exist) -> added the checks to the create-reservation flow; accepted as a v1-acceptable check-then-insert race condition rather than an atomic database operation.
- Asked Copilot to implement Customer CRUD following the existing pattern -> generated the full stack, but used reflection to set the entity id on update.
- Asked for a proper domain update method instead of reflection, since reflection bypasses entity invariants -> Copilot replaced it with a domain update method.
- Noticed the update request duplicated the id (required to match the route id, with an extra check) -> removed the id from the request body, made the route id authoritative.
- Asked for Rooms/Hotel/Reservation CRUD endpoints -> Copilot hit its per-turn tool-call limit mid-way and a Visual Studio debugger lock blocked the build; resolved by finishing on request and stopping the debugger before rebuilding.
- Asked to change POST responses to return the full created resource instead of a bare success response -> applied across all controllers, accepted.

## 2026-07-29 — Documentation pass, Identity/JWT authentication, authorization

- Asked ChatGPT to generate project documentation and architecture-decision docs -> produced the early reference docs (tech stack, decisions, architecture overview), accepted and later superseded by this living log.
- Asked Copilot to add Identity + JWT Bearer authentication (register/login, role-based authorization) without leaking auth concerns into the Domain layer -> first pass put JWT logic partly inside the controller and Infrastructure directly.
- Asked to move the JWT token contract out of Infrastructure -> relocated it into the Application layer so Application doesn't depend on Identity types.
- Asked to move the whole auth workflow out of the controller -> extracted it into a dedicated auth service; the controller now only delegates to it.
- Decided the JWT subject claim should be the immutable user id, not username/email, per OAuth/OIDC convention -> implemented, keeping a name claim alongside for convenience.
- Asked to enable JWT auth in Swagger -> first attempts referenced package types that didn't compile against the installed version, several rounds of trial and error.
- Called out the repeated wrong guesses directly -> corrected to the actually-available package API and shipped a reduced config; full resolution deferred to the next day.
- Decided registration should never accept a client-supplied role, since that's a privilege-escalation risk -> forced the role unconditionally server-side, removed the role field from the register request.
- Defined per-controller authorization rules (customers admin-only; rooms/hotel public read with admin-only writes; reservations customer-create/own-view with admin-manage) -> applied via role-based authorization, register/login left anonymous.
- Asked for reservation ownership authorization so customers only see their own reservations -> added a current-user service and enforced ownership checks inside the relevant flows.
- Refined the design: a client-supplied customer id on create is a spoofing risk -> removed it entirely (always inferred from the JWT instead), added a separate "my reservations" endpoint.
- Simplified further: mixed ownership branching inside one endpoint was confusing -> split cleanly into an admin-only get-by-id and a customer-facing "mine" endpoint, no in-handler branching.
- Caught a modeling issue: the JWT user id was being parsed directly into the customer's own id -> corrected by giving Customer a separate identity-user reference, since the auth identity and the business customer are linked but distinct concepts.
- Decided registration should auto-create the linked domain Customer rather than needing a separate call -> register now creates the identity user, assigns the role, and creates the linked customer record together; removed the now-redundant standalone customer-create endpoint.

## 2026-07-30 — Swagger JWT resolved, signing-key hardening

- Hit a runtime error: the dev JWT signing key was too short for the signing algorithm -> fixed by validating key length at token-generation time with a clear failure message instead of a silent crypto error.
- Continued resolving the Swagger/JWT integration from the day before -> settled on a specific package version whose API shape actually compiled and worked, after repeated version swaps failed.
- Diagnosed a confusing authentication failure in Swagger -> traced it to pasting the token with a redundant "Bearer" prefix into the Authorize dialog, fixed by documenting "paste the raw token only".
- Asked whether Swagger's security-requirement metadata was actually required for auth to work -> clarified it only affects displayed metadata (lock icons, codegen), not whether the header is actually sent, so dropped it for simplicity.

## 2026-08-01 — Full test suite (Domain/Application/Integration)

- Asked Copilot to create a three-tier test suite, explicitly no controller unit tests, organized by feature -> generated the domain, application, and integration test projects covering entity invariants, use-case scenarios, and an end-to-end auth-then-call-protected-endpoint flow.
- Build broke on incorrect project references and an unavailable test-mocking package version -> fixed the references and pinned a working version.
- Hit a database-provider conflict between the production and in-memory test setup -> fixed properly on the third attempt by isolating the test database's service registration from the production one.
- An integration test failed silently due to a case-sensitive property mismatch when deserializing the auth response -> fixed by deserializing into the actual response type instead of a loosely-typed one.
- Asked Copilot to run and fix the failing test itself rather than just proposing fixes -> it iterated to all tests green; adopted "run it and show me green" as the preferred approach for test debugging going forward.

## 2026-08-02 — Docs archive, README status (Claude Code)

- Asked Claude Code to archive the raw ChatGPT/Copilot chat exports and update the README status line -> done, reflecting the implemented backend (Clean Architecture, JWT auth, three-tier tests) with Angular/Docker/CI still marked in progress.

## 2026-08-03 — Repo restructure, self-maintaining log, roadmap, dependency fixes (Claude Code)

- Asked Claude Code to plan the overall project structure (monorepo vs. multi-repo, folder layout, frontend framework, phase sequencing) before any deep backend review -> agreed on a single monorepo restructured into backend/ + frontend/, with Angular confirmed as the frontend framework after being weighed against Vue and React.
- Asked Claude Code to make the AI-workflow log self-maintaining instead of manual -> this file was reconstructed from the ChatGPT summary and the full Copilot chat logs, dated using git history, and promoted into a living document; a standing instruction was added to `CLAUDE.md` so future sessions keep appending to it without being asked.
- Asked Claude Code to lay out the roadmap -> wrote the phase-by-phase plan: UX/design pass before the backend contract pass (so the frontend's real needs drive the API, not the reverse), then Angular, DevOps, application logging, the deferred deep backend review, and a final documentation pass.
- Visual Studio flagged vulnerable and deprecated packages -> investigated and confirmed two fixable root causes (a deprecated test framework, a high-severity transitive dependency vulnerability), upgraded/pinned both; tests stayed green.

## 2026-08-03 — Phase 2 backend contract pass: room filtering, reservation price, cancel (Claude Code)

Implemented the three flagged decisions from the wireframe review, alongside a separate Phase 1 design session.

- Asked Claude Code to inventory the current rooms/reservations code before implementing anything -> confirmed rooms had no filtering or availability concept, the reservation repository had no update method, and reservation cancellation already existed as a tested domain method that was never wired up to the API.
- Implemented server-side room filtering -> the rooms listing endpoint now accepts type, price-range, and date-range query parameters, filtered at the database level.
- Implemented the reservation price field -> reservations now store the room's price at the time of booking, added the accompanying database migration.
- Implemented the cancel-reservation endpoint -> added an action that verifies the caller owns the reservation before cancelling it.
- Ran the full test suite after the change -> all tests passed, added new coverage for the price field and the cancel ownership check.
- Asked whether the ownership check was still needed given admins already have a hard-delete option -> clarified it protects customers from cancelling each other's reservations, independent of admin's separate path.
- Pointed out hard-deleting now destroys the reservation's price history, and asked whether admins should get the softer cancel action too -> extended the cancel endpoint to admins, bypassing the ownership check for that role.
- Asked whether the room date filter could run inside the database query instead of in memory -> moved the date-availability filter into the database query.
- Asked whether the price-field migration had been applied to the database -> confirmed it hadn't been yet, then applied it after checking with the user first.
- User pointed out several bullets read as user decisions when they were actually the agent's own unprompted implementation choices, and that entries carried too much code-level detail -> rewrote the whole file to the stricter one-arrow schema, split same-day entries into topic-based sections, and updated the standing instruction and memory to match.
