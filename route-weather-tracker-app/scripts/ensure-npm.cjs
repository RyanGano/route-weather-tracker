// Fails the command when anything other than npm is used to install or build.
//
// Runs from `preinstall`, i.e. before node_modules exists, so it must have no
// imports. It is .cjs because package.json sets "type": "module".
//
// Two entry points call this, and both are needed:
//   1. package.json "preinstall" / "prebuild" — catches npm-compatible managers
//      (pnpm, bun) via npm_config_user_agent.
//   2. .yarnrc "yarn-path" — yarn classic delegates to this script before it
//      even parses package.json, so it never reaches the hooks above.
//
// Yarn invokes yarn-path with no user agent set, which is why an empty agent
// must be treated as "not npm" rather than waved through.

const agent = process.env.npm_config_user_agent || "";
const manager = agent.split("/")[0] || "yarn";

if (manager === "npm") {
  process.exit(0);
}

const attempted = process.argv.slice(2).join(" ");

process.stderr.write(
  `\n  This project uses npm. Detected: ${manager}\n` +
    (attempted ? `  You ran: ${manager} ${attempted}\n` : "") +
    `
  npm equivalents:
    ${manager} install                    ->  npm install
    ${manager} install --frozen-lockfile  ->  npm ci
    ${manager} add <pkg>                  ->  npm install <pkg>
    ${manager} add -D <pkg>               ->  npm install -D <pkg>
    ${manager} remove <pkg>               ->  npm uninstall <pkg>
    ${manager} <script>                   ->  npm run <script>
    ${manager} <script> --flag            ->  npm run <script> -- --flag

  Why: the repo previously used yarn 1.x, which is maintenance-only and emits
  Node deprecation warnings from its own code with no fix forthcoming. Running
  a second package manager here regenerates a competing lockfile and a
  divergent dependency tree.

  Switching package managers on purpose? Change these, not just this check:
    route-weather-tracker-app/package.json   ("packageManager", preinstall/prebuild)
    route-weather-tracker-app/.yarnrc        (yarn-path)
    route-weather-tracker-app/Dockerfile
    route-weather-tracker-service.AppHost/AppHost.cs  (.WithNpm())
    .github/workflows/azure-dev.yml
\n`,
);

process.exit(1);
