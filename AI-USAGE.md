AI Usage & Audit

Purpose

This document explains how AI coding tools were used while implementing and testing the Inventory Hold project, describes decisions taken based on AI suggestions, and documents how outputs were verified. It's written to be human-readable and to show the reasoning/guardrails used during AI-assisted development.

1) AI Strategy

- Tools used: a code-capable LLM (used interactively to assist with design, refactor suggestions, unit test generation and small code snippets) and local developer tools (IDE, `dotnet` CLI, `docker compose` for Mongo). AI was used as a focused assistant, not an autopilot.
- Scope & context fed to the AI: For each request I supplied minimal, targeted context rather than whole repository dumps. Typical context included the relevant files and signatures (for example, `HoldService.cs`, `IHoldRepository.cs`, `Hold.cs`, `HoldDto.cs`) plus a short description of the intended behavior (e.g., "Create a unit test for expiry reconciliation that asserts expired holds are released and outbox entries created").
- Prompt structure and conventions I used:
  - Start with a short intent statement (1 sentence).
  - Provide only the code that matters (function/class snippet + a list of interfaces it touches).
  - Ask for small, reviewable outputs (1 patch or 1 test file) and request explanations for non-trivial suggestions.
  - Ask for alternative approaches when the domain required trade-offs (e.g., eventual consistency vs strong consistency for holds).
- Context management: Whenever possible I copy-pasted the relevant method signatures and small helper types into the prompt. If the change would touch multiple files, I included a short file map (file names + responsibilities) rather than full files.

2) Human Audit (Examples of accepted and rejected suggestions)

Accepted suggestions (examples):
- Unit test scaffolding for `HoldService`: AI produced a clear sample unit test that followed the existing test style in `InventoryHold.UnitTests/HoldServiceTests.cs`. I accepted the structure and adapted the mock setup to our `IInventoryRepository`/`IHoldRepository` interfaces. Why accepted: the test was precise, aligned with project conventions, and easy to reason about; I ran it locally and iterated quickly.
- Outbox dispatch retry pattern: AI suggested a small backoff retry wrapper for transient exceptions in the outbox dispatcher. I accepted the concept and implemented a conservative retry with logging in `OutboxDispatcher.cs`, adjusting timeouts to match the system's existing hosted service cadence. Why accepted: improved resilience and aligned with our hosted service design.

Rejected suggestions (examples) and rationale:
- Storing connection strings in source code: AI suggested hardcoding a Mongo connection string for quick testing. Rejected for security and environment parity — we use environment variables and `MONGO_CONN` (see README and run scripts) instead.
- Naive in-memory cache replacement: AI suggested replacing `RedisCache.cs` with a simple static dictionary to simplify local dev. Rejected because it introduced thread-safety risks and a divergent behavior model versus production Redis. Instead, I accepted a small `InMemoryCache` implementation already in `Caching/` for local scenarios which mirrors Redis semantics more closely.
- Large, unrequested refactors: AI sometimes proposed bigger structural changes (e.g., collapsing `Repositories/` into a single file). I rejected large refactors unless they clearly reduced complexity and I could test them incrementally.

Where judgement improved on AI output:
- AI-generated code often required minor changes for null-safety, cancellation token propagation, and adherence to established logging conventions. For example, an AI suggestion for `HoldService.CreateHold` didn't surface cancellation token usage — I added it before accepting.

3) Verification: how AI helped generate tests and how I validated correctness

- Test generation: I asked the AI to propose focused unit tests for `HoldService` covering core scenarios (create hold, release hold, expiration reconciliation, failure paths). The AI produced starter `xUnit` test code which I used as a scaffold.
- Validation steps taken after accepting AI suggestions:
  - Static review: read the generated code and compared it to interfaces in `Repositories/` and models in `Models/` to ensure correct usage.
  - Run unit tests locally: I executed:

