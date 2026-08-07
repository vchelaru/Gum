# Focused fidelity crawl: new public content sites (no Tula / login shells).
# Writes Tool/HtmlToGum/.bookmark-fidelity/new-sites-crawl-summary.json
from __future__ import annotations

import json
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import urlparse

FIDELITY = Path(__file__).resolve().parent
CONVERTER = FIDELITY.parent / "converter"
SITE_ROOT = FIDELITY.parent / ".site-fidelity"
OUT = FIDELITY.parent / ".bookmark-fidelity" / "new-sites-crawl-summary.json"

# Fresh public content / docs — not live canaries, not Tula, not interactive games.
SEEDS = [
    "https://arstechnica.com/",
    "https://old.reddit.com/r/programming/",
    "https://diy.stackexchange.com/",
    "https://lobste.rs/",
    "https://dev.to/",
    "https://css-tricks.com/",
    "https://web.dev/",
    "https://react.dev/",
    "https://www.typescriptlang.org/docs/",
    "https://doc.rust-lang.org/book/",
    "https://www.kernel.org/",
    "https://infoq.com/",
    "https://knowmax.ai/blog/troubleshooting-guides-for-customer-service/",
    "https://catfishing.net/",
    "https://www.hankgreen.com/fourbythree",
    "https://www.smashingmagazine.com/",
    "https://crates.io/",
    "https://pypi.org/",
]


def slug_for(url: str) -> str:
    u = urlparse(url)
    path = (u.path or "/").replace("//", "/").strip("/") or "root"
    host = u.hostname or "unknown"
    raw = f"{host}-{path}"
    out = "".join(c if c.isalnum() or c in "._-" else "-" for c in raw)[:80]
    return out


def run_one(url: str, timeout: int = 420) -> dict:
    t0 = time.time()
    slug = slug_for(url)
    report_path = SITE_ROOT / slug / "report.json"
    before_mtime = report_path.stat().st_mtime if report_path.exists() else None
    cmd = [
        "npm.cmd" if sys.platform == "win32" else "npm",
        "run",
        "site-fidelity",
        "--",
        url,
        "--max-pages=1",
        "--max-pct=5",
        "--width=800",
        "--height=900",
    ]
    try:
        r = subprocess.run(
            cmd,
            cwd=str(CONVERTER),
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=timeout,
            shell=False,
        )
        elapsed = time.time() - t0
        pct = None
        status = "error"
        pages = []
        combined = ((r.stdout or "") + "\n" + (r.stderr or "")).strip()
        fresh = False
        if report_path.exists():
            mtime = report_path.stat().st_mtime
            fresh = before_mtime is None or mtime > before_mtime
            if fresh:
                data = json.loads(report_path.read_text(encoding="utf-8"))
                status = data.get("status") or status
                pages = data.get("pages") or []
                pcts = [p.get("pct") for p in pages if p.get("pct") is not None]
                pct = sum(pcts) / len(pcts) if pcts else None
            else:
                status = "stale-report"
        if status == "error" and "rejected" in combined.lower():
            status = "rejected"
        return {
            "url": url,
            "host": urlparse(url).hostname,
            "slug": slug,
            "status": status,
            "pct": pct,
            "elapsedSec": round(elapsed, 1),
            "exit": r.returncode,
            "freshReport": fresh,
            "tail": combined[-600:],
            "pages": [
                {"screen": p.get("screen"), "pct": p.get("pct"), "status": p.get("status")}
                for p in pages
            ],
        }
    except subprocess.TimeoutExpired:
        return {
            "url": url,
            "host": urlparse(url).hostname,
            "slug": slug,
            "status": "timeout",
            "pct": None,
            "elapsedSec": timeout,
            "exit": -1,
            "freshReport": False,
            "tail": "timeout",
            "pages": [],
        }


def main() -> int:
    OUT.parent.mkdir(parents=True, exist_ok=True)
    results = []
    print(f"new-sites crawl: {len(SEEDS)} seeds", flush=True)
    for i, url in enumerate(SEEDS, 1):
        print(f"\n[{i}/{len(SEEDS)}] {url}", flush=True)
        row = run_one(url)
        results.append(row)
        pct = "—" if row["pct"] is None else f"{row['pct']:.2f}%"
        print(f"  -> {row['status']}  {pct}  {row['elapsedSec']}s", flush=True)
        OUT.write_text(
            json.dumps(
                {
                    "generatedAt": datetime.now(timezone.utc).isoformat(),
                    "seeds": SEEDS,
                    "results": results,
                },
                indent=2,
            ),
            encoding="utf-8",
        )

    scored = [r for r in results if r.get("pct") is not None]
    scored.sort(key=lambda r: r["pct"])
    print("\n=== ranked by pct ===", flush=True)
    for r in scored:
        print(f"  {r['pct']:6.2f}%  {r['status']:8}  {r['host']}", flush=True)
    print(f"\nreport: {OUT}", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
