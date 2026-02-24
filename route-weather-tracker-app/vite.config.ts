import type { Connect } from "vite";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

/**
 * Vite plugin that makes the preview server behave like an SPA host:
 * any request that isn't a real file and doesn't start with /api is
 * rewritten to /index.html so React Router can handle it client-side.
 */
function spaFallback() {
  return {
    name: "spa-fallback",
    configurePreviewServer(server: { middlewares: Connect.Server }) {
      server.middlewares.use((req, _res, next) => {
        const url = req.url ?? "/";
        const isFile = /\.[a-zA-Z0-9]+$/.test(url.split("?")[0]);
        if (!url.startsWith("/api") && !isFile) {
          req.url = "/index.html";
        }
        next();
      });
    },
  };
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), spaFallback()],
  server: {
    host: true,
    port: parseInt(process.env["PORT"] ?? "5173"),
    proxy: {
      "/api": {
        // Aspire injects service discovery URLs via process.env at dev-server startup
        target:
          process.env["services__api__https__0"] ||
          process.env["services__api__http__0"],
        changeOrigin: true,
        secure: false,
      },
    },
  },
  // Mirror the dev-server proxy for `vite preview` (used by the production Docker image).
  // Aspire / Azure Container Apps injects the same service-discovery env vars at container start.
  preview: {
    host: true,
    allowedHosts: true,
    port: parseInt(process.env["PORT"] ?? "4173"),
    proxy: {
      "/api": {
        target:
          process.env["services__api__https__0"] ||
          process.env["services__api__http__0"],
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
