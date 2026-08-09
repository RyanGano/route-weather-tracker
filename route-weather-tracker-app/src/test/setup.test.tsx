// Proves the test harness itself is wired up: jsdom environment, JSX/TSX
// compilation, React Testing Library rendering, and the jest-dom matchers
// added in setup.ts.
//
// This is a wiring check, not coverage of the app. Real component and service
// tests still need to be written.
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

describe("test harness", () => {
  it("runs in a DOM environment", () => {
    expect(typeof document).toBe("object");
    expect(document.body).toBeTruthy();
  });

  it("renders a component and applies the jest-dom matchers", () => {
    render(<p>pass conditions</p>);
    expect(screen.getByText("pass conditions")).toBeInTheDocument();
  });
});