```bash
cd src/InventoryHold.UnitTests
# AI Usage & Audit

Purpose

This document explains how AI coding tools were used while implementing and testing the Inventory Hold project. It documents the AI strategy, examples of accepted and rejected suggestions, verification steps, and practical guardrails. The goal is to provide a clear, human-readable audit trail of AI-assisted work.

## 1) AI Strategy

- **Tools used:** interactive, code-capable LLM (for design suggestions, small patches, and test scaffolding) plus local developer tools: the IDE, `dotnet` CLI, and `docker compose` for Mongo.
- **Scope & context shared with the AI:** I preferred minimal, targeted context over full-repo dumps. Typical context included the small set of relevant types and method signatures (for example, `HoldService.cs`, `IHoldRepository.cs`, `Hold.cs`) and a one-line intent.
- **Prompt conventions I followed:**
  - One-line intent at the top.
  - Paste only the smallest meaningful code snippets (method signatures, small classes, or interface contracts).
  - Ask for a single, small deliverable (one patch, one test file, or a single helper function) and request a short explanation for non-trivial choices.
  - Ask for alternatives when trade-offs existed (e.g., eventual vs strong consistency for holds).
- **Context management:** for multi-file changes I provided a short file map (file name → responsibility) rather than full files, and I always redacted secrets before sending anything.

## 2) Human audit: accepted vs rejected suggestions

Accepted examples
- **Unit test scaffolding for `HoldService`** — the AI produced an xUnit test structure that matched the project's style. I adapted the mocks to `IInventoryRepository`/`IHoldRepository` and iterated until green. Why: small, precise, and testable.
- **Outbox retry pattern** — AI suggested an exponential-backoff retry for the outbox dispatcher. I implemented a conservative version with logging in `OutboxDispatcher.cs` and tuned timing to the hosted service cadence. Why: improved resilience without changing semantics.

Rejected examples
- **Hardcoded connection strings** — AI sometimes suggested inlined `mongodb://...` strings for quick proofs. Rejected for security and parity; we use environment variables (see `MONGO_CONN`).
- **Naive static dictionary as Redis replacement** — rejected because it changes concurrency and behavior compared to Redis. We retained `InMemoryCache` in `Caching/` that mirrors Redis semantics for local dev.
- **Large, unsolicited refactors** — AI occasionally suggested big structural rewrites (e.g., collapsing `Repositories/`). I only accept large refactors when they clearly reduce complexity and can be tested incrementally.

Where human judgment improved AI output
- The AI often missed small but important details: null-safety, cancellation token propagation, and consistent logging. I reviewed and corrected these before merging suggestions (for example, adding cancellation tokens to `HoldService.CreateHold`).

## 3) Verification: how AI helped generate tests and how I validated them

- **Test generation workflow:** I used the AI to generate focused unit test scaffolds for `HoldService` (create, release, expire, and failure cases). The outputs were treated as starting points.
- **Validation steps:**
  - Static review: compare generated code to local interfaces in `Repositories/` and models in `Models/`.
  - Run unit tests locally:

```bash
cd src/InventoryHold.UnitTests
dotnet test
```

  - Run integration / smoke checks for API or hosted-service changes:

```bash
# start local mongo if needed
docker compose up -d mongo

# run the web API with the configured Mongo connection
MONGO_CONN="mongodb://admin:admin@localhost:27017/?authSource=admin" dotnet run --project src/InventoryHold.WebApi --urls http://localhost:5002

# quick read endpoint smoke test
curl -sS http://localhost:5001/api/inventory | jq .
```

  - Inspect logs for expected outbox dispatches and reconciler runs.
  - Iterate: when tests failed, inspect traces, correct code or assertions, and re-run until passing.

## 4) Safety & security guardrails

- Never accept suggestions that embed secrets in source. Use environment variables and platform secrets stores.
- Prefer small, incremental patches and require human review before merging.
- Keep test and local implementations behaviorally similar to production (e.g., Redis ↔ `InMemoryCache`).

## 5) Practical prompts (sanitized examples)

- Intent: Add a unit test for `HoldService` that asserts expired holds are released and an outbox entry is created. Context: provide `HoldService.CreateHold(...)` signature and `IOutboxRepository` interface. Deliverable: one xUnit test file following project style.
- Intent: Propose a retry wrapper for transient exceptions in `OutboxDispatcher`. Requirements: exponential backoff, 3 attempts max, log each failure.

If you want the exact prompts and raw AI outputs for auditing, I can add them to an `AI-EXAMPLES/` folder after removing any sensitive data.

## 6) Recommendations / next steps

- Continue to use AI for scaffolding, tests, and small refactors, but keep human review mandatory.
- Add automated integration tests for the outbox → message bus path to reduce manual validation.
- Add an `AI-EXAMPLES/` folder containing sanitized prompts and chosen outputs to make future audits faster.

## Appendix: pointers (quick links)

- Core service: `src/InventoryHold.Domain/Services/HoldService.cs`
- Tests: `src/InventoryHold.UnitTests/HoldServiceTests.cs`
- Web API: `src/InventoryHold.WebApi/Program.cs`
- Caching implementations: `src/InventoryHold.Infrastructure/Caching/`

---
Last updated: 2026-06-07 — edited for clarity and auditability.