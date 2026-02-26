import Alert from "react-bootstrap/Alert";
import Badge from "react-bootstrap/Badge";
import type { PassSummary } from "../types/passTypes";
import { TravelRestriction } from "../types/passTypes";
import { formatRestriction } from "../utils/formatters";

/** Days ahead to scan for drive-planning advice */
const LOOK_AHEAD_DAYS = 7;
const MS_PER_DAY = 24 * 60 * 60 * 1000;

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
  if (s >= 5) return "danger";
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

/**
 * Turn a list of numeric offsets into a readable, combined string.
 * - Consecutive runs of 3+ days -> "Start through End"
 * - Runs of 2 -> "Start or End"
 * - Single days -> "Day"
 * Multiple disjoint runs are joined with commas and an Oxford ", or ".
 */
function formatOffsets(offsets: number[], slots: DaySlot[]): string {
  if (!offsets || offsets.length === 0) return "";
  const sorted = [...offsets].sort((a, b) => a - b);
  const runs: number[][] = [];
  let run: number[] = [sorted[0]];
  for (let i = 1; i < sorted.length; i++) {
    if (sorted[i] === sorted[i - 1] + 1) {
      run.push(sorted[i]);
    } else {
      runs.push(run);
      run = [sorted[i]];
    }
  }
  runs.push(run);

  const parts = runs.map((r) => {
    if (r.length >= 3) {
      return `${dayLabel(r[0], slots[r[0]].date)} through ${dayLabel(r[r.length - 1], slots[r[r.length - 1]].date)}`;
    }
    if (r.length === 2) {
      return `${dayLabel(r[0], slots[r[0]].date)} or ${dayLabel(r[1], slots[r[1]].date)}`;
    }
    return dayLabel(r[0], slots[r[0]].date);
  });

  if (parts.length === 1) return parts[0];
  if (parts.length === 2) return `${parts[0]} or ${parts[1]}`;
  return `${parts.slice(0, -1).join(", ")}, or ${parts[parts.length - 1]}`;
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

  // Build per-offset, per-pass data so we can find days where ALL passes are good
  const perOffset = new Map<
    number,
    Array<{
      passId: string;
      severity: Severity;
      high?: number;
      description: string;
    }>
  >();
  for (let i = 0; i < LOOK_AHEAD_DAYS; i++) perOffset.set(i, []);

  for (const pass of passes) {
    if (!pass.weather) continue;
    for (const day of pass.weather.dailyForecasts) {
      const date = new Date(day.date + "T00:00:00");
      date.setHours(0, 0, 0, 0);
      const offset = Math.round(
        (date.getTime() - today.getTime()) / MS_PER_DAY,
      );
      if (offset < 0 || offset >= LOOK_AHEAD_DAYS) continue;
      const s = getSeverity(day.description, day.iconCode);
      perOffset.get(offset)!.push({
        passId: pass.info.id,
        severity: s,
        high: day.highFahrenheit,
        description: day.description,
      });
    }
  }

  // Create slots with aggregated info (worst severity, avg high, and whether every pass had data)
  const slots: DaySlot[] = Array.from({ length: LOOK_AHEAD_DAYS }, (_, i) => {
    const date = new Date(today);
    date.setDate(today.getDate() + i);
    const entries = perOffset.get(i) || [];
    const severity =
      entries.length === 0
        ? 0
        : (entries.reduce((m, e) => Math.max(m, e.severity), 0) as Severity);
    const worstDescription =
      entries.length === 0
        ? "clear skies"
        : entries.reduce(
            (a, b) =>
              getSeverity(a, "") >= getSeverity(b.description, "")
                ? a
                : b.description,
            entries[0].description,
          );
    return {
      date,
      offset: i,
      severity,
      worstDescription,
    };
  });

  // Helper collections for multi-option recommendations
  const allClearOffsets: number[] = []; // no snow (severity < 4) on every pass and every pass has data
  const driveableOffsets: number[] = []; // every pass has severity < BAD_THRESHOLD
  const avgHighByOffset = new Map<number, number>();
  for (let i = 0; i < LOOK_AHEAD_DAYS; i++) {
    const entries = perOffset.get(i) || [];
    if (entries.length === passes.length && entries.length > 0) {
      const allNoSnow = entries.every((e) => e.severity < 4);
      const allDriveable = entries.every((e) => e.severity < BAD_THRESHOLD);
      if (allNoSnow) allClearOffsets.push(i);
      if (allDriveable) driveableOffsets.push(i);
      const avgHigh =
        entries.reduce((s, e) => s + (e.high ?? 0), 0) / entries.length;
      avgHighByOffset.set(i, avgHigh);
    }
  }

  if (slots.length === 0) return null;

  const nowSlot = slots[0];
  const badNow = nowSlot.severity >= BAD_THRESHOLD;

  let variant: string;
  let icon: string;
  let message: string;

  if (!badNow) {
    // If there are days where every pass reports no snow, prefer those
    if (allClearOffsets.length > 0) {
      const formatted = formatOffsets(allClearOffsets.slice(0, 3), slots);
      variant = "success";
      icon = "✅";
      message = `Looks good on ${formatted} — all passes show no snow.`;
    } else if (driveableOffsets.length > 0) {
      const formatted = formatOffsets(driveableOffsets.slice(0, 3), slots);
      variant = "success";
      icon = "✅";
      message =
        driveableOffsets.length === 1
          ? `${formatted} looks like a good day to drive across all passes.`
          : `${formatted} look like good days to drive across all passes.`;
    } else {
      // No fully-clear day across all passes — fall back to when conditions turn bad
      const nextBad = slots.slice(1).find((s) => s.severity >= BAD_THRESHOLD);
      if (!nextBad) {
        variant = "info";
        icon = "✅";
        message = "No major storms expected — use your judgment for timing.";
      } else if (nextBad.offset === 1) {
        variant = "warning";
        icon = severityIcon(nextBad.severity);
        message = `Drive today — ${capitalize(nextBad.worstDescription)} arrives tomorrow.`;
      } else {
        variant = nextBad.severity >= 4 ? "warning" : "info";
        icon = severityIcon(nextBad.severity);
        message = `Drive before ${dayLabel(nextBad.offset, nextBad.date)} — ${capitalize(nextBad.worstDescription)} expected ${dayLabel(nextBad.offset, nextBad.date)} onward.`;
      }
    }
  } else {
    // Currently bad — look for the best windows (when all passes improve or the warmest options)
    variant = severityVariant(nowSlot.severity);
    icon = severityIcon(nowSlot.severity);
    if (driveableOffsets.length > 0) {
      const formatted = formatOffsets(driveableOffsets.slice(0, 3), slots);
      message = `Poor conditions now — plan for ${formatted}.`;
    } else if (allClearOffsets.length > 0) {
      const formatted = formatOffsets(allClearOffsets.slice(0, 3), slots);
      message =
        allClearOffsets.length === 1
          ? `Current conditions are poor — ${formatted} looks best (no snow across passes).`
          : `Current conditions are poor — ${formatted} look best (no snow across passes).`;
    } else {
      // Suggest the warmest offsets available (by average high)
      const candidateOffsets = Array.from(avgHighByOffset.entries())
        .sort((a, b) => b[1] - a[1])
        .slice(0, 2)
        .map(([o]) => o);
      if (candidateOffsets.length > 0) {
        const formatted = formatOffsets(candidateOffsets, slots);
        message = `Conditions are poor now — warmer windows may be ${formatted}.`;
      } else {
        const leastBad = slots.reduce((a, b) =>
          b.severity < a.severity ? b : a,
        );
        message = `Challenging conditions forecast all week — ${dayLabel(leastBad.offset, leastBad.date)} may be the best window.`;
      }
    }
  }

  // Collect passes with active restrictions for the overview banner
  const restrictedPasses = passes
    .filter(
      (p) =>
        p.condition &&
        (p.condition.eastboundRestriction !== TravelRestriction.None ||
          p.condition.westboundRestriction !== TravelRestriction.None),
    )
    .map((p) => {
      const eb = p.condition!.eastboundRestriction;
      const wb = p.condition!.westboundRestriction;
      const ebText = p.condition!.eastboundRestrictionText;
      const wbText = p.condition!.westboundRestrictionText;

      // Build a concise per-pass restriction description
      const sameRestriction = eb === wb && ebText === wbText;

      const detail = sameRestriction
        ? formatRestriction(eb, ebText)
        : `EB: ${formatRestriction(eb, ebText)} / WB: ${formatRestriction(wb, wbText)}`;

      return { name: p.info.name, detail };
    });

  return (
    <>
      <Alert
        variant={variant}
        className="py-2 d-flex align-items-center gap-2 mb-1"
      >
        <span role="img" aria-label="status" style={{ fontSize: "1.25rem" }}>
          {icon}
        </span>
        <div>
          <strong>Best time to drive:</strong> {message}
        </div>
      </Alert>

      <p className="text-muted mb-3" style={{ fontSize: "0.72rem" }}>
        This site is for informational purposes only. Always use your best
        judgment when deciding when to travel.
      </p>

      {restrictedPasses.length > 0 && (
        <Alert variant="warning" className="py-2 mb-4">
          <span className="me-2">⚠️</span>
          <strong>Active Restrictions:</strong>{" "}
          {restrictedPasses.map((rp, i) => (
            <span key={rp.name}>
              {i > 0 && <span className="mx-1 text-muted">·</span>}
              <span className="fw-semibold">{rp.name}</span>
              {rp.detail && (
                <>
                  {" — "}
                  <Badge bg="warning" text="dark">
                    {rp.detail}
                  </Badge>
                </>
              )}
            </span>
          ))}
        </Alert>
      )}
    </>
  );
}
