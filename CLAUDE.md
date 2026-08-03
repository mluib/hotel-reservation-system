# Hotel Reservation System — Project Brief

A CV portfolio project: a hotel reservation system built with deliberate, documented AI-agent collaboration, not just AI-generated code. The point of the project is as much the visible decision-making and correction trail as the app itself.

## Stack & architecture

- **Backend** (`backend/`): ASP.NET Core Web API on .NET 10, Clean Architecture — `HotelReservation.Domain` (entities/enums, no external deps) → `HotelReservation.Application` (use cases, DTOs, repository interfaces) ← `HotelReservation.Infrastructure` (EF Core, repositories, Identity, JWT) ← `HotelReservation.Api` (thin controllers, DI wiring). Three test projects: `Tests.Domain` (unit), `Tests.Application` (unit, Moq), `Tests.Integration` (`WebApplicationFactory` + SQLite in-memory).
- **Frontend** (`frontend/`): Angular — not yet scaffolded, see `docs/ROADMAP.md`.
- Auth: ASP.NET Core Identity + JWT Bearer, role-based authorization. `IdentityUser` (auth) and domain `Customer` (business entity) are deliberately separate, linked via `IdentityUserId` — see `docs/raw-ai-logs/ChatGPT/decisions.md` for the full rationale.
- Full architectural rationale lives in `docs/raw-ai-logs/ChatGPT/{tech-stack,decisions}.md` (written early in the project, still accurate for the backend).

## Where things stand / what's next

See [`docs/ROADMAP.md`](docs/ROADMAP.md) for the phase-by-phase plan (UX design → backend contract pass → Angular → DevOps → logging → deep backend review → final docs). Don't jump ahead to the deep backend review backlog before the frontend phases — it's deliberately sequenced last so it's informed by real usage.

## Tool-attribution note (for the CV writeup)

Which AI tool did what, across the project's history:
- **ChatGPT**: initial project idea, planning, architecture discussion, generating implementation prompts.
- **GitHub Copilot** (Visual Studio, ask/agent mode): the bulk of the initial backend implementation — CRUD, validation, auth, tests — working from those prompts.
- **Claude / Claude Code**: architecture/code review, repo restructuring, Angular frontend scaffolding, DevOps setup, and maintaining `docs/WORKFLOW_LOG.md`.

## Standing instruction: maintain the AI-workflow log

**Every session in this repo must proactively keep [`docs/WORKFLOW_LOG.md`](docs/WORKFLOW_LOG.md) up to date** — this is what makes the log complete without the user having to remember to ask for it.

- After any meaningful prompt, decision, correction, or non-trivial piece of work in a session, append a dated entry in the existing format: `[what was asked] -> [what the agent produced] -> [correction/decision made, and why]`.
- Prefix each entry's date (or date-grouped section) with the actual date the work happened.
- Keep entries to a few bullet points — headwords/short sentences are fine. The log should have no gaps (every session's notable work should be represented), but it does not need prose detail; save that for the Phase 7 final writeup.
- Do this without being asked each time — it's a standing expectation for this repo, not a one-off task.
- `docs/raw-ai-logs/` (the original ChatGPT/Copilot chat exports) is a frozen historical archive — don't edit it; it already fed into `WORKFLOW_LOG.md` once and doesn't need to change again.

## Git conventions

- Commit messages: short, single-line, no explanatory body (unless the *why* genuinely isn't derivable from the diff).
- `Co-Authored-By: Claude` trailers on AI-assisted commits are intentional here — they're part of the documented-AI-collaboration story this repo is meant to demonstrate, not just attribution hygiene.
- Never commit without the user's explicit go-ahead in the current session, regardless of permission/auto-mode settings.
