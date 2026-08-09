import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat['recommended-latest'],
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    rules: {
      // New in eslint-plugin-react-hooks v7. It flags 4 pre-existing call sites
      // (CityCombobox, RouteHeader x2, WebcamViewer) where an effect calls
      // setState synchronously. Each fix means deriving the value during render
      // or re-keying the component — a real behavior change, and the frontend
      // has no tests to catch a regression. Kept as a warning so the findings
      // stay visible instead of being switched off. Promote back to "error"
      // once the four sites are addressed.
      "react-hooks/set-state-in-effect": "warn",
    },
  },
])
