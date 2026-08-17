# Phase 4 (DevOps) — Guided Implementation Plan

## Context

Phase 3 (Angular frontend) is complete and verified; Phase 4 is next in `docs/ROADMAP.md`, scoped exactly as:

> backend/Dockerfile, frontend/Dockerfile, root docker-compose.yml for local full-stack runs, GitHub Actions CI (build+test backend, build+lint frontend). CD is optional/stretch.

Unlike prior phases, **the developer will write and run everything themselves this time** — Claude Code's role is guidance/review, not execution. This plan is therefore a teaching-oriented checklist: exact files, exact commands, and what "done" looks like at each step, plus the conceptual background needed to understand *why* each piece exists (Docker, GitHub Actions vs. CI/CD generally, and terminal-vs-website-vs-MCP for the GitHub side).

Confirmed repo state (already explored, no need to re-check): backend is flat under `backend/` (`HotelReservation.Api/`, `.Application/`, `.Domain/`, `.Infrastructure/`, three `Tests.*` projects, `HotelReservationSystem.slnx`), target framework `net10.0`, SQL Server via `ConnectionStrings:DefaultConnection`, ports 5044 (http) / 7290 (https), xunit tests via `dotnet test`. Frontend is Angular ^22.1.0, no lint tooling yet, build output `dist/hotel-reservation-frontend/browser/`, `environment.ts` has a placeholder comment explicitly naming this phase as the place to resolve the production API URL. No Docker/CI files exist anywhere yet; `.github/workflows/` is present but empty.

**Decisions locked in with the developer:**
- **DB schema**: auto-migrate on backend startup (one guarded `Database.Migrate()` call in `Program.cs`) — `docker compose up` alone always yields a fully working DB, no manual step.
- **Frontend lint**: add ESLint via `ng add @angular-eslint/schematics` so CI's "lint" step is real, not skipped.
- **GitHub workflow style**: mixed — at each step, this plan notes both the terminal (`git`/`gh` CLI) and GitHub-website way to do the GitHub-specific parts, so the developer can pick per-step.

---

## Primer: answering the conceptual questions before diving in

**Is GitHub Actions the same thing as "a CI/CD pipeline"?**
No — one's a concept, one's a product. **CI/CD** is a general practice: *Continuous Integration* (automatically build+test every change so breakage is caught immediately) and *Continuous Delivery/Deployment* (automatically package/ship that change further, up to and including production). It's implemented by many competing tools: Jenkins, GitLab CI, CircleCI, Azure Pipelines, Travis — and **GitHub Actions**, which is GitHub's own built-in implementation. Concretely, GitHub Actions = YAML files under `.github/workflows/` that describe *when* to run (push, PR, schedule, ...) and *what* to run (jobs made of steps, on GitHub-hosted or self-hosted runners). This phase's `ci.yml` file **is** the CI half of a CI/CD pipeline, built using GitHub Actions as the engine. CD is explicitly out of scope for this phase (stretch-only).

**Terminal vs. GitHub website vs. MCP — which for what?**
- **Terminal (`git` + `gh` CLI)**: how you actually do local git work (branch, commit, push) — there's no alternative to this. The `gh` CLI additionally lets you create PRs, view Actions run status, and read logs *without leaving the terminal*. Best for learning what's actually happening under the hood, and it's what real engineering jobs use day-to-day.
- **GitHub website**: some things are website-only or website-easiest — first-time repo/Action enablement, adding repository *secrets* (Settings → Secrets and variables — needed only if you later do the CD stretch goal), and watching a live Actions run with its expandable step-by-step log viewer is genuinely more readable in the browser than in a terminal.
- **MCP (GitHub tools via Claude)**: lets Claude perform GitHub operations directly through chat. **Not recommended for this phase** — the whole point is you doing the git/GitHub mechanics yourself to learn them; having Claude do it via MCP would defeat that. Skipped throughout this plan.

Each step below marks GitHub-specific actions with **[terminal]** and/or **[website]** so you can pick.

---

## Step-by-step sequence

Build one piece, prove it works alone, then wire it into the next. Order: backend Dockerfile → frontend Dockerfile → nginx `/api` proxy → docker-compose.yml → ESLint → GitHub Actions CI.

