import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { listTraces } from "../api/client";
import type { Trace } from "../types";

const STATUS_COLORS: Record<string, string> = {
  Running: "text-yellow-400",
  Success: "text-green-400",
  Error: "text-red-400",
};

export default function TracesListPage() {
  const [traces, setTraces] = useState<Trace[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    listTraces({ limit: 50 })
      .then((res) => setTraces(res.items))
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="text-gray-400">Loading traces...</div>;

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Traces</h1>
      {traces.length === 0 ? (
        <p className="text-gray-500">No traces yet. Start an agent with the Waypoint SDK to see traces here.</p>
      ) : (
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-gray-500 border-b border-gray-800">
              <th className="pb-2">Agent</th>
              <th className="pb-2">Status</th>
              <th className="pb-2">Started</th>
              <th className="pb-2">Duration</th>
            </tr>
          </thead>
          <tbody>
            {traces.map((t) => (
              <tr key={t.id} className="border-b border-gray-800/50 hover:bg-gray-900/50">
                <td className="py-3">
                  <Link to={`/traces/${t.id}`} className="text-blue-400 hover:text-blue-300">
                    {t.agentName}
                  </Link>
                </td>
                <td className={`py-3 ${STATUS_COLORS[t.status] ?? "text-gray-400"}`}>
                  {t.status}
                </td>
                <td className="py-3 text-gray-400">
                  {new Date(t.startTime).toLocaleString()}
                </td>
                <td className="py-3 text-gray-400">
                  {t.endTime
                    ? `${((new Date(t.endTime).getTime() - new Date(t.startTime).getTime()) / 1000).toFixed(1)}s`
                    : "—"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
