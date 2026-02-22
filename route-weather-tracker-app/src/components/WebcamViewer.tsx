import type { CameraImage } from "../types/passTypes";
import { useRefresh } from "../contexts/RefreshContext";

interface WebcamViewerProps {
  cameras: CameraImage[];
}

export default function WebcamViewer({ cameras }: WebcamViewerProps) {
  const { cacheBuster, lastUpdated } = useRefresh();

  if (cameras.length === 0) {
    return (
      <div className="text-muted small fst-italic py-2">
        No webcam available
      </div>
    );
  }

  return (
    <div>
      <div className="d-flex flex-wrap gap-2">
        {cameras.map((cam) => (
          <div key={cam.cameraId} className="flex-grow-1">
            <img
              src={`${cam.imageUrl}?t=${cacheBuster}`}
              alt={cam.description}
              className="img-fluid rounded border"
              style={{ maxHeight: "200px", objectFit: "cover", width: "100%" }}
              onError={(e) => {
                (e.target as HTMLImageElement).style.display = "none";
              }}
            />
            <div className="text-muted small mt-1">{cam.description}</div>
          </div>
        ))}
      </div>
      <div className="text-muted small mt-1">
        Last refreshed: {lastUpdated.toLocaleTimeString()}
      </div>
    </div>
  );
}
