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

**Custom-font multi-line `<p>`/`<h*>`:** BitmapFont wrap ≠ Chromium for faces like Graphik/Doyle (Pocket). `shouldRasterTextHeavyCell` also bakes multi-line blocks (≥2 client rects) whose first `font-family` is not a system face, narrow (`≤280px`) wrapping `<a>`/`<li>` even in Arial (TL Community News), and centered multi-line system-font `<p>` marketing copy (Pi-hole). Wide left-aligned system-font article prose (HN / Wikipedia) stays structured Text.

**Multi-line `<pre>` / `white-space:pre*`:** leaf extract used to collapse all whitespace (`/\s+/g` → space), so indented code became one soft-wrapped line and Gum broke mid-token (`new` / `Foo();` on tabsoverspaces). Prefer baking multi-line preformatted hosts (`shouldRasterTextHeavyCell` on `<pre>` or phrasing-only pre hosts, counting distinct client-rect Ys — not raw rect count, or pastebin highlighter spans false-trigger). When structured, `textForWhiteSpace` must preserve newlines/spaces for `pre` / `pre-wrap` / `break-spaces` / `pre-line`.

**Font Awesome / icon-font `::before`:** glyphs use `content:"\uf0xx"` with `width/height:auto` (no border/bg box). `needsRasterPaint` must treat icon-font families and Private Use Area content as pseudo chrome — otherwise Gum draws empty bordered squares (Embrace the Red header social icons).

**Empty-content pseudo backdrops:** overlays often use `::before { content:""; inset:0; background:…; opacity:… }` (Pi-hole hero tint). Do not discard the pseudo because its unquoted content is empty. Bake the host chrome (background + pseudo) while hiding descendants so nav/text remain structured; icon/glyph pseudos still bake the whole host.

**Transparent inline SVG rasterization:** Playwright `omitBackground` clears the page canvas but still captures painted DOM ancestors through transparent SVG pixels. Isolate the SVG by temporarily neutralizing ancestor chrome and hiding sibling branches (`raster-isolation.ts`), then restore exact inline styles. Otherwise a separator SVG over a photo bakes the photo and Gum paints a duplicate strip (Pi-hole hero).

**Google Fonts unicode-range subsets:** each weight has many `@font-face` rules (Latin / Latin-ext / Cyrillic / …). Picking the first CSS match often bakes a Cyrillic-only TTF → empty KernSmith atlas → Arial fallback. Prefer faces whose `unicode-range` covers basic Latin (`unicodeRangeCoversBasicLatin`); reject baked TTFs that lack `A`/`a`/`M`/`m` and try the next URL.

**Empty custom FontCache atlases:** some web `.ttf` bakes (e.g. Poppins Light) yield `chars count=2` (space only) → invisible text. `repairEmptyCustomFonts` in `fonts.ts` rewrites those `Font=Fonts/….ttf` refs to Arial and re-bakes. Do not chase “missing text” with layout probes until you’ve checked `FontCache/*.fnt` `chars count`.

**Mac-only faces on Windows:** `Menlo` / `Monaco` / `Helvetica Neue` resolve via `FACE_ALIASES` to Consolas / Arial so gumcli can embed them.
**Bad font downloads:** `@font-face` URLs can return HTML/empty bytes. `looksLikeFontBuffer` + multi-URL retry in `materializeWebFonts` skip non-sfnt/woff payloads and try the next candidate (KORE Proxima Nova w400).

