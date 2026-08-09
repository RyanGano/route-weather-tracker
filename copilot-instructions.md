Copilot instructions for this repository:

- Use `npm` for all Node dependency management and scripts (do not use `yarn` or
  `pnpm`). A `preinstall` guard and yarn's own `packageManager` check will fail
  the command if you do.
- To install frontend dependencies, run `cd route-weather-tracker-app && npm install`.
- To start the frontend in development mode, run `cd route-weather-tracker-app && npm run dev`.
- When updating contributor docs or CI, prefer `npm` commands and the lockfile
  (`package-lock.json`).
- Passing flags through a script needs `--`: `npm run lint -- --fix`.

- Verify UI changes locally before committing: start the frontend (`npm run dev`),
  confirm the new UI behavior in a browser (or fetch the root HTML), and run
  any relevant unit or smoke tests. Commit only after a local verification.

- Continuous deployment: This repository uses GitHub Actions to deploy both the
    backend and frontend. The CI workflow must build the frontend and publish the
    generated `dist` files. Ensure the workflow runs the following steps for the
    frontend job before deploying:

    - `cd route-weather-tracker-app`
    - `npm ci`
    - `npm run build`

    The built output (`route-weather-tracker-app/dist`) is what should be deployed
    to the static host. Do NOT deploy raw `/src` files — browsers cannot execute
    TypeScript source and serving them causes the MIME-type/module errors we saw.

    `.github/workflows/azure-dev.yml` runs `npm ci` before deployment. If you use
    a different workflow or staging branch, mirror the same build steps there.

Rationale: the project standardized on npm and commits a `package-lock.json`.
It previously used yarn 1.x, which has been maintenance-only since 2020 and
emits Node deprecation warnings from its own code with no fix forthcoming.
Running a second package manager regenerates a competing lockfile and a
divergent dependency tree.
