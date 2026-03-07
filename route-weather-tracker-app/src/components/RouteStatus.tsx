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

// (removed unused `capitalize`) Kept inline text formatting in messages.

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
    // Build a per-pass map of forecast entries so we only ever add one entry
    // per pass per offset (and can merge in current pass conditions for today).
    const perPass = new Map<number, { severity: Severity; high?: number; description: string }>();

    if (pass.weather) {
      for (const day of pass.weather.dailyForecasts) {
        const date = new Date(day.date + "T00:00:00");
        date.setHours(0, 0, 0, 0);
        const offset = Math.floor((date.getTime() - today.getTime()) / MS_PER_DAY);
        if (offset < 0 || offset >= LOOK_AHEAD_DAYS) continue;
        const s = getSeverity(day.description, day.iconCode);
        perPass.set(offset, {
          severity: s,
          high: day.highFahrenheit,
          description: day.description,
        });
      }
    }

    // If we have current DOT/pass conditions, fold them into today's severity.
    if (pass.condition) {
      const { eastboundRestriction, westboundRestriction } = pass.condition;
      const worstRestriction = Math.max(eastboundRestriction, westboundRestriction);
      let condSeverity: Severity | null = null;
      if (worstRestriction === 3) condSeverity = 5; // Closed
      else if (worstRestriction === 2 || worstRestriction === 1) condSeverity = 4; // Chains/traction

      if (condSeverity != null) {
        const condText = formatRestriction(worstRestriction as TravelRestriction,
          eastboundRestriction === westboundRestriction ? pass.condition.eastboundRestrictionText : `${formatRestriction(eastboundRestriction, pass.condition.eastboundRestrictionText)} / ${formatRestriction(westboundRestriction, pass.condition.westboundRestrictionText)}`
        );
        const existing = perPass.get(0);
        if (existing) {
          // Merge: take the worse of forecast and current condition
          const mergedSeverity = Math.max(existing.severity, condSeverity) as Severity;
          const mergedDescription = existing.description && existing.description !== condText ? `${existing.description}; ${condText}` : condText;
          perPass.set(0, { severity: mergedSeverity, high: existing.high, description: mergedDescription });
        } else {
          // No forecast for today for this pass — add condition-derived entry
          perPass.set(0, { severity: condSeverity as Severity, description: condText });
        }
      }
    }

    // Finally push each per-pass/offset entry into the global map
    for (const [offset, entry] of perPass.entries()) {
      perOffset.get(offset)!.push({
        passId: pass.info.id,
        severity: entry.severity,
        high: entry.high,
        description: entry.description,
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
        : (entries.reduce<number>((m, e) => Math.max(m, e.severity), 0) as Severity);
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
  const driveableOffsets: number[] = []; // every pass has severity < BAD_THRESHOLD (and every pass provided data)
  const avgHighByOffset = new Map<number, number>();
  for (let i = 0; i < LOOK_AHEAD_DAYS; i++) {
    const entries = perOffset.get(i) || [];
    // Consider an offset for recommendations when at least one pass provided data.
    // This is less strict than requiring every pass to provide a forecast,
    // but still avoids suggesting a day with no information.
    if (entries.length > 0) {
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

  // Prioritize explicit safe-day messaging (grouped). We consider a day "safe"
  // only when every pass provided data and each pass reports severity < BAD_THRESHOLD.
  if (driveableOffsets.length === LOOK_AHEAD_DAYS) {
    variant = "success";
    icon = "✅";
    message = "All days look great — pack snacks and go anytime this week!";
  } else if (driveableOffsets.length > 0) {
    const formatted = formatOffsets(driveableOffsets.slice(0, 3), slots);
    variant = badNow ? severityVariant(nowSlot.severity) : "success";
    icon = badNow ? severityIcon(nowSlot.severity) : "✅";
    message = badNow
      ? `Currently poor — best safe windows: ${formatted}.`
      : `${formatted} ${driveableOffsets.length === 1 ? "is" : "are"} safe to drive across all passes.`;
  } else {
    // No explicit safe days found
    variant = badNow ? severityVariant(nowSlot.severity) : "warning";
    icon = badNow ? severityIcon(nowSlot.severity) : "⚠️";
    message = `No days in the next ${LOOK_AHEAD_DAYS} days are considered safe to drive across all passes based on current road conditions and forecasts.`;
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
