# Frontend

Angular app for the hotel reservation system — see the [repo root README](../README.md) and [`roadmap.md`](roadmap.md) for the overall project. Built against the Claude Design mockup at `docs/raw-ai-logs/claude-design/mockup/`. For running instructions, see [`running.md`](running.md).

## Structure

- `core/` — auth (JWT decode/storage, guards, interceptor), models mirroring the backend's DTOs, and the HTTP services wrapping each controller.
- `shared/` — generic dialogs (confirm / error) reused across features.
- `layout/` — the role-aware nav bar.
- `features/` — one folder per screen (`home`, `auth`, `rooms`, `booking`, `reservations`, `admin`), routed via `app.routes.ts`.

No state management library — a handful of injectable services hold shared state (e.g. the hotel record, the logged-in user) as Angular signals; everything else is local component state.

## Other commands

```bash
npm run build   # production build, output in dist/
npm test        # unit tests (Vitest)
```
