import { Outlet, Link } from "react-router-dom";

export default function Layout() {
  return (
    <div className="min-h-screen flex flex-col">
      <header className="border-b border-gray-800 px-6 py-3 flex items-center gap-4">
        <Link to="/" className="text-xl font-bold text-blue-400 hover:text-blue-300">
          ◆ Waypoint
        </Link>
        <nav className="flex gap-4 ml-8 text-sm text-gray-400">
          <Link to="/" className="hover:text-gray-200">Traces</Link>
          <Link to="/health" className="hover:text-gray-200">Health</Link>
        </nav>
      </header>
      <main className="flex-1 p-6">
        <Outlet />
      </main>
    </div>
  );
}
