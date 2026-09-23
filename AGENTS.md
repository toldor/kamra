# AGENTS.md – Smart Pantry & Recipe Manager

This file is for AI coding agents (Claude Code, Gemini CLI, Cline, Copilot Agent, etc.).
Read it before every task. If this file and the code contradict each other, **stop and ask**.

## 1. Project overview

A web-based pantry and recipe management app with AI-assisted inventory tracking (BSc thesis, University of Szeged).

- Add inventory items via a form **or** via natural-language quick entry (the LLM extracts name, quantity, expiry date).
- Expiry estimation based on product category when the date is missing.
- Recipe suggestions from current stock, prioritising items that expire soon; AI-driven serving adjustment and ingredient substitution.
- Automatic stock deduction after cooking; low-stock items are added to the shopping list.
- Chat assistant that queries data through **MCP** tools.

## 2. Tech stack

| Layer | Technology |
|---|---|
| Backend | C# / .NET (LTS), ASP.NET Core Web API, Clean Architecture |
| Database | PostgreSQL, EF Core with migrations |
| AI | ChatCompletion API (text parsing), separate MCP server (tool calls) |
| Frontend | React + TypeScript (Vite), responsive, mobile-friendly |
| Tests | xUnit, FluentAssertions, Testcontainers (Postgres), Playwright (e2e), Vitest (frontend) |
| Infra | Docker Compose, GitHub Actions |

Add a new dependency (NuGet or npm package) **only after approval**, and state whether it needs an ADR.

## 3. Repository structure

```
src/backend/KamraApp.Domain/          # entities, value objects, business rules – NO external dependencies
src/backend/KamraApp.Application/     # use cases, interfaces (ports), DTOs, validation
src/backend/KamraApp.Infrastructure/  # EF Core, migrations, LLM client, external adapters
src/backend/KamraApp.Api/             # REST /api/v1, error model, /health, logging
src/backend/KamraApp.McpServer/       # MCP tools – calls the Application layer
src/frontend/                         # React app
tests/                                # *.Tests projects + e2e/
docs/                                 # Docs-as-Code (00_index.md is the index)
.github/workflows/                    # CI
```

### Dependency rules (non-negotiable)

- `Domain` → nothing.
- `Application` → `Domain` only.
- `Infrastructure` → `Application`, `Domain`.
- `Api` and `McpServer` → `Application` (plus `Infrastructure` for DI registration).
- No business logic in controllers, MCP tools or React components.
- The API and the MCP server call the **same** Application use cases – no duplicated logic.

## 4. Commands

The exact commands live in the README; if they change, update both places.

```bash
docker compose up -d db                 # database
dotnet build                            # backend build
dotnet test                             # all .NET tests
dotnet format --verify-no-changes       # formatting check
dotnet ef database update -p src/backend/KamraApp.Infrastructure -s src/backend/KamraApp.Api
cd src/frontend && npm ci && npm run lint && npm test && npm run build
npx playwright test                     # e2e (requires running stack)
docker compose up --build               # full system
```

A task is done only when build, format, lint and **all** tests are green.

## 5. Coding conventions

### Backend
- `nullable` enabled, warnings as errors.
- Async I/O everywhere, passing `CancellationToken` through.
- Validation in the Application layer (FluentValidation or equivalent).
- Unified error model: RFC 7807 `ProblemDetails` with a stable `code` field (e.g. `PANTRY_ITEM_NOT_FOUND`). Never send stack traces to the client.
- Error categories: validation (400), unauthorized (401), forbidden (403), not found (404), conflict (409), rate limit (429), internal (500).
- Structured logging (Serilog, JSON) with `correlationId`. **Never log PII, prompts or API keys.**
- Configuration: `appsettings.json` + environment overrides, validated at startup (fail fast).
- Schema changes only via EF migrations with descriptive names (`AddExpiryEstimateToPantryItem`).

### AI / MCP
- Every LLM call sits behind an interface defined in the Application layer (e.g. `IIngredientParser`) so it can be mocked.
- **Always** validate LLM output against a schema (JSON schema or DTO validation) before it reaches the database; on invalid output return a clear error – do not guess.
- MCP tools are read-only or write only through narrow, validated use cases. Never expose raw SQL or arbitrary queries.
- Every MCP tool has a description, an input schema and a test. The list lives in `docs/03_design/mcp_tools.md`.
- Keep prompt texts in dedicated files or constants, versioned, not scattered across the code.

### Frontend
- Function components, TypeScript `strict`.
- API calls live under `src/frontend/src/api/`; no direct `fetch` from components.
- Every data-loading view has loading, empty, error and success states.
- User-facing messages are in **Hungarian**, clear, and tell the user what to do next. No HTTP codes or technical text.
- Basic a11y: labelled inputs, keyboard-operable controls, sufficient contrast.

## 6. Testing rules

