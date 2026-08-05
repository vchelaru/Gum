# HtmlToGum fidelity harness

Pixel-gate tooling for live sites, frozen captures, and local layout-zoo canaries.
**Not** the HTML→Gum convert pipeline.

| Here (`fidelity/`) | Next door (`../converter/`) |
|---|---|
| `site-fidelity.ts`, `crawl.ts`, `rejection.ts` | `convert.ts`, `map.ts`, `extract.ts` |
| `canaries.ts`, `canaries.json`, `freeze-page.ts` | `assets.ts`, `fonts.ts`, `gumcli.ts` |
| `diff_screenshots.py`, bookmark batch | `dom-quiescence.ts` (used by convert before extract) |

Do **not** put map/extract/font fixes in this folder — those belong in `converter/`.

## Canary suite (anti-overfit gate)

After a shared converter fix, run canaries before calling the fix done:

```powershell
cd Tool/HtmlToGum/fidelity
npm install
npm run canaries -- --tier=local          # layout zoo (~1–2 min)
npm run canaries -- --tier=frozen         # needs freeze first
npm run canaries -- --tier=live           # 10 curated CS/general sites (~5–8 min)
npm run canaries                          # all tiers
npm run canaries -- --update-baselines    # after a known-good run
```

Fail conditions per entry: `pct > maxPct`, or `pct - baselinePct > maxDeltaPct`.
Baselines live in `canaries.json` (checked in). Outputs under `../.canaries/` (gitignored).

Freeze a page for tier 2:

```powershell
npm run freeze -- https://developer.mozilla.org/en-US/docs/Web/CSS/background-size --id=mdn-background-size
npm run freeze -- https://docs.python.org/3/ --id=python-docs-3
```

## Single-site loop

```powershell
npm run site-fidelity -- https://news.ycombinator.com/ --max-pages=1 --max-pct=5
npm test
```

Or keep using the thin forwards from `converter/`:

```powershell
cd ../converter
npm run canaries -- --tier=local
npm run site-fidelity -- https://news.ycombinator.com/ --max-pages=1 --max-pct=5
```

Outputs stay under `Tool/HtmlToGum/.site-fidelity/`, `.canaries/`, and `.bookmark-fidelity/` (gitignored).
