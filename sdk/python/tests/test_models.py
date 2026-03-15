import pytest
from waypoint.models import CreateEventRequest, EventType, TraceStatus


def test_trace_status_values():
    assert TraceStatus.RUNNING == "Running"
    assert TraceStatus.SUCCESS == "Success"
    assert TraceStatus.ERROR == "Error"


def test_event_type_values():
    assert EventType.PROMPT == "Prompt"
    assert EventType.TOOL_CALL == "ToolCall"
    assert EventType.MODEL_RESPONSE == "ModelResponse"


def test_create_event_request_serialization():
    req = CreateEventRequest(
        trace_id="00000000-0000-0000-0000-000000000001",
        event_type=EventType.PROMPT,
        depth=0,
        payload='{"prompt": "hello"}',
    )
    data = req.model_dump(by_alias=True, exclude_none=True, mode="json")
    assert data["traceId"] == "00000000-0000-0000-0000-000000000001"
    assert data["eventType"] == "Prompt"
    assert data["depth"] == 0
