---
name: html-to-gum-fidelity
description: HtmlToGum site-fidelity loop — convert pixel gate, rejection fail-fast, rotating-media stabilize. Triggers: site-fidelity, bookmark fidelity, HtmlToGum converter pixel diff iteration.
---

# HtmlToGum site fidelity

## Where

| Piece | Path |
|---|---|
| Convert pipeline | `Tool/HtmlToGum/converter/convert.ts` |
| Map / extract / assets / fonts | `Tool/HtmlToGum/converter/` |
| DOM settle + media freeze | `converter/dom-quiescence.ts` (`waitForDomQuiescence`, `stabilizeDynamicMedia`) — used by **convert**, not harness-only |
| Fidelity harness | `Tool/HtmlToGum/fidelity/` — `site-fidelity.ts`, `crawl.ts`, `rejection.ts`, `diff_screenshots.py`, bookmark batch |
| Outputs | `Tool/HtmlToGum/.site-fidelity/<slug>/` |

Run (either works):

```powershell
cd Tool/HtmlToGum/converter
npm run site-fidelity -- <url> --max-pages=1 --max-pct=5
# or: cd Tool/HtmlToGum/fidelity && npm run site-fidelity -- <url> ...
```

Keep converter fixes in `converter/`; keep crawl/gate/diff scripts in `fidelity/`.

## Landmines

**Empty custom FontCache atlases:** some web `.ttf` bakes (e.g. Poppins Light) yield `chars count=2` (space only) → invisible text. `repairEmptyCustomFonts` in `fonts.ts` rewrites those `Font=Fonts/….ttf` refs to Arial and re-bakes. Do not chase “missing text” with layout probes until you’ve checked `FontCache/*.fnt` `chars count`.

**Bad font downloads:** `@font-face` URLs can return HTML/empty bytes. `looksLikeFontBuffer` + multi-URL retry in `materializeWebFonts` skip non-sfnt/woff payloads and try the next candidate (KORE Proxima Nova w400).

**System font stacks:** CSS `-apple-system, BlinkMacSystemFont, "Segoe UI", …` must resolve via `resolveCssFontFamily` to `Segoe UI` (not the synthetic first token). Otherwise Gum falls back to Arial while Chromium on Windows uses Segoe.

**Percent-encoded `data:image/svg+xml`:** select chevrons etc. need `parseDataImageUrl` (`decodeURIComponent`) — the old `;base64`-only regex dropped charset URLs (Pocket).

**Rotating heroes / carousels are not converter bugs.** `stabilizeDynamicMedia` runs in convert *before* extract (pins `.newsitem` / swiper / carousel slides, pauses CSS animations, clears + noops timers/rAF). If `capture-meta.json` has `suspectedRotatingMedia: true`, **do not** write probe scripts or spend iterations on timer races — fix mapping/fonts/layout or move to the next site after one re-run.

**Rejected hosts:** crawl aborts on HTTP 4xx / challenge / seed nav timeout (`fidelity/rejection.ts`). Empty crawl → `status: rejected`, not a fidelity fail. Do not retry max-pages on the same wall.

**Vacuous login shells** (blank Azure/Outlook chrome under 5%) are not wins — skip for iteration.

**Space Jam regression:** after shared converter edits, smoke `jam.htm` (or `--max-pages=3`) before calling a fix done.

## Loop cap

Per site: diagnose top `diff/` regions → one focused converter patch + tests → re-run fidelity. If still failing for the *same* rotating-media hypothesis after stabilize already ran, stop that hypothesis and move on.
