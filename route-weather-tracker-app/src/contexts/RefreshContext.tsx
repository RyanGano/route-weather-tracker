import { useEffect, useState } from "react";
import { REFRESH_INTERVAL_MS } from "./refreshConstants";
import type { RefreshState } from "./refreshConstants";
import { RefreshContext } from "./refreshContextCore";

/**
 * Provides a single shared refresh timer for all webcam images in the tree.
 * All WebcamViewer components consume the same cacheBuster value so they
 * refresh simultaneously rather than each managing their own interval.
 */
export function RefreshProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<RefreshState>(() => ({
    cacheBuster: Date.now(),
    lastUpdated: new Date(),
  }));

  useEffect(() => {
    const timer = setInterval(() => {
      setState({ cacheBuster: Date.now(), lastUpdated: new Date() });
    }, REFRESH_INTERVAL_MS);
    return () => clearInterval(timer);
  }, []);

  return (
    <RefreshContext.Provider value={state}>{children}</RefreshContext.Provider>
  );
}

// `useRefresh` is now exported from `refreshContextCore.ts` so this file
// only exports the provider component (satisfies react-refresh rule).
