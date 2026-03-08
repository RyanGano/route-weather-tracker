Copilot instructions for this repository:

- Use `yarn` for all Node dependency management and scripts (do not use `npm`).
- To install frontend dependencies, run `cd route-weather-tracker-app && yarn`.
- To start the frontend in development mode, run `cd route-weather-tracker-app && yarn dev`.
- When updating contributor docs or CI, prefer `yarn` commands and lockfile (`yarn.lock`).

- Verify UI changes locally before committing: start the frontend (`yarn dev`),
  confirm the new UI behavior in a browser (or fetch the root HTML), and run
  any relevant unit or smoke tests. Commit only after a local verification.

Rationale: the project uses Yarn and includes a `yarn.lock`; using Yarn ensures consistent installs and reproducible lockfile behavior across environments.
