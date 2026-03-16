import { Handle, Position, type NodeProps } from "@xyflow/react";
import type { TraceEvent } from "../../types";

const TYPE_ICONS: Record<string, string> = {
  Prompt: "💬",
  ToolCall: "🔧",
  ModelResponse: "🤖",
  MemoryWrite: "💾",
  Error: "❌",
  Retry: "🔄",
};

interface EventNodeData {
  event: TraceEvent;
  label: string;
  [key: string]: unknown;
}

export default function EventNode({ data }: NodeProps) {
  const nodeData = data as unknown as EventNodeData;
  const event = nodeData.event;
  const hasSideEffects = !!event.sideEffects;
  const isPaused = event.hitlStatus === "Paused";

  return (
    <div className="p-3 text-xs min-w-[180px]">
      <Handle type="target" position={Position.Top} className="!bg-gray-600 !w-2 !h-2" />
      <div className="flex items-center gap-1 font-semibold mb-1">
        <span>{TYPE_ICONS[event.eventType] ?? "📌"}</span>
        <span>{event.eventType}</span>
        {hasSideEffects && <span title="Has side effects">⚠️</span>}
        {isPaused && <span title="HITL Paused">⏸</span>}
      </div>
      <div className="text-gray-400">
        Step {event.stepOrder} · {event.branchName}
      </div>
      {event.latencyMs != null && (
        <div className="text-gray-500 mt-1">{event.latencyMs}ms</div>
      )}
      <Handle type="source" position={Position.Bottom} className="!bg-gray-600 !w-2 !h-2" />
    </div>
  );
}
