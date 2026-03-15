import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { getTrace } from "../api/client";
import type { TraceDetail, TraceEvent } from "../types";
import DagViewer from "../components/dag/DagViewer";
import Timeline from "../components/timeline/Timeline";
import HitlPanel from "../components/hitl/HitlPanel";
import ReplayControls from "../components/replay/ReplayControls";

const STATUS_COLORS: Record<string, string> = {
  Running: "text-yellow-400",
  Success: "text-green-400",
  Error: "text-red-400",
};

export default function TraceDetailPage() {
  const { traceId } = useParams<{ traceId: string }>();
  const [trace, setTrace] = useState<TraceDetail | null>(null);
  const [selected, setSelected] = useState<TraceEvent | null>(null);
  const [loading, setLoading] = useState(true);

  const loadTrace = () => {
    if (!traceId) return;
    getTrace(traceId)
      .then(setTrace)
      .catch(console.error)
      .finally(() => setLoading(false));
  };

  useEffect(loadTrace, [traceId]);

  if (loading) return <div className="text-gray-400">Loading trace...</div>;
  if (!trace) return <div className="text-red-400">Trace not found</div>;

  const pausedEvent = trace.events.find((e) => e.hitlStatus === "Paused");

  return (
    <div>
      <div className="flex items-center gap-4 mb-6">
        <h1 className="text-2xl font-bold">{trace.agentName}</h1>
        <span className={`text-sm ${STATUS_COLORS[trace.status] ?? ""}`}>{trace.status}</span>
        <span className="text-sm text-gray-500">{trace.events.length} events</span>
      </div>

      <div className="grid grid-cols-[1fr_280px] gap-6">
        <div>
          <DagViewer events={trace.events} onSelectEvent={setSelected} />

          {pausedEvent && <HitlPanel event={pausedEvent} onResumed={loadTrace} />}

          {selected && (
            <div className="mt-4 border border-gray-800 rounded-lg p-4">
              <h3 className="text-sm font-semibold mb-2">Event Detail</h3>
              <div className="grid grid-cols-2 gap-2 text-xs text-gray-400">
                <div>Type: <span className="text-gray-200">{selected.eventType}</span></div>
                <div>Branch: <span className="text-gray-200">{selected.branchName}</span></div>
                <div>Step: <span className="text-gray-200">#{selected.stepOrder}</span></div>
                <div>Depth: <span className="text-gray-200">{selected.depth}</span></div>
                {selected.latencyMs != null && <div>Latency: <span className="text-gray-200">{selected.latencyMs}ms</span></div>}
                {selected.cost != null && <div>Cost: <span className="text-gray-200">${selected.cost}</span></div>}
              </div>
              {selected.payload && (
                <pre className="mt-3 bg-gray-900 rounded p-2 text-xs font-mono overflow-x-auto max-h-48">
                  {selected.payload}
                </pre>
              )}
              {selected.sideEffects && (
                <div className="mt-2 text-xs text-orange-400">
                  ⚠️ Side effects: {selected.sideEffects}
                </div>
              )}
              <ReplayControls event={selected} onReplayed={loadTrace} />
            </div>
          )}
        </div>

        <Timeline
          events={trace.events}
          selectedEventId={selected?.id}
          onSelectEvent={setSelected}
        />
      </div>
    </div>
  );
}
