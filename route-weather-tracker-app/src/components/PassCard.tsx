import Card from "react-bootstrap/Card";
import Badge from "react-bootstrap/Badge";
import Alert from "react-bootstrap/Alert";
import type { PassSummary } from "../types/passTypes";
import { TravelRestriction } from "../types/passTypes";
import { formatRestriction } from "../utils/formatters";
import WebcamViewer from "./WebcamViewer";
import WeatherDisplay from "./WeatherDisplay";

interface PassCardProps {
  pass: PassSummary;
}

function restrictionBadge(restriction: TravelRestriction, text?: string) {
  const label = formatRestriction(restriction, text);
  switch (restriction) {
    case TravelRestriction.Closed:
      return <Badge bg="danger">{label}</Badge>;
    case TravelRestriction.ChainsRequired:
    case TravelRestriction.TiresOrTraction:
      return (
        <Badge bg="warning" text="dark">
          {label}
        </Badge>
      );
    default:
      return <Badge bg="success">{label}</Badge>;
  }
}

function conditionBadge(condition: string | undefined) {
  if (!condition) return null;
  const lower = condition.toLowerCase();
  if (lower.includes("bare") || lower.includes("dry")) {
    return (
      <Badge bg="success" className="ms-1">
        {condition}
      </Badge>
    );
  }
  if (
    lower.includes("snow") ||
    lower.includes("ice") ||
    lower.includes("slush")
  ) {
    return (
      <Badge bg="warning" text="dark" className="ms-1">
        {condition}
      </Badge>
    );
  }
  return (
    <Badge bg="secondary" className="ms-1">
      {condition}
    </Badge>
  );
}

export default function PassCard({ pass }: PassCardProps) {
  const { info, condition, cameras, weather } = pass;
  const hasRestriction =
    condition &&
    (condition.eastboundRestriction !== TravelRestriction.None ||
      condition.westboundRestriction !== TravelRestriction.None);

  return (
    <Card className="mb-4 shadow-sm">
      <Card.Header className="d-flex justify-content-between align-items-center bg-secondary bg-opacity-10">
        <div className="d-flex align-items-center gap-2 flex-wrap">
          <span className="fs-5 fw-bold">{info.name}</span>
          <Badge bg="secondary">{info.highway}</Badge>
          <Badge bg="light" text="dark">
            {info.elevationFeet.toLocaleString()} ft
          </Badge>
          <Badge bg="dark">{info.state}</Badge>
          {info.officialUrl && (
            <a
              href={info.officialUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="badge bg-primary text-decoration-none"
              title="Official pass conditions page"
            >
              Official &#8599;
            </a>
          )}
        </div>
        {condition && conditionBadge(condition.roadCondition)}
      </Card.Header>

      {hasRestriction && (
        <Alert
          variant="warning"
          className="mb-0 rounded-0 py-2 px-3 border-0 border-bottom"
        >
          <strong>Travel Restrictions</strong>
          <div className="d-flex flex-wrap gap-3 mt-1 small">
            <span>
              <span className="fw-semibold">EB:</span>{" "}
              {restrictionBadge(
                condition!.eastboundRestriction,
                condition!.eastboundRestrictionText || undefined,
              )}
            </span>
            <span>
              <span className="fw-semibold">WB:</span>{" "}
              {restrictionBadge(
                condition!.westboundRestriction,
                condition!.westboundRestrictionText || undefined,
              )}
            </span>
          </div>
        </Alert>
      )}

      <Card.Body>
        <div className="row g-3">
          {/* Webcam column */}
          <div className="col-12 col-lg-6">
            <h6
              className="text-muted text-uppercase fw-semibold mb-2"
              style={{ fontSize: "0.7rem", letterSpacing: "0.08em" }}
            >
              Live Camera
            </h6>
            <WebcamViewer cameras={cameras} />
          </div>

          {/* Weather column */}
          <div className="col-12 col-lg-6">
            <h6
              className="text-muted text-uppercase fw-semibold mb-2"
              style={{ fontSize: "0.7rem", letterSpacing: "0.08em" }}
            >
              Weather
            </h6>
            <WeatherDisplay weather={weather} />
            {condition && (
              <div className="mt-2 text-muted small">
                &#127777; {condition.temperatureFahrenheit}°F &nbsp;|&nbsp;
                {condition.weatherCondition} &nbsp;|&nbsp; Updated:{" "}
                {new Date(condition.lastUpdated).toLocaleTimeString()}
              </div>
            )}
          </div>
        </div>
      </Card.Body>
    </Card>
  );
}
