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
      ITProvisionService.Api/     # Minimal API, health endpoint, Swagger
      ITProvisionService.Application/
      ITProvisionService.Domain/
      ITProvisionService.Infrastructure/ # ProvisionCorporateAccountConsumer, EF Core outbox, Migrations
    NotificationService/
      NotificationService.Api/    # Minimal API, health endpoint, Swagger
      NotificationService.Application/  # SendWelcomeEmailConsumer
      NotificationService.Infrastructure/ # EF Core outbox, Migrations
    GatewayService/               # YARP Reverse Proxy, Correlation ID middleware
    InternshipTracker.Tests/      # Unit, Application, Saga, and API tests
```

Dependencies flow inward within each service: **Api → Infrastructure → Application → Domain**.
The Domain layer has no external dependencies.

### Services

**UserService** -- Owns user data. Handles user write operations (create, delete). No read endpoints — all user reads are served by CoreService from its own read-replica. Each mutation publishes an event to RabbitMQ via MassTransit with an EF Core transactional outbox, backed by its own `user_db` PostgreSQL database.

**CoreService** -- Owns internship and application data. Handles all read and write operations for internships and applications, and serves user reads from its own read-replica (`core_db`). Consumes user events from RabbitMQ (MassTransit) to keep the local user projection in sync. Hosts the **Onboarding Saga** state machine and its compensation consumers.

**ITProvisionService** -- Leaf service backed by its own `it_provision_db` PostgreSQL database. Consumes `ProvisionCorporateAccountCommand` from the saga, simulates creating a corporate email account, and replies with `AccountProvisionedEvent` or `AccountProvisioningFailedEvent`. Uses a MassTransit EF Core transactional outbox to guarantee reliable event publishing. Exposes Swagger for API documentation.

**NotificationService** -- Leaf service backed by its own `notification_db` PostgreSQL database. Consumes `SendWelcomeEmailCommand` from the saga, simulates sending a welcome email, and replies with `EmailSentEvent` or `EmailSendingFailedEvent`. Uses a MassTransit EF Core transactional outbox to guarantee reliable event publishing. Exposes Swagger for API documentation.

**GatewayService** -- YARP reverse proxy. Single entry point for all clients (port `8000`). Routes read requests for users (`GET /users`) to CoreService and write requests (`POST`, `DELETE /users`) to UserService. All internship and application traffic is routed to CoreService. Health-check and Swagger routes are exposed for every downstream service (Core, Users, IT Provision, Notification). Aggregated Swagger UI at `/swagger`. Wraps every proxied request with a **Polly resilience pipeline** (circuit breaker → retry → per-attempt timeout) via a custom `IForwarderHttpClientFactory`. Propagates `X-Correlation-ID` from the incoming client request, or generates a new `Guid` if the header is absent, and forwards it to every downstream service so the same ID flows through the entire request chain.

**Contracts** -- Shared class library (no dependencies) referenced by every service. Contains all MassTransit event and command record types so message schemas match exactly across the bus.

### Communication

- **Synchronous** -- HTTP via the Gateway to each downstream service.
- **Asynchronous** -- RabbitMQ (MassTransit) for domain events, saga commands, and saga reply events. CoreService, ITProvisionService, and NotificationService each use a MassTransit EF Core transactional outbox backed by their own PostgreSQL database to guarantee reliable message publishing.

### Swagger via Gateway

The YARP gateway aggregates Swagger JSON from every downstream service into a single Swagger UI at `http://localhost:8000/swagger`. Available API docs:

| Service             | Gateway Swagger JSON path                        |
|---------------------|--------------------------------------------------|
| Core Service        | `/core/swagger/v1/swagger.json`                  |
| User Service        | `/users/swagger/v1/swagger.json`                 |
| IT Provision Service| `/it-provision/swagger/v1/swagger.json`          |
| Notification Service| `/notification/swagger/v1/swagger.json`          |

### Transactional Outbox Pattern

Every service that publishes messages to RabbitMQ uses the **MassTransit EF Core transactional outbox** to avoid the dual-write problem (writing to the database and publishing to the broker in the same logical operation).

