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
5. **Rejection state guard** -- An application cannot be rejected after the candidate has already officially enrolled (or is mid-enrollment).

## Architecture

The system is decomposed into five microservices, each following Clean Architecture internally.

```
InternshipTracker/
  src/
    Contracts/                    # Shared message contracts (events & commands)
    UserService/
      UserService.Api/            # Minimal API endpoints, Exception middleware
      UserService.Application/    # Use Cases, DTOs, Interfaces
      UserService.Domain/         # Entities, Domain Rules, Exceptions
      UserService.Infrastructure/ # EF Core, Repositories, MassTransit outbox, Migrations
    CoreService/
      CoreService.Api/            # Minimal API endpoints, Exception middleware
      CoreService.Application/    # Use Cases, DTOs, Interfaces, Domain Services
      CoreService.Domain/         # Entities, Domain Rules, Exceptions
      CoreService.Infrastructure/ # EF Core, Repositories, MassTransit consumers, Saga, Migrations
    ITProvisionService/
      ITProvisionService.Api/     # Minimal API, health endpoint
      ITProvisionService.Application/
      ITProvisionService.Domain/
      ITProvisionService.Infrastructure/ # ProvisionCorporateAccountConsumer
    NotificationService/
      NotificationService.Api/    # Minimal API, health endpoint
      NotificationService.Application/  # SendWelcomeEmailConsumer
    GatewayService/               # YARP Reverse Proxy, Correlation ID middleware
    InternshipTracker.Tests/      # Unit, Application, Saga, and API tests
```

Dependencies flow inward within each service: **Api → Infrastructure → Application → Domain**.
The Domain layer has no external dependencies.

### Services

**UserService** -- Owns user data. Handles user write operations (create, delete). Each mutation publishes an event to RabbitMQ via MassTransit with an EF Core transactional outbox, backed by its own `user_db` PostgreSQL database.

**CoreService** -- Owns internship and application data. Handles all read and write operations for internships and applications, and serves user reads from its own read-replica (`core_db`). Consumes user events from RabbitMQ (MassTransit) to keep the local user projection in sync. Hosts the **Onboarding Saga** state machine and its compensation consumers.

**ITProvisionService** -- Stateless leaf service. Consumes `ProvisionCorporateAccountCommand` from the saga, simulates creating a corporate email account, and replies with `AccountProvisionedEvent` or `AccountProvisioningFailedEvent`.

**NotificationService** -- Stateless leaf service. Consumes `SendWelcomeEmailCommand` from the saga, simulates sending a welcome email, and replies with `EmailSentEvent` or `EmailSendingFailedEvent`.

**GatewayService** -- YARP reverse proxy. Single entry point for all clients (port `8000`). Routes read requests for users (`GET /users`) to CoreService and write requests (`POST`, `DELETE /users`) to UserService. All internship and application traffic is routed to CoreService. Health-check routes are exposed for every downstream service. Injects an `X-Correlation-ID` header on every request.

**Contracts** -- Shared class library (no dependencies) referenced by every service. Contains all MassTransit event and command record types so message schemas match exactly across the bus.

### Communication

- **Synchronous** -- HTTP via the Gateway to each downstream service.
- **Asynchronous** -- RabbitMQ (MassTransit) for domain events, saga commands, and saga reply events. CoreService uses a transactional EF Core outbox for reliable publishing.

---

## Onboarding Saga

When an API caller sets an application's status to `Enrolled`, CoreService does **not** flip the status directly. Instead it transitions the application to the intermediate `Enrolling` state and publishes an `OnboardingStartedEvent`. A MassTransit `MassTransitStateMachine<OnboardingSagaState>` hosted in CoreService then orchestrates the full enrollment flow across services.

### Application Status Lifecycle

```
Pending ──► Accepted ──► Enrolling ──► Enrolled            (happy path)
                │              │
                │              ├──► EnrolledNotificationFault  (IT OK, email failed)
                │              │
                │              └──► Accepted                   (IT failed — compensation)
                │
                └──► Rejected
```

| Value | Status                       | Description                                        |
|-------|------------------------------|----------------------------------------------------|
| 0     | `Pending`                    | Initial state after applying                       |
| 1     | `Accepted`                   | Internship offered the position to the candidate   |
| 2     | `Enrolling`                  | Saga lock — onboarding in progress                 |
| 3     | `Enrolled`                   | Saga completed successfully                        |
| 4     | `Rejected`                   | Application rejected                               |
| 5     | `EnrolledNotificationFault`  | IT account created but welcome email failed        |

### Saga State Machine

The saga is correlated by `ApplicationId` and persisted in the `OnboardingSagaState` table (EF Core, PostgreSQL).

**States:** `ProvisioningIT`, `SendingNotification`, `Completed`, `Faulted`

**Transitions:**