**Every step that involves a file follows the same fixed sub-steps, always in this order — this is the part that was ambiguous before, now made explicit:**
- **a) Create** — make the empty file (only for new files; skip if editing an existing one)
- **b) Write** — put the actual content into that file (skeleton given; you fill in placeholders)
- **c) Review** — paste the content back here before running anything, so mistakes get caught before they cost a build/run cycle
- **d) Test** — the terminal commands that prove it works, typed into PowerShell — never into the file itself
- **e) Commit** — one small `git commit` checkpoint per step (short, single-line message, `Co-Authored-By: Claude` trailer, consistent with this repo's convention)

### 0. Branch first
**a) N/A** (no file) **b) N/A** **c) N/A**
**d) Test/run:**
```powershell
git checkout -b phase4-devops
```
`main` stays clean/deployable while you work. **[terminal]** only — no website equivalent for a local branch.
**e) Commit:** N/A (nothing to commit yet, branch itself isn't a commit).

### 1. `backend/Dockerfile` + `backend/.dockerignore`
**a) Create:**
```powershell
New-Item -ItemType File -Path backend/Dockerfile
New-Item -ItemType File -Path backend/.dockerignore
```
**b) Write** — open `backend/Dockerfile` in your editor and put in the two-stage build content: `mcr.microsoft.com/dotnet/sdk:10.0` stage to restore+publish `HotelReservation.Api`, then `mcr.microsoft.com/dotnet/aspnet:10.0` as the slim runtime stage. Build context is `backend/` (flat paths — `HotelReservation.Api/`, no `src/` prefix). Only copy the four projects the API references, never the three `Tests.*` projects. HTTP-only inside the container (`ENV ASPNETCORE_URLS=http://+:8080`, `EXPOSE 8080`) — `UseHttpsRedirection()` in `Program.cs` stays untouched, it's a harmless no-op without an HTTPS port configured.
Put in `backend/.dockerignore`: `**/bin/`, `**/obj/`, `**/.vs/`, `**/*.user`, `**/wwwroot/uploads/*` (keep a `.gitkeep`), `.git`, `.gitignore`, `*.md`.
**c) Review:** paste both files' contents here before moving to (d).
**d) Test/run** (needs a reachable SQL Server — your existing local install is fine):
```powershell
cd backend
docker build -t hotel-backend .
docker run --rm -e ASPNETCORE_ENVIRONMENT=Development -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=HotelReservationDb;Trusted_Connection=False;User Id=sa;Password=<yourpwd>;TrustServerCertificate=True;" -e Cors__AllowedOrigins__0="http://localhost:4200" -p 8080:8080 hotel-backend
```
Note the added `-e ASPNETCORE_ENVIRONMENT=Development` — without it the app defaults to Production, where Swagger isn't registered (`Program.cs` gates it behind `IsDevelopment()`), so `/swagger` would 404 even on a perfectly working container. The startup log line `Hosting environment: ...` confirms which mode it's actually in.
In another terminal: `curl http://localhost:8080/swagger/index.html` should return 200.
**Done when:** image builds, container starts without crashing (check the log for `Now listening on: http://[::]:8080` and `Application started`), Swagger responds 200. A `WebRootPath was not found: /app/wwwroot` warning is expected at this stage (uploads folder is intentionally excluded by `.dockerignore`) — revisit with a named volume when writing `docker-compose.yml` in Step 5.
**e) Commit:** `git add backend/Dockerfile backend/.dockerignore && git commit -m "Add backend Dockerfile"`.

### 2. `frontend/Dockerfile` + `.dockerignore` + `nginx.conf` (static-serve only, no API proxy yet)
**a) Create:**
```powershell
New-Item -ItemType File -Path frontend/Dockerfile
New-Item -ItemType File -Path frontend/.dockerignore
New-Item -ItemType File -Path frontend/nginx.conf
```
**b) Write** — `frontend/Dockerfile`: two-stage build, `node:22-alpine` running `npm ci && npm run build`, then `nginx:alpine` serving `dist/hotel-reservation-frontend/browser/` on port 80.
`frontend/nginx.conf` (SPA-fallback only for this step):
```nginx
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;
    location / { try_files $uri $uri/ /index.html; }
}
```
`frontend/.dockerignore`: `node_modules/`, `dist/`, `.angular/`, `.git`, `.gitignore`, `*.md`.
**c) Review:** paste all three files' contents here before (d).
**d) Test/run:**
```powershell
cd frontend
docker build -t hotel-frontend .
docker run --rm -p 4200:80 hotel-frontend
```
Open `http://localhost:4200` — app shell/login page should render. Deep-link refresh (e.g. `http://localhost:4200/rooms`) should not 404. API calls failing is expected/OK here — fixed next step.
**Done when:** above holds.
**e) Commit:** `git add frontend/Dockerfile frontend/.dockerignore frontend/nginx.conf && git commit -m "Add frontend Dockerfile"`.

