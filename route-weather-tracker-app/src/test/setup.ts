// Runs once before every test file (wired via `test.setupFiles` in vite.config.ts).
//
// Adds the jest-dom matchers — toBeInTheDocument, toHaveTextContent, and the
// rest — to Vitest's expect, and unmounts anything React Testing Library
// rendered after each test so state doesn't leak between them.
import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";

afterEach(() => {
  cleanup();
});
