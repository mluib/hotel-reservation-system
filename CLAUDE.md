# Hotel Reservation System — Project Brief

A CV portfolio project: a hotel reservation system built with deliberate, documented AI-agent collaboration, not just AI-generated code. The point of the project is as much the visible decision-making and correction trail as the app itself.

## Stack & architecture

- **Backend** (`backend/`): ASP.NET Core Web API on .NET 10, Clean Architecture — `HotelReservation.Domain` (entities/enums, no external deps) → `HotelReservation.Application` (use cases, DTOs, repository interfaces) ← `HotelReservation.Infrastructure` (EF Core, repositories, Identity, JWT) ← `HotelReservation.Api` (thin controllers, DI wiring). Three test projects: `Tests.Domain` (unit), `Tests.Application` (unit, Moq), `Tests.Integration` (`WebApplicationFactory` + SQLite in-memory).
- **Frontend** (`frontend/`): Angular (standalone components, signals, no NgRx), built against the Claude Design mockup — see `docs/ROADMAP.md`.
- Auth: ASP.NET Core Identity + JWT Bearer, role-based authorization. `IdentityUser` (auth) and domain `Customer` (business entity) are deliberately separate, linked via `IdentityUserId` — see `docs/raw-ai-logs/ChatGPT/decisions.md` for the full rationale.
- Full architectural rationale lives in `docs/raw-ai-logs/ChatGPT/{tech-stack,decisions}.md` (written early in the project, still accurate for the backend).

## Where things stand / what's next

See [`docs/ROADMAP.md`](docs/ROADMAP.md) for the phase-by-phase plan (UX design → backend contract pass → Angular → DevOps → logging → deep backend review → final docs). Don't jump ahead to the deep backend review backlog before the frontend phases — it's deliberately sequenced last so it's informed by real usage.

## Tool-attribution note (for the CV writeup)

Which AI tool did what, across the project's history:
- **ChatGPT**: initial project idea, planning, architecture discussion, generating implementation prompts.
- **GitHub Copilot** (Visual Studio, ask/agent mode): the bulk of the initial backend implementation — CRUD, validation, auth, tests — working from those prompts.
- **Claude Design**: the Phase 1 UX/design pass — the screen mockups under `docs/raw-ai-logs/ClaudeDesignMockup/` that the Angular frontend was built against.
- **Claude Code**: architecture/code review, repo restructuring, Angular frontend implementation, DevOps setup, and maintaining `docs/WORKFLOW_LOG.md`.

## Standing instruction: maintain the AI-workflow log

**Every session in this repo must proactively keep [`docs/WORKFLOW_LOG.md`](docs/WORKFLOW_LOG.md) up to date** — this is what makes the log complete without the user having to remember to ask for it.

- After any meaningful prompt, decision, correction, or non-trivial piece of work, append a bullet: `[developer input] -> [what resulted]` — **exactly one `->` per bullet**.
  - **Before the arrow: developer input only** — something the developer actually said, asked, decided, corrected, or reported in the conversation. Never an agent action, an agent-found event, or an autonomous continuation dressed up as if it were requested. If a stretch of work has no developer input driving it, it doesn't get its own bullet — fold it into the *after* side of the bullet for whichever developer input actually set it in motion, even if that was a few turns earlier.
  - After the arrow: everything else — what was produced, found, decided, or done, including the agent's own unprompted implementation choices. Never phrase an agent's own choice as if the developer requested or decided it.
  - A follow-up correction, refinement, or decision *from the developer* is its own bullet, never a clause tacked onto the original one — it's a separate ask, even seconds later. Routine follow-through with no real decision point (running the test suite, adding the test coverage a change obviously needs, following an existing convention) isn't a decision point either — fold it into the bullet for the substantive work it was part of rather than giving it its own line.
- Describe outcomes and reasoning, not specific classes/methods/files — that detail already lives in the diffs and commits; the log needs to stay legible to a reader who won't open the code (e.g. an employer reviewing the CV writeup).
- Never write "I"/"me"/"mine" for the agent's own actions — name the tool ("Claude Code"/"Copilot") or phrase it passively instead. This applies generally, not just to the log.
- Prefix each entry's date (or date-grouped section) with the actual date the work happened. If a single day covers more than one topic (e.g. a design pass and a separate backend pass), use separate date-headed sections per topic rather than one mixed list.
- Keep it lightweight — each entry is one bullet, headwords/short sentences are fine. The log should have no gaps (every session's notable work should be represented), but it does not need prose detail; save that for the Phase 7 final writeup.
- Stale entries — including from prior sessions, not just the current one — are fair game to rewrite into this format when spotted, not just flagged.
- Do this without being asked each time — it's a standing expectation for this repo, not a one-off task.
- `docs/raw-ai-logs/` (the original ChatGPT/Copilot chat exports) is a frozen historical archive — don't edit it; it already fed into `WORKFLOW_LOG.md` once and doesn't need to change again.

## Git conventions

- Commit messages: short, single-line, no explanatory body (unless the *why* genuinely isn't derivable from the diff).
- `Co-Authored-By: Claude` trailers on AI-assisted commits are intentional here — they're part of the documented-AI-collaboration story this repo is meant to demonstrate, not just attribution hygiene.
- Never commit without the user's explicit go-ahead in the current session, regardless of permission/auto-mode settings.
