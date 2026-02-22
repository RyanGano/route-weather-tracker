import { createContext, useContext, useEffect, useState } from "react";

const REFRESH_INTERVAL_MS = 2 * 60 * 1000; // 2 minutes

interface RefreshState {
  cacheBuster: number;
  lastUpdated: Date;
}

const RefreshContext = createContext<RefreshState>({
  cacheBuster: Date.now(),
  lastUpdated: new Date(),
});

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

export function useRefresh(): RefreshState {
  return useContext(RefreshContext);
}
