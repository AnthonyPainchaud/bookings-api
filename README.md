# Bookings API

A production-style REST API for a bookings / reservations system — resources
(meeting rooms, equipment, appointment slots) that can be reserved for time
slots. Built with **ASP.NET Core (.NET 8)**, **Entity Framework Core**, and
**PostgreSQL**, and fully containerized with Docker.

> **Status:** JWT-authenticated, versioned bookings API — resource/user
> management and booking functionality with overlap-conflict detection,
> concurrency-safe reservations, ownership-based authorization, pagination,
> rate limiting, and structured logging.

## Tech stack

- ASP.NET Core Web API (.NET 8), versioned (`/api/v1/...`)
- Entity Framework Core 8 + Npgsql (PostgreSQL)
- PostgreSQL 16
- JWT bearer authentication + BCrypt password hashing
- Built-in ASP.NET Core rate limiting
- Serilog structured logging
- Docker + Docker Compose
- Swagger / OpenAPI (development), versioned docs + bearer auth support

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

## API versioning

Every route is versioned via a URL segment: `/api/v1/...`. A request with no
version specified is routed to the default (`v1`); the resolved version is
echoed back on the `api-supported-versions` response header. Swagger publishes
one document per version (`/swagger/v1/swagger.json`), so a future `v2` can be
introduced without touching `v1`'s contract.

## Authentication

Register or log in to obtain a JWT, then send it as a bearer token on every
other request:

```bash
# Register (or POST /api/v1/auth/login with existing credentials)
curl -X POST http://localhost:8080/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","fullName":"Alice","password":"S3cret123"}'
# -> { "accessToken": "eyJ...", "expiresAt": "...", "user": { ... } }

# Use the token
curl http://localhost:8080/api/v1/me/bookings \
  -H "Authorization: Bearer eyJ..."
```

Every endpoint requires authentication **except** `POST /api/v1/auth/register`,
`POST /api/v1/auth/login`, and `GET /health` (secure-by-default authorization).
`register`/`login` are also rate-limited more strictly than the rest of the API
(see [Rate limiting](#rate-limiting)) to slow down credential stuffing.

| Method | Route                    | Description                        | Auth        |
|--------|--------------------------|-------------------------------------|-------------|
| `POST` | `/api/v1/auth/register`  | Create an account, returns a token | Anonymous   |
| `POST` | `/api/v1/auth/login`     | Exchange credentials for a token   | Anonymous   |
| `GET`  | `/api/v1/users/me`       | The current user's profile         | Bearer      |

## Resource API

| Method   | Route                    | Description            | Success        |
|----------|--------------------------|-------------------------|----------------|
| `GET`    | `/api/v1/resources`      | List resources (paged)  | `200 OK`       |
| `GET`    | `/api/v1/resources/{id}` | Get a resource by id    | `200` / `404`  |
| `POST`   | `/api/v1/resources`      | Create a resource       | `201 Created`  |
| `PUT`    | `/api/v1/resources/{id}` | Update a resource       | `200` / `404`  |
| `DELETE` | `/api/v1/resources/{id}` | Delete a resource       | `204` / `404`  |

All resource endpoints require a bearer token. Example — create a resource:

```bash
curl -X POST http://localhost:8080/api/v1/resources \
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

| Method | Route                                      | Description                | Success                       |
|--------|---------------------------------------------|-----------------------------|-------------------------------|
| `POST` | `/api/v1/bookings`                          | Create a booking            | `201` / `400` / `404` / `409` |
| `GET`  | `/api/v1/bookings/{id}`                     | Get one of my bookings      | `200` / `403` / `404`         |
| `POST` | `/api/v1/bookings/{id}/cancel`              | Cancel one of my bookings   | `200` / `403` / `404` / `409` |
| `GET`  | `/api/v1/resources/{resourceId}/bookings`   | A resource's schedule (paged) | `200` / `404`                |
| `GET`  | `/api/v1/me/bookings`                       | My bookings (paged)         | `200`                         |

The list endpoints accept optional `from`, `to` (ISO-8601), and
`includeCancelled` (default `false`) query parameters, plus [pagination](#pagination).

Example — create a booking (no `userId`; it comes from the token):

```bash
curl -X POST http://localhost:8080/api/v1/bookings \
  -H "Authorization: Bearer eyJ..." \
  -H "Content-Type: application/json" \
  -d '{
        "resourceId": "<resource-id>",
        "startsAt": "2026-08-01T10:00:00Z",
        "endsAt": "2026-08-01T11:00:00Z",
        "notes": "Sprint planning"
      }'
```

## Pagination

Every list endpoint (`GET /api/v1/resources`, a resource's bookings, and
`/api/v1/me/bookings`) accepts `page` (default `1`) and `pageSize` (default
`20`, max `100`) query parameters and returns a consistent envelope:

```json
{
  "items": [ /* ... */ ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 57,
  "totalPages": 3
}
```

`page < 1` or `pageSize` outside `1..100` is rejected with `400 Bad Request`.

## Rate limiting

Built on ASP.NET Core's `Microsoft.AspNetCore.RateLimiting` middleware:

- **Global limiter** — 100 requests/minute, partitioned by authenticated user
  id (falling back to client IP for anonymous requests), applied to the whole
  API.
- **Auth policy** — a stricter 5 requests/minute, partitioned by client IP,
  applied only to `register`/`login` to slow down credential stuffing.
- `GET /health` is exempt (orchestrator probes should never be throttled).

Exceeding a limit returns `429 Too Many Requests` as ProblemDetails, with a
`Retry-After` header.

## Structured logging

Serilog replaces the default logging provider. Every request is logged as a
single structured line (method, path, status code, elapsed time) via
`UseSerilogRequestLogging`, in addition to the application's own log events.
Output is human-readable in Development and compact JSON (suited to log
aggregation) otherwise; minimum levels are configured under the `Serilog`
section in `appsettings.json`.

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
- Automated test suite (integration tests against PostgreSQL) and CI.
