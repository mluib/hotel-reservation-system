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

## 2026-08-03 — Phase 1 UX/design pass: wireframe and handoff (Claude Code)

- Asked how to connect claude.ai/design to Claude Code for the UX pass -> clarified they're separate products with no automatic context bridge, so decided to draft wireframes directly in this session as an artifact instead.
- Asked Claude Code to inventory the real API before sketching wireframes -> surfaced three gaps: no room filtering, no stored reservation price, no customer self-service cancel.
- Asked for a low-fidelity wireframe of the key screens -> produced a 4-screen artifact (auth, browse rooms, book & manage, admin), explicitly flagging the three gaps above as candidates for the backend contract pass rather than silently designing past them.
- Reviewed the first wireframe pass -> decided room filtering should be server-side, reservations should snapshot their price at booking time, and customers should get a soft-cancel action distinct from admin's existing hard delete; wireframe updated accordingly.
- Discussed adding a photo to room cards -> decided on one photo per room with a placeholder default for rooms without one, flagged as a fourth backend candidate rather than blocking the design pass on it.
- Asked for the wireframe's buttons and forms to actually work instead of being static placeholders -> wired real inline create/edit forms, working delete/cancel actions, and a full browse-to-booking-to-my-reservations click-through, entirely client-side within the artifact.
- Imported the finished claude.ai/design mockup back into this session -> confirmed it covers all four flows plus supporting dialogs with a consistent visual language; next step is translating it into the real Angular frontend.
- Noticed the finished mockup also gives the hotel itself a hero photo, not just rooms -> extended the earlier photo decision to the hotel record too (one photo, same placeholder-until-uploaded treatment), rather than treating it as a separate feature.
- Noticed the mockup's tightened delete flow expects a server-side rejection when a room or customer still has reservations -> flagged as a real backend requirement, not yet confirmed as implemented: deletes need to reject (not cascade or crash) in that case, with the error surfaced to the UI.
- Considered shipping the Angular frontend with placeholder photos and building the real upload feature afterward -> corrected: that would mean touching the backend again after the frontend build starts, defeating the point of doing the contract pass first; decided to implement photo upload now as a fourth Phase 2 item alongside the other three, and handed a scoped brief to the parallel backend session.
- Noticed the downloaded claude.ai/design export had been saved under `frontend/`, which is reserved for the real Angular app -> relocated it to `docs/raw-ai-logs/ClaudeDesignMockup/`, alongside the project's other frozen AI-tool exports, since the bundle is an explicitly static reference for implementation (UI files, not a chat transcript), not runnable app code.

## 2026-08-03 — Phase 1 UX/design pass: mockup (Claude Design)

- Answered claude.ai/design's setup questions before it built anything -> specified all four flows at clickable-prototype fidelity, an airy editorial feel throughout except a more neutral/functional admin section, and one admin-uploadable placeholder photo per room.
- First prototype came back on a serif "Classical" design system with an outline-only gold accent and justified text -> reviewed and asked for something more modern while still natural instead: sans-serif type, tighter non-touching margins (room grid, booking summary), more color (status colors, distinct destructive vs. neutral action colors), price filters, a proper Home page, and admin-only photo editing.
- Redesign came back -> a modern sans-serif look (warm neutral palette, terracotta accent), a Home page added, "Browse Rooms" renamed to "Rooms", min/max price filters, edge-to-edge room photos instead of double-margined cards, a plain non-card-looking booking summary row, a temporary fading highlight on cross-tab jumps, an unsaved-changes guard on the Hotel admin form (site header only updates after Save), and photo editing restricted to admins.
- Found the redesign's cross-tab highlight still lingering too long and the photo-upload control still visible to non-admin roles -> fixed both: a short (~1s) fade, and non-admin roles now see a plain placeholder instead of an upload prompt.
- Asked for reservation Cancel to go through the same confirm dialog as Delete -> done, both actions now share it.
- Asked for check-in/check-out validation errors to show while typing instead of only after submitting -> fixed, shows live once both dates are entered.
- Asked for deleting a room or customer to be blocked when it still has reservations, verified server-side rather than just skipping the confirm dialog -> the mockup's delete flow now shows a server-rejection error dialog instead of a silent confirm.

