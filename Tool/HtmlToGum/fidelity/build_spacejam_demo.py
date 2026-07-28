"""Build a self-contained HtmlToGum Space Jam fidelity demo HTML for Discord."""
from __future__ import annotations

import base64
import io
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1] / ".site-fidelity" / "www.spacejam.com-1996-jam.htm"
PAGES = ROOT / "pages"
OUT = Path(__file__).resolve().parents[1] / "HtmlToGum-SpaceJam-Demo.html"

PICKS = [
    ("Jam", "Homepage", "Tiled starfield, image-map planets, Absolute layout"),
    ("Sitemap", "Sitemap tables", "HTML border bevels, multi-line prose, rasterized cells"),
    ("Jamcentral", "Jam Central", "Nested frameset content + chrome"),
    ("Bball", "Basketball", "Dense table layout + legacy assets"),
    ("Lineup", "The Lineup", "Cast grid + body copy"),
]


def side_by_side(screen: str, max_h: int = 520, col_w: int = 340) -> tuple[bytes, tuple[int, int]]:
    base = PAGES / screen
    ref = Image.open(base / "chromium.png").convert("RGB")
    gum = Image.open(base / "gum-aligned.png").convert("RGB")

    def fit(im: Image.Image) -> Image.Image:
        r = col_w / im.width
        h = int(im.height * r)
        if h > max_h:
            r = max_h / im.height
            return im.resize((int(im.width * r), max_h), Image.Resampling.LANCZOS)
        return im.resize((col_w, h), Image.Resampling.LANCZOS)

    a, b = fit(ref), fit(gum)
    h = max(a.height, b.height)
    gap = 10
    label_h = 28
    out = Image.new("RGB", (a.width + b.width + gap + 4, h + label_h + 4), (8, 8, 16))
    draw = ImageDraw.Draw(out)
    try:
        font = ImageFont.truetype("arial.ttf", 14)
    except OSError:
        font = ImageFont.load_default()
    draw.text((8, 6), "Chromium", fill=(180, 190, 210), font=font)
    draw.text((a.width + gap + 8, 6), "Gum (HtmlToGum)", fill=(254, 255, 137), font=font)
    out.paste(a, (2, label_h))
    out.paste(b, (a.width + gap + 2, label_h))
    buf = io.BytesIO()
    out.save(buf, format="JPEG", quality=78, optimize=True)
    return buf.getvalue(), out.size


def main() -> None:
    report = json.loads((ROOT / "report.json").read_text(encoding="utf-8"))
    pct = {p["screen"]: p["pct"] for p in report["pages"]}

    sections: list[tuple[str, str, str, float, str, tuple[int, int]]] = []
    total = 0
    for screen, title, blurb in PICKS:
        data, size = side_by_side(screen)
        total += len(data)
        b64 = base64.b64encode(data).decode("ascii")
        sections.append((screen, title, blurb, float(pct.get(screen, 0)), b64, size))
        print(f"{screen}: {len(data) / 1024:.0f} KB  {size}")
    print(f"total images {total / 1024:.0f} KB")

    parts = [
        """<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>HtmlToGum — Space Jam 1996 fidelity</title>
<style>
  :root {
    --bg: #05050c;
    --panel: #0c0c18;
    --yellow: #feff89;
    --muted: #9aa3b5;
    --line: #1e2438;
  }
  * { box-sizing: border-box; }
  body {
    margin: 0;
    font-family: "Segoe UI", system-ui, sans-serif;
    background:
      radial-gradient(ellipse 80% 50% at 50% -10%, #1a1540 0%, transparent 55%),
      radial-gradient(circle at 20% 80%, #12102a 0%, transparent 40%),
      var(--bg);
    color: #e8ecf5;
    line-height: 1.45;
  }
  .wrap {
    max-width: 760px;
    margin: 0 auto;
    padding: 40px 20px 80px;
  }
  header {
    text-align: center;
    margin-bottom: 40px;
  }
  .eyebrow {
    color: var(--yellow);
    letter-spacing: 0.18em;
    text-transform: uppercase;
    font-size: 11px;
    font-weight: 600;
    margin: 0 0 12px;
  }
  h1 {
    font-family: Georgia, "Times New Roman", serif;
    font-size: clamp(28px, 5vw, 40px);
    font-weight: 400;
    margin: 0 0 12px;
    color: #fff;
  }
  .lede {
    color: var(--muted);
    font-size: 15px;
    max-width: 520px;
    margin: 0 auto 22px;
  }
  .stats {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    justify-content: center;
  }
  .stat {
    border: 1px solid var(--line);
    background: rgba(255,255,255,0.03);
    padding: 8px 14px;
    border-radius: 6px;
    font-size: 13px;
  }
  .stat strong { color: var(--yellow); font-weight: 600; }
  section {
    margin: 36px 0;
    background: var(--panel);
    border: 1px solid var(--line);
    border-radius: 10px;
    overflow: hidden;
  }
  .meta {
    padding: 16px 18px 10px;
    display: flex;
    flex-wrap: wrap;
    align-items: baseline;
    gap: 8px 16px;
    border-bottom: 1px solid var(--line);
  }
  .meta h2 {
    margin: 0;
    font-size: 18px;
    font-weight: 600;
  }
  .pct {
    color: var(--yellow);
    font-variant-numeric: tabular-nums;
    font-size: 13px;
  }
  .blurb {
    width: 100%;
    margin: 0;
    color: var(--muted);
    font-size: 13px;
  }
  .pair {
    display: block;
    width: 100%;
    height: auto;
  }
  footer {
    text-align: center;
    color: var(--muted);
    font-size: 12px;
    margin-top: 48px;
  }
  footer code { color: #c5cce0; }
</style>
</head>
<body>
  <div class="wrap">
    <header>
      <p class="eyebrow">HtmlToGum · converter R&amp;D</p>
      <h1>Space Jam (1996) → Gum</h1>
      <p class="lede">
        Chromium on the left, Gum on the right — same pages after an HTML/CSS → Gum
        conversion via Playwright box trees. Target: under 5% pixel difference.
      </p>
      <div class="stats">
        <div class="stat"><strong>15/15</strong> pages passed</div>
        <div class="stat">max <strong>5%</strong> budget</div>
        <div class="stat">viewport <strong>800×900</strong></div>
      </div>
    </header>
"""
    ]

    for _screen, title, blurb, p, b64, size in sections:
        parts.append(
            f"""
    <section>
      <div class="meta">
        <h2>{title}</h2>
        <span class="pct">{p:.2f}% diff</span>
        <p class="blurb">{blurb}</p>
      </div>
      <img class="pair" alt="{title}: Chromium vs Gum"
           src="data:image/jpeg;base64,{b64}" width="{size[0]}" height="{size[1]}" />
    </section>
"""
        )

    parts.append(
        """
    <footer>
      Seed: <code>https://www.spacejam.com/1996/jam.htm</code><br />
      Built with HtmlToGum (<code>npm run site-fidelity</code>) · self-contained file for Discord
    </footer>
  </div>
</body>
</html>
"""
    )

    OUT.write_text("".join(parts), encoding="utf-8")
    print(f"wrote {OUT} ({OUT.stat().st_size / 1024 / 1024:.2f} MB)")


if __name__ == "__main__":
    main()