How it works:
1. When a consumer (or use case) calls `Publish()`/`Send()`, the message is written to an `OutboxMessage` table in the same database transaction as the business data change.
2. A background delivery service polls the outbox table and forwards messages to RabbitMQ.
3. An `InboxState` table provides idempotent message consumption (duplicate detection).

| Service              | Database           | Outbox DbContext        |
|----------------------|--------------------|-------------------------|
| CoreService          | `core_db`          | `CoreDbContext`         |
| UserService          | `user_db`          | `UserDbContext`         |
| ITProvisionService   | `it_provision_db`  | `ITProvisionDbContext`  |
| NotificationService  | `notification_db`  | `NotificationDbContext` |

### Resilience (Gateway — Polly)

Every request forwarded by the Gateway passes through a Polly **resilience pipeline** implemented via a custom `IForwarderHttpClientFactory` (`ResilientForwarderHttpClientFactory`). The pipeline layers wrap the actual `SocketsHttpHandler` in the following order (outer → inner):

```
Circuit Breaker → Retry → Timeout (per-attempt) → SocketsHttpHandler
```

| Policy | Behaviour |
|---|---|
| **Circuit Breaker** | Opens after ≥ 50 % failure rate across ≥ 5 requests in a 60 s sampling window. Stays open for 30 s, then half-opens to probe one request. Only counts failures **after all retries have been exhausted**, so a single flaky request does not trip the breaker. |
| **Retry** | Up to 3 attempts with exponential back-off (base 1 s) + jitter. Retries on `HttpRequestException` (network errors) and HTTP 502 / 503 / 504 status codes. Does **not** retry timeouts or 4xx responses. |
| **Timeout** | 10 s per individual attempt. A timed-out attempt is counted as a failure by the circuit breaker but is not retried. |

All three policies log structured events (`LogWarning` for retries and timeouts, `LogError` when the circuit opens) with the cluster ID so failures are traceable in dashboards.

Tune the defaults without redeploying by changing the `Resilience` section in `appsettings.json`:

```json
"Resilience": {
  "TimeoutSeconds": 10,
  "Retry": { "MaxAttempts": 3, "BaseDelaySeconds": 1 },
  "CircuitBreaker": {
    "MinimumThroughput": 5,
    "FailureRatio": 0.5,
    "BreakDurationSeconds": 30,
    "SamplingDurationSeconds": 60
  }
}
```

### Correlation IDs

The system uses a lightweight, header-based correlation ID pattern — no external tracing library required.

#### How it works

| Step | Location | Behaviour |
|------|----------|-----------|
| 1 | **Gateway** inline middleware | Reads `X-Correlation-ID` from the incoming client request. If the header is absent, generates `Guid.NewGuid().ToString()`. Injects the header on the forwarded request (so YARP carries it to the downstream service) and echoes it on the response via `OnStarting`. |
| 2 | **Downstream services** `CorrelationIdMiddleware` | Reads `X-Correlation-ID` forwarded by the Gateway. Falls back to a new `Guid` if missing (e.g. direct calls that bypass the Gateway). Stores the value in `HttpContext.Items` for in-process use and echoes it on the response. |

Because the Gateway injects the header before YARP proxies the request, every service in the call chain receives the **same** `X-Correlation-ID` for a given client request, making cross-service log correlation straightforward.

### Distributed Tracing (OpenTelemetry → Jaeger)

Every service is instrumented with the **OpenTelemetry .NET SDK** and exports traces via OTLP gRPC to **Jaeger**. No Jaeger-specific client libraries are used — the OTLP exporter is vendor-neutral.

#### Instrumentation

| Package | Instruments |
|---|---|
| `OpenTelemetry.Instrumentation.AspNetCore` | Incoming HTTP requests (server spans) |
| `OpenTelemetry.Instrumentation.Http` | Outgoing `HttpClient` calls (client spans) |
| `AddSource("MassTransit")` | MassTransit publish/consume/saga spans (built-in ActivitySource) |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | OTLP gRPC export to Jaeger |

The exporter endpoint and service name are configured entirely through environment variables — no URLs are hardcoded:

