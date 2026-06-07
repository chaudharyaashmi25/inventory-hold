# Inventory Management & Hold Service

This repository contains a .NET 10 Web API and background worker that manages inventory reservations, holds, and event publishing (outbox pattern). It includes MongoDB for persistence, Redis for caching, and RabbitMQ for event dissemination.

## Quick Start (Local Run)

To run the application, make sure you have the [.NET 10 SDK](https://dotnet.microsoft.com/download) installed.

### 1. Running Backing Services (MongoDB, Redis, RabbitMQ)
Use Docker Compose to run the required infrastructure services:
```bash
docker-compose up -d mongo redis rabbitmq
```

### 2. Run the Web API
To run the main Web API locally:
```bash
dotnet run --project src/InventoryHold.WebApi/InventoryHold.WebApi.csproj
```
The API will start up on `http://localhost:5001`.

### 3. Run Unit Tests
To execute NUnit/xUnit tests:
```bash
dotnet test src/InventoryHold.UnitTests/InventoryHold.UnitTests.csproj
```

---

## Run with Docker Compose (Full Stack)

You can launch the entire stack (API + backing services) using Docker Compose:
```bash
docker-compose up --build
```
This builds the `inventoryhold-webapi` container and coordinates startup:
- Backing services (`mongo`, `redis`, `rabbitmq`) are started first.
- The API container waits until all backing services are fully healthy before launching.

---

## Cache configuration (InventoryHold)

The service relies on Redis for high-performance read paths and precise cache invalidation.

- **Configuring Redis**: Specify the connection string in the `Redis:ConnectionString` configuration block or by setting the `REDIS_CONN` environment variable (e.g. `localhost:6379`).
- **Cache Enforcement Policy**:
  - In **Development** mode (`ASPNETCORE_ENVIRONMENT=Development`): If Redis is not configured, the application falls back to a process-local `InMemoryCache` and logs a loud warning. This allows developers to run tests and mock behaviors without local Redis overhead.
  - In **Production / Staging** environments: Redis is strictly required. The application will **fail-fast and throw an exception** on startup if Redis configuration is missing, preventing cache state inconsistencies.
- **Dynamic Caching**: Caching for the inventory list is parameter-aware to prevent incorrect cache-hits on filtered listings:
  - Cache Key: `inventory:list:sku={sku}:loc={location}:avail={availableOnly}`
  - Hold and release operations register cache key dependencies against target SKUs in Redis sets to facilitate targeted invalidations when quantities shift.
