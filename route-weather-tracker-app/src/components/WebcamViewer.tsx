import type { CameraImage } from '../types/passTypes';
import { useEffect, useState } from 'react';

interface WebcamViewerProps {
  cameras: CameraImage[];
}

const REFRESH_INTERVAL_MS = 2 * 60 * 1000; // 2 minutes

export default function WebcamViewer({ cameras }: WebcamViewerProps) {
  const [cacheBuster, setCacheBuster] = useState(() => Date.now());
  const [lastUpdated, setLastUpdated] = useState(() => new Date());

  useEffect(() => {
    const timer = setInterval(() => {
      setCacheBuster(Date.now());
      setLastUpdated(new Date());
    }, REFRESH_INTERVAL_MS);
    return () => clearInterval(timer);
  }, []);

  if (cameras.length === 0) {
    return (
      <div className="text-muted small fst-italic py-2">No webcam available</div>
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
              style={{ maxHeight: '200px', objectFit: 'cover', width: '100%' }}
              onError={(e) => {
                (e.target as HTMLImageElement).style.display = 'none';
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