| Variable | Value (Kubernetes) | Value (Docker Compose) |
|---|---|---|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://jaeger:4317` | `http://jaeger:4317` |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` | `grpc` |
| `OTEL_SERVICE_NAME` | e.g. `core-service` | e.g. `core-service` |

The Jaeger UI is proxied by the Gateway via the `jaeger-ui-route` YARP route (`/jaeger/{**catch-all}`). Jaeger is started with `--query.base-path=/jaeger` so all its internal asset and API links are prefix-aware.

#### What you can trace

- Full HTTP request chain: Gateway → CoreService → UserService (via HttpClient)
- Saga flows: `OnboardingStartedEvent` → `ProvisionCorporateAccountCommand` → `AccountProvisionedEvent` → `SendWelcomeEmailCommand` → `FinalizeEnrollmentCommand` visible as linked MassTransit spans across services
- The existing `X-Correlation-ID` header travels alongside the W3C `traceparent` header — both are propagated independently

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
   Jaeger tracing UI is at `http://localhost:16686`.

3. Stop:
   ```bash
   docker compose down
   ```

### Run Tests

```bash
cd InternshipTracker
dotnet test
```

Tests include saga state-machine tests (`OnboardingSagaTests`) that use the MassTransit in-memory test harness to verify every state transition and compensation path, plus an end-to-end saga smoke test (`SagaSmokeTests`) that boots CoreService against a real PostgreSQL via **Testcontainers**, wires MassTransit in-memory with mock leaf consumers, and drives the full enrollment flow through the HTTP API.

## Kubernetes Deployment

### Cluster Setup

```bash
minikube start
minikube addons enable ingress
```

### Build and Deploy with Make (recommended)

A `Makefile` at the repository root automates image building and deployment. Each image is
tagged with `APP_VERSION-<git-sha>` (e.g. `v1.0.0-a1b2c3d`) and the Kustomize overlay in
`k8s/kustomization.yaml` is updated in-place before applying.

**Prerequisites:** `make`, `minikube`, `kubectl`, `kustomize`, Docker.

#### macOS / Linux

Run from the repository root:

```bash
# Override the version tag if needed:
make APP_VERSION=v2.0.0
```

```bash
# Build all images inside minikube's Docker daemon and deploy
make

# Or run steps separately:
make build-all    # eval $(minikube docker-env) + docker compose build
make deploy-all   # kustomize edit set image + kubectl apply -k k8s/
```

#### Windows (WSL required)

The `build-all` target runs `eval $(minikube docker-env)` to redirect Docker to
minikube's daemon. This works correctly only in a real bash environment. **WSL
(Windows Subsystem for Linux)** provides that environment and is the only
supported path on Windows.

> **Git Bash / PowerShell / CMD are not supported** for `make build-all` because
> Git Bash's minimal bash implementation does not reliably export the docker
> environment variables produced by `eval $(minikube docker-env)`, and
> PowerShell/CMD lack both `make` and `eval` entirely.

Install WSL (Ubuntu is fine), then run from the repository root inside the WSL
terminal:

```bash
# WSL – build all images inside minikube's Docker daemon and deploy
make

# Or run steps separately:
make build-all
make deploy-all

# Override the version tag if needed:
make APP_VERSION=v2.0.0
```

Make sure `minikube`, `kubectl`, `kustomize`, and `docker` are available inside
the WSL environment (install them the same way as on Linux).

### Deploy (manual / without Make)

```bash
# Apply namespace first (other resources depend on it)
kubectl apply -f k8s/namespace.yaml

# Apply all manifests via Kustomize
kubectl apply -k k8s/
```

### Verify

```bash
# All pods should be Running
kubectl get pods -n internship-tracker

# Check services
kubectl get svc -n internship-tracker

# Check ingress
kubectl get ingress -n internship-tracker
```

### Reach the Gateway

Start the minikube tunnel (keep it running in a separate terminal):

```bash
minikube tunnel
```

Add to your hosts file (`C:\Windows\System32\drivers\etc\hosts` on Windows, `/etc/hosts` on Linux/macOS):

```
127.0.0.1  internship-tracker.local
```

Verify all services are healthy:

```bash
curl http://internship-tracker.local/core/health
curl http://internship-tracker.local/users/health
curl http://internship-tracker.local/it-provision/health
curl http://internship-tracker.local/notification/health
```

