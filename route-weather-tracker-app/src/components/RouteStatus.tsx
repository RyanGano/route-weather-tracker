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

/** Join names as "A", "A and B", or "A, B, and C". */
function formatList(names: string[]): string {
  if (names.length === 0) return "";
  if (names.length === 1) return names[0];
  if (names.length === 2) return `${names[0]} and ${names[1]}`;
  return `${names.slice(0, -1).join(", ")}, and ${names[names.length - 1]}`;
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
    // Build a per-pass map of forecast entries so we only ever add one entry
    // per pass per offset (and can merge in current pass conditions for today).
    const perPass = new Map<
      number,
      { severity: Severity; high?: number; description: string }
    >();

    if (pass.weather) {
      for (const day of pass.weather.dailyForecasts) {
        const date = new Date(day.date + "T00:00:00");
        date.setHours(0, 0, 0, 0);
        const offset = Math.floor(
          (date.getTime() - today.getTime()) / MS_PER_DAY,
        );
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
      const worstRestriction = Math.max(
        eastboundRestriction,
        westboundRestriction,
      );
      let condSeverity: Severity | null = null;
      if (worstRestriction === 3)
        condSeverity = 5; // Closed
      else if (worstRestriction === 2 || worstRestriction === 1)
        condSeverity = 4; // Chains/traction

      if (condSeverity != null) {
        const condText = formatRestriction(
          worstRestriction as TravelRestriction,
          eastboundRestriction === westboundRestriction
            ? pass.condition.eastboundRestrictionText
            : `${formatRestriction(eastboundRestriction, pass.condition.eastboundRestrictionText)} / ${formatRestriction(westboundRestriction, pass.condition.westboundRestrictionText)}`,
        );
        const existing = perPass.get(0);
        if (existing) {
          // Merge: take the worse of forecast and current condition
          const mergedSeverity = Math.max(
            existing.severity,
            condSeverity,
          ) as Severity;
          const mergedDescription =
            existing.description && existing.description !== condText
              ? `${existing.description}; ${condText}`
              : condText;
          perPass.set(0, {
            severity: mergedSeverity,
            high: existing.high,
            description: mergedDescription,
          });
        } else {
          // No forecast for today for this pass — add condition-derived entry
          perPass.set(0, {
            severity: condSeverity as Severity,
            description: condText,
          });
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
        : (entries.reduce<number>(
            (m, e) => Math.max(m, e.severity),
            0,
          ) as Severity);
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

  // Offsets that have any forecast/condition data at all.
  const dataOffsets: number[] = [];
  for (let i = 0; i < LOOK_AHEAD_DAYS; i++) {
    if ((perOffset.get(i) || []).length > 0) dataOffsets.push(i);
  }

  if (slots.length === 0) return null;

  // ── Headline: what's happening RIGHT NOW ───────────────────────────────────
  // Live DOT restrictions are the most reliable and actionable signal, so they
  // drive the headline ahead of (less certain) weather forecasts. We deliberately
  // avoid calling routine winter snow "unsafe" — a snowy pass is usually driveable
  // with chains. Red is reserved for closures and severe/blizzard conditions.
  const closedNames: string[] = [];
  const chainNames: string[] = [];
  for (const p of passes) {
    if (!p.condition) continue;
    const worst = Math.max(
      p.condition.eastboundRestriction,
      p.condition.westboundRestriction,
    );
    if (worst === TravelRestriction.Closed) closedNames.push(p.info.name);
    else if (
      worst === TravelRestriction.ChainsRequired ||
      worst === TravelRestriction.TiresOrTraction
    )
      chainNames.push(p.info.name);
  }

  const nowSeverity = slots[0].severity;
  let variant: string;
  let icon: string;
  let message: string;

  if (closedNames.length > 0) {
    variant = "danger";
    icon = "🚧";
    const list = formatList(closedNames);
    message = `${list} ${closedNames.length === 1 ? "is" : "are"} closed right now — check the official pass page before heading out.`;
  } else if (nowSeverity >= 5) {
    variant = "danger";
    icon = "🌨️";
    message =
      "Severe winter weather on the passes right now — travel only if necessary.";
  } else if (chainNames.length > 0) {
    variant = "warning";
    icon = "❄️";
    message = `Chains or traction tires required on ${formatList(chainNames)} — doable with the right gear.`;
  } else if (nowSeverity >= 4) {
    variant = "warning";
    icon = "❄️";
    message =
      "Snow or ice around the passes right now — carry chains and take it slow.";
  } else if (nowSeverity >= BAD_THRESHOLD) {
    variant = "warning";
    icon = "⛈️";
    message =
      "Thunderstorms near the passes right now — watch for sudden downpours.";
  } else if (nowSeverity >= 2) {
    variant = "success";
    icon = "🌧️";
    message = "Wet but open — roads may be slick, but the passes are driveable.";
  } else {
    variant = "success";
    icon = "✅";
    message = "Passes are clear right now — good to go.";
  }

  // ── Secondary line: the calmest day(s) to cross this week ────────────────────
  // Always frames a positive recommendation (the least-severe days) instead of
  // declaring days "unsafe", which is unhelpful for mountain passes in winter.
  let windowLine: string | null = null;
  if (dataOffsets.length > 0) {
    const bestSeverity = Math.min(...dataOffsets.map((i) => slots[i].severity));
    // Treat anything up to light rain as equally fine; otherwise surface the
    // least-bad days available.
    const threshold = Math.max(bestSeverity, 2);
    const calm = dataOffsets.filter((i) => slots[i].severity <= threshold);
    if (calm.length === LOOK_AHEAD_DAYS && bestSeverity <= 2) {
      windowLine =
        "The whole week looks good — pack snacks and go whenever suits you.";
    } else if (bestSeverity >= 4) {
      windowLine = `Wintry all week. Calmest days to cross: ${formatOffsets(calm.slice(0, 3), slots)}.`;
    } else {
      windowLine = `Calmest days to cross this week: ${formatOffsets(calm.slice(0, 3), slots)}.`;
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
        className="py-2 d-flex align-items-start gap-2 mb-1"
      >
        <span role="img" aria-label="status" style={{ fontSize: "1.25rem" }}>
          {icon}
        </span>
        <div>
          <div>
            <strong>Right now:</strong> {message}
          </div>
          {windowLine && (
            <div className="small mt-1 opacity-75">📅 {windowLine}</div>
          )}
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
