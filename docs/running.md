# Running the Project

Three ways to run each half of the stack. If you just want to see the app working with the
least effort, use **docker-compose** — it's the only option that needs zero setup, and it's
what [`README.md`](../README.md) leads with. This doc covers that plus the two native
options (Visual Studio for the backend, VS Code for the frontend), which is the day-to-day
development setup this project was actually built with.

The three backend and three frontend options can be mixed freely — e.g. backend via Visual
Studio + frontend via VS Code's dev server is a normal combination, not just
docker-compose-all-or-nothing.

## Prerequisites

| Scenario | Needs |
|---|---|
| Docker / docker-compose (either half) | Docker Desktop |
| Backend, native (Visual Studio) | .NET 10 SDK, Visual Studio, a locally reachable SQL Server instance, one-time user-secrets setup (see below) |
| Frontend, native (VS Code) | Node.js/npm (`package.json` pins `npm@11.17.0` under `packageManager`) |

---

## Backend

### Docker (standalone container)

```bash
docker build -t hotel-reservation-api ./backend
```

This mainly verifies the image builds — the resulting container ([`backend/Dockerfile`](../backend/Dockerfile), multi-stage: SDK build → ASP.NET runtime, listening on `8080` internally) still needs a reachable SQL Server and its config (connection string, `Jwt:Key`, `Seed:Admin*`) supplied via `-e`/`--env-file` at `docker run` time, since there's no compose network to provide `db` or the `.env` values here. For actually running the backend standalone, docker-compose (below) is the real path — this is mostly a build sanity-check.

### docker-compose (full stack: db + backend + frontend)

```bash
docker compose up -d --build
```

Run from the repo root. No setup needed — the committed [`.env`](../.env) (see its own top-of-file comment for why it's committed rather than gitignored here) already supplies everything: the SQL Server SA password, `Jwt:Key`, and the seed admin credentials, wired into [`docker-compose.yml`](../docker-compose.yml) via `${VAR}` substitution.

- Backend / Swagger: http://localhost:5044/swagger
- SQL Server runs as its own `db` container (named volume `mssql-data`, persists across restarts)
- On startup the backend auto-migrates the database and seeds a dev-only admin login + placeholder hotel — see `SeedDevAdminAsync` in [`Program.cs`](../backend/HotelReservation.Api/Program.cs)

```bash
docker compose logs -f   # tail logs
docker compose down      # stop (add -v to also clear the database volume)
```

### Visual Studio (native, no containers)

1. **A locally reachable SQL Server instance.** `appsettings.json`'s connection string (`Server=localhost;...;Trusted_Connection=True`) expects a SQL Server / SQL Server Express / Developer Edition already running on the machine using Windows auth — this is a *different* database from docker-compose's containerized one, not shared with it.
2. **One-time user-secrets setup.** `Jwt:Key` and the seed admin credentials are no longer committed anywhere (Phase 6 secrets hardening) — set them once per machine:

   ```bash
   cd backend/HotelReservation.Api
   dotnet user-secrets set "Jwt:Key" "<a real random 256-bit Base64 value, e.g. from `openssl rand -base64 32`>"
   dotnet user-secrets set "Seed:AdminEmail" "admin@hotel.local"
   dotnet user-secrets set "Seed:AdminPassword" "Admin123!"
   ```

   These are stored per-user, outside the repo (`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` on Windows), keyed to the project's `UserSecretsId` in `HotelReservation.Api.csproj` — Visual Studio and the `dotnet` CLI read the same file, so this only needs doing once regardless of which one you use afterward. If the app fails at startup with `"Jwt:Key is not configured"` right after pulling changes that touched the `.csproj`, try a full rebuild first — `UserSecretsId` is embedded into the compiled assembly, so a stale build won't see it.
3. Open the solution, select the **`http`** launch profile (the only one — the old `https` profile was removed, since its redirect broke the frontend's CORS requests), and run.

Runs at http://localhost:5044, Swagger at `/swagger`. Migrations auto-apply on startup against whichever database the connection string points to.

---

## Frontend

### Docker (standalone container)

```bash
docker build -t hotel-reservation-frontend ./frontend
docker run -p 4200:80 hotel-reservation-frontend
```

Multi-stage build ([`frontend/Dockerfile`](../frontend/Dockerfile)): Node 22 (`npm ci` + `ng build`) → served by `nginx:alpine`. **Caveat**: [`nginx.conf`](../frontend/nginx.conf) proxies `/api/` to `http://backend:8080/api/` — `backend` is a docker-compose service hostname, not resolvable outside that network. A standalone container run like this serves the static site fine, but API calls will fail with no backend to reach. Same as the backend's standalone case: mostly a build check, not a real way to use the app.

### docker-compose (full stack)

Same single command as the backend section — `docker compose up -d --build` starts frontend, backend, and db together, with nginx's `/api/` proxy correctly resolving `backend` inside the compose network.

- Frontend: http://localhost:4200

### VS Code (native dev server)

```bash
cd frontend
npm install   # first time only
npm start     # same as `ng serve`
```

Or use the pre-configured task: VS Code → Run Task → **npm: start** (defined in [`.vscode/tasks.json`](../frontend/.vscode/tasks.json)), or launch the **"ng serve"** debug configuration in [`.vscode/launch.json`](../frontend/.vscode/launch.json), which runs that task automatically and opens the app in Edge.

Runs at http://localhost:4200. Unlike the Docker path, there's no nginx proxy here — the dev server talks **directly** to `http://localhost:5044` (see [`environment.development.ts`](../frontend/src/environments/environment.development.ts)). Any backend listening on that port works — Visual Studio (the native path above) and a docker-compose-run backend both end up reachable at `localhost:5044` either way, so it doesn't matter which one is actually running.
