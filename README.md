# Inventory Management & Hold Service

This repository contains a set of .NET services and a small web UI that manage inventory, reservations (holds), and event publishing using an outbox pattern. The system uses MongoDB for persistence, Redis for caching, and RabbitMQ for event delivery.

Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker & Docker Compose (for backing services)
- Node.js / npm (optional — to run the web UI)

Quick start (local)

1) Start backing services (Mongo, Redis, RabbitMQ):

```bash
docker compose up -d mongo redis rabbitmq
```

2) Run the API(s) locally

- Inventory API (main read/management surface):

```bash
dotnet run --project src/InventoryApi --urls http://localhost:5001
```

- Inventory Hold API (holds + background workers):

```bash
# set MONGO_CONN for local Mongo if needed
MONGO_CONN="mongodb://admin:admin@localhost:27017/?authSource=admin" \
  dotnet run --project src/InventoryHold.WebApi --urls http://localhost:5002
```

3) Run unit tests

```bash
dotnet test src/InventoryHold.UnitTests
```

4) (Optional) Web UI for manual testing

```bash
cd src/InventoryHold.WebApp
npm install
npm run dev
```

Run full stack with Docker Compose

To build and run the API alongside backing services:

```bash
docker compose up --build
```

Environment variables & configuration notes
- `MONGO_CONN`: Mongo connection string used by the hold service during local runs.
- `REDIS_CONN` (or `Redis:ConnectionString` in config): Redis endpoint for caching.
- Other service-specific settings are available in each project's `appsettings.json` and can be overridden with environment variables.

Caching behavior (developer notes)
- In development, if Redis is not provided the project includes an `InMemoryCache` implementation to preserve behavior for local testing.
- In production the system expects a real Redis instance; the application may fail-fast if Redis is required but not configured.

Troubleshooting
- If a service fails to start, inspect service logs:

```bash
docker compose logs --no-log-prefix --tail=200 <service>
```

- Common quick checks:
  - `docker compose ps` — confirm backing services are up
  - `curl -sS http://localhost:5001/api/inventory | jq .` — quick inventory smoke-test

Contributing & AI usage
- This repo includes `AI-USAGE.md` with a short audit explaining how AI tools were used while developing tests and small patches. Keep AI use documented: sanitize prompts and never commit secrets.

Appendix — important paths
- API projects: `src/InventoryApi`, `src/InventoryHold.WebApi`
- Domain: `src/InventoryHold.Domain`
- Infrastructure / hosted services: `src/InventoryHold.Infrastructure`
- Tests: `src/InventoryHold.UnitTests`
- Web UI: `src/InventoryHold.WebApp`

Last updated: 2026-06-07 — edited for clarity and developer ergonomics.
