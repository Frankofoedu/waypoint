import pytest
from waypoint.buffer import EventBuffer
from waypoint.models import CreateEventRequest, EventType
from unittest.mock import AsyncMock, MagicMock


@pytest.fixture
def mock_client():
    client = MagicMock()
    client.create_event = AsyncMock()
    return client


@pytest.mark.asyncio
async def test_buffer_enqueue_and_flush(mock_client):
    buffer = EventBuffer(mock_client, max_size=100, flush_interval=0.1)

    event = CreateEventRequest(
        trace_id="00000000-0000-0000-0000-000000000001",
        event_type=EventType.PROMPT,
        depth=0,
    )
    buffer.enqueue(event)
    await buffer._flush_remaining()

    mock_client.create_event.assert_called_once_with(event)


@pytest.mark.asyncio
async def test_buffer_max_size_drops_oldest(mock_client):
    buffer = EventBuffer(mock_client, max_size=2, flush_interval=10)

    for i in range(3):
        buffer.enqueue(
            CreateEventRequest(
                trace_id="00000000-0000-0000-0000-000000000001",
                event_type=EventType.PROMPT,
                depth=i,
            )
        )

    await buffer._flush_remaining()
    assert mock_client.create_event.call_count == 2
