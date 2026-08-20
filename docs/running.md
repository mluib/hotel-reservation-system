# Running the Project

Different ways to run each half of the stack. If you just want to see the app working with the
least effort, use **docker-compose** — it's the only option that needs zero setup, and it's
what [`README.md`](../README.md) leads with. This doc covers Docker (both docker-compose and
standalone containers) just as much as the native options (Visual Studio for the backend, VS
Code for the frontend), which is the day-to-day development setup this project was actually
built with. The backend and frontend options can be mixed freely.

## 1. Docker-Compose (full stack: db + backend + frontend)

**Prerequisites:** Docker Desktop, in Linux containers mode (this repo's images are all Linux-based)

### a) Build & run

Run from the repo root. No setup needed — the committed [`.env`](../.env) (see its own top-of-file comment for why it's committed rather than gitignored here) already supplies everything: the SQL Server SA password, `Jwt:Key`, and the seed admin credentials, wired into [`docker-compose.yml`](../docker-compose.yml) via `${VAR}` substitution.

```bash
docker compose up -d --build
```

- **Frontend:** http://localhost:4200
- **Backend/Swagger:** http://localhost:5044/swagger
- **SQL Server:** `localhost,14330` — its own `db` container (named volume `mssql-data`, persists across restarts); connect via SSMS/Azure Data Studio or a connection string, not a browser
- **Migrations:** auto-applied on backend startup
- **Seeding:** a dev-only admin login + placeholder hotel, seeded on backend startup (see `SeedDevAdminAsync` in [`Program.cs`](../backend/HotelReservation.Api/Program.cs))
- **Configuration:** described in the Docker standalone paragraphs below (§2) — same environment variables, just supplied via [`docker-compose.yml`](../docker-compose.yml)/[`.env`](../.env) instead of by hand

### b) Further commands

```bash
docker compose logs -f   # tail logs
docker compose down      # stop (add -v to also clear the database volume)
```

## 2. Docker (standalone containers)

**Prerequisites:** Docker Desktop, in Linux containers mode

Builds and runs each half in isolation, outside docker-compose's shared network — a build/config sanity-check, not a real way to run the app day to day (use docker-compose above for that).

### a) Backend: Build

```bash
docker build -t hotel-reservation-backend ./backend
```

- **Build configuration:** `Release` (set in [`Dockerfile`](../backend/Dockerfile))

### b) Backend: Run

Needs a reachable SQL Server and its config (connection string, `Jwt:Key`) supplied via `-e`/`--env-file`, since there's no compose network to provide `db` or `.env` here:

```bash
docker run --rm -p 5044:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,14330;Database=HotelReservationDb;User Id=sa;Password=HotelDev_2026!;TrustServerCertificate=True;" \
  -e Jwt__Key="ql8UiF/DgXWrZniua1SrrhAWE8QhNDWBZOwrAtlTGuE=" \
  hotel-reservation-backend
```

- **Database target:** docker-compose's `db` container (`docker compose up -d db` first, if it isn't already running), reached via `host.docker.internal` (Docker Desktop's DNS name for the host machine, the easy way to reach anything on `localhost` from inside a container) on port `14330` (`db`'s published host port)
- **Jwt Key:** `Jwt:Key` is mandatory (startup fails fast without it, Phase 6 secrets hardening)
- **Seeding:** `Seed:Admin*` just skips creating a dev admin if omitted
- **Launch configuration:** `ASPNETCORE_ENVIRONMENT=Development` (set above) → `appsettings.Development.json` applies
- **Port configuration:** `-p 5044:8080` maps host port `5044` to the container's Kestrel port (`ASPNETCORE_URLS=http://+:8080` in the Dockerfile)

### c) Frontend: Build

```bash
docker build -t hotel-reservation-frontend ./frontend
```

- **Build configuration:** `production` (Angular's default for `ng build`/`npm run build`, executed in [`Dockerfile`](../frontend/Dockerfile))

### d) Frontend: Run

```bash
docker run --rm -p 4200:80 hotel-reservation-frontend
```

- **Launch configuration:** plain `environment.ts` — a production build doesn't file-replace it with `environment.development.ts`, so the app calls the relative `/api` path, which [`nginx.conf`](../frontend/nginx.conf) proxies to `http://backend:8080/api/`.
- **Port configuration:** `-p 4200:80` maps host port `4200` to the container's nginx port (`EXPOSE 80` in the Dockerfile)
- **Caveat:** `backend` is a docker-compose service hostname, not resolvable outside that network — this standalone container serves the static site fine, but API calls fail with no backend to reach.

## 3. Visual Studio (native, backend)

**Prerequisites:** .NET 10 SDK, Visual Studio

### a) SQL Server instance

A locally reachable SQL Server instance. `appsettings.json`'s connection string (`Server=localhost;...;Trusted_Connection=True`) expects a SQL Server / SQL Server Express / Developer Edition already running on the machine using Windows auth — this is a *different* database from docker-compose's containerized one, not shared with it.

### b) User-secrets setup

```bash
cd backend/HotelReservation.Api
dotnet user-secrets set "Jwt:Key" "<a real random 256-bit Base64 value, e.g. from `openssl rand -base64 32`>"
dotnet user-secrets set "Seed:AdminEmail" "admin@hotel.local"
dotnet user-secrets set "Seed:AdminPassword" "Admin123!"
dotnet clean   # UserSecretsId is embedded into the assembly at compile time, so a stale build won't see a newly-added secret
```

- One-time per machine. `Jwt:Key` and the seed admin credentials are no longer committed anywhere (Phase 6 secrets hardening).
- Stored per-user, outside the repo (`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` on Windows), keyed to the project's `UserSecretsId` in `HotelReservation.Api.csproj` — Visual Studio and the `dotnet` CLI read the same file, so this only needs doing once regardless of which one you use afterward.

### c) Build & run

Two equivalent ways to start it:

- **Visual Studio:** run the `http` launch profile (the only one — the old `https` profile was removed, since its redirect broke the frontend's CORS requests)
- **CLI:**

   ```bash
   cd backend/HotelReservation.Api
   dotnet run --launch-profile http
   ```

Notes:

- **Build configuration:** `Debug` — both Visual Studio's default Run and `dotnet run` without an explicit `-c`/`--configuration` flag build Debug; nothing in this project overrides that.
- **Launch configuration:** `ASPNETCORE_ENVIRONMENT=Development` (set in [`launchSettings.json`](../backend/HotelReservation.Api/Properties/launchSettings.json) `http` profile) → `appsettings.Development.json` applies
- **Port configuration:** http://localhost:5044 (set in [`launchSettings.json`](../backend/HotelReservation.Api/Properties/launchSettings.json) `http` profile)
- **Migrations:** auto-apply on startup against whichever database the connection string points to.

## 4. Visual Studio Code (native, frontend)

**Prerequisites:** Node.js/npm (`package.json` pins `npm@11.17.0` under `packageManager`)

### a) Install

```bash
cd frontend
npm install   # first time only
```

### b) Build & run

Three equivalent ways to start it:

- **CLI:** `npm start` (same as `ng serve`)
- **VS Code Run & Debug:** run the `ng serve` debug configuration (defined in [`.vscode/launch.json`](../frontend/.vscode/launch.json))
- **VS Code Task:** Run Task → `npm: start` (defined in [`.vscode/tasks.json`](../frontend/.vscode/tasks.json))

Notes:

- **Build configuration:** `development` (Angular's default for `ng serve`/`npm start`)
- **Launch configuration:** `environment.development.ts` (file-replaces plain `environment.ts`) → talks **directly** to `http://localhost:5044`, no nginx proxy involved.
- **Port configuration:** http://localhost:4200 — the Angular CLI dev-server's built-in default; nothing in this project overrides it (`angular.json`'s `serve` target has no explicit `port` option).
