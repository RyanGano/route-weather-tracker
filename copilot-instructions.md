Copilot instructions for this repository:

- Use `yarn` for all Node dependency management and scripts (do not use `npm`).
- To install frontend dependencies, run `cd route-weather-tracker-app && yarn`.
- To start the frontend in development mode, run `cd route-weather-tracker-app && yarn dev`.
- When updating contributor docs or CI, prefer `yarn` commands and lockfile (`yarn.lock`).

- Verify UI changes locally before committing: start the frontend (`yarn dev`),
  confirm the new UI behavior in a browser (or fetch the root HTML), and run
  any relevant unit or smoke tests. Commit only after a local verification.

- Continuous deployment: This repository uses GitHub Actions to deploy both the
    backend and frontend. The CI workflow must build the frontend and publish the
    generated `dist` files. Ensure the workflow runs the following steps for the
    frontend job before deploying:

    - `cd route-weather-tracker-app`
    - `yarn install --frozen-lockfile`
    - `yarn build`

    The built output (`route-weather-tracker-app/dist`) is what should be deployed
    to the static host. Do NOT deploy raw `/src` files — browsers cannot execute
    TypeScript source and serving them causes the MIME-type/module errors we saw.

    I updated `.github/workflows/azure-dev.yml` to run `yarn build` before
    deployment. If you use a different workflow or staging branch, mirror the same
    build steps there.

Rationale: the project uses Yarn and includes a `yarn.lock`; using Yarn ensures consistent installs and reproducible lockfile behavior across environments.
