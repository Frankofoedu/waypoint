from __future__ import annotations

import sys
from datetime import datetime
from typing import Any

if sys.version_info >= (3, 11):
    from enum import StrEnum
else:
    from enum import Enum

    class StrEnum(str, Enum):
        pass
from uuid import UUID

from pydantic import BaseModel, Field


class TraceStatus(StrEnum):
    RUNNING = "Running"
    SUCCESS = "Success"
    ERROR = "Error"


class EventType(StrEnum):
    PROMPT = "Prompt"
    TOOL_CALL = "ToolCall"
    MODEL_RESPONSE = "ModelResponse"
    MEMORY_WRITE = "MemoryWrite"
    ERROR = "Error"
    RETRY = "Retry"


class HitlStatus(StrEnum):
    NONE = "None"
    PAUSED = "Paused"
    RESUMED = "Resumed"
    TIMED_OUT = "TimedOut"


class CreateTraceRequest(BaseModel):
    agent_name: str = Field(alias="agentName")
    metadata: str | None = None

    model_config = {"populate_by_name": True}


class CreateEventRequest(BaseModel):
    trace_id: UUID = Field(alias="traceId")
    parent_id: UUID | None = Field(default=None, alias="parentId")
    branch_name: str | None = Field(default=None, alias="branchName")
    event_type: EventType = Field(alias="eventType")
    payload: str | None = None
    latency_ms: int | None = Field(default=None, alias="latencyMs")
    cost: float | None = None
    depth: int = 0
    state_snapshot: str | None = Field(default=None, alias="stateSnapshot")
    side_effects: str | None = Field(default=None, alias="sideEffects")

    model_config = {"populate_by_name": True}


class TraceResponse(BaseModel):
    id: UUID
    agent_name: str = Field(alias="agentName")
    status: TraceStatus
    start_time: datetime = Field(alias="startTime")
    end_time: datetime | None = Field(default=None, alias="endTime")
    organization_id: UUID = Field(alias="organizationId")
    workspace_id: UUID = Field(alias="workspaceId")
    metadata: str | None = None

    model_config = {"populate_by_name": True}


class EventResponse(BaseModel):
    id: UUID
    trace_id: UUID = Field(alias="traceId")
    parent_id: UUID | None = Field(default=None, alias="parentId")
    branch_name: str = Field(alias="branchName")
    timestamp: datetime
    event_type: EventType = Field(alias="eventType")
    payload: str | None = None
    latency_ms: int | None = Field(default=None, alias="latencyMs")
    cost: float | None = None
    step_order: int = Field(alias="stepOrder")
    depth: int
    state_snapshot: str | None = Field(default=None, alias="stateSnapshot")
    side_effects: str | None = Field(default=None, alias="sideEffects")
    hitl_status: HitlStatus = Field(alias="hitlStatus")
    hitl_decision: str | None = Field(default=None, alias="hitlDecision")

    model_config = {"populate_by_name": True}
