import Alert from "react-bootstrap/Alert";
import type { PassSummary } from "../types/passTypes";

/** Number of days ahead to inspect for worst-case conditions */
const DAYS_AHEAD = 2;

type Severity = 0 | 1 | 2 | 3 | 4 | 5;

/**
 * Maps a weather description + OWM icon code to a severity level.
 *   5 = blizzard / heavy snow
 *   4 = snow / freezing
 *   3 = thunderstorm / heavy rain
 *   2 = rain / drizzle
 *   1 = clouds / fog / mist
 *   0 = clear
 */
function getSeverity(description: string, iconCode: string): Severity {
  const d = description.toLowerCase();
  if (d.includes("blizzard") || d.includes("heavy snow")) return 5;
  if (d.includes("snow") || d.includes("sleet") || iconCode.startsWith("13"))
    return 4;
  if (d.includes("freezing") || d.includes("ice")) return 4;
  if (d.includes("thunderstorm") || iconCode.startsWith("11")) return 3;
  if (
    d.includes("heavy rain") ||
    d.includes("rain") ||
    d.includes("drizzle") ||
    d.includes("shower") ||
    iconCode.startsWith("09") ||
    iconCode.startsWith("10")
  )
    return 2;
  if (
    d.includes("cloud") ||
    d.includes("overcast") ||
    d.includes("fog") ||
    d.includes("mist") ||
    iconCode.startsWith("04") ||
    iconCode.startsWith("50")
  )
    return 1;
  return 0;
}

function severityVariant(s: Severity): string {
  if (s >= 5) return "danger";
  if (s >= 4) return "warning";
  if (s >= 3) return "warning";
  if (s >= 2) return "info";
  return "success";
}

function severityIcon(s: Severity): string {
  if (s >= 5) return "🌨️";
  if (s >= 4) return "❄️";
  if (s >= 3) return "⛈️";
  if (s >= 2) return "🌧️";
  if (s >= 1) return "🌥️";
  return "✅";
}

function capitalize(s: string) {
  return s.charAt(0).toUpperCase() + s.slice(1);
}

interface Props {
  passes: PassSummary[];
}

export default function RouteStatus({ passes }: Props) {
  if (passes.length === 0) return null;

  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const cutoff = new Date(today);
  cutoff.setDate(cutoff.getDate() + DAYS_AHEAD);

  let worstSeverity: Severity = 0;
  let worstDescription = "clear skies";

  for (const pass of passes) {
    if (!pass.weather) continue;
    for (const day of pass.weather.dailyForecasts) {
      const date = new Date(day.date);
      if (date > cutoff) continue;
      const s = getSeverity(day.description, day.iconCode);
      if (s > worstSeverity) {
        worstSeverity = s;
        worstDescription = day.description;
      }
    }
  }

  const variant = severityVariant(worstSeverity);
  const icon = severityIcon(worstSeverity);

  let message: string;
  if (worstSeverity === 0) {
    message = "Clear conditions expected across all passes.";
  } else if (worstSeverity === 1) {
    message = `${capitalize(worstDescription)} expected on route in the next ${DAYS_AHEAD} days.`;
  } else {
    message = `${capitalize(worstDescription)} expected on route in the next ${DAYS_AHEAD} days — check conditions before travel.`;
  }

  return (
    <Alert
      variant={variant}
      className="py-2 d-flex align-items-center gap-2 mb-4"
    >
      <span role="img" aria-label="status" style={{ fontSize: "1.25rem" }}>
        {icon}
      </span>
      <div>
        <strong>Route status:</strong> {message}
      </div>
    </Alert>
  );
}