| #  | Trigger Event                    | From State          | Command Published                    | To State            |
|----|----------------------------------|---------------------|--------------------------------------|---------------------|
| 1  | `OnboardingStartedEvent`         | Initial             | `ProvisionCorporateAccountCommand`   | ProvisioningIT      |
| 2a | `AccountProvisionedEvent`        | ProvisioningIT      | `SendWelcomeEmailCommand`            | SendingNotification |
| 2b | `AccountProvisioningFailedEvent` | ProvisioningIT      | `RevertApplicationStatusCommand`     | Faulted             |
| 3a | `EmailSentEvent`                 | SendingNotification | `FinalizeEnrollmentCommand`          | Completed (removed) |
| 3b | `EmailSendingFailedEvent`        | SendingNotification | `FaultApplicationEnrollmentCommand`  | Faulted             |

```
                        ┌──────────────────────────────────────────┐
                        │          OnboardingStartedEvent          │
                        └────────────────────┬─────────────────────┘
                                             │
                                             ▼
                                  ┌─────────────────────┐
                                  │   ProvisioningIT    │
                                  └──────┬────────┬─────┘
                          success ◄──────┘        └──────► failure
                                  │                       │
                    AccountProvisionedEvent    AccountProvisioningFailedEvent
                                  │                       │
                                  ▼                       ▼
                      ┌──────────────────────┐   ┌──────────────┐
                      │ SendingNotification  │   │   Faulted    │
                      └──────┬─────────┬─────┘   └──────────────┘
               success ◄─────┘         └─────► failure      ▲
                       │                       │             │
                EmailSentEvent       EmailSendingFailedEvent │
                       │                       │             │
                       ▼                       └─────────────┘
                ┌─────────────┐
                │  Completed  │ → finalized & removed
                └─────────────┘
```

### Shared Contracts

All saga messages live in the `Contracts` project:

```
src/Contracts/
    Events/
        OnboardingStartedEvent          (ApplicationId, CandidateId, CandidateName, CandidateEmail)
        AccountProvisionedEvent         (ApplicationId, CorporateEmail)
        AccountProvisioningFailedEvent  (ApplicationId, Reason)
        EmailSentEvent                  (ApplicationId)
        EmailSendingFailedEvent         (ApplicationId, Reason)
    Commands/
        ProvisionCorporateAccountCommand  (ApplicationId, CandidateId, CandidateName, CandidateEmail)
        SendWelcomeEmailCommand           (ApplicationId, CandidateEmail, CorporateEmail)
        RevertApplicationStatusCommand    (ApplicationId)
        FinalizeEnrollmentCommand         (ApplicationId)
        FaultApplicationEnrollmentCommand (ApplicationId, Reason)
```

### CoreService Saga Consumers

Three consumers in `CoreService.Infrastructure/Messaging/Consumers/` react to saga commands:

| Consumer                              | Command                            | Action                                     |
|---------------------------------------|------------------------------------|--------------------------------------------|
| `FinalizeEnrollmentConsumer`          | `FinalizeEnrollmentCommand`        | `Enrolling → Enrolled`                     |
| `RevertApplicationStatusConsumer`     | `RevertApplicationStatusCommand`   | `Enrolling → Accepted` (compensation)      |
| `FaultApplicationEnrollmentConsumer`  | `FaultApplicationEnrollmentCommand`| `Enrolling → EnrolledNotificationFault`    |

### Leaf Service Consumers

| Service              | Consumer                              | Consumes                             | Publishes on Success             | Publishes on Failure                  |
|----------------------|---------------------------------------|--------------------------------------|----------------------------------|---------------------------------------|
| ITProvisionService   | `ProvisionCorporateAccountConsumer`   | `ProvisionCorporateAccountCommand`   | `AccountProvisionedEvent`        | `AccountProvisioningFailedEvent`      |
| NotificationService  | `SendWelcomeEmailConsumer`            | `SendWelcomeEmailCommand`            | `EmailSentEvent`                 | `EmailSendingFailedEvent`             |

---

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
   RabbitMQ management UI is at `http://localhost:15672` (guest/guest).

3. Stop:
   ```bash
   docker compose down
   ```

### Run Tests

```bash
cd InternshipTracker
dotnet test
```

Tests include saga state-machine tests (`OnboardingSagaTests`) that use the MassTransit in-memory test harness to verify every state transition and compensation path.

## API Examples

All requests go through the Gateway on port `8000`.

### Health Checks

```bash
curl http://localhost:8000/core/health
curl http://localhost:8000/users/health
curl http://localhost:8000/it-provision/health
curl http://localhost:8000/notification/health
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

Status values: `0` = Pending, `1` = Accepted, `2` = Enrolled (triggers onboarding saga), `3` = Rejected

> **Note:** Setting status to `Enrolled` (2) does not immediately enroll the candidate. It transitions the application to `Enrolling` and kicks off the onboarding saga. The final `Enrolled` (3) or `EnrolledNotificationFault` (5) status is set asynchronously by the saga upon completion.
