"""
Batch site-fidelity across unique bookmark hosts.

Usage:
  python bookmark_fidelity_batch.py seeds.json
  python bookmark_fidelity_batch.py seeds.json --max-pages=5 --max-pct=5 --timeout=480 --limit=10
  # or from converter/: npm run bookmark-fidelity --

Writes:
  Tool/HtmlToGum/.bookmark-fidelity/
    run-log.jsonl
    meta-report.json
    meta-report.html
    sites/<host-slug>/   (symlink/copy of site-fidelity out + site-summary.json)
"""
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import urlparse

FIDELITY = Path(__file__).resolve().parent
CONVERTER = FIDELITY.parent / "converter"
HTML_TO_GUM = FIDELITY.parent
SITE_FIDELITY_ROOT = HTML_TO_GUM / ".site-fidelity"
OUT_ROOT = HTML_TO_GUM / ".bookmark-fidelity"


def slug_host(host: str) -> str:
    return re.sub(r"[^a-zA-Z0-9._-]+", "-", host)[:80]


def find_latest_report_for_seed(seed: str) -> Path | None:
    """site-fidelity writes .site-fidelity/<slug-from-seed>/report.json"""
    u = urlparse(seed)
    path = (u.path or "/").replace("//", "/").strip("/") or "root"
    slug = f"{u.hostname}-{path}"
    slug = re.sub(r"[^a-zA-Z0-9._-]+", "-", slug)[:80]
    candidate = SITE_FIDELITY_ROOT / slug / "report.json"
    if candidate.exists():
        return candidate
    # Fallback: newest report.json whose seed matches
    if not SITE_FIDELITY_ROOT.exists():
        return None
    matches = []
    for p in SITE_FIDELITY_ROOT.glob("*/report.json"):
        try:
            data = json.loads(p.read_text(encoding="utf-8"))
        except Exception:
            continue
        if data.get("seed") == seed:
            matches.append((p.stat().st_mtime, p))
    if not matches:
        return None
    matches.sort(reverse=True)
    return matches[0][1]


def summarize_site(seed_entry: dict, report_path: Path | None, status: str, error: str | None, elapsed: float) -> dict:
    pages = []
    passed = failed = errored = 0
    pcts = []
    abort_reason = None
    report_status = None
    if report_path and report_path.exists():
        report = json.loads(report_path.read_text(encoding="utf-8"))
        report_status = report.get("status")
        abort_reason = report.get("abortReason")
        for p in report.get("pages") or []:
            st = p.get("status")
            pct = p.get("pct")
            pages.append({
                "url": p.get("url"),
                "screen": p.get("screen"),
                "status": st,
                "pct": pct,
            })
            if st == "pass":
                passed += 1
                if pct is not None:
                    pcts.append(pct)
            elif st == "fail":
                failed += 1
                if pct is not None:
                    pcts.append(pct)
            elif st == "rejected":
                errored += 1
            else:
                errored += 1
        site_status = status
        if status == "ok":
            if report_status == "rejected" or (len(pages) == 0 and abort_reason):
                site_status = "rejected"
            elif len(pages) == 0:
                # Empty crawl with no explicit abort — still not a fidelity fail.
                site_status = "rejected"
                abort_reason = abort_reason or "no pages discovered"
            else:
                site_status = "pass" if failed == 0 and errored == 0 and passed > 0 else "fail"
        if abort_reason and not error:
            error = abort_reason
    else:
        site_status = status
        report = None

    return {
        "host": seed_entry["host"],
        "title": seed_entry.get("title"),
        "seed": seed_entry["url"],
        "status": site_status,
        "error": error,
        "elapsedSec": round(elapsed, 1),
        "reportPath": str(report_path) if report_path else None,
        "passed": passed,
        "failed": failed,
        "errored": errored,
        "pageCount": len(pages),
        "meanPct": round(sum(pcts) / len(pcts), 3) if pcts else None,
        "maxPct": round(max(pcts), 3) if pcts else None,
        "minPct": round(min(pcts), 3) if pcts else None,
        "pages": pages,
        "rawReport": report,
    }


