import Card from "react-bootstrap/Card";
import Badge from "react-bootstrap/Badge";
import Alert from "react-bootstrap/Alert";
import type { PassSummary } from "../types/passTypes";
import { TravelRestriction } from "../types/passTypes";
import { formatRestriction } from "../utils/formatters";
import { useAdConfig } from "../contexts/AdContext";
import { getPassOffer } from "../utils/adContextUtils";
import WebcamViewer from "./WebcamViewer";
import WeatherDisplay from "./WeatherDisplay";
// NWS links are provided by the backend via `pass.weatherForecastUrl`.

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
  // For long advisory text, show a short label with the full text in a tooltip.
  const isLong = condition.length > 40;
  const truncated = condition.slice(0, 40);
  const lastSpace = truncated.lastIndexOf(" ");
  const label = isLong
    ? (lastSpace > 0 ? truncated.slice(0, lastSpace) : truncated) + "…"
    : condition;
  const lower = condition.toLowerCase();
  if (lower.includes("bare") || lower.includes("dry")) {
    return (
      <Badge bg="success" className="ms-1" title={isLong ? condition : undefined}>
        {label}
      </Badge>
    );
  }
  if (
    lower.includes("snow") ||
    lower.includes("ice") ||
    lower.includes("slush")
  ) {
    return (
      <Badge bg="warning" text="dark" className="ms-1" title={isLong ? condition : undefined}>
        {label}
      </Badge>
    );
  }
  return (
    <Badge bg="secondary" className="ms-1" title={isLong ? condition : undefined}>
      {label}
    </Badge>
  );
}

export default function PassCard({ pass }: PassCardProps) {
  const { info, condition, cameras, weather } = pass;
  const adConfig = useAdConfig();
  const passOffer = getPassOffer(pass, adConfig);
  const hasRestriction =
    condition &&
    (condition.eastboundRestriction !== TravelRestriction.None ||
      condition.westboundRestriction !== TravelRestriction.None);

  return (
    <Card className="mb-4 shadow-sm">
      <Card.Header className="bg-secondary bg-opacity-10">
        <div className="d-flex justify-content-between align-items-start">
          <span className="fs-5 fw-bold">{info.name}</span>
          {condition && conditionBadge(condition.roadCondition)}
        </div>
        <div className="d-flex align-items-center gap-2 flex-wrap mt-1">
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
          {pass.weatherSource === "nws" && pass.weatherForecastUrl && (
            <a
              href={pass.weatherForecastUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="badge bg-info text-decoration-none"
              title="NWS forecast"
            >
              Weather &#8599;
            </a>
          )}
        </div>
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
          {passOffer && (
            <div className="mt-2">
              <a
                href={passOffer.url}
                target="_blank"
                rel="noopener noreferrer sponsored"
                className="text-decoration-none small"
              >
                {passOffer.emoji} {passOffer.headline} &#8599;
              </a>
              {passOffer.provider === "amazon" && (
                <span
                  className="text-muted ms-2"
                  style={{ fontSize: "0.7rem" }}
                >
                  (Amazon Associate link)
                </span>
              )}
            </div>
          )}
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
