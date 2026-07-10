# Bookings API

[![CI](https://github.com/AnthonyPainchaud/bookings-api/actions/workflows/ci.yml/badge.svg)](https://github.com/AnthonyPainchaud/bookings-api/actions/workflows/ci.yml)

A REST API for booking shared resources — meeting rooms, equipment, appointment
slots — for a time range, with the one guarantee that actually matters for a
scheduling system: **the same resource can never be double-booked, even under
concurrent requests.** It's built as a portfolio piece demonstrating what a
production ASP.NET Core service looks like end to end — layered architecture,
authentication and authorization, correctness under concurrency, automated
tests, and CI — rather than a toy CRUD demo.

## Architecture

Four projects, dependencies pointing inward toward the domain:

```
Bookings.Api             HTTP surface: controllers, middleware, DI composition root
   └─ depends on ─▶ Bookings.Application
Bookings.Application     Use cases (services), DTOs, the IApplicationDbContext abstraction
   └─ depends on ─▶ Bookings.Domain
Bookings.Infrastructure  EF Core, Npgsql, migrations, JWT/BCrypt — the concrete implementations
   └─ depends on ─▶ Bookings.Application, Bookings.Domain
Bookings.Domain          Entities and enums. No package references, no framework dependency.
```

**Application depends on an abstraction (`IApplicationDbContext`), not EF Core's
concrete `DbContext`.** Infrastructure implements it. This means the business
logic in `Bookings.Application` has no compile-time dependency on Postgres or
Npgsql, and can be exercised in unit tests against EF Core's InMemory provider —
the same service classes that run in production, not a re-implementation of
their logic.

**Expected failures are values, not exceptions.** A `Result<T>` /
`Error` pair (with a `Validation` / `NotFound` / `Conflict` / `Unauthorized` /
`Forbidden` type) flows from services up to a single place in the API layer
that maps it to the right HTTP status and an RFC 7807 `application/problem+json`
body. A double-booking or a missing resource is an expected outcome the type
system forces every caller to handle — not a thrown exception a caller might
forget to catch. Genuinely unexpected exceptions still exist, and are caught by
one global handler that logs the detail and returns a generic `500`, so nothing
ever leaks a stack trace to a client.

### The booking conflict problem, and the trade-off in solving it

This is the part of the system where correctness actually matters, so it gets
the most explanation.

**Overlap detection.** Bookings are half-open intervals `[startsAt, endsAt)`.
Two bookings on the same resource conflict exactly when
`existing.startsAt < new.endsAt AND new.startsAt < existing.endsAt` — one
predicate, and it's the *only* one needed: it correctly rejects every overlap
shape (one starts mid-way through the other, one fully contains the other,
an exact match) with no special-casing, and correctly *allows* back-to-back
bookings (one ends exactly when the next starts), because at that boundary
instant neither inequality holds.

**Concurrency.** An overlap check that just queries "does anything conflict?"
before inserting has a classic check-then-act race: two requests for the same
slot can both pass the check before either commits its insert, and both
succeed — an actual double-booking, not a hypothetical one. Two ways to close
that race were on the table:

- **Serializable transaction isolation** around the check-and-insert. Correct,
  but every conflicting transaction has to be caught, and one of the two has to
  be retried or fail — more moving parts, and throughput drops as contention
  rises, since serializable transactions abort under contention that
  `READ COMMITTED` would just quietly serialize.
- **A database constraint that makes the impossible state unrepresentable.**
  Chosen. A **PostgreSQL exclusion constraint**
  (`EXCLUDE USING gist (resource_id WITH =, tstzrange(starts_at, ends_at) WITH &&)`,
  partial on `status <> 'Cancelled'`) makes two overlapping, non-cancelled
  bookings for the same resource impossible to insert *at the storage engine
  level* — no application logic, no isolation level, no retry loop can get it
  wrong, because the invariant is enforced where the data actually lives.

The trade-off: this is genuinely Postgres-specific (the migration that adds it
is raw SQL, since EF Core has no exclusion-constraint API) and the invariant
now lives partly outside the application layer, which is a real cost for
anyone who has to reason about the system without knowing to look at the
schema. In exchange, the guarantee holds regardless of how many API
instances are running, is provably correct rather than relying on carefully
threading isolation levels through the code, and can't regress silently in a
future refactor. Given that "can this ever double-book?" is the one question
this system has to always answer "no" to, that trade was worth making.

The application layer still runs the overlap check *first* — it's the same
predicate, evaluated in C# against the database — purely so the common case
(no race) returns a friendly `409` immediately rather than round-tripping
through a database constraint violation. The constraint is the backstop for
the race, not the primary mechanism; removing the app-level check would still
be correct, just slower and uglier on the happy path. This was verified, not
assumed: the integration suite fires 10 concurrent identical booking requests
at the same slot and asserts exactly one `201` and nine `409`s.

### Other decisions worth naming

- **Secure-by-default authorization.** A global fallback policy requires
  authentication on every endpoint; anonymous access is an explicit opt-in
  (`[AllowAnonymous]` on register/login/health), not something that has to be
  remembered for every new controller.
- **A booking's owner comes from the JWT, never the request body.** There's no
  `userId` field a client can set on `POST /bookings` — the alternative
  (trusting a client-supplied owner) is a straightforward authorization bypass.
- **Roles as a claim, not a lookup.** `role` rides in the JWT itself
  (`Admin`/`User`), so authorization checks don't need a database round-trip,
  and is also echoed on `/me` so a frontend never has to decode the token to
  know what a user can do.

## Tech stack

| Choice | Why |
|---|---|
| **ASP.NET Core 8** | Mature, fast, first-class DI and middleware pipeline; LTS. |
| **PostgreSQL** | The one relational engine with a native exclusion-constraint mechanism — the actual reason this database and not SQL Server/MySQL. |
| **EF Core + Npgsql** | Productive migrations and LINQ, without hiding Postgres-specific features the app actually needs (raw SQL for the exclusion constraint). |
| **JWT + BCrypt** | Stateless auth that scales horizontally with no shared session store; BCrypt because rolling your own password hashing is not a place to save time. |
| **Serilog** | Structured, queryable logs (JSON in non-dev) instead of formatted strings. |
| **xUnit + Testcontainers** | Integration tests run against a real, disposable Postgres — including the exclusion constraint — not a mock that can't catch a schema-level bug. |
| **Docker Compose** | One command brings up the API and a matching Postgres; no "works on my machine" setup step. |

## Running it

```bash
docker compose up --build
```

Brings up PostgreSQL and the API, waits for the database to be healthy, and
applies migrations automatically — no manual setup step.

- API: `http://localhost:8080`
- Swagger UI: `http://localhost:8080/swagger`
- Health check: `http://localhost:8080/health`

A demo `Admin` account is seeded on first migration: `admin@bookings.local` /
`AdminPass123!` — a local/demo credential, not a production secret (see
[Roles](#roles)).

To run outside Docker (.NET 8 SDK + a local PostgreSQL), or manage migrations
by hand, see [Local development](#local-development) below.

### Tests

```bash
dotnet test Bookings.sln
```

Runs both suites — see [Testing](#testing) for what each one covers. The
integration suite needs Docker running locally (it starts a real, disposable
Postgres via Testcontainers).

## API summary

Full interactive documentation, generated from the code, is at
`/swagger` when the API is running (or see
[`.github/workflows/ci.yml`](.github/workflows/ci.yml) for what CI actually
runs). The complete endpoint reference — every route, status code, and
query parameter — is in [API reference](#api-reference) below. Two examples to
get the shape of it:

**Register, then create a booking:**

```bash
curl -X POST http://localhost:8080/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","fullName":"Alice","password":"S3cret123"}'
# -> 200 { "accessToken": "eyJ...", "expiresAt": "...", "user": { "role": "User", ... } }

curl -X POST http://localhost:8080/api/v1/bookings \
  -H "Authorization: Bearer eyJ..." -H "Content-Type: application/json" \
  -d '{"resourceId":"<resource-id>","startsAt":"2026-08-01T10:00:00Z","endsAt":"2026-08-01T11:00:00Z"}'
# -> 201 { "id": "...", "status": "Confirmed", ... }
```

**The double-booking case** — the same request again, or any overlapping
range, on the same resource:

```json
// -> 409 Conflict
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "The resource is already booked for an overlapping time range.",
  "traceId": "00-65c88c0f0cebe2831b7b4cf77d5ead45-97973e2bc51979ef-00"
}
```

## What I'd do next / known limitations

Honest gaps, given more time:

- **No refresh tokens.** Access tokens are short-lived and there's no
  revocation or rotation — a stolen token is valid until it expires, and an
  expired session means a full re-login. Fine for a portfolio API; not what
  I'd ship for a real product without adding refresh tokens and a revocation
  list.
- **Rate limiting is in-memory, per instance.** Correct for one API replica;
  running more than one behind a load balancer would need a shared store
  (Redis) for the limiter to actually enforce a global limit rather than one
  limit *per instance*.
- **No role management API.** There's exactly one way to become an `Admin` —
  the seeded account — and no endpoint to promote another user; that requires
  direct database access today. Deliberately simple for now, not something
  I'd leave out of a real product.
- **No metrics/tracing.** Structured logs exist; there's no OpenTelemetry
  export, so answering "what's slow, right now, in production" requires log
  scraping rather than a trace.
- **Deletes are real deletes.** No soft-delete or audit trail on resources or
  bookings — reasonable for this scope, a real product would likely want a
  history of who changed what.
- **CI builds and tests; it doesn't deploy.** No CD step publishes an image or
  ships to an environment yet.

## Local development

<details>
<summary>Running without Docker, managing migrations, environment variables</summary>

### Locally (requires the .NET 8 SDK + a running PostgreSQL)

```bash
dotnet run --project src/Bookings.Api
```

The default connection string (`src/Bookings.Api/appsettings.json`) expects
PostgreSQL on `localhost:5432` with `postgres`/`postgres`. Override with the
`ConnectionStrings__Default` environment variable.

> **JWT signing key.** Never stored in source. `docker-compose.yml` provides a
> dev-only `Jwt__Key`; anywhere else, supply your own via the `Jwt__Key`
> environment variable (or a secret store). It must be at least 32 bytes — the
> app refuses to start otherwise.

### Migrations

Migrations live in `src/Bookings.Infrastructure/Persistence/Migrations` and
apply automatically on API startup. To manage them by hand (requires the
`dotnet-ef` tool, pinned in `.config/dotnet-tools.json`):

```bash
dotnet tool restore
dotnet ef migrations add <Name> \
  --project src/Bookings.Infrastructure \
  --startup-project src/Bookings.Infrastructure \
  --output-dir Persistence/Migrations
```

</details>

## API reference

### Versioning

Every route is versioned via URL segment: `/api/v1/...`. An unversioned
request routes to the default (`v1`); the resolved version is echoed on the
`api-supported-versions` response header. Swagger publishes one document per
version, so a future `v2` can change without breaking `v1`'s contract.

### Authentication

| Method | Route                    | Description                          | Auth      |
|--------|--------------------------|---------------------------------------|-----------|
| `POST` | `/api/v1/auth/register`  | Create an account, returns a token    | Anonymous |
| `POST` | `/api/v1/auth/login`     | Exchange credentials for a token      | Anonymous |
| `GET`  | `/api/v1/users/me`       | The current user's profile (+ `role`) | Bearer    |

Every other endpoint requires a bearer token (secure-by-default authorization).
`register`/`login` are rate-limited more strictly than the rest of the API
(see [Rate limiting](#rate-limiting)).

### Roles

Every account has a `role` (`User` or `Admin`), carried as a JWT claim and
echoed on `/me`. Registration always creates a `User` — there's no
self-service path to `Admin` (that would be a privilege-escalation hole).
One `Admin` account is seeded by the `AddUserRole` migration:

```
email:    admin@bookings.local
password: AdminPass123!
```

A local/demo credential — the migration stores only its BCrypt hash, same as
any other account. Rotate or remove it before any real deployment.

### Resources

| Method   | Route                    | Description             | Success       | Auth      |
|----------|--------------------------|--------------------------|---------------|-----------|
| `GET`    | `/api/v1/resources`      | List resources (paged)   | `200`         | Bearer    |
| `GET`    | `/api/v1/resources/{id}` | Get a resource by id     | `200` / `404` | Bearer    |
| `POST`   | `/api/v1/resources`      | Create a resource        | `201`         | **Admin** |
| `PUT`    | `/api/v1/resources/{id}` | Update a resource        | `200` / `404` | **Admin** |
| `DELETE` | `/api/v1/resources/{id}` | Delete a resource        | `204` / `404` | **Admin** |

Reading only requires being signed in; mutating requires `Admin` (`403`
otherwise).

### Bookings

The owner is always taken from the caller's token, never the request body,
and a user may only view or cancel their **own** bookings (`403` otherwise).

| Method | Route                                    | Description                   | Success                       |
|--------|-------------------------------------------|--------------------------------|-------------------------------|
| `POST` | `/api/v1/bookings`                        | Create a booking               | `201` / `400` / `404` / `409` |
| `GET`  | `/api/v1/bookings/{id}`                   | Get one of my bookings         | `200` / `403` / `404`         |
| `POST` | `/api/v1/bookings/{id}/cancel`             | Cancel one of my bookings      | `200` / `403` / `404` / `409` |
| `GET`  | `/api/v1/resources/{resourceId}/bookings` | A resource's schedule (paged)  | `200` / `404`                 |
| `GET`  | `/api/v1/me/bookings`                     | My bookings (paged)            | `200`                         |

List endpoints accept optional `from`, `to` (ISO-8601) and `includeCancelled`
(default `false`), plus [pagination](#pagination). A booking is rejected with
`400` when the end isn't after the start, the start is in the past, or the
duration exceeds 24 hours; booking a non-existent resource is `404`, an
inactive one is `400`. See [Architecture](#architecture) for how overlap and
concurrency are handled.

### Admin

| Method | Route                    | Description                                  | Success | Auth      |
|--------|--------------------------|------------------------------------------------|---------|-----------|
| `GET`  | `/api/v1/admin/bookings` | Every booking, every resource/user, newest first | `200` | **Admin** |

Returns `AdminBookingResponse` — a booking enriched with its resource name and
owner's email/name (via a join), so a caller doesn't need extra round-trips to
resolve them. Same `from`/`to`/`includeCancelled`/pagination as the other list
endpoints, plus optional `resourceId`/`userId` filters.

### Pagination

Every list endpoint accepts `page` (default `1`) and `pageSize` (default `20`,
max `100`) and returns:

```json
{ "items": [ /* ... */ ], "page": 1, "pageSize": 20, "totalCount": 57, "totalPages": 3 }
```

`page < 1` or `pageSize` outside `1..100` is rejected with `400`.

### Rate limiting

Built on ASP.NET Core's `Microsoft.AspNetCore.RateLimiting`:

- **Global** — 100 requests/minute, partitioned by authenticated user id
  (falling back to client IP), across the whole API.
- **Auth policy** — 5 requests/minute, partitioned by client IP, on
  `register`/`login` only, to slow down credential stuffing.
- `GET /health` is exempt.

Exceeding a limit returns `429` as ProblemDetails with a `Retry-After` header.

### Structured logging

Serilog replaces the default provider. Every request logs as one structured
line (method, path, status, elapsed) via `UseSerilogRequestLogging`, plus the
application's own events. Human-readable in Development, compact JSON
otherwise; levels are configured under `Serilog` in `appsettings.json`.

### Security notes

- **Passwords** — BCrypt, work factor 12, per-hash random salt; plaintext is
  never stored, and a malformed stored hash fails verification rather than
  throwing.
- **JWTs** — HS256, validated issuer/audience/lifetime/signature, 30-second
  clock skew. The signing key is validated at startup (≥256 bits) and never
  committed to source.
- **No user enumeration** — login returns the same generic `401` (and does
  equivalent hashing work) whether or not the email exists.
- **Consistent errors** — every failure is RFC 7807
  `application/problem+json`; a global exception handler converts unhandled
  exceptions into a generic `500`, logging the real detail server-side only.

## Testing

Two projects, split by what they need to run:

- **`Bookings.UnitTests`** — fast, no Docker, no network. Uses EF Core's
  InMemory provider and exercises the real `BookingService`/`ResourceService`
  classes, not a re-implementation of their logic.
  - `BookingConflictDetectionTests` — every overlap shape, the two
    non-conflicting adjacent cases, a cancelled booking freeing its slot, and
    the same range being fine on a different resource.
  - `BookingDomainRuleTests` — end-not-after-start, start-in-the-past,
    over-long duration, resource-not-found, inactive-resource.
- **`Bookings.IntegrationTests`** — real HTTP calls
  (`WebApplicationFactory`) against a real, disposable PostgreSQL
  ([Testcontainers](https://dotnet.testcontainers.org/)), running the actual
  migrations including the exclusion constraint. One container is shared
  across the run for speed; tests use unique emails/names for isolation.
  - `ResourceCrudTests`, `AdminBookingsTests` — CRUD, validation, and
    role-gating (`403` for a non-admin).
  - `BookingConflictTests` — every overlap rejected with `409` over real HTTP
    against the real constraint, back-to-back succeeding, cancel-then-rebook,
    and **10 concurrent requests for one slot producing exactly one `201` and
    nine `409`s**.
  - `AuthorizationTests`, `PaginationTests` — token/ownership checks, page
    slicing and bounds.

Coverage is deliberately not 100%: trivial mapping/DI-registration code isn't
tested in isolation, since the integration suite exercises it anyway. Effort
went into the booking conflict/concurrency logic — the most important and
most failure-prone rule in the system — and the security-relevant paths.

## Continuous integration

[.github/workflows/ci.yml](.github/workflows/ci.yml) builds and runs the full
test suite (unit + integration) on every push and PR to `main`. GitHub-hosted
runners have Docker natively, so the Testcontainers-based integration tests
need no special CI setup. Test results upload as a `.trx` artifact on failure.