## 2026-08-03 — Phase 2 backend contract pass: room filtering, reservation price, cancel (Claude Code)

Implemented the three flagged decisions from the wireframe review, in parallel with the Phase 1 design session above.

- Asked Claude Code to inventory the current rooms/reservations code before implementing anything -> confirmed rooms had no filtering or availability concept, the reservation repository had no update method, and reservation cancellation already existed as a tested domain method that was never wired up to the API.
- Implemented server-side room filtering -> the rooms listing endpoint now accepts type, price-range, and date-range query parameters, filtered at the database level.
- Implemented the reservation price field -> reservations now store the room's price at the time of booking, added the accompanying database migration.
- Implemented the cancel-reservation endpoint -> added an action that verifies the caller owns the reservation before cancelling it; ran the full test suite afterward, all green, with added coverage for the price field and the cancel ownership check.
- Asked whether the ownership check was still needed given admins already have a hard-delete option -> clarified it protects customers from cancelling each other's reservations, independent of admin's separate path.
- Pointed out hard-deleting now destroys the reservation's price history, and asked whether admins should get the softer cancel action too -> extended the cancel endpoint to admins, bypassing the ownership check for that role.
- Asked whether the room date filter could run inside the database query instead of in memory -> moved the date-availability filter into the database query.
- Asked whether the price-field migration had been applied to the database -> confirmed it hadn't been yet, then applied it after checking with the user first.
- User pointed out several bullets read as user decisions when they were actually the agent's own unprompted implementation choices, and that entries carried too much code-level detail -> rewrote the whole file to the stricter one-arrow schema, split same-day entries into topic-based sections, and updated the standing instruction and memory to match.

## 2026-08-05 — Phase 2 backend contract pass: room/hotel photo upload (Claude Code)

The fourth flagged item from the wireframe review, handed over as a scoped brief from the parallel design session.

- Asked Claude Code to implement admin-only photo upload for the room and hotel records, one photo each, local disk storage -> implemented across all layers: domain fields settable only through dedicated methods, application-layer validation and use cases, a disk-backed storage service, admin-only upload endpoints, and static file serving so the stored images are reachable.
- Decided the saved file name and extension should come from the server, not the client -> derived the extension from the validated content type and keyed the file name to the entity's own id, so a re-upload always overwrites the same file rather than accumulating client-supplied names.
- Considered giving the storage service a direct dependency on ASP.NET Core's hosting abstractions to locate the upload folder -> resolved the folder path in the Api layer instead, keeping the storage service a plain class with no web-framework dependency; generated the accompanying database migration and added matching unit test coverage, full three-tier suite green afterward.

## 2026-08-05 — Phase 2 backend contract pass: reject deleting rooms/customers with reservations (Claude Code)

A second item flagged from the same mockup review; asked to implement it directly in this session rather than as another brief to the backend session.

- Asked to implement the fix directly in this session -> both delete use cases now check for related reservations first (any status — a cancelled reservation is still a historical record that would be orphaned) and reject with a clear error instead of letting the database cascade or throw a raw exception, with matching unit test coverage added and the full three-tier suite green afterward.

## 2026-08-06 — Phase 3 kickoff: planning (Claude Code)

- Asked to execute Phase 3, but first confirm Phase 1 and 2 were actually finished, using the Claude Design mockup as the entry point -> cross-checked the workflow log against the current controllers and DTOs, confirmed both phases complete, and confirmed the mockup as the starting point.
- Asked whether planning was needed given no prior Angular experience -> entered a planning pass covering prerequisites, folder structure, routing, auth handling, and styling approach.
- Asked how Node.js should be installed and how the build should be sequenced given no Angular experience -> chose to install Node.js manually with guidance rather than have it installed automatically, and to build Phase 3 as one continuous pass rather than staged review checkpoints.
- Approved fixing two backend gaps surfaced while reviewing the API for the frontend build (no CORS policy, no way to obtain an Admin account), plus a dev-only Admin account seed -> proposed as small, scoped additions alongside the frontend work rather than deferred.
- Draft plan reviewed and annotated with four questions — whether Angular uses TypeScript, whether an already-installed Node.js needed a PATH change, a request to pause for a commit before starting the frontend, and what the in-app browser tool is -> confirmed TypeScript, found the PATH was already correct but not yet visible to this session's own shells, added a mid-build commit checkpoint, and explained the browser tool; plan updated with all four.
- Asked whether the real, already-attached SQL Server database would be used for verification -> confirmed it would be, the same database already used throughout backend development rather than an isolated test environment; existing row counts were checked and the plan updated to say so plainly.
- Asked whether the chosen Angular architecture was standard practice or a personal choice, given modern alternatives exist -> laid out which parts follow Angular's current defaults (standalone components, functional guards/interceptors, signals over a state-management library) versus deliberate choices against real alternatives (zoneless change detection, Angular Material or Tailwind, template-driven forms), then kept the plan as recommended once confirmed.

