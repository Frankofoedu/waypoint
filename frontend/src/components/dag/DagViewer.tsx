import { useCallback, useMemo } from "react";
import {
  ReactFlow,
  Background,
  Controls,
  type NodeTypes,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import type { TraceEvent } from "../../types";
import { transformEventsToGraph } from "./dagTransform";
import EventNode from "./EventNode";

interface DagViewerProps {
  events: TraceEvent[];
  onSelectEvent?: (event: TraceEvent) => void;
}

const nodeTypes: NodeTypes = {
  eventNode: EventNode,
};

export default function DagViewer({ events, onSelectEvent }: DagViewerProps) {
  const { nodes, edges } = useMemo(() => transformEventsToGraph(events), [events]);

  const onNodeClick = useCallback(
    (_: React.MouseEvent, node: { id: string }) => {
      const event = events.find((e) => e.id === node.id);
      if (event && onSelectEvent) onSelectEvent(event);
    },
    [events, onSelectEvent],
  );

  return (
    <div className="h-[600px] rounded-lg border border-gray-800 bg-gray-900">
      <ReactFlow
        nodes={nodes}
        edges={edges}
        nodeTypes={nodeTypes}
        onNodeClick={onNodeClick}
        fitView
        minZoom={0.1}
        maxZoom={2}
      >
        <Background color="#374151" gap={20} />
        <Controls />
      </ReactFlow>
    </div>
  );
}
