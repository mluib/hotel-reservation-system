## Context

Backend (ASP.NET Core, Clean Architecture: Domain/Application/Infrastructure/Api + 3 test projects) is functionally done; Angular frontend, Docker, and CI/CD are still open per the README. The user wants to pause before doing a deep backend review, and instead lock in an overall project structure and sequencing plan first — because frontend requirements should drive what backend work is actually worth doing, not the other way around.

This project is a CV portfolio piece proving deliberate, documented AI-agent collaboration (see [project_purpose_and_goals.md](../../../.claude/projects/D--Dokumente-Eigenprojekt-hotel-reservation-system/memory/project_purpose_and_goals.md) memory). `docs/raw-ai-logs/ChatGPT/` already contains prior planning output — `tech-stack.md` even pre-defines an attribution split: *"Claude / Claude Code: architecture discussion, code generation and review, Angular frontend scaffolding, DevOps setup, and maintaining the AI-workflow log."* This plan operationalizes that: it restructures the repo for the frontend addition, promotes the existing ad-hoc workflow log into a living document, sets up a standing instruction so the log gets maintained automatically across future sessions without the user having to ask, and lays out the phase order for review/frontend/devops/logging.

User decisions already confirmed for this plan:
- **Single monorepo** (not separate backend/frontend repos) — one coherent CV artifact, one CI pipeline, one workflow-log narrative.
- **Restructure into `backend/` + `frontend/`** subfolders (not backend-stays-at-root) — symmetric layout, done via `git mv` to preserve history.
- **Frontend: Angular** — reconsidered explicitly against Vue (the stack the rejecting job actually asked for) and React (broadest job-market demand), and confirmed: Angular's DI/services/modules/RxJS structure transfers best from the user's ASP.NET Core experience.
- "Design with Git Claude Design" = `claude.ai/design` — functionally the same as "use Claude to draft mockups/wireframes before/alongside coding," just via that specific Claude surface instead of plain chat. Recommended: use it (or Claude artifacts) to nail down screens/flows *before* Angular scaffolding, so component structure follows real UX decisions instead of guesses.

**Terminology note**: "raw-ai-logs exports" = the markdown files already sitting in `docs/raw-ai-logs/ChatGPT/` and `docs/raw-ai-logs/Copilot/` — these are saved/copied transcripts and summaries of the actual ChatGPT and Copilot chat sessions from early development. They're called "raw" to distinguish them from the curated, continuously-maintained `docs/WORKFLOW_LOG.md` this plan introduces — raw-ai-logs stays a frozen historical archive; WORKFLOW_LOG.md is the living document built from it and extended going forward.

## Target folder structure

```
hotel-reservation-system/
├── backend/
│   ├── HotelReservationSystem.slnx
│   ├── HotelReservation.Api/
│   ├── HotelReservation.Application/
│   ├── HotelReservation.Domain/
│   ├── HotelReservation.Infrastructure/
│   ├── HotelReservation.Tests.Domain/
│   ├── HotelReservation.Tests.Application/
│   └── HotelReservation.Tests.Integration/
├── frontend/                     (placeholder now, Angular app scaffolded in a later session)
│   └── README.md
├── docs/
│   ├── WORKFLOW_LOG.md           (promoted from raw-ai-logs, the living log going forward)
│   ├── ROADMAP.md                (phase plan, see below)
│   └── raw-ai-logs/              (unchanged — frozen historical exports: ChatGPT/, Copilot/)
├── .github/workflows/            (still empty here; filled in during the DevOps phase)
├── CLAUDE.md                     (new — project brief + standing log-maintenance instruction)
└── README.md
```

Backend move is a `git mv` of the 7 project folders + `HotelReservationSystem.slnx` into `backend/` as a group — internal relative paths between projects (ProjectReferences, `.slnx` project paths) are unaffected since siblings move together. Verified afterward with `dotnet build` and `dotnet test` from `backend/`.

## Steps

1. **Move backend into `backend/`**: `git mv` each of the 7 project directories and `HotelReservationSystem.slnx` into a new `backend/` folder. Run `dotnet build` and `dotnet test` from `backend/HotelReservationSystem.slnx` to confirm nothing broke (migrations, launchSettings, and project references are all relative and travel with the move).

2. **Add `frontend/` placeholder**: create `frontend/README.md` stating the Angular app will be scaffolded in a dedicated future session (Phase 2/3 of the roadmap), so the folder exists and is git-tracked without prematurely generating Angular boilerplate now.

3. **Reconstruct and promote the workflow log** (not a plain rename — the existing ChatGPT-authored log undersells the Copilot chats, and has no dates):
   - Read the full `Copilot/ChatLog1.md`, `ChatLog2.md`, `ChatLog3.md` in full (not just excerpts) alongside the existing `ChatGPT/ai-workflow-log...md`, and merge them into one complete, chronological set of entries — filling in concrete implementation moments the ChatGPT summary skipped (e.g. the SQL Server/SQLite "single database provider" conflict, the multi-round Swagger-JWT/OpenApi package struggle spanning two chat logs, the reflection-based-update anti-pattern fix).
   - Date each entry using the actual git commit history as the anchor (already pulled — day-level granularity, e.g. `2026-07-21` initial architecture/domain model, `2026-07-22` persistence/API/CRUD, `2026-07-29` Identity/JWT/authorization, `2026-07-30` Swagger-JWT fix, `2026-08-01` test suite), rather than leaving entries undated.
   - Write the result to `docs/WORKFLOW_LOG.md` as the new living document (keep the same lightweight bullet format: `[what was asked] -> [what the agent produced] -> [correction/decision made, and why]`, one line prefixed with its date). `docs/raw-ai-logs/` stays untouched as the frozen raw archive feeding this doc.
   - This session's own restructuring work becomes the log's first entry going forward, dated today.

