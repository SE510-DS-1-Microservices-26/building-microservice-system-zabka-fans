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

```
InternshipTracker/
  src/
    InternshipTracker.Domain/           # Entities, Value Objects, Domain Rules, Exceptions
    InternshipTracker.Application/      # Use Cases, DTOs, Interfaces, Domain Services
    InternshipTracker.Infrastructure/   # EF Core, Repositories, Migrations, DI
    InternshipTracker.UI/               # Minimal API Endpoints
    InternshipTracker.Tests/            # Unit, Application, and API tests
```

Dependencies flow inward: **UI -> Infrastructure -> Application -> Domain**.
The Domain layer has no external dependencies.

## Why Modular Monolith First?

We have chosen a modular monolith over microservices because:

- **Easier development** -- no need for service discovery, API gateways, or distributed tracing at this stage.
- **Clear module boundaries** -- Clean Architecture layers enforce separation, making a future migration to
  microservices straightforward if needed.
- **Reduced operational complexity** -- one database, one CI pipeline, one Docker image.

## How to Run Locally

### Prerequisites

- .NET 10 SDK
- Docker (for PostgreSQL)

### Steps

1. Start PostgreSQL:
   ```bash
   cd InternshipTracker
   docker compose up db -d
   ```

2. Set up user secrets (first time only):
   ```bash
   cd src/InternshipTracker.UI
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=internship_tracker_db;Username=postgres;Password=Password123!"
   ```

3. Run the application:
   ```bash
   dotnet run --project src/InternshipTracker.UI
   ```

   The API will be available at `http://localhost:5000`.

### Run Tests

```bash
cd InternshipTracker
dotnet test
```

## How to Run with Docker

1. Create a `.env` file (see `.env.example` for reference):
   ```
   POSTGRES_USER=postgres
   POSTGRES_PASSWORD=Password123!
   POSTGRES_DB=internship_tracker_db
   ```

2. Build and start:
   ```bash
   cd InternshipTracker
   docker compose up --build
   ```

   The API will be available at `http://localhost:8080`.

3. Stop:
   ```bash
   docker compose down
   ```

## API Examples

### Health Check

```bash
curl http://localhost:8080/health
```

### Create a User

```bash
curl -X POST http://localhost:8080/users \
  -H "Content-Type: application/json" \
  -d '{"name": "John Doe", "level": 1}'
```

Level values: `0` = Trainee, `1` = Junior, `2` = Middle, `3` = Senior

### Get a User

```bash
curl http://localhost:8080/users/{id}
```

### Create an Internship

```bash
curl -X POST http://localhost:8080/internships \
  -H "Content-Type: application/json" \
  -d '{"title": "Software Engineering Intern", "capacity": 10, "minimumLevel": 1}'
```

### Get an Internship

```bash
curl http://localhost:8080/internships/{id}
```

### Apply for an Internship

```bash
curl -X POST http://localhost:8080/applications \
  -H "Content-Type: application/json" \
  -d '{"userId": "<user-id>", "internshipId": "<internship-id>"}'
```

### Change Application Status

```bash
curl -X PATCH http://localhost:8080/applications/{id}/status \
  -H "Content-Type: application/json" \
  -d '{"applicationId": "<id>", "newStatus": 1}'
```

Status values: `0` = Pending, `1` = Accepted, `2` = Enrolled, `3` = Rejected
