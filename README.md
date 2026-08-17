# Hotel Reservation System

A portfolio project demonstrating modern software engineering using:

- ASP.NET Core Web API
- Angular
- Entity Framework Core
- SQL Server
- Docker
- GitHub Actions
- AI-assisted Development

Status: Backend implemented (Clean Architecture, ASP.NET Core Web API, EF Core, JWT authentication, unit/application/integration tests). Angular frontend implemented (auth, room browsing/booking, customer self-service, full admin section). Dockerized (backend, frontend, and a local docker-compose stack including SQL Server), with GitHub Actions CI running build+test on the backend and build+lint on the frontend.

## Running with Docker

```bash
docker compose up -d --build
```

- Frontend: http://localhost:4200
- Backend / Swagger: http://localhost:5044/swagger
- A default hotel and an admin login (`admin@hotel.local` / `Admin123!`) are seeded automatically on first run (development only)
- Needs ports `4200`, `5044`, and `14330` free on the host — startup fails for whichever service's port is already taken (e.g. a native dev server already running there); `14330` (not SQL Server's default `1433`) is deliberate, since a locally-installed SQL Server commonly already holds that one

```bash
docker compose logs -f   # tail logs
docker compose down      # stop (add -v to also clear the database volume)
```

See [`docs/running.md`](docs/running.md) for the other five ways to run this (backend/frontend, each via Docker standalone, docker-compose, or natively in Visual Studio/VS Code).