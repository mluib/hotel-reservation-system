# Frontend

Angular app for the hotel reservation system (see the [repo root README](../README.md) and [`docs/roadmap.md`](../docs/roadmap.md) for the overall project). Built against the Claude Design mockup at `docs/raw-ai-logs/ClaudeDesignMockup/`.

## Running locally

Needs the backend running alongside it (`dotnet run --launch-profile http` from `backend/HotelReservation.Api`, plain HTTP on port 5044 — see `src/environments/environment.development.ts` for why HTTPS is deliberately avoided in dev). CORS is already configured backend-side for `http://localhost:4200`.

```bash
npm install
npm start   # ng serve, http://localhost:4200
```

There's no way to get an Admin account through normal sign-up (registration always creates a Customer). A dev-only seed creates one admin login on backend startup — see `Seed:AdminEmail` / `Seed:AdminPassword` in `backend/HotelReservation.Api/appsettings.Development.json`.

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
