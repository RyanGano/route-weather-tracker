# Architecture & Reference

Read this when a request needs detail beyond the summary in `CLAUDE.md`.

## Overview

"When to Drive" (Route Weather Tracker) surfaces road conditions, weather forecasts, and webcams for mountain passes that fall along a driving route between two cities. The user picks an origin and destination; the backend computes driving routes, geometrically matches known passes to each route polyline, and aggregates live condition/weather/camera data per pass. Despite the README's Stanwood→Kalispell framing, the pass registry and data sources now span many states (WA, OR, ID, MT, WY, CO, UT, NV, NM, CA, VA, NC/TN).

## Commands

Use **`yarn`** for all frontend Node work — never `npm` (a `yarn.lock` is committed).

Run everything together (API + Vite frontend + Aspire dashboard at `http://localhost:18888`):
```bash
dotnet run --project route-weather-tracker-service.AppHost
```

Frontend only (from `route-weather-tracker-app/`):
```bash
yarn install          # or: yarn install --frozen-lockfile (CI)
yarn dev              # Vite dev server
yarn build            # tsc -b && vite build  → dist/  (this is what gets deployed)
yarn lint             # eslint .
yarn preview          # serve the production build with SPA + /api proxy
```

Backend tests (xUnit + Moq, from repo root):
```bash
dotnet test                                              # all tests
dotnet test --filter "FullyQualifiedName~PassAggregator" # one class
dotnet test --filter "DisplayName~specific test name"    # one test
```

Build the backend alone: `dotnet build route-weather-tracker-service/route-weather-tracker-service.csproj`

## Architecture

### Orchestration (.NET Aspire)
`route-weather-tracker-service.AppHost/AppHost.cs` is the entry point that wires the API project and the Vite frontend together. Aspire injects service-discovery env vars (`services__api__https__0`, `VITE_API_URL`) that the Vite proxy (`vite.config.ts`) and axios client (`services/passService.ts`) read. There is **no hardcoded backend URL** — both dev and prod rely on these injected values; the axios `BASE_URL` falls back to `""` so the Vite proxy can handle `/api`.

### Backend (`route-weather-tracker-service/`, .NET 10 Web API)
- **Static registries** (`Data/`) are the source of truth for geography: `PassRegistry` (every pass with coords/elevation/highway), `RouteRegistry` (highways), `RouteEndpointRegistry` (cities, including `RoutingHubs` used to bias route computation).
- **`PassAggregatorService`** is the core fan-out: for a pass ID it merges road condition (from the matching `IPassDataSource`), webcams, and NWS weather into a `PassSummary`. Results are cached in `IMemoryCache` for **5 minutes** with a per-pass `SemaphoreSlim` to prevent a thundering herd on cache miss. When a pass has no official condition source, condition is *derived* from weather (`DeriveCondition`/`InferRoadCondition`).
- **`IPassDataSource`** is the per-state plugin point. Each implementation (`WsdotPassDataSource`, `IdahoPassDataSource`, `MontanaPassDataSource`, etc.) declares `SupportedPassIds` and returns conditions + cameras. **Adding a new state = add a `PassInfo` to `PassRegistry` + a new `IPassDataSource` registered in `Program.cs`; no aggregator changes.**
- **Weather** comes solely from NWS (`NwsService`); OpenWeather is no longer used as a live source (tests/interfaces remain). NWS requires a `User-Agent` + `From` header sourced from config `NwsContact`.
- **Routing**: `OpenRouteServiceRoutingService` (current; `OsrmRoutingService` is the legacy implementation) calls ORS for up to ~3 route options, querying extra waypoint sets through `RoutingHubs` to surface alternate corridors. `PassLocatorService` then does pure in-memory Haversine matching of registry passes to each route polyline (default 15 km threshold). ORS's resilience handler is intentionally removed in `Program.cs` in favor of a flat 30s timeout (no retries).
- **`SensitiveUrlRedactionHandler`** wraps outbound HTTP clients to scrub API keys from logged URLs.
- Controllers: `PassesController` (`/api/passes`, `/api/passes/{id}`, `/api/passes/waypoints`), `RoutesController` (`/api/routes`, `/api/routes/compute`), `EndpointsController` (`/api/endpoints`), `WarmupController` (`/api/warmup`, accepts GET+HEAD for cache priming).

### Frontend (`route-weather-tracker-app/`, React 19 + Vite + react-bootstrap)
- `main.tsx` sets up React Router; `App.tsx` is the single page driven by URL params `/:fromId/:toId/:routeSlug` (deep-linkable permalinks). It calls `computeRoutes` → renders one `PassCard` per matched pass ID, with `PassLoadError` + per-pass retry for individual failures.
- `services/passService.ts` is the only API layer (axios). Per-pass fetches run in parallel and tolerate individual failures.
- On load `main.tsx` fires a best-effort `/api/warmup` to prime backend caches.
- Cross-cutting state via contexts: `RefreshContext` (auto-refresh cadence) and `AdContext` (affiliate/ad slots — `AdBanner`, `AdSlot`, `ContextualOfferCard`).

### Secrets & config
Secrets resolve through Azure Key Vault via `DefaultAzureCredential` (`az login` locally; Managed Identity in prod). `KeyVaultUri` is set via user-secrets (local) or `azd env set` (prod) and baked in by AppHost. In Development the app runs without Key Vault; in non-Development a missing `KeyVaultUri` fails fast. Required secrets: `WsdotApiKey`, `OpenRouteServiceApiKey` (and historically `OpenWeatherApiKey`).

### Deployment
GitHub Actions (`.github/workflows/azure-dev.yml`) deploys on push to `main`: backend via `dotnet publish` → Azure App Service, frontend via `yarn install --frozen-lockfile` + build → Azure Static Web App. **Deploy the built `dist/`, never raw `/src`** (browsers can't execute the TS source). Frontend build-time `VITE_*` vars (API URL, ad/affiliate IDs) are supplied as GitHub Actions vars.
