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

```bash
docker compose logs -f   # tail logs
docker compose down      # stop (add -v to also clear the database volume)
```

See [`docs/RUNNING.md`](docs/RUNNING.md) for the other five ways to run this (backend/frontend, each via Docker standalone, docker-compose, or natively in Visual Studio/VS Code).