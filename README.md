# Shopiy E-Commerce & Admin Platform
**Senior Developer Candidate Assessment Project Documentation**

---

## 1. Project Overview

**Shopiy** is a high-performance, containerized e-commerce order management system and administrative dashboard. Built with a focus on data integrity, high concurrency, and clean code principles, the platform provides:
- A customer-facing storefront for product catalog browsing, sorting, and secure order placement.
- An admin management dashboard with global order filtering, real-time metrics, and inventory lifecycle control.
- Enterprise-grade middleware protections including **distributed request idempotency** to completely eliminate duplicate order submissions.

---

## 2. Tech Stack Architecture

The system is split into decoupled layers, utilizing state-of-the-art frameworks and services:

*   **Backend Framework:** ASP.NET Core 10.0 (Web API)
*   **Architectural Pattern:** Clean Architecture with CQRS (Command Query Responsibility Segregation) via **MediatR**
*   **Database persistence:** PostgreSQL 17 (leveraging JSONB fields for dynamic product specifications metadata)
*   **Caching & Concurrency Control:** Redis 7 (via `StackExchange.Redis` for lock state management and caching)
*   **Logging & Observability:** **Serilog** with structured JSON formatting pushing to **Seq** for real-time telemetry
*   **Frontend Dashboard:** React + Vite + TypeScript, styled using premium responsive CSS layouts
*   **Testing Suite:** xUnit, Moq, and FluentAssertions for automated unit testing

---

## 3. Quick Start & Installation

Ensure you have [Docker](https://www.docker.com/) and [Docker Compose](https://docs.docker.com/compose/) installed on your machine.

### Step 1: Clone and Set Up Environment Variables
Create a `.env` file in the root directory:
```bash
cp .env.example .env
```
Ensure the `.env` file contains your connection credentials and JWT secrets:
```env
POSTGRES_DB=shopiy
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
JWT_SECRET=YourSuperSecretUnbreakableKeyOf32Bytes!
ASPNETCORE_ENVIRONMENT=Development
SEQ_ADMIN_PASSWORD=SeqPassword123!
```

### Step 2: Spin Up the Infrastructure & Application
Run Docker Compose to pull, build, and start the primary database, cache provider, logging server, and API backend:
```bash
docker-compose --env-file .env up -d --build
```
This command automatically boots up:
- **PostgreSQL Container (`shopiy-postgres`)** on port `5432`
- **Redis Container (`shopiy-redis`)** on port `6379`
- **Seq Logging Web UI (`shopiy-seq`)** on port `5341`
- **Shopiy Web API Core (`shopiy-api`)** on port `5000`

Access the Swagger API Documentation directly at: `http://localhost:5000/swagger`

### Step 3: Run Backend Unit Tests
To execute the automated backend unit tests (including the idempotency filter suite, handler checks, and validations), run:
```bash
dotnet test tests/Shopiy.Application.UnitTests/Shopiy.Application.UnitTests.csproj
```

---

## 4. System Design & Architectural Trade-offs

Building software for a 5-day assessment requires balance between enterprise-grade robustness and delivery speed. Below is a summary of key architectural decisions and their trade-offs:

### A. Single-Node PostgreSQL vs. Multi-Node Replication
*   **Decision:** Configured a single-node PostgreSQL instance with Docker volume persistence.
*   **Trade-off:** In a scale-out production environment, database reads would be distributed across read replicas using replica database contexts (like `ReadOnlyApplicationDbContext`). For the scope of this assessment, establishing a live primary-replica replication group adds significant container orchestrations (e.g. pgpool, patroni) without proving additional coding proficiency. Instead, read-replica segregation is simulated at the code layer via DI, while database isolation is preserved cleanly.

### B. Redis-Based Idempotency Locks (Duplicate Order Prevention)
*   **Decision:** Implemented a custom `[IdempotentRequest]` Action Filter backed by Redis rather than relying solely on database unique constraints.
*   **Rationale:** Double-billing is a high-cost failure. While a database unique constraint on `(user_id, total, items_checksum)` might stop duplicates at the persistence layer, it does so after significant work has already occurred downstream. Checking Redis via an atomic `StringSetAsync(..., When.NotExists)` at the API entry point short-circuits the pipeline in under 5ms, avoiding database roundtrips, EF Core change tracking overhead, and downstream payment provider double-invocations.

### C. EF Core In-Memory Provider vs. Testcontainers
*   **Decision:** Used `Microsoft.EntityFrameworkCore.InMemory` for business logic unit tests instead of spinning up physical Postgres containers via `Testcontainers`.
*   **Trade-off:** Testcontainers provides high-fidelity database testing (supporting real Postgres SQL features like JSONB querying and triggers). However, it requires a running Docker socket in the runner, adding execution boot overhead (5–10s startup per run). EF Core In-Memory runs unit tests instantly (<300ms total suite execution), driving high developer productivity and fast CI/CD loops. Real PostgreSQL features are instead verified via target integration tests in dockerized staging environments.

### D. Separation of Pipeline Lifecycle Lock vs. Caching
*   **Decision:** Designed the `[IdempotentRequest]` filter to act as a lifecycle manager:
    - Sets key status to `processing` during action execution.
    - Evicts key immediately if downstream processing throws an exception or returns an HTTP Error (`>= 400`), avoiding lockout.
    - Caches successful responses (`200-299`) and returns them directly for subsequent duplicate client retries to handle dropped connection scenarios.

---

## 5. Order Idempotency & Concurrency Design

Our strategy for preventing duplicate submissions (double billing, duplicate inventory decrement) employs a multi-layered defense-in-depth model across the API, memory caches, database, and client interface:

### A. Backend Idempotency Key (`X-Idempotency-Key`)
The API exposes a custom `[IdempotentRequest]` action filter that enforces client-supplied idempotency.
- The filter intercepts incoming requests on the `CreateOrder` endpoint and inspects the `X-Idempotency-Key` header.
- The header value is validated against a strict UUID structure. Requests with missing or invalid keys are immediately rejected with a `400 Bad Request` to preserve database and network resources.

### B. Redis Lock and Lifecycle Management
To guarantee concurrency protection:
- The filter attempts to set a lock in Redis (`idempotency:{key}`) with the state `processing` using an atomic `StringSetAsync(..., When.NotExists)` with a 5-minute TTL.
- If the key exists in the `processing` state, a `409 Conflict` is returned.
- If it exists in the `completed` state, the cached response payload (HTTP status code and response JSON body) is returned immediately to the client. This handles edge-cases where the first request succeeds but the network drops before the client receives the response.

### C. Database Integrity & Unique Constraints
While the Redis cache represents the fast-path gateway check, persistence constraints form the ultimate source of truth:
- The database schema enforces a unique constraint on target entities to prevent double records.
- If Redis fails, is rebooted, or encounters a rare split-brain/network-partition state, PostgreSQL unique constraints act as the absolute final safeguard, raising a duplicate key exception which is handled gracefully.

### D. Transaction Handling & Fail-Safe Eviction
- Downstream business logic (order creation, ledger updates, inventory checks) is executed within a database ACID transaction.
- If any operation fails or throws an exception, the database transaction is completely rolled back to maintain consistency.
- Concurrently, the `[IdempotentRequest]` filter catches the exception (or detects a `>= 400` status code response), and instantly calls `KeyDeleteAsync` in Redis. This evicts the lock key immediately, making the request **retry-safe** so the customer can correct parameters and re-submit without a 5-minute lockout.

### E. Structured Telemetry & Duplicate Logging
- The filter captures duplicate request attempts and logs them using structured **Serilog** templates:
  `_logger.LogWarning("Duplicate submission detected. Intercepted request with idempotency key: {IdempotencyKey}", parsedGuid);`
- These logs automatically capture the key, request path, status code, IP address, and other ambient trace metrics, providing immediate visibility and audit trails for security operations (detecting potential replay attacks).

### F. Frontend Button Disabling (Supporting UI Measure Only)
- During order checkout submission, the React application disables the submit button and displays a loading spinner to prevent accidental double-clicks.
- **Critical Principle:** This client-side lockout is purely a supporting UX optimization. It is never relied upon as a security or concurrency guarantee, as the backend remains the sole, authoritative source of truth.

---

## 6. Future Feature & Production Roadmap

To scale this codebase to an enterprise-level storefront processing millions of daily transactions, the following architectural additions would be implemented next:

1.  **Transactional Outbox Pattern:**
    Currently, ordering publishes side-effects (e.g. sending payment request, decrementing warehouse stock, dispatching confirmation email) directly in the transaction. To prevent system failures, dynamic events should be saved in an `Outbox` table within the same ACID database transaction, then parsed and dispatched asynchronously using a worker (e.g., Quartz.NET or Hangfire) to a message broker (RabbitMQ/Kafka).
2.  **Elasticsearch Catalog Service:**
    Relational databases are inefficient for full-text search, facets, and type-ahead matching. We would synchronize catalog changes to an Elasticsearch index using a Change Data Capture (CDC) tool like Debezium, allowing search latency to drop to sub-50ms under heavy concurrent read volumes.
3.  **BFF (Backend-For-Frontend) API Gateway:**
    Introduce YARP (Yet Another Reverse Proxy) as a secure gateway to manage routing, global rate-limiting, SSL termination, and CORS headers centrally, isolating micro-services from public networks.
4.  **Direct-to-Cloud Product Image Ingestion:**
    To handle product media assets at scale without bloating the core application servers or database storage:
    - Implement a secure pre-signed URL generation flow. The backend API authenticates the request and generates short-lived upload signatures.
    - The client client-side uploads high-resolution assets directly to cloud object storage (e.g., AWS S3 or Azure Blob Storage), avoiding transit server memory overhead.
    - Run an automated media pipeline (e.g., serverless function converting to WebP) and cache optimized assets globally via a CDN (e.g., AWS CloudFront or Cloudflare) to minimize latency.
5.  **Asynchronous Real-Time Notifications & Status Synchronization:**
    To establish a highly reactive storefront and dashboard system:
    - Integrate ASP.NET Core SignalR hubs (WebSockets with Server-Sent Events fallback) to stream real-time order state progression (e.g., `Processing` to `Shipped`) directly to client dashboards.
    - Decouple external communication systems. Avoid calling email (SendGrid) or SMS (Twilio) APIs synchronously during order state transitions. Instead, dispatch notifications asynchronously via the Outbox pattern to eliminate API blocking and guarantee delivery.
6.  **Containerized Integration Testing via Testcontainers:**
    While unit tests leverage the lightweight EF Core In-Memory provider to maintain rapid development cycles:
    - Implement a secondary integration test suite utilizing **Testcontainers for .NET**.
    - The pipeline dynamically spins up Docker-backed PostgreSQL 17 and Redis 7 instances during CI runs.
    - This validates database-specific dialect logic (like JSONB index lookups and row locks) and real Redis connection and eviction behaviors under actual I/O constraints before code is promoted.
7. **Add payment gateway integration**
    While the current storefront manages stock and order records internally, a full-scale e-commerce platform requires robust payment processing. To support this evolution, we would integrate **Stripe** (or a local payment gateway provider) via the official SDK. This integration would focus on:
    - Secure tokenization of cardholder data (leveraging Stripe Elements to keep sensitive data off our PCI scope).
    - Orchestration of the payment lifecycle: authorization during checkout and capture upon shipment (or full immediate capture, depending on business rules).
    - Asynchronous handling of payment webhooks (via Outbox) to reconcile and update order statuses, protecting the core transaction logic from external payment gateway downtimes.