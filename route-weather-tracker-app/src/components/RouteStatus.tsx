import Alert from "react-bootstrap/Alert";
import type { PassSummary } from "../types/passTypes";

/** Days ahead to scan for drive-planning advice */
const LOOK_AHEAD_DAYS = 7;

/**
 * Conditions at or above this threshold are considered "bad" for driving
 * (snow, ice, thunderstorms). Rain/clouds alone don't block a drive.
 *   5 = blizzard / heavy snow
 *   4 = snow / sleet / freezing
 *   3 = thunderstorm / heavy rain
 *   2 = rain / drizzle  ← below threshold: driveable
 *   1 = clouds / fog
 *   0 = clear
 */
type Severity = 0 | 1 | 2 | 3 | 4 | 5;
const BAD_THRESHOLD: Severity = 3;

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
  if (s >= 4) return "warning";
  if (s >= 3) return "warning";
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

/** "today", "tomorrow", or weekday name for offsets ≥ 2 */
function dayLabel(offset: number, date: Date): string {
  if (offset === 0) return "today";
  if (offset === 1) return "tomorrow";
  return date.toLocaleDateString("en-US", { weekday: "long" });
}

interface DaySlot {
  date: Date;
  offset: number;
  severity: Severity;
  worstDescription: string;
}

interface Props {
  passes: PassSummary[];
}

export default function RouteStatus({ passes }: Props) {
  if (passes.length === 0) return null;

  const today = new Date();
  today.setHours(0, 0, 0, 0);

  // Aggregate worst severity per day-offset across all passes
  const byOffset = new Map<number, { severity: Severity; description: string }>();
  for (const pass of passes) {
    if (!pass.weather) continue;
    for (const day of pass.weather.dailyForecasts) {
      const date = new Date(day.date);
      date.setHours(0, 0, 0, 0);
      const offset = Math.round(
        (date.getTime() - today.getTime()) / 86_400_000
      );
      if (offset < 0 || offset >= LOOK_AHEAD_DAYS) continue;
      const s = getSeverity(day.description, day.iconCode);
      const cur = byOffset.get(offset);
      if (!cur || s > cur.severity)
        byOffset.set(offset, { severity: s, description: day.description });
    }
  }

  const slots: DaySlot[] = Array.from({ length: LOOK_AHEAD_DAYS }, (_, i) => {
    const date = new Date(today);
    date.setDate(today.getDate() + i);
    const data = byOffset.get(i) ?? { severity: 0 as Severity, description: "clear skies" };
    return { date, offset: i, severity: data.severity as Severity, worstDescription: data.description };
  });

  if (slots.length === 0) return null;

  const nowSlot = slots[0];
  const badNow = nowSlot.severity >= BAD_THRESHOLD;

  let variant: string;
  let icon: string;
  let message: string;

  if (!badNow) {
    // Conditions are currently driveable — find when they turn bad
    const nextBad = slots.slice(1).find((s) => s.severity >= BAD_THRESHOLD);
    if (!nextBad) {
      variant = "success";
      icon = "✅";
      message = "Clear conditions forecast all week — good to drive anytime.";
    } else if (nextBad.offset === 1) {
      variant = "warning";
      icon = severityIcon(nextBad.severity);
      message = `Drive today — ${capitalize(nextBad.worstDescription)} arrives tomorrow.`;
    } else {
      variant = nextBad.severity >= 4 ? "warning" : "info";
      icon = severityIcon(nextBad.severity);
      message = `Drive before ${dayLabel(nextBad.offset, nextBad.date)} — ${capitalize(nextBad.worstDescription)} expected ${dayLabel(nextBad.offset, nextBad.date)} onward.`;
    }
  } else {
    // Conditions are currently bad — find when they improve
    variant = severityVariant(nowSlot.severity);
    icon = severityIcon(nowSlot.severity);
    const nextGood = slots.slice(1).find((s) => s.severity < BAD_THRESHOLD);
    if (!nextGood) {
      const leastBad = slots.reduce((a, b) => (b.severity < a.severity ? b : a));
      message = `Challenging conditions forecast all week — ${dayLabel(leastBad.offset, leastBad.date)} may be the best window.`;
    } else if (nextGood.offset === 1) {
      message = `${capitalize(nowSlot.worstDescription)} today — conditions improve tomorrow.`;
    } else {
      const lastBad = slots[nextGood.offset - 1];
      message = `${capitalize(nowSlot.worstDescription)} expected through ${dayLabel(lastBad.offset, lastBad.date)} — plan to drive ${dayLabel(nextGood.offset, nextGood.date)} or later.`;
    }
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
        <strong>Best time to drive:</strong> {message}
      </div>
    </Alert>
  );
}
