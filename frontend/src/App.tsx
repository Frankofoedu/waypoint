import { Routes, Route } from "react-router-dom";
import Layout from "./components/layout/Layout";
import TracesListPage from "./pages/TracesListPage";
import TraceDetailPage from "./pages/TraceDetailPage";
import HealthPage from "./pages/HealthPage";

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<TracesListPage />} />
        <Route path="/traces/:traceId" element={<TraceDetailPage />} />
        <Route path="/health" element={<HealthPage />} />
      </Route>
    </Routes>
  );
}
