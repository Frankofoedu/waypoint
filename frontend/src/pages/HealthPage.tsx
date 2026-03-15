import { useState, useEffect } from "react";

interface HealthStatus {
  status: string;
  timestamp: string;
}

export default function HealthPage() {
  const [health, setHealth] = useState<HealthStatus | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch("/v1/health")
      .then((r) => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`);
        return r.json();
      })
      .then(setHealth)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="max-w-md mx-auto mt-12">
      <h1 className="text-2xl font-bold mb-6">System Health</h1>
      {loading && <p className="text-gray-400">Checking...</p>}
      {error && (
        <div className="bg-red-900/30 border border-red-700 rounded-lg p-4">
          <p className="text-red-400 font-medium">Unhealthy</p>
          <p className="text-sm text-gray-400 mt-1">{error}</p>
        </div>
      )}
      {health && (
        <div className="bg-green-900/30 border border-green-700 rounded-lg p-4">
          <p className="text-green-400 font-medium">Healthy</p>
          <p className="text-sm text-gray-400 mt-1">
            Status: {health.status}
          </p>
          <p className="text-sm text-gray-400">
            Checked: {new Date(health.timestamp).toLocaleString()}
          </p>
        </div>
      )}
    </div>
  );
}
