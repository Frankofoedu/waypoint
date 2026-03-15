import type { Node, Edge } from "@xyflow/react";
import type { TraceEvent } from "../../types";

const EVENT_TYPE_COLORS: Record<string, string> = {
  Prompt: "#3b82f6",
  ToolCall: "#8b5cf6",
  ModelResponse: "#10b981",
  MemoryWrite: "#f59e0b",
  Error: "#ef4444",
  Retry: "#f97316",
};

export interface DagNode extends Node {
  data: {
    event: TraceEvent;
    label: string;
  };
}

export function transformEventsToGraph(events: TraceEvent[]): { nodes: DagNode[]; edges: Edge[] } {
  const nodes: DagNode[] = [];
  const edges: Edge[] = [];
  const branchOffsets = new Map<string, number>();
  let branchIndex = 0;

  for (const event of events) {
    const branch = event.branchName;
    if (!branchOffsets.has(branch)) {
      branchOffsets.set(branch, branchIndex * 300);
      branchIndex++;
    }

    const xOffset = branchOffsets.get(branch) ?? 0;
    const hasSideEffects = !!event.sideEffects;
    const isPaused = event.hitlStatus === "Paused";

    nodes.push({
      id: event.id,
      type: "eventNode",
      position: { x: xOffset, y: event.depth * 120 },
      data: {
        event,
        label: `${event.eventType}${hasSideEffects ? " ⚠️" : ""}${isPaused ? " ⏸" : ""}`,
      },
      style: {
        border: `2px solid ${EVENT_TYPE_COLORS[event.eventType] ?? "#6b7280"}`,
        borderRadius: 8,
        background: "#1f2937",
        color: "#e5e7eb",
        padding: 0,
        width: 200,
      },
    });

    if (event.parentId) {
      edges.push({
        id: `${event.parentId}-${event.id}`,
        source: event.parentId,
        target: event.id,
        animated: event.hitlStatus === "Paused",
        style: { stroke: EVENT_TYPE_COLORS[event.eventType] ?? "#6b7280" },
      });
    }
  }

  return { nodes, edges };
}
