# HtmlToGum fidelity harness

Pixel-gate tooling for live sites and bookmark batches. **Not** the HTML→Gum convert pipeline.

| Here (`fidelity/`) | Next door (`../converter/`) |
|---|---|
| `site-fidelity.ts`, `crawl.ts`, `rejection.ts` | `convert.ts`, `map.ts`, `extract.ts` |
| `diff_screenshots.py`, bookmark batch | `assets.ts`, `fonts.ts`, `gumcli.ts` |
| One-off probes (`_*.mjs` / `_*.py`) | `dom-quiescence.ts` (used by convert before extract) |

Do **not** put map/extract/font fixes in this folder — those belong in `converter/`.

## Run

From this directory:

```powershell
npm install
npm run site-fidelity -- https://news.ycombinator.com/ --max-pages=1 --max-pct=5
npm test
```

Or keep using the thin forwards from `converter/`:

```powershell
cd ../converter
npm run site-fidelity -- https://news.ycombinator.com/ --max-pages=1 --max-pct=5
npm run test:fidelity
```

Outputs stay under `Tool/HtmlToGum/.site-fidelity/` and `.bookmark-fidelity/` (gitignored).