### Verify Workflow (Saga)

Test the success path:

```bash
# Create a user
curl -X POST http://internship-tracker.local/users \
  -H "Content-Type: application/json" \
  -d '{"name": "John Doe", "email": "john.doe@example.com", "level": 1}'

# Create an internship
curl -X POST http://internship-tracker.local/internships \
  -H "Content-Type: application/json" \
  -d '{"title": "Software Engineering Intern", "capacity": 10, "minimumLevel": 1}'

# Apply for the internship
curl -X POST http://internship-tracker.local/applications \
  -H "Content-Type: application/json" \
  -d '{"userId": "<user-id>", "internshipId": "<internship-id>"}'

# Accept the application
curl -X POST http://internship-tracker.local/applications/{id}/accept

# Enroll (triggers the onboarding saga)
curl -X POST http://internship-tracker.local/applications/{id}/enroll

# Check final status (should be Enrolled=3 or EnrolledNotificationFault=5)
curl http://internship-tracker.local/applications/{id}
```

Test the compensation path: if ITProvisionService fails during onboarding, the saga reverts the application status from `Enrolling` back to `Accepted`.

### K8s Manifest Structure

```
k8s/
├── namespace.yaml              # internship-tracker namespace
├── configmap.yaml              # Shared config (RabbitMQ host, service URLs, OTEL endpoint)
├── secret.yaml                 # Database passwords, connection strings
├── rabbitmq.yaml               # Deployment + Service (AMQP + management UI)
├── jaeger.yaml                 # Deployment + Service (OTLP gRPC/HTTP + query UI)
├── postgres-core.yaml          # StatefulSet + PVC + headless Service
├── postgres-users.yaml         # StatefulSet + PVC + headless Service
├── postgres-it-provision.yaml  # StatefulSet + PVC + headless Service
├── postgres-notification.yaml  # StatefulSet + PVC + headless Service
├── core-service.yaml           # Deployment + Service
├── user-service.yaml           # Deployment + Service
├── it-provision-service.yaml   # Deployment + Service
├── notification-service.yaml   # Deployment + Service
├── gateway-service.yaml        # ConfigMap (YARP overrides) + Deployment + Service
└── ingress.yaml                # nginx Ingress → Gateway
```

### Teardown

```bash
# macOS / Linux – stop Docker redirection to minikube
eval $(minikube docker-env --unset)
```

```powershell
# Windows PowerShell – stop Docker redirection to minikube
& minikube docker-env --unset --shell powershell | Invoke-Expression
```

```bash
kubectl delete namespace internship-tracker
minikube stop
```

## Accessing Infrastructure Services

RabbitMQ and Jaeger are internal cluster services — they are **not** exposed via the Gateway or Ingress. Access them directly during development using `kubectl port-forward`:

```bash
# RabbitMQ management UI → http://localhost:15672  (guest / guest)
kubectl port-forward -n internship-tracker svc/rabbitmq 15672:15672

# Jaeger tracing UI → http://localhost:16686
kubectl port-forward -n internship-tracker svc/jaeger 16686:16686
```

In **Docker Compose** both ports are mapped directly on `localhost`:

| Service | URL |
|---|---|
| RabbitMQ Management | `http://localhost:15672` (guest / guest) |
| Jaeger UI | `http://localhost:16686` |

---

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

### Get an Application

```bash
curl http://localhost:8000/applications/{id}
```

### Enroll an Application (triggers onboarding saga)

```bash
curl -X POST http://localhost:8000/applications/{id}/enroll
```

This transitions the application from `Accepted` → `Enrolling` and kicks off the onboarding saga. The final `Enrolled` (3) or `EnrolledNotificationFault` (5) status is set asynchronously by the saga.

### Accept an Application

```bash
curl -X POST http://localhost:8000/applications/{id}/accept
```

### Reject an Application

```bash
curl -X POST http://localhost:8000/applications/{id}/reject
```

Rejection is allowed from `Pending` or `Accepted` status. Applications that are `Enrolling`, `Enrolled`, or `EnrolledNotificationFault` cannot be rejected.

