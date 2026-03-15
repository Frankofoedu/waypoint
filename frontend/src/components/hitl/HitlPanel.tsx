import { useState } from "react";
import { resumeEvent } from "../../api/client";
import type { TraceEvent } from "../../types";

interface HitlPanelProps {
  event: TraceEvent;
  onResumed: () => void;
}

export default function HitlPanel({ event, onResumed }: HitlPanelProps) {
  const [comments, setComments] = useState("");
  const [submitting, setSubmitting] = useState(false);

  if (event.hitlStatus !== "Paused") return null;

  const handleDecision = async (decision: string) => {
    setSubmitting(true);
    try {
      await resumeEvent(event.id, decision, comments || undefined);
      onResumed();
    } catch (err) {
      console.error("Failed to resume:", err);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="border border-yellow-600/50 bg-yellow-950/30 rounded-lg p-4 mt-4">
      <h3 className="text-sm font-semibold text-yellow-400 mb-2">⏸ Human Decision Required</h3>
      <p className="text-xs text-gray-400 mb-3">
        Step #{event.stepOrder} ({event.eventType}) is paused and waiting for human input.
      </p>
      <textarea
        value={comments}
        onChange={(e) => setComments(e.target.value)}
        placeholder="Optional comments..."
        className="w-full bg-gray-900 border border-gray-700 rounded p-2 text-xs mb-3 resize-none"
        rows={2}
      />
      <div className="flex gap-2">
        <button
          onClick={() => handleDecision("approve")}
          disabled={submitting}
          className="px-3 py-1.5 bg-green-600 hover:bg-green-500 rounded text-xs font-medium disabled:opacity-50"
        >
          Approve
        </button>
        <button
          onClick={() => handleDecision("reject")}
          disabled={submitting}
          className="px-3 py-1.5 bg-red-600 hover:bg-red-500 rounded text-xs font-medium disabled:opacity-50"
        >
          Reject
        </button>
        <button
          onClick={() => handleDecision("escalate")}
          disabled={submitting}
          className="px-3 py-1.5 bg-yellow-600 hover:bg-yellow-500 rounded text-xs font-medium disabled:opacity-50"
        >
          Escalate
        </button>
      </div>
    </div>
  );
}
