# Bookings API

A production-style REST API for a bookings / reservations system — resources
(meeting rooms, equipment, appointment slots) that can be reserved for time
slots. Built with **ASP.NET Core (.NET 8)**, **Entity Framework Core**, and
**PostgreSQL**, and fully containerized with Docker.

> **Status:** Resource + User management and Booking functionality with
> overlap-conflict detection and concurrency-safe reservations. Authentication
> arrives next.

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

## User API

| Method | Route              | Description         | Success              |
|--------|--------------------|---------------------|----------------------|
| `GET`  | `/api/users`       | List all users      | `200 OK`             |
| `GET`  | `/api/users/{id}`  | Get a user by id    | `200` / `404`        |
| `POST` | `/api/users`       | Register a user     | `201` / `400` / `409`|

## Booking API

| Method | Route                              | Description               | Success                     |
|--------|------------------------------------|---------------------------|-----------------------------|
| `POST` | `/api/bookings`                    | Create a booking          | `201` / `400` / `404` / `409` |
| `GET`  | `/api/bookings/{id}`               | Get a booking by id       | `200` / `404`               |
| `POST` | `/api/bookings/{id}/cancel`        | Cancel a booking          | `200` / `404` / `409`       |
| `GET`  | `/api/resources/{resourceId}/bookings` | List a resource's bookings | `200` / `404`          |
| `GET`  | `/api/users/{userId}/bookings`     | List a user's bookings    | `200` / `404`               |

The list endpoints accept optional `from`, `to` (ISO-8601), and
`includeCancelled` (default `false`) query parameters.

Example — create a booking:

```bash
curl -X POST http://localhost:8080/api/bookings \
  -H "Content-Type: application/json" \
  -d '{
        "resourceId": "<resource-id>",
        "userId": "<user-id>",
        "startsAt": "2026-08-01T10:00:00Z",
        "endsAt": "2026-08-01T11:00:00Z",
        "notes": "Sprint planning"
      }'
```

### Domain rules

A booking is rejected with `400 Bad Request` when the end is not after the
start, the start is in the past, or the duration exceeds 24 hours. Booking a
non-existent resource or user returns `404`, and booking an inactive resource
returns `400`.

### Conflict detection & concurrency

A resource cannot be double-booked. Bookings are modelled as half-open intervals
`[startsAt, endsAt)`, so two bookings conflict exactly when
`existing.startsAt < new.endsAt AND new.startsAt < existing.endsAt` — a single
predicate that covers every overlap shape (starts-during, ends-during,
containing, contained, exact). Back-to-back bookings (one ends exactly when the
next starts) do **not** conflict. Cancelled bookings free their slot.

Correctness under concurrency is guaranteed by a **PostgreSQL exclusion
constraint** (`EXCLUDE USING gist` over a `tstzrange`, partial on
`status <> 'Cancelled'`): the database itself refuses any two overlapping,
non-cancelled bookings for the same resource, so two simultaneous requests for
the same slot can never both succeed. The service also runs an application-level
overlap check first for a fast, friendly `409` in the common case; the
constraint is the definitive backstop for the race. Both paths return
`409 Conflict`.

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

- Authentication & authorization.
- Validation hardening, rate limiting, pagination.
- Automated test suite (integration tests against PostgreSQL) and CI.
