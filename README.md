# Bookings API

A production-style REST API for a bookings / reservations system — resources
(meeting rooms, equipment, appointment slots) that can be reserved for time
slots. Built with **ASP.NET Core (.NET 8)**, **Entity Framework Core**, and
**PostgreSQL**, and fully containerized with Docker.

> **Status:** JWT-authenticated bookings API — resource/user management and
> booking functionality with overlap-conflict detection, concurrency-safe
> reservations, ownership-based authorization, and consistent error responses.

## Tech stack

- ASP.NET Core Web API (.NET 8)
- Entity Framework Core 8 + Npgsql (PostgreSQL)
- PostgreSQL 16
- JWT bearer authentication + BCrypt password hashing
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

> **JWT signing key.** The signing key is a secret and is **not** stored in
> source. `docker-compose.yml` provides a dev-only `Jwt__Key`; for any other
> environment supply your own via the `Jwt__Key` environment variable (or a
> secret store). It must be at least 32 bytes — the app refuses to start
> otherwise.

## Authentication

Register or log in to obtain a JWT, then send it as a bearer token on every
other request:

```bash
# Register (or POST /api/auth/login with existing credentials)
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","fullName":"Alice","password":"S3cret123"}'
# -> { "accessToken": "eyJ...", "expiresAt": "...", "user": { ... } }

# Use the token
curl http://localhost:8080/api/me/bookings \
  -H "Authorization: Bearer eyJ..."
```

Every endpoint requires authentication **except** `POST /api/auth/register`,
`POST /api/auth/login`, and `GET /health` (secure-by-default authorization).

| Method | Route                 | Description                        | Auth        |
|--------|-----------------------|------------------------------------|-------------|
| `POST` | `/api/auth/register`  | Create an account, returns a token | Anonymous   |
| `POST` | `/api/auth/login`     | Exchange credentials for a token   | Anonymous   |
| `GET`  | `/api/users/me`       | The current user's profile         | Bearer      |

## Resource API

| Method   | Route                 | Description            | Success        |
|----------|-----------------------|------------------------|----------------|
| `GET`    | `/api/resources`      | List all resources     | `200 OK`       |
| `GET`    | `/api/resources/{id}` | Get a resource by id   | `200` / `404`  |
| `POST`   | `/api/resources`      | Create a resource      | `201 Created`  |
| `PUT`    | `/api/resources/{id}` | Update a resource      | `200` / `404`  |
| `DELETE` | `/api/resources/{id}` | Delete a resource      | `204` / `404`  |

All resource endpoints require a bearer token. Example — create a resource:

```bash
curl -X POST http://localhost:8080/api/resources \
  -H "Authorization: Bearer eyJ..." \
  -H "Content-Type: application/json" \
  -d '{
        "name": "Conference Room A",
        "description": "10-seat room with projector",
        "type": "MeetingRoom",
        "capacity": 10
      }'
```

## Booking API

The booking owner is always taken from the caller's token — never from the
request body — and a user may only view or cancel their **own** bookings
(`403 Forbidden` otherwise).

| Method | Route                              | Description               | Success                             |
|--------|------------------------------------|---------------------------|-------------------------------------|
| `POST` | `/api/bookings`                    | Create a booking          | `201` / `400` / `404` / `409`       |
| `GET`  | `/api/bookings/{id}`               | Get one of my bookings    | `200` / `403` / `404`               |
| `POST` | `/api/bookings/{id}/cancel`        | Cancel one of my bookings | `200` / `403` / `404` / `409`       |
| `GET`  | `/api/resources/{resourceId}/bookings` | A resource's schedule | `200` / `404`                       |
| `GET`  | `/api/me/bookings`                 | My bookings               | `200`                               |

The list endpoints accept optional `from`, `to` (ISO-8601), and
`includeCancelled` (default `false`) query parameters.

Example — create a booking (no `userId`; it comes from the token):

```bash
curl -X POST http://localhost:8080/api/bookings \
  -H "Authorization: Bearer eyJ..." \
  -H "Content-Type: application/json" \
  -d '{
        "resourceId": "<resource-id>",
        "startsAt": "2026-08-01T10:00:00Z",
        "endsAt": "2026-08-01T11:00:00Z",
        "notes": "Sprint planning"
      }'
```

### Domain rules

A booking is rejected with `400 Bad Request` when the end is not after the
start, the start is in the past, or the duration exceeds 24 hours. Booking a
non-existent resource returns `404`, and booking an inactive resource returns
`400`.

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

## Security notes

- **Password storage** — passwords are hashed with **BCrypt** (work factor 12,
  per-hash random salt); plaintext is never stored. A malformed stored hash
  fails verification rather than erroring.
- **Authentication** — HS256 JWTs with validated issuer, audience, lifetime, and
  signature and a tight (30s) clock skew. The signing key is validated at
  startup (≥ 256 bits) and never committed to source.
- **Authorization** — secure by default: an authorization fallback policy
  requires an authenticated user on every endpoint, with `register`/`login`/
  `health` explicitly anonymous. A booking's owner comes from the token, so a
  client cannot book or act on behalf of another user.
- **No user enumeration** — login returns the same generic `401` (and does the
  same hashing work) whether or not the email exists.
- **Consistent errors** — all failures return RFC7807 `application/problem+json`.
  A global exception handler converts unhandled exceptions into a clean `500`
  ProblemDetails, logging the detail server-side rather than leaking it.

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

- Role-based authorization (e.g. resource administration), refresh tokens.
- Rate limiting and pagination.
- Automated test suite (integration tests against PostgreSQL) and CI.
