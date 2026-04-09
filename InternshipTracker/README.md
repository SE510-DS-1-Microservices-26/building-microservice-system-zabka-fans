# Internship Tracker

## Theme

- **Theme:** Internship Tracker
- **Core Item:** `InternshipApplication`
- **Core Action:** `ApplyForInternship`

## Domain Rules

1. **Underqualified candidate rejection** -- A candidate cannot apply for an internship if their level is below the
   internship's minimum requirement.
2. **Duplicate application prevention** -- A candidate cannot apply to the same internship more than once.
3. **Capacity limit** -- An internship cannot accept more candidates than its defined capacity.
4. **Exclusive enrollment** -- A candidate cannot be enrolled in more than one internship at a time.
5. **Rejection state guard** -- An application cannot be rejected after the candidate has already officially enrolled.

## Architecture

The system is decomposed into three microservices, each following Clean Architecture internally.

```
InternshipTracker/
  src/
    UserService/
      UserService.Api/            # Minimal API endpoints, Exception middleware
      UserService.Application/    # Use Cases, DTOs, Interfaces
      UserService.Domain/         # Entities, Domain Rules, Exceptions
      UserService.Infrastructure/ # EF Core, Repositories, MassTransit outbox, Migrations
    CoreService/
      CoreService.Api/            # Minimal API endpoints, Exception middleware
      CoreService.Application/    # Use Cases, DTOs, Interfaces, Domain Services
      CoreService.Domain/         # Entities, Domain Rules, Exceptions
      CoreService.Infrastructure/ # EF Core, Repositories, MassTransit consumers, Migrations
    GatewayService/               # YARP Reverse Proxy, Correlation ID middleware
    InternshipTracker.Tests/      # Unit, Application, and API tests
```

Dependencies flow inward within each service: **Api -> Infrastructure -> Application -> Domain**.
The Domain layer has no external dependencies.

### Services

**UserService** -- Owns user data. Handles user write operations (create, delete). Each mutation publishes an event to RabbitMQ via MassTransit with an EF Core transactional outbox, backed by its own `user_db` PostgreSQL database.

**CoreService** -- Owns internship and application data. Handles all read and write operations for internships and applications, and serves user reads from its own read-replica (`core_db`). Consumes user events from RabbitMQ (MassTransit) to keep the local user projection in sync.

**GatewayService** -- YARP reverse proxy. Single entry point for all clients (port `8000`). Routes read requests for users (`GET /users`) to CoreService and write requests (`POST`, `DELETE /users`) to UserService. All internship and application traffic is routed to CoreService. Injects an `X-Correlation-ID` header on every request.

### Communication

- **Synchronous** -- HTTP via the Gateway to each downstream service.
- **Asynchronous** -- RabbitMQ (MassTransit) with a transactional EF Core outbox for user domain events.

## How to Run with Docker

### Prerequisites

- .NET 10 SDK
- Docker

### Steps

1. Create a `.env` file at the root of `InternshipTracker/`:
   ```
   POSTGRES_USER=postgres
   POSTGRES_PASSWORD=Password123!
   RABBITMQ_USER=guest
   RABBITMQ_PASS=guest
   ```

2. Build and start all services:
   ```bash
   cd InternshipTracker
   docker compose up --build
   ```

   The API will be available at `http://localhost:8000`.

3. Stop:
   ```bash
   docker compose down
   ```

### Run Tests

```bash
cd InternshipTracker
dotnet test
```

## API Examples

All requests go through the Gateway on port `8000`.

### Health Checks

```bash
curl http://localhost:8000/core/health
curl http://localhost:8000/users/health
```

### Create a User

```bash
curl -X POST http://localhost:8000/users \
  -H "Content-Type: application/json" \
  -d '{"name": "John Doe", "email": "john.doe@example.com", "level": 1}'
```

Level values: `0` = Trainee, `1` = Junior, `2` = Middle, `3` = Senior

### Get a User

```bash
curl http://localhost:8000/users/{id}
```

### Get All Users

```bash
curl "http://localhost:8000/users?page=1&pageSize=10"
```

### Delete a User

```bash
curl -X DELETE http://localhost:8000/users/{id}
```

### Create an Internship

```bash
curl -X POST http://localhost:8000/internships \
  -H "Content-Type: application/json" \
  -d '{"title": "Software Engineering Intern", "capacity": 10, "minimumLevel": 1}'
```

### Get an Internship

```bash
curl http://localhost:8000/internships/{id}
```

### Get All Internships

```bash
curl "http://localhost:8000/internships?page=1&pageSize=10"
```

### Apply for an Internship

```bash
curl -X POST http://localhost:8000/applications \
  -H "Content-Type: application/json" \
  -d '{"userId": "<user-id>", "internshipId": "<internship-id>"}'
```

### Get All Applications

```bash
curl "http://localhost:8000/applications?page=1&pageSize=10"
```

### Change Application Status

```bash
curl -X PATCH http://localhost:8000/applications/{id}/status \
  -H "Content-Type: application/json" \
  -d '{"applicationId": "<id>", "newStatus": 1}'
```

Status values: `0` = Pending, `1` = Accepted, `2` = Enrolled, `3` = Rejected