**White canvas default:** browsers paint the page canvas white when neither `<html>` nor `<body>` sets an opaque background (CSS "canvas"). Gum has no such default → the root stays transparent and the screenshot is transparent (RGB 0,0,0 under alpha 0) where Chromium is white; the diff scores every such pixel as a full miss (OWASP content band was ~40% alone). `extractBoxTree` propagates the effective page background (html → body → white) onto the root `body`/`html` node so `BodyBg` paints a backmost fill. Only fires when the root is body/html with a transparent bg and no background-image (Space Jam's opaque/starfield body is untouched).

**Out-of-flow descendants inflating a backdrop:** `backdropHeight`/`textOverflowPad` walk a styled container's subtree to pad for BitmapFont spill. A `position:fixed`/`absolute` descendant (e.g. a cookie banner nested in `<header>`, painted at `y=800`) would stretch the header's painted backdrop from ~159px to ~1544px and tint the whole page with the header color (OWASP `#disclaimer-container`). `textOverflowPad` skips out-of-flow subtrees — they paint at their own coordinates and are not part of an ancestor's content box.

**Nested fixed cookie banners:** `stabilizeDynamicMedia` hides GDPR toasts so fidelity measures page chrome, not consent UI. Match by id/class *and* by cookie-copy text on **any** `position:fixed`/`sticky` node — not only `body > *`. OWASP nests `#disclaimer-container` under `<header>`; a body-direct scan never sees it (~3% of the residual gate).

**System font stacks:** CSS `-apple-system, BlinkMacSystemFont, "Segoe UI", …` must resolve via `resolveCssFontFamily` to `Segoe UI` (not the synthetic first token). Otherwise Gum falls back to Arial while Chromium on Windows uses Segoe.

**Percent-encoded `data:image/svg+xml`:** select chevrons etc. need `parseDataImageUrl` (`decodeURIComponent`) — the old `;base64`-only regex dropped charset URLs (Pocket).

**`<input type="submit|button|reset">` labels:** value lives in `.value`, not `textContent`. Extract must copy `el.value` or buttons render as chrome-only (KORE “Sign In”).

**HTML form controls → Gum Forms:** by default, mappable controls become `Controls/TextBox`, `PasswordBox`, `ButtonStandard`, `CheckBox`, `RadioButton`, `ComboBox` and the project bootstraps with `gumcli new --template forms`. Default Forms chrome ≠ site-styled widgets, so **site-fidelity always passes `--no-forms`** (visual Rectangle/Text path) until styled matching exists. Fixture: `samples/features/forms-controls.html`.

**Flex item `width`/`height: 100%`:** Chromium’s *used* size is flex-constrained; do not emit Gum `PercentageOfParent` for stack main-axis — use Absolute measured px (KORE login column shifted ~192px left).

**`background-size: Npx` / `auto` / `contain` + `no-repeat`:** place a Sprite at the resolved size + `background-position`, not stretch-fill the box (`resolveBackgroundImageLayout`). Stretching the KORE logo (`100px`) / hero (`400px`) and TL header banner (`auto` + `50% 0%`) costs multiple % of the pixel gate.

**Off-page raster clips abort convert:** `needsRaster` nodes with boxes outside `scrollWidth/Height` (transformed SVGs, sticky overflow) made Playwright throw `Clipped area is either empty or outside the resulting image`. `intersectScreenshotClip` clamps/skips those instead of failing the whole page (kali.org/tools, opencv.org).

**Rotating heroes / carousels are not converter bugs.** `stabilizeDynamicMedia` runs in convert *before* extract (pins `.newsitem` / swiper / carousel slides, pauses CSS animations, clears + noops timers/rAF). If `capture-meta.json` has `suspectedRotatingMedia: true`, **do not** write probe scripts or spend iterations on timer races — fix mapping/fonts/layout or move to the next site after one re-run.

**Interactive hash-routed diagrams** (e.g. ndpsoftware git-cheatsheet `#loc=index;`) can land extract vs screenshot on different modes → 90%+ diffs. Same rule: one stabilize attempt, then move on — not a layout primitive gap.

**Rejected hosts:** crawl aborts on HTTP 4xx / challenge / seed nav timeout (`fidelity/rejection.ts`). Empty crawl → `status: rejected`, not a fidelity fail. Do not retry max-pages on the same wall.

**Vacuous login shells** (blank Azure/Outlook chrome under 5%) are not wins — skip for iteration.

**Space Jam regression:** after shared converter edits, smoke `jam.htm` (or `--max-pages=3`) before calling a fix done.

**Canary suite (anti-overfit):** after shared converter edits, run the curated gate before calling a fix done:

```powershell
cd Tool/HtmlToGum/fidelity
npm run canaries -- --tier=local    # layout zoo (~1–2 min) — run every fix
npm run canaries -- --tier=live     # 10 CS/general sites (~5–8 min) — run before commit
# optional: npm run canaries -- --tier=frozen   # after npm run freeze …
```

Fail if any entry exceeds `maxPct` **or** rises more than `maxDeltaPct` above its checked-in `baselinePct` (`canaries.json`). Use `--update-baselines` only after a known-good intentional improvement. Do not use personal bookmark batches as the regression net.

## Loop cap

Per site: diagnose top `diff/` regions → one focused converter patch + tests → re-run fidelity. If still failing for the *same* rotating-media hypothesis after stabilize already ran, stop that hypothesis and move on.
