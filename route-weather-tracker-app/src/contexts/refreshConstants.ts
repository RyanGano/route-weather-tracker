export const REFRESH_INTERVAL_MS = 2 * 60 * 1000; // 2 minutes

export interface RefreshState {
  cacheBuster: number;
  lastUpdated: Date;
}

