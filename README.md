# Bookings API

A production-style REST API for a bookings / reservations system — resources
(meeting rooms, equipment, appointment slots) that can be reserved for time
slots. Built with **ASP.NET Core (.NET 8)**, **Entity Framework Core**, and
**PostgreSQL**, and fully containerized with Docker.

> **Status:** Stage 1 — project skeleton, core domain, and Resource CRUD.
> Bookings logic, conflict detection, and auth arrive in later stages.

## Tech stack

- ASP.NET Core Web API (.NET 8)
- Entity Framework Core 8 + Npgsql (PostgreSQL)
- PostgreSQL 16
- Docker + Docker Compose
- Swagger / OpenAPI (development)

## Solution structure

The solution follows a layered / clean-architecture style. Dependencies point
inward toward the domain:

```
Bookings.Api            HTTP surface: controllers, DI wiring, request pipeline
   └─ depends on ─▶ Bookings.Application
Bookings.Application    Use cases (services), DTOs, persistence abstraction
   └─ depends on ─▶ Bookings.Domain
Bookings.Infrastructure EF Core context, entity configs, migrations (Npgsql)
   └─ depends on ─▶ Bookings.Application, Bookings.Domain
Bookings.Domain         Entities and enums — no external dependencies
```

- **Domain** is the framework-agnostic core (the `Resource`, `Booking`, and
  `User` entities).
- **Application** holds use-case logic and depends only on the
  `IApplicationDbContext` abstraction, so services aren't coupled to the
  database provider.
- **Infrastructure** provides the concrete EF Core / PostgreSQL implementation.
- **Api** is the thin composition root and HTTP layer.

## Running it

### With Docker (recommended — one command)

```bash
docker compose up --build
```

This starts PostgreSQL and the API, waits for the database to be healthy,
applies migrations automatically, and serves the API at:

- API base: `http://localhost:8080`
- Swagger UI: `http://localhost:8080/swagger`
- Health check: `http://localhost:8080/health`

### Locally (requires the .NET 8 SDK + a running PostgreSQL)

```bash
dotnet run --project src/Bookings.Api
```

The default connection string (see `src/Bookings.Api/appsettings.json`) expects
PostgreSQL on `localhost:5432` with `postgres`/`postgres`. Override it with the
`ConnectionStrings__Default` environment variable.

## Resource API

| Method   | Route                 | Description            | Success        |
|----------|-----------------------|------------------------|----------------|
| `GET`    | `/api/resources`      | List all resources     | `200 OK`       |
| `GET`    | `/api/resources/{id}` | Get a resource by id   | `200` / `404`  |
| `POST`   | `/api/resources`      | Create a resource      | `201 Created`  |
| `PUT`    | `/api/resources/{id}` | Update a resource      | `200` / `404`  |
| `DELETE` | `/api/resources/{id}` | Delete a resource      | `204` / `404`  |

Example — create a resource:

```bash
curl -X POST http://localhost:8080/api/resources \
  -H "Content-Type: application/json" \
  -d '{
        "name": "Conference Room A",
        "description": "10-seat room with projector",
        "type": "MeetingRoom",
        "capacity": 10
      }'
```

## Database migrations

Migrations live in `src/Bookings.Infrastructure/Persistence/Migrations` and are
applied automatically on API startup. To manage them manually (requires the
`dotnet-ef` tool, pinned in `.config/dotnet-tools.json`):

```bash
dotnet tool restore
dotnet ef migrations add <Name> \
  --project src/Bookings.Infrastructure \
  --startup-project src/Bookings.Api \
  --output-dir Persistence/Migrations
```

## Roadmap

- **Stage 2** — `User` and `Booking` CRUD + booking-conflict prevention.
- **Stage 3** — authentication & authorization.
- **Stage 4** — validation hardening, rate limiting, pagination, tests, CI.