def render_site_html(summary: dict) -> str:
    rows = []
    for p in summary.get("pages") or []:
        pct = "—" if p.get("pct") is None else f"{p['pct']:.2f}%"
        rows.append(
            f"<tr class='{p.get('status')}'><td>{p.get('screen')}</td>"
            f"<td><span class='pill'>{p.get('status')}</span></td>"
            f"<td>{pct}</td><td class='url'>{p.get('url')}</td></tr>"
        )
    mean = "—" if summary.get("meanPct") is None else f"{summary['meanPct']:.2f}%"
    err = summary.get("error") or ""
    return f"""<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8"/>
<title>{summary['host']} — HtmlToGum fidelity</title>
<style>
  body {{ font-family: Segoe UI, system-ui, sans-serif; background:#0b0d12; color:#e8ecf5; margin:0; padding:32px; }}
  h1 {{ font-weight:600; margin:0 0 6px; }}
  .sub {{ color:#9aa3b5; margin-bottom:20px; word-break:break-all; }}
  .stat {{ display:inline-block; border:1px solid #243044; background:#121826; padding:8px 12px; border-radius:8px; margin:0 8px 8px 0; font-size:13px; }}
  .stat strong {{ color:#feff89; }}
  table {{ width:100%; border-collapse:collapse; font-size:13px; margin-top:16px; }}
  th, td {{ text-align:left; padding:8px 10px; border-bottom:1px solid #1e2438; }}
  th {{ color:#9aa3b5; }}
  .pill {{ font-size:11px; padding:2px 8px; border-radius:999px; background:#1e2438; }}
  tr.pass .pill {{ background:#14301f; color:#6ddea0; }}
  tr.fail .pill {{ background:#3a1a1a; color:#ff9b9b; }}
  .url {{ color:#9aa3b5; word-break:break-all; }}
  .err {{ color:#ffd27a; margin-top:12px; white-space:pre-wrap; }}
</style></head><body>
<h1>{summary['host']}</h1>
<p class="sub">{summary.get('title') or ''}<br/>{summary.get('seed')}</p>
<div>
  <span class="stat">status <strong>{summary.get('status')}</strong></span>
  <span class="stat">pages <strong>{summary.get('passed',0)} pass</strong> / {summary.get('failed',0)} fail / {summary.get('pageCount',0)} total</span>
  <span class="stat">mean <strong>{mean}</strong></span>
  <span class="stat">{summary.get('elapsedSec',0)}s</span>
</div>
{"<p class='err'>" + err.replace("<","&lt;") + "</p>" if err else ""}
<table><thead><tr><th>Screen</th><th>Status</th><th>Diff %</th><th>URL</th></tr></thead>
<tbody>{''.join(rows) or "<tr><td colspan=4>No pages converted</td></tr>"}</tbody></table>
</body></html>"""


def write_meta(out_root: Path, run: dict, sites: list[dict]) -> None:
    scored = [s for s in sites if s.get("meanPct") is not None]
    finished = [s for s in sites if s["status"] not in ("pending", "running")]

    def slim_site(s: dict) -> dict:
        out = {k: v for k, v in s.items() if k != "rawReport"}
        return out

    meta = {
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "run": run,
        "totals": {
            "seeds": len(sites),
            "finished": len(finished),
            "passSites": sum(1 for s in sites if s["status"] == "pass"),
            "failSites": sum(1 for s in sites if s["status"] == "fail"),
            "rejectedSites": sum(1 for s in sites if s["status"] == "rejected"),
            "errorSites": sum(1 for s in sites if s["status"] in ("error", "timeout")),
            "pagesPassed": sum(s.get("passed", 0) for s in sites),
            "pagesFailed": sum(s.get("failed", 0) for s in sites),
            "pagesErrored": sum(s.get("errored", 0) for s in sites),
            "meanPctAcrossScoredPages": round(
                sum(s["meanPct"] * max(s["pageCount"], 1) for s in scored)
                / max(sum(max(s["pageCount"], 1) for s in scored), 1),
                3,
            ) if scored else None,
            "bestSite": slim_site(min(scored, key=lambda s: s["meanPct"])) if scored else None,
            "worstSite": slim_site(max(scored, key=lambda s: s["meanPct"])) if scored else None,
        },
        "sites": [slim_site(s) for s in sites],
    }

    (out_root / "meta-report.json").write_text(json.dumps(meta, indent=2), encoding="utf-8")
    (out_root / "meta-report.html").write_text(render_meta_html(meta), encoding="utf-8")


