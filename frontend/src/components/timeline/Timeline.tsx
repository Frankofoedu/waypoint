import type { TraceEvent } from "../../types";

interface TimelineProps {
  events: TraceEvent[];
  selectedEventId?: string;
  onSelectEvent: (event: TraceEvent) => void;
}

const TYPE_COLORS: Record<string, string> = {
  Prompt: "border-blue-500",
  ToolCall: "border-purple-500",
  ModelResponse: "border-green-500",
  MemoryWrite: "border-yellow-500",
  Error: "border-red-500",
  Retry: "border-orange-500",
};

export default function Timeline({ events, selectedEventId, onSelectEvent }: TimelineProps) {
  return (
    <div className="space-y-1 overflow-y-auto max-h-[600px]">
      <h3 className="text-sm font-semibold text-gray-400 mb-3">Timeline</h3>
      {events.map((event) => (
        <button
          key={event.id}
          onClick={() => onSelectEvent(event)}
          className={`w-full text-left p-2 rounded text-xs border-l-2 ${
            TYPE_COLORS[event.eventType] ?? "border-gray-600"
          } ${
            selectedEventId === event.id ? "bg-gray-800" : "bg-gray-900/50 hover:bg-gray-800/50"
          }`}
        >
          <div className="flex justify-between">
            <span className="font-medium">{event.eventType}</span>
            <span className="text-gray-500">#{event.stepOrder}</span>
          </div>
          <div className="text-gray-500 mt-0.5">
            {event.branchName} · d{event.depth}
            {event.sideEffects && " ⚠️"}
            {event.hitlStatus === "Paused" && " ⏸"}
          </div>
        </button>
      ))}
    </div>
  );
}
