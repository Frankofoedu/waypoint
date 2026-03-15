# Tracewire — Product Requirements Document (v2.3)

> See the original literate source at the repo root: `# Copilot-Friendly PRD — Tracewire v1.litcoffee`

This document summarizes the product requirements for Tracewire — an AI agent observability and control platform.

## Vision

Tracewire provides developers with a complete toolkit to **observe**, **replay**, and **control** AI agent executions. It captures every step an agent takes as a branching DAG (Directed Acyclic Graph), enables human-in-the-loop intervention, and flags side-effects when replaying from arbitrary points.

## Core Capabilities

### 1. Trace & Event Capture
- Every agent run is a **Trace** containing ordered **Events**
- Events form a DAG via `parent_id` references
- Events are typed: `Prompt`, `ToolCall`, `ModelResponse`, `MemoryWrite`, `Error`, `Retry`
- OTEL-aligned wire format: `trace_id`, `span_id`, `parent_span_id`

### 2. Branching DAG Visualization
- React Flow-powered interactive DAG viewer
- Color-coded nodes by event type
- Branch visualization with collapsible branches
- Click-to-inspect event detail (payload, state, latency, cost)

### 3. Human-in-the-Loop (HITL)
- SDK can pause execution at any event: `pause_for_human(timeout, fallback)`
- Frontend shows approval panel: Approve / Reject / Escalate
- Agent resumes after human decision with configurable timeout + fallback behavior

### 4. Replay with Side-Effect Warnings
- "Replay from here" on any event node
- System scans ancestor events for `side_effects` (e.g., emails sent, DB writes)
- Warning modal before replay, respecting workspace policy: `Warn`, `Block`, `RequireApproval`
- Creates a new branch on replay

### 5. Auto-Instrumentation SDKs
- **Python SDK**: httpx async client, Pydantic models, framework adapters
- **TypeScript SDK**: native fetch client, typed models, framework adapters
- Framework-agnostic adapter pattern: LangChain, AutoGen, CrewAI
- Local event buffer with background flush

## Architecture

| Layer | Technology |
|-------|-----------|
| API Server | .NET 9 / ASP.NET Minimal API |
| Database | PostgreSQL 16 with EF Core |
| Frontend | React 19 + Vite + React Flow |
| Python SDK | httpx + Pydantic v2 |
| TypeScript SDK | Native fetch + TypeScript |
| Deployment | Docker Compose |

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/v1/health` | Health check |
| POST | `/v1/traces` | Create trace |
| GET | `/v1/traces` | List traces (cursor pagination) |
| GET | `/v1/traces/{id}` | Get trace with events |
| POST | `/v1/events` | Create event |
| POST | `/v1/events/{id}/replay` | Replay from event |
| POST | `/v1/events/{id}/pause` | Pause for HITL |
| POST | `/v1/events/{id}/resume` | Resume after HITL |

## Multi-Tenancy

- Organizations → Workspaces → API Keys
- API key auth with SHA256 hashing, scoped permissions
- Workspace-level replay policy configuration

## Scope Boundaries

**Included in MVP:**
- Trace/Event CRUD, DAG storage + visualization
- HITL pause/resume, replay with side-effect warnings
- Auto-instrumentation adapters (LangChain/AutoGen/CrewAI)
- API key auth, multi-tenant schema, Docker Compose

**Excluded (Phase 2+):**
- WebSocket/SSE real-time updates
- Cloud deployment / CI/CD
- Cost analytics dashboards
- Slack/webhook alert integrations
- AI-assisted fix suggestions
- Versioned prompt management
