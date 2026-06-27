#!/usr/bin/env python3
"""
validate_passes.py — repeatable data-accuracy check for the pass registry.

Parses route-weather-tracker-service/Data/PassRegistry.cs and validates every
pass against authoritative, key-free public sources:

  1. Coordinate sanity — compares the registry ElevationFeet against the real
     terrain elevation at the stored (lat, lon) using the Open-Meteo elevation
     API. A large mismatch means the coordinate is probably off the summit.
  2. Official URL liveness — HTTP status of each OfficialUrl.

It prints a console table and writes docs/passes-validation.md so the results
are committed and easy to diff next time.

Usage:
    python tools/validate_passes.py                # full check (network)
    python tools/validate_passes.py --no-url        # skip URL liveness checks
    python tools/validate_passes.py --elev-tolerance 600

Requires only the Python standard library.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
import urllib.request
import urllib.error
from datetime import datetime, timezone
from pathlib import Path

# Windows consoles default to cp1252; force UTF-8 so the report's symbols print.
try:
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")
except Exception:  # noqa: BLE001 - older Python / non-reconfigurable streams
    pass

REPO_ROOT = Path(__file__).resolve().parent.parent
REGISTRY = REPO_ROOT / "route-weather-tracker-service" / "Data" / "PassRegistry.cs"
OUTPUT = REPO_ROOT / "docs" / "passes-validation.md"

M_TO_FT = 3.28084
# Default tolerance: terrain elevation at a coordinate can legitimately differ
# from the "summit" figure (the point may sit just below the marked summit, and
# DEM resolution adds noise). Flag only clearly-wrong coordinates.
DEFAULT_ELEV_TOLERANCE_FT = 500


def parse_registry(text: str) -> list[dict]:
    """Extract each `new PassInfo { ... }` block into a dict of its fields."""
    passes = []
    for block in re.findall(r"new PassInfo\s*\{(.*?)\}", text, re.DOTALL):
        fields: dict[str, str] = {}
        for key, val in re.findall(r'(\w+)\s*=\s*("[^"]*"|[-\d.]+|true|false)', block):
            fields[key] = val.strip('"')
        if "Id" not in fields:
            continue
        passes.append(
            {
                "id": fields.get("Id", ""),
                "name": fields.get("Name", ""),
                "highway": fields.get("Highway", ""),
                "state": fields.get("State", ""),
                "elevation_ft": int(float(fields["ElevationFeet"])) if "ElevationFeet" in fields else None,
                "lat": float(fields["Latitude"]) if "Latitude" in fields else None,
                "lon": float(fields["Longitude"]) if "Longitude" in fields else None,
                "official_url": fields.get("OfficialUrl", ""),
                "has_official": fields.get("HasOfficialConditions", "true") == "true",
            }
        )
    return passes


def fetch_elevations(passes: list[dict]) -> dict[str, float]:
    """Batch-query Open-Meteo for terrain elevation (metres) at each coordinate."""
    located = [p for p in passes if p["lat"] is not None and p["lon"] is not None]
    if not located:
        return {}
    lats = ",".join(f"{p['lat']:.5f}" for p in located)
    lons = ",".join(f"{p['lon']:.5f}" for p in located)
    url = f"https://api.open-meteo.com/v1/elevation?latitude={lats}&longitude={lons}"
    with urllib.request.urlopen(url, timeout=60) as resp:
        data = json.load(resp)
    elevations = data.get("elevation", [])
    return {p["id"]: elevations[i] for i, p in enumerate(located) if i < len(elevations)}


def check_url(url: str) -> tuple[int | None, str]:
    """Return (status_code, note). Tries HEAD, falls back to GET."""
    if not url:
        return None, "no URL"
    # Use a browser-like UA: several DOT sites (e.g. 511.idaho.gov) serve 404 to
    # unknown agents, which would otherwise produce false "dead link" flags.
    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
        "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"
    }
    for method in ("HEAD", "GET"):
        try:
            req = urllib.request.Request(url, method=method, headers=headers)
            with urllib.request.urlopen(req, timeout=20) as resp:
                return resp.status, "ok"
        except urllib.error.HTTPError as e:
            if method == "HEAD" and e.code in (403, 405, 501):
                continue  # some servers reject HEAD; retry with GET
            return e.code, e.reason
        except Exception as e:  # noqa: BLE001 - report any transport error
            if method == "HEAD":
                continue
            return None, type(e).__name__
    return None, "unreachable"


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--no-url", action="store_true", help="skip OfficialUrl liveness checks")
    ap.add_argument("--elev-tolerance", type=int, default=DEFAULT_ELEV_TOLERANCE_FT,
                    help="elevation mismatch (ft) above which a coordinate is flagged")
    args = ap.parse_args()

    text = REGISTRY.read_text(encoding="utf-8")
    passes = parse_registry(text)
    print(f"Parsed {len(passes)} passes from {REGISTRY.name}")

    print("Fetching terrain elevations (Open-Meteo)…")
    try:
        elevations_m = fetch_elevations(passes)
    except Exception as e:  # noqa: BLE001
        print(f"  elevation fetch failed: {e}", file=sys.stderr)
        elevations_m = {}

    rows = []
    elev_flags = 0
    url_flags = 0
    for p in passes:
        actual_ft = None
        delta = None
        elev_status = "—"
        if p["id"] in elevations_m:
            actual_ft = round(elevations_m[p["id"]] * M_TO_FT)
            if p["elevation_ft"] is not None:
                delta = actual_ft - p["elevation_ft"]
                if abs(delta) > args.elev_tolerance:
                    elev_status = "⚠️ CHECK"
                    elev_flags += 1
                else:
                    elev_status = "ok"

        url_code: int | None = None
        url_note = "skipped"
        if not args.no_url:
            url_code, url_note = check_url(p["official_url"])
            if p["official_url"] and (url_code is None or url_code >= 400):
                url_flags += 1

        rows.append({**p, "actual_ft": actual_ft, "delta": delta,
                     "elev_status": elev_status, "url_code": url_code, "url_note": url_note})

        d = f"{delta:+d}" if delta is not None else "—"
        a = str(actual_ft) if actual_ft is not None else "—"
        u = "skip" if args.no_url else (str(url_code) if url_code else f"ERR({url_note})")
        print(f"  {p['id']:<20} {p['state']:<6} reg={str(p['elevation_ft']):>6} "
              f"actual={a:>6} d={d:>6} {elev_status:<8} url={u}")

    write_report(rows, args)
    print(f"\nElevation flags: {elev_flags}   URL flags: {url_flags}")
    print(f"Report written to {OUTPUT.relative_to(REPO_ROOT)}")
    return 0


def write_report(rows: list[dict], args) -> None:
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    now = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
    lines = [
        "# Pass data validation report",
        "",
        f"_Generated {now} by `tools/validate_passes.py` "
        f"(elevation tolerance ±{args.elev_tolerance} ft)._",
        "",
        "Elevation is the real terrain height at the stored coordinate "
        "(Open-Meteo DEM); a large Δ vs the registry figure means the coordinate "
        "may be off the summit. URL is the HTTP status of `OfficialUrl`.",
        "",
        "| Pass | State | Hwy | Reg ft | Actual ft | Δ ft | Coord | URL | Notes |",
        "|------|-------|-----|-------:|----------:|-----:|-------|-----|-------|",
    ]
    for r in sorted(rows, key=lambda x: (x["state"], x["id"])):
        d = f"{r['delta']:+d}" if r["delta"] is not None else "—"
        a = str(r["actual_ft"]) if r["actual_ft"] is not None else "—"
        url = "—" if args.no_url else (str(r["url_code"]) if r["url_code"] else f"ERR")
        coord = r["elev_status"]
        notes = []
        if not r["has_official"]:
            notes.append("NWS-derived")
        if not args.no_url and r["official_url"] and (r["url_code"] is None or r["url_code"] >= 400):
            notes.append(f"url {r['url_note']}")
        lines.append(
            f"| {r['name']} | {r['state']} | {r['highway']} | "
            f"{r['elevation_ft']} | {a} | {d} | {coord} | {url} | {', '.join(notes)} |"
        )
    lines.append("")
    OUTPUT.write_text("\n".join(lines), encoding="utf-8")


if __name__ == "__main__":
    raise SystemExit(main())