def render_meta_html(meta: dict) -> str:
    t = meta["totals"]
    rows = []
    for s in meta["sites"]:
        mean = "—" if s.get("meanPct") is None else f"{s['meanPct']:.2f}%"
        mx = "—" if s.get("maxPct") is None else f"{s['maxPct']:.2f}%"
        err = (s.get("error") or "")[:120]
        passed = s.get("passed", 0)
        failed = s.get("failed", 0)
        errored = s.get("errored", 0)
        page_count = s.get("pageCount", 0)
        # "0/1" looked like no measurement; spell out pass/fail so a scored fail
        # (with mean/max %) is obvious.
        if page_count == 0:
            pages_cell = "—"
        else:
            parts = [f"{passed} pass"]
            if failed:
                parts.append(f"{failed} fail")
            if errored:
                parts.append(f"{errored} err")
            pages_cell = ", ".join(parts)
        rows.append(
            f"<tr class='{s['status']}'>"
            f"<td>{s['host']}</td>"
            f"<td><span class='pill'>{s['status']}</span></td>"
            f"<td>{pages_cell}</td>"
            f"<td>{mean}</td><td>{mx}</td>"
            f"<td>{s.get('elapsedSec',0)}s</td>"
            f"<td class='err'>{err}</td>"
            f"</tr>"
        )
    best = t.get("bestSite")
    worst = t.get("worstSite")
    best_s = f"{best['host']} ({best['meanPct']:.2f}%)" if best else "—"
    worst_s = f"{worst['host']} ({worst['meanPct']:.2f}%)" if worst else "—"
    return f"""<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8"/>
<title>HtmlToGum bookmark fidelity — meta</title>
<style>
  body {{ font-family: Segoe UI, system-ui, sans-serif; background:#0b0d12; color:#e8ecf5; margin:0; padding:32px; }}
  h1 {{ font-weight:600; margin:0 0 8px; }}
  .sub {{ color:#9aa3b5; margin-bottom:24px; }}
  .stats {{ display:flex; flex-wrap:wrap; gap:10px; margin-bottom:28px; }}
  .stat {{ border:1px solid #243044; background:#121826; padding:10px 14px; border-radius:8px; font-size:13px; }}
  .stat strong {{ color:#feff89; }}
  table {{ width:100%; border-collapse:collapse; font-size:13px; }}
  th, td {{ text-align:left; padding:8px 10px; border-bottom:1px solid #1e2438; vertical-align:top; }}
  th {{ color:#9aa3b5; font-weight:600; }}
  .pill {{ font-size:11px; padding:2px 8px; border-radius:999px; background:#1e2438; }}
  tr.pass .pill {{ background:#14301f; color:#6ddea0; }}
  tr.fail .pill {{ background:#3a1a1a; color:#ff9b9b; }}
  tr.rejected .pill {{ background:#1a243a; color:#9bb8ff; }}
  tr.error .pill, tr.timeout .pill {{ background:#3a3010; color:#ffd27a; }}
  .err {{ color:#9aa3b5; max-width:280px; word-break:break-word; }}
</style></head><body>
<h1>HtmlToGum bookmark fidelity</h1>
<p class="sub">Unique hosts from bookmarks · max 5 pages/site · generated {meta['generatedAt']}</p>
<div class="stats">
  <div class="stat"><strong>{t['finished']}</strong> / {t['seeds']} sites finished</div>
  <div class="stat"><strong>{t['passSites']}</strong> sites pass</div>
  <div class="stat"><strong>{t['failSites']}</strong> sites fail</div>
  <div class="stat"><strong>{t.get('rejectedSites', 0)}</strong> sites rejected</div>
  <div class="stat"><strong>{t['errorSites']}</strong> site errors</div>
  <div class="stat">pages <strong>{t['pagesPassed']}</strong> pass / <strong>{t['pagesFailed']}</strong> fail / <strong>{t['pagesErrored']}</strong> err</div>
  <div class="stat">best <strong>{best_s}</strong></div>
  <div class="stat">worst <strong>{worst_s}</strong></div>
</div>
<table>
<thead><tr><th>Host</th><th>Status</th><th>Pages</th><th>Mean %</th><th>Max %</th><th>Time</th><th>Error</th></tr></thead>
<tbody>
{''.join(rows)}
</tbody></table>
</body></html>"""