### 3. Wire the nginx `/api` reverse proxy
**a) Create:** N/A — editing the existing `frontend/nginx.conf` from Step 2.
**b) Write** — add this block inside the existing `server { }` in `frontend/nginx.conf`, alongside the `location /` block already there. This resolves the placeholder comment sitting in `frontend/src/environments/environment.ts` (`apiBaseUrl: '/api'`):
```nginx
    location /api/ {
        proxy_pass http://backend:8080/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
```
**c) Review:** paste the updated `nginx.conf` here.
**d) Test/run:**
```powershell
cd frontend
docker build -t hotel-frontend .
```
The hostname `backend` only resolves inside the docker-compose network built in Step 5, so it can't be fully tested yet — this just confirms the image still builds cleanly with the edited config.
**Done when:** build succeeds.
**e) Commit:** `git commit -m "Add nginx API reverse proxy"`.

### 4. Auto-migrate on startup (the locked-in DB decision)
**a) Create:** N/A — editing the existing `backend/HotelReservation.Api/Program.cs`.
**b) Write** — add this block after `var app = builder.Build();` and before `app.RunAsync()`:
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
    if (db.Database.IsSqlServer())
    {
        await db.Database.MigrateAsync();
    }
}
```
Guard note (verified, not assumed): `HotelReservation.Tests.Integration`'s `CustomWebApplicationFactory` swaps the DB provider to SQLite for tests *and* forces `Development` environment — so an `IsDevelopment()` guard alone would NOT have excluded the test host (a real container can also run as Development). Guarding on `Database.IsSqlServer()` instead is provider-based, not environment-based: true only against a real SQL Server, always false in the SQLite-swapped test host, regardless of environment name.
**c) Review:** paste the edited section of `Program.cs` here.
**d) Test/run:**
```powershell
cd backend
dotnet test HotelReservationSystem.slnx
```
**Done when:** all three test projects still pass, especially `Tests.Integration`.
**e) Commit:** `git commit -m "Auto-apply EF Core migrations on startup"`.

### 5. Root `docker-compose.yml`
**a) Create:**
```powershell
New-Item -ItemType File -Path docker-compose.yml
```
**b) Write** — three services: `db` (`mcr.microsoft.com/mssql/server:2022-latest`, `ACCEPT_EULA=Y`, `MSSQL_SA_PASSWORD`, named volume `mssql-data`, healthcheck via `sqlcmd`), `backend` (build `./backend`, `depends_on: db` with `condition: service_healthy`, env-var overrides for `ConnectionStrings__DefaultConnection` pointed at `Server=db`, `Cors__AllowedOrigins__0`, `Seed__AdminEmail`/`Seed__AdminPassword`, `ASPNETCORE_ENVIRONMENT=Development`), `frontend` (build `./frontend`, `depends_on: backend`, port `4200:80`). Ask for the exact YAML skeleton when you reach this step if you want a starting point to edit rather than write from scratch. Also edit root `.gitignore` to add `docker-compose.override.yml` / `.env`.
Note: `ASPNETCORE_ENVIRONMENT=Development` also re-enables Swagger UI inside the container — fine for a portfolio/local-dev compose file, not a "production" config.
**c) Review:** paste `docker-compose.yml` here before (d).
**d) Test/run:**
```powershell
docker compose build
docker compose up
```
Browser: `http://localhost:4200` → log in as seeded admin → confirm a real API call round-trips through nginx → backend → SQL Server. Then prove it's reproducible from scratch:
```powershell
docker compose down -v
docker compose up
```
**Done when:** both `up` runs work end-to-end — including actually logging in as admin and creating/seeing real data through the full nginx → backend → SQL Server path, not just an empty shell.

**Gap found during verification (not part of the original plan):** the app has no "create hotel" endpoint at all — `UpdateHotel`/`GetHotel` look up a single Hotel row with no id, and throw if none exists. A brand-new database has no way to ever get that first row through the UI, so a fresh compose stack would always look broken on first use. Fixed by extending the existing dev-only seed in `Program.cs` (`SeedDevAdminAsync`) to also insert a placeholder Hotel row if none exists — same `IsDevelopment()`-gated, dev-only pattern already used for the admin seed, not a new app feature. This is unrelated to `Tests.Integration` (that project never calls `SeedDevAdminAsync` at all), so it doesn't affect the test suite.