4. **Write `docs/ROADMAP.md`** capturing the phase order (detail below) so "what's next" is answered in the repo itself, not just in chat.

5. **Create root `CLAUDE.md`**: concise project brief (stack, architecture, layer rules pulled from `decisions.md`/`tech-stack.md`), a **tool-attribution note** — which AI tool did what across the project's history, so the CV writeup credits/corrections accurately (`tech-stack.md` already documents this split: ChatGPT = initial idea/planning/prompts, Copilot = ask/agent-mode implementation, Claude/Claude Code = review, Angular scaffolding, DevOps, workflow-log upkeep) — and an explicit standing instruction: *maintain `docs/WORKFLOW_LOG.md` proactively across all future sessions — append a dated entry for every meaningful prompt/decision without being asked, keep entries to a few bullet points, save detail for the end-of-project writeup.* This is what makes the logging "automatic" from the user's side — a repo-level instruction any Claude Code session here will pick up, not a mechanism the user has to trigger.

6. **Save project memories** (user-memory system, not repo files): the reconstructed ChatGPT → Copilot → Claude project history and key architecture decisions (from the raw-ai-logs digest), and the repo-structure decisions made in this session — so future sessions don't need to re-read the raw logs to get this context.

7. Leave everything **staged but uncommitted** for the user to review; no commit happens unless explicitly requested (per [[commit-message-preferences]] memory and standing git-safety rules).

## Roadmap (`docs/ROADMAP.md` content)

- **Phase 0 (this session)**: repo restructure, docs, living workflow log, roadmap.
- **Phase 1 — UX/design pass**: use Claude (`claude.ai/design` or artifacts) to sketch key screens/flows (auth, browse rooms, book/manage reservation, admin views) *first* — before touching the API contract, since the screens determine what endpoints/fields are actually needed.
- **Phase 2 — Backend contract pass**: informed by Phase 1's screens, adjust/extend the DTOs and endpoints the frontend will actually consume (naming, response shapes, any missing fields or endpoints the design surfaced) — *not* the full review-backlog from `tech-stack.md`. Deep optimization (DDD value objects, pagination, FluentValidation, perf) is deliberately deferred to Phase 6, once real frontend usage shows what actually matters.
- **Phase 3 — Angular scaffold + implementation**: build against the stabilized API contract; small backend contract tweaks feed back as they surface.
- **Phase 4 — DevOps**: `backend/Dockerfile`, `frontend/Dockerfile`, root `docker-compose.yml` for local full-stack runs, GitHub Actions CI (build+test backend, build+lint frontend), CD optional/stretch.
- **Phase 5 — Application/runtime logging**: add structured *operational* logging to the running backend (e.g. Serilog — request logs, error logs; already flagged in the existing review backlog as "Add proper logging"). **The deliverable here is code, not files**: the Serilog setup/configuration in `Program.cs` and `ILogger` calls at the right points in Application/Infrastructure are what get committed and are what a reviewer actually sees — the same way test *code* is committed but a test run's console output isn't. The log *output* itself is transient runtime data (console/stdout while the app runs, same category as `bin/`/`obj/` — never committed, nothing to gitignore-and-wonder-about because it's not written to a repo folder in the first place). This is a different thing from `docs/WORKFLOW_LOG.md`: that's the meta-record of *how the project was built with AI*, is an actual committed doc, and isn't a phase at all — it's set up in Phase 0 (via the standing `CLAUDE.md` instruction) and maintained continuously across every phase.
- **Phase 6 — Deep backend review**: the full backlog already captured in `tech-stack.md` and reconfirmed by the user — done last, informed by real frontend/DevOps needs:
  - Consistent REST style
  - Proper Swagger response documentation (`ProducesResponseType`)
  - DTO review
  - DDD improvements (value objects like `EmailAddress`/`Money`, domain services/events, aggregate review)
  - API response improvements (status codes)
  - Validation improvements (required/stringlength/emailaddress, ModelState handling, FluentValidation, `ProblemDetails`, global exception handling)
  - Review unnecessary database calls
  - Repository review (e.g. `Include`s)
  - Pagination for larger datasets
  - Proper logging
  - JWT: role/user seeding, JWT key cleanup
  - Tests: authorization integration tests, application ownership tests, repository/infrastructure tests (save/load reservations)
- **Phase 7 — Final documentation pass**: polished architecture write-up and README, using `docs/WORKFLOW_LOG.md` as raw material — the CV deliverable.

## Verification

- `dotnet build` and `dotnet test` succeed from `backend/HotelReservationSystem.slnx` after the move (confirms the restructure didn't break anything).
- `git status` / `git diff --stat` reviewed with the user before any commit, showing the moves as renames (not delete+add) so history is preserved.
- New files (`CLAUDE.md`, `docs/ROADMAP.md`, `docs/WORKFLOW_LOG.md`, `frontend/README.md`) read back to confirm content is accurate and consistent with the raw-ai-logs history.