- New logic → new tests. For a bug fix, write a reproducing regression test **first**, then fix.
- Unit: Domain and Application (expiry estimation, recipe matching, stock deduction, shopping list).
- Integration: API + real Postgres in Testcontainers, not an in-memory DB.
- E2E: main flows (add item → suggestion → cook → shopping list; chat query).
- **Always mock the LLM in tests**; real API calls must never run in CI. Live smoke tests only, marked `[Trait("Category","LiveAI")]`.
- Include negative cases for every module: invalid input, non-existent ID, empty pantry, malformed LLM response, missing permission.
- Never delete, skip or weaken a test just to make it pass. If you believe the test is wrong, explain why and ask for approval.
- Target: 30+ automated tests (≥18 unit, ≥6 integration, ≥6 e2e/contract), of which ≥5 negative.

## 7. Security

**Security baseline (non-negotiable):** Every endpoint that handles user data must be protected with actual authentication (not just UI-level guards). All inputs must be validated before reaching the domain or database. Error responses must never leak stack traces, internal paths, or implementation details to the client.

- **Never commit secrets**: API keys, passwords, connection strings with real values. When adding an env variable, update `.env.example` and the README.
- Do not read `.env` files into your context and never commit them.
- Protect secured endpoints on the backend, not only in the UI.
- Parameterised queries (EF); no string-concatenated SQL.
- User text goes into the LLM: assume prompt injection. LLM output never grants permissions and never triggers write operations without validation.

## 8. Git workflow

- Branches: `main` (stable, PR-only), `feature/<short-name>`, `fix/<short-name>`, `docs/<short-name>`.
- Conventional Commits: `feat:`, `fix:`, `test:`, `docs:`, `refactor:`, `chore:`, `style:` – max 72 characters, imperative mood.
- Small, focused commits: prompt → review → test → commit. No giant end-of-day commit.
- Never push directly to `main`; no force pushes.
- Before committing: build, lint and tests green; no debug output or secrets in the diff.

## 9. Documentation – updated together with code

See [docs/00_index.md](docs/00_index.md) for the full documentation index.

| If you change… | …update this |
|---|---|
| Endpoint, request/response | [docs/03_design/api.md](docs/03_design/api.md) (+ OpenAPI) |
| Entity, schema, migration | [docs/03_design/data_model.md](docs/03_design/data_model.md) |
| Error code, error handling | [docs/03_design/error_handling.md](docs/03_design/error_handling.md) |
| MCP tool | [docs/03_design/mcp_tools.md](docs/03_design/mcp_tools.md) |
| Architectural decision | new ADR: [docs/02_architecture/adr/0001-template.md](docs/02_architecture/adr/0001-template.md) (copy & increment number) |
| Env variable, startup | `README.md`, `.env.example` |
| Tests | [docs/04_quality/test_report.md](docs/04_quality/test_report.md) |
| Logs, metrics, health | [docs/05_security_ops/observability.md](docs/05_security_ops/observability.md) |
| Capability status | [docs/01_product/capability_map.md](docs/01_product/capability_map.md) |

- Do not document anything that is not implemented. Missing items go under "Known limitations".
- **No TODO rule:** No section in the submitted documentation may be left empty or marked with `TODO`. Anything not yet addressed belongs under a "Known limitations" heading — never a placeholder.
- **ADR rule:** Every significant engineering or architectural decision (e.g. database choice, auth strategy, framework selection) must be recorded as an ADR under `docs/02_architecture/adr/`. Each ADR must include: the alternatives that were considered, the trade-offs and consequences of the chosen option, and how the decision was or will be verified. Copy `0001-template.md` and increment the number.
- Scope is defined in [docs/01_product/scope_contract.md](docs/01_product/scope_contract.md). Anything outside it is **not built** without a request.

## 10. AI transparency (mandatory deliverable)

At the end of every meaningful session, propose an entry for [docs/07_ai/prompt_log.md](docs/07_ai/prompt_log.md) (ID e.g. `P-07`):
date, goal, tool, prompt summary, AI proposal summary, affected files. The **"What I changed / my decision"** field is filled in by the developer – do not invent it.

When you make a claim about security, performance, correctness or licensing (e.g. "this validation is sufficient", "this query is fast"), flag it and propose a verification entry for [docs/07_ai/verification_log.md](docs/07_ai/verification_log.md): claim, risk, verification method, result, conclusion.

If you were wrong and it turned out, log that too. This is not a failure but expected engineering practice.

## 11. When to ask and what never to do

Ask before you:
- add a dependency or swap a technology;
- change layer boundaries or the public API contract;
- write a migration that deletes or transforms data;
- start a feature outside the scope;
- modify a test to make it pass.

Never:
- commit secrets or `.env`;
- disable the linter or warnings, or use `#pragma warning disable` without justification;
- call a real LLM API from tests;
- invent non-existent packages, APIs or methods. If unsure, say so.

## 12. End-of-task response format

Briefly: what you changed (per file), which commands you ran and their results, what you did **not** do or what remains open, and which docs or prompt log entries still need updating.
