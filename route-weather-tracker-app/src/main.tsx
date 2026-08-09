import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import "bootstrap/dist/css/bootstrap.min.css";
import "./index.css";
import App from "./App.tsx";
import PrivacyPolicy from "./pages/PrivacyPolicy.tsx";

// Fire a best-effort warmup request to the backend as early as possible so
// the service can populate caches while the frontend finishes loading.
// Use the same base URL as the axios API client so the request reaches the
// backend when the frontend is deployed separately (e.g. Azure Static Web App
// + App Service). A relative URL would hit the static host (404).
const _warmupBase = import.meta.env.VITE_API_URL ?? "";
void fetch(`${_warmupBase}/api/warmup`).catch(() => {
  /* ignore errors; warmup is optional */
});

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/privacy" element={<PrivacyPolicy />} />
        <Route path="/:fromId/:toId/:routeSlug" element={<App />} />
        <Route path="*" element={<App />} />
      </Routes>
    </BrowserRouter>
  </StrictMode>,
);