## 2026-08-06 — Phase 3: backend prep (Claude Code)

- Backend tweaks approved during planning (CORS policy, string-serialized enums, a customer "my profile" endpoint, a dev-only admin account seed) -> all four implemented; full three-tier test suite stayed green.
- Asked why Program's entry point became async and switched to RunAsync -> explained the dev admin seed's asynchronous Identity calls required it, with RunAsync following for consistency once the method was already async.
- Pointed out a CORS code comment described config-driven origins that weren't actually configured anywhere -> corrected by adding the real config entry and rewording the comment to match.
- Approved committing the backend tweaks as their own commit, separate from the upcoming frontend work -> committed.

## 2026-08-06 — Phase 3: Angular scaffold and full implementation (Claude Code)

- Reported the session had been restarted to pick up the newly installed Node.js -> still not visible to this session's shells; traced to the Machine-level PATH being correct but not yet inherited by the already-running process tree.
- Reported multiple Claude Code processes still running after closing and reopening the application -> diagnosed via process start times as several stale process trees left over from prior days rather than a fresh restart, most likely because closing the window doesn't fully quit a tray-resident Electron app.
- Asked what Electron is -> explained in plain terms as background context to the troubleshooting.
- Asked whether that makes it a hybrid app -> clarified the similarity to mobile hybrid apps and the key difference (Electron bundles its own Chromium rather than using the OS's WebView).
- Reported the application had been fully restarted -> Node.js and npm became visible to this session's shells; from there, with no further developer input until the flows were shown for review, the entire frontend was built and verified in one continuous pass: the Angular workspace was scaffolded (standalone components, no state-management library, plain CSS reproducing the mockup's palette and typography); the public/customer flow went in against the real API in place of the mockup's fake roles and data (Home, sign-in/sign-up, room browsing with server-side filtering, booking, My Reservations); a live click-through of those screens surfaced and fixed two bugs (a wrong guess at the JWT's role-claim identifier, corrected after decoding a real token to confirm the actual key; the booking page's live total freezing at zero, fixed by bridging a form's value stream into a signal); the full admin section went in against the real API (rooms, reservations, customers, and hotel tabs, matching dialogs, a cross-tab highlight, and an unsaved-changes guard on the hotel form); two mockup features were dropped rather than built against endpoints that don't exist (editing a reservation's dates/status directly, and uploading a room photo before the room exists); and every flow was verified end to end against the real backend and its dev database, including both delete-rejection paths, using Claude Code's own browser tool.
- Asked whether the log entries for this session actually held to the stated conventions -> re-reviewed them against the rules and found three bullets missing their required arrow, a chronology error, a factual reversal in the JWT-bug description, and several real exchanges missing entirely (the plan-annotation round, the database question, the architecture-alternatives question, the Electron/hybrid-app questions collapsed into one bullet instead of two); rewrote the affected entries.
- Asked whether a specific rewritten bullet actually reflected something the developer had asked for -> confirmed it didn't; reframed it as a continuation of already-approved work rather than a fabricated prompt.
- Pressed further on whether that reframed bullet was itself developer-prompted -> clarified it wasn't, and that the log's own precedent already allows a bullet's trigger to be a reported event or a developer decision rather than a literal request, as long as it doesn't misattribute who acted.
- Clarified that the clause before the arrow must be developer input only, with everything else moved after it, and asked for the standing instruction to exist in one place instead of duplicated between `CLAUDE.md` and session memory -> tightened the rule in `CLAUDE.md` accordingly, removed the duplicate memory file, and rewrote this session's entries to match.
