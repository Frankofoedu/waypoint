import { useState } from "react";
import { Outlet, Link } from "react-router-dom";

export default function Layout() {
  const [showKeyInput, setShowKeyInput] = useState(false);
  const [apiKey, setApiKey] = useState(localStorage.getItem("Tracewire_api_key") ?? "");

  const saveKey = () => {
    localStorage.setItem("Tracewire_api_key", apiKey);
    window.location.reload();
  };

  return (
    <div className="min-h-screen flex flex-col">
      <header className="border-b border-gray-800 px-6 py-3 flex items-center gap-4">
        <Link to="/" className="text-xl font-bold text-blue-400 hover:text-blue-300">
          ◆ Tracewire
        </Link>
        <nav className="flex gap-4 ml-8 text-sm text-gray-400">
          <Link to="/" className="hover:text-gray-200">Traces</Link>
          <Link to="/health" className="hover:text-gray-200">Health</Link>
        </nav>
        <div className="ml-auto">
          {showKeyInput ? (
            <div className="flex items-center gap-2">
              <input
                type="password"
                value={apiKey}
                onChange={e => setApiKey(e.target.value)}
                onKeyDown={e => e.key === "Enter" && saveKey()}
                placeholder="API Key"
                className="bg-gray-800 border border-gray-700 rounded px-2 py-1 text-sm text-gray-200 w-56"
                autoFocus
              />
              <button onClick={saveKey} className="text-sm text-blue-400 hover:text-blue-300">Save</button>
              <button onClick={() => setShowKeyInput(false)} className="text-sm text-gray-500 hover:text-gray-300">✕</button>
            </div>
          ) : (
            <button
              onClick={() => setShowKeyInput(true)}
              className="text-sm text-gray-500 hover:text-gray-300"
            >
              {localStorage.getItem("Tracewire_api_key") ? "🔑 Key Set" : "🔑 Set API Key"}
            </button>
          )}
        </div>
      </header>
      <main className="flex-1 p-6">
        <Outlet />
      </main>
    </div>
  );
}