def run_one(seed_entry: dict, max_pages: int, max_pct: float, width: int, height: int, timeout: int) -> tuple[str, Path | None, str | None, float]:
    seed = seed_entry["url"]
    cmd = (
        f'npm run site-fidelity -- "{seed}" '
        f"--max-pages={max_pages} --max-pct={max_pct} "
        f"--width={width} --height={height}"
    )
    t0 = time.time()
    # Windows: shell=True + npm.cmd spawns a process tree; TimeoutExpired only kills the
    # shell unless we taskkill /T the whole tree.
    creationflags = subprocess.CREATE_NEW_PROCESS_GROUP if sys.platform == "win32" else 0  # type: ignore[attr-defined]
    proc = subprocess.Popen(
        cmd,
        cwd=str(CONVERTER),
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        shell=True,
        creationflags=creationflags,
    )
    try:
        stdout, stderr = proc.communicate(timeout=timeout)
        elapsed = time.time() - t0
        report = find_latest_report_for_seed(seed)
        if proc.returncode not in (0, 1) and not report:
            err = (stderr or stdout or f"exit {proc.returncode}")[-1500:]
            return "error", None, err, elapsed
        if report:
            return "ok", report, None, elapsed
        err = (stderr or stdout or f"exit {proc.returncode}, no report")[-1500:]
        return "error", None, err, elapsed
    except subprocess.TimeoutExpired:
        elapsed = time.time() - t0
        try:
            if sys.platform == "win32":
                subprocess.run(
                    ["taskkill", "/PID", str(proc.pid), "/T", "/F"],
                    capture_output=True,
                    text=True,
                )
            else:
                proc.kill()
            proc.wait(timeout=15)
        except Exception:
            pass
        try:
            report = find_latest_report_for_seed(seed)
        except Exception:
            report = None
        return "timeout", report, f"timed out after {timeout}s", elapsed
    except Exception as e:
        elapsed = time.time() - t0
        try:
            proc.kill()
        except Exception:
            pass
        return "error", None, str(e), elapsed


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("seeds", type=Path, help="seeds.json from parse_bookmark_seeds.py")
    ap.add_argument("--max-pages", type=int, default=5)
    ap.add_argument("--max-pct", type=float, default=5.0)
    ap.add_argument("--width", type=int, default=800)
    ap.add_argument("--height", type=int, default=900)
    ap.add_argument("--timeout", type=int, default=480, help="per-site seconds")
    ap.add_argument("--limit", type=int, default=0, help="only first N seeds (0=all)")
    ap.add_argument("--offset", type=int, default=0)
    ap.add_argument("--resume", action="store_true", help="skip hosts already in meta-report.json")
    args = ap.parse_args()

    data = json.loads(args.seeds.read_text(encoding="utf-8"))
    seeds = data["seeds"][args.offset:]
    if args.limit:
        seeds = seeds[: args.limit]

    OUT_ROOT.mkdir(parents=True, exist_ok=True)
    sites_dir = OUT_ROOT / "sites"
    sites_dir.mkdir(exist_ok=True)
    log_path = OUT_ROOT / "run-log.jsonl"

    done_hosts: set[str] = set()
    sites: list[dict] = []
    if args.resume and (OUT_ROOT / "meta-report.json").exists():
        prev = json.loads((OUT_ROOT / "meta-report.json").read_text(encoding="utf-8"))
        sites = prev.get("sites") or []
        done_hosts = {s["host"] for s in sites if s.get("status") not in ("pending", "running")}
        print(f"resume: {len(done_hosts)} hosts already finished")

    run = {
        "startedAt": datetime.now(timezone.utc).isoformat(),
        "maxPages": args.max_pages,
        "maxPct": args.max_pct,
        "viewport": {"width": args.width, "height": args.height},
        "timeoutSec": args.timeout,
        "bookmarksPath": data.get("bookmarksPath"),
        "uniqueHosts": data.get("uniqueHosts"),
        "skippedDuplicates": data.get("skippedDuplicates"),
    }

    # Pre-fill pending entries for meta visibility
    by_host = {s["host"]: s for s in sites}
    for seed_entry in seeds:
        if seed_entry["host"] not in by_host:
            by_host[seed_entry["host"]] = {
                "host": seed_entry["host"],
                "title": seed_entry.get("title"),
                "seed": seed_entry["url"],
                "status": "pending",
                "error": None,
                "elapsedSec": 0,
                "passed": 0,
                "failed": 0,
                "errored": 0,
                "pageCount": 0,
                "meanPct": None,
                "maxPct": None,
                "minPct": None,
                "pages": [],
            }

    total = len(seeds)
    for i, seed_entry in enumerate(seeds, 1):
        host = seed_entry["host"]
        if host in done_hosts:
            print(f"[{i}/{total}] skip (resume) {host}")
            continue

        print(f"\n========== [{i}/{total}] {host} ==========", flush=True)
        print(seed_entry["url"], flush=True)
        by_host[host]["status"] = "running"
        write_meta(OUT_ROOT, run, list(by_host.values()))

        status, report_path, error, elapsed = run_one(
            seed_entry, args.max_pages, args.max_pct, args.width, args.height, args.timeout,
        )
        summary = summarize_site(seed_entry, report_path, status, error, elapsed)
        by_host[host] = summary

        # Per-site summary file
        site_out = sites_dir / slug_host(host)
        site_out.mkdir(exist_ok=True)
        slim = dict(summary)
        raw = slim.pop("rawReport", None)
        (site_out / "site-summary.json").write_text(json.dumps(slim, indent=2), encoding="utf-8")
        (site_out / "report.html").write_text(render_site_html(slim), encoding="utf-8")
        if raw:
            (site_out / "report.json").write_text(json.dumps(raw, indent=2), encoding="utf-8")
        if report_path and report_path.exists():
            # pointer to fidelity folder
            (site_out / "site-fidelity-dir.txt").write_text(str(report_path.parent), encoding="utf-8")

        with log_path.open("a", encoding="utf-8") as log:
            log.write(json.dumps({
                "ts": datetime.now(timezone.utc).isoformat(),
                "host": host,
                "status": summary["status"],
                "meanPct": summary.get("meanPct"),
                "elapsedSec": elapsed,
                "error": error,
            }) + "\n")

        write_meta(OUT_ROOT, run, list(by_host.values()))
        print(
            f"-> {summary['status']}  {summary['passed']} pass / {summary['failed']} fail"
            f" / {summary['pageCount']} pages  mean={summary.get('meanPct')}  {elapsed:.0f}s",
            flush=True,
        )

    run["finishedAt"] = datetime.now(timezone.utc).isoformat()
    write_meta(OUT_ROOT, run, list(by_host.values()))
    print(f"\nMeta report: {OUT_ROOT / 'meta-report.html'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