**e) Commit:** `git add docker-compose.yml .gitignore backend/HotelReservation.Api/Program.cs && git commit -m "Add docker-compose for local full-stack runs"` (bundle the hotel-seed fix into this commit since it's what made this step's own verification actually pass — or split into two commits if you'd rather keep the seed fix separately labeled; your call).

### 6. Add ESLint to the frontend
**a) Create:** N/A — generated by the schematic in (b).
**b) Write** — run the schematic (this is a command, not manual file-writing — it generates `frontend/eslint.config.js` and edits `frontend/package.json` for you):
```powershell
cd frontend
ng add @angular-eslint/schematics
```
**c) Review:** paste the generated `eslint.config.js` and the diff to `package.json` here.
**d) Test/run:**
```powershell
npm run lint
```
If this surfaces a large number of pre-existing violations, fix the easy ones and use `// eslint-disable-next-line` sparingly for the rest — flag back if it becomes a big detour, since descoping to build-only is still an option.
**Done when:** `npm run lint` runs clean (or with acknowledged, justified exceptions).
**e) Commit:** `git add -A && git commit -m "Add ESLint to frontend"`.

### 7. GitHub Actions CI — `.github/workflows/ci.yml`
**a) Create:**
```powershell
New-Item -ItemType File -Path .github/workflows/ci.yml
```
**b) Write** — two jobs, triggered on push/PR to `main`:
- **backend job**: `actions/setup-dotnet@v4` (`dotnet-version: '10.0.x'`), `dotnet restore HotelReservationSystem.slnx`, `dotnet build ... -c Release`, `dotnet test ... -c Release` (covers all three test projects; `Tests.Integration` uses SQLite in-process, no DB service container needed in CI).
- **frontend job**: `actions/setup-node@v4` (`node-version: '22'`), `npm ci`, `npm run build`, `npm run lint`.
No registry credentials needed — runs directly via `dotnet`/`npm` on the GitHub-hosted runner, not through the Dockerfiles.
**c) Review:** paste `ci.yml` here before (d).
**d) Test/run** — verify locally first (catch failures before GitHub does):
```powershell
cd backend; dotnet restore HotelReservationSystem.slnx; dotnet build HotelReservationSystem.slnx -c Release; dotnet test HotelReservationSystem.slnx -c Release
cd ../frontend; npm ci; npm run build; npm run lint
```
Then push and watch it run for real:
- **[terminal]**: `git push -u origin phase4-devops`, then `gh pr create --fill`, then `gh pr checks --watch` or `gh run watch`.
- **[website]**: push the same way, then open the repo on github.com → **Pull requests** → **New pull request**, or the **Actions** tab directly to watch the run with its expandable per-step logs.
**Done when:** both jobs show green on a real GitHub Actions run.
**e) Commit:** `git add .github/workflows/ci.yml && git commit -m "Add GitHub Actions CI workflow"` (before pushing).

### 8. Merge back to `main`
- **[terminal]**: `gh pr merge --squash` (or regular merge, your preference), then `git checkout main && git pull`.
- **[website]**: click **Merge pull request** on the PR page.

---

## Optional/stretch — CD (not part of this pass)

Briefly, for later: a third job triggered on push to `main`, using `docker/login-action` against GHCR with the automatic `${{ secrets.GITHUB_TOKEN }}` (no extra secret setup needed for GHCR specifically), then `docker/build-push-action` for both images tagged `ghcr.io/<owner>/hotel-backend:latest` / `hotel-frontend:latest`. No actual deploy target (VPS/Azure/etc.) is implied by the roadmap — this would be image-publishing only, not deployment. Explicitly deferred; do not implement now.

## Verification summary (end-to-end, after all steps)
1. `docker compose up` from a clean `down -v` → full app reachable at `http://localhost:4200`, admin login works, a booking round-trips through nginx → backend → SQL Server.
2. `dotnet test` (backend, all 3 projects) and `npm run build && npm run lint` (frontend) pass locally.
3. A real GitHub Actions run on the pushed branch/PR shows both jobs green.
4. After merge, `docs/WORKFLOW_LOG.md` gets a new dated entry for this phase (per the repo's standing workflow-log convention) and root `README.md`'s "Docker and CI/CD pipeline in progress" line gets updated to reflect what's actually done.

## Critical files
- `backend/Dockerfile`, `backend/.dockerignore`
- `backend/HotelReservation.Api/Program.cs` (migration call)
- `frontend/Dockerfile`, `frontend/.dockerignore`, `frontend/nginx.conf`
- `docker-compose.yml` (repo root)
- `.github/workflows/ci.yml`
- `docs/WORKFLOW_LOG.md`, `README.md` (updated at the end, not during)
