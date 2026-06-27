import { useEffect, useState } from "react";
import type { CameraImage } from "../types/passTypes";
import { useRefresh } from "../contexts/refreshContextCore";
import { proxiedCameraUrl } from "../services/passService";

interface WebcamViewerProps {
  cameras: CameraImage[];
}

/** Append the cache-buster with the correct separator so URLs that already
 *  carry a query string aren't corrupted. */
function withCacheBuster(url: string, buster: number): string {
  const sep = url.includes("?") ? "&" : "?";
  return `${url}${sep}t=${buster}`;
}

export default function WebcamViewer({ cameras }: WebcamViewerProps) {
  const { cacheBuster, lastUpdated } = useRefresh();
  // Track cameras whose image failed to load so we can show a placeholder
  // instead of a blank gap, and detect when every camera is offline.
  const [failed, setFailed] = useState<Set<string>>(new Set());

  // On each refresh cycle, retry every camera — one that was briefly offline
  // may be back.
  useEffect(() => {
    setFailed((prev) => (prev.size === 0 ? prev : new Set()));
  }, [cacheBuster]);

  if (cameras.length === 0) {
    return (
      <div className="text-muted small fst-italic py-2">No webcam available</div>
    );
  }

  const allFailed = cameras.every((cam) => failed.has(cam.cameraId));

  return (
    <div>
      <div className="d-flex flex-wrap gap-2">
        {cameras.map((cam) => {
          const isFailed = failed.has(cam.cameraId);
          return (
            <div key={cam.cameraId} className="flex-grow-1">
              {isFailed ? (
                <div
                  className="d-flex align-items-center justify-content-center rounded border bg-light text-muted small text-center px-2"
                  style={{ height: "120px" }}
                >
                  📷 Camera temporarily offline
                </div>
              ) : (
                <img
                  src={withCacheBuster(
                    proxiedCameraUrl(cam.imageUrl),
                    cacheBuster,
                  )}
                  alt={cam.description || "Roadside camera"}
                  className="img-fluid rounded border"
                  style={{
                    maxHeight: "200px",
                    objectFit: "cover",
                    width: "100%",
                  }}
                  loading="lazy"
                  onError={() =>
                    setFailed((prev) => {
                      if (prev.has(cam.cameraId)) return prev;
                      const next = new Set(prev);
                      next.add(cam.cameraId);
                      return next;
                    })
                  }
                />
              )}
              {cam.description && (
                <div className="text-muted small mt-1">{cam.description}</div>
              )}
            </div>
          );
        })}
      </div>
      {!allFailed && (
        <div className="text-muted small mt-1">
          Last refreshed: {lastUpdated.toLocaleTimeString()}
        </div>
      )}
    </div>
  );
}
