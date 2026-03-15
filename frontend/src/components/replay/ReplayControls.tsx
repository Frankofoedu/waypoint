import { useState } from "react";
import { replayEvent } from "../../api/client";
import type { TraceEvent, SideEffectWarning } from "../../types";

interface ReplayControlsProps {
  event: TraceEvent;
  onReplayed: () => void;
}

export default function ReplayControls({ event, onReplayed }: ReplayControlsProps) {
  const [payload, setPayload] = useState(event.payload ?? "");
  const [warnings, setWarnings] = useState<SideEffectWarning[]>([]);
  const [showWarning, setShowWarning] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const handleReplay = async () => {
    setSubmitting(true);
    try {
      const result = await replayEvent(event.id, payload || undefined);
      if (result.warnings.length > 0) {
        setWarnings(result.warnings);
        setShowWarning(true);
      }
      onReplayed();
    } catch (err) {
      console.error("Replay failed:", err);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="border border-gray-700 rounded-lg p-4 mt-4">
      <h3 className="text-sm font-semibold text-gray-300 mb-2">🔄 Replay from this event</h3>
      <textarea
        value={payload}
        onChange={(e) => setPayload(e.target.value)}
        placeholder="Edit payload (JSON)..."
        className="w-full bg-gray-900 border border-gray-700 rounded p-2 text-xs font-mono mb-3 resize-none"
        rows={4}
      />
      <button
        onClick={handleReplay}
        disabled={submitting}
        className="px-3 py-1.5 bg-blue-600 hover:bg-blue-500 rounded text-xs font-medium disabled:opacity-50"
      >
        Replay
      </button>

      {showWarning && warnings.length > 0 && (
        <div className="mt-3 border border-orange-600/50 bg-orange-950/30 rounded p-3">
          <h4 className="text-xs font-semibold text-orange-400 mb-2">⚠️ Side-Effect Warnings</h4>
          {warnings.map((w) => (
            <div key={w.eventId} className="text-xs text-gray-400 mb-1">
              Step #{w.stepOrder} at {new Date(w.timestamp).toLocaleString()} — {w.sideEffects}
            </div>
          ))}
          <p className="text-xs text-orange-300 mt-2">
            These side effects from ancestor events cannot be undone.
          </p>
        </div>
      )}
    </div>
  );
}
