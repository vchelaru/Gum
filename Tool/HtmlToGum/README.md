# HtmlToGum

Gum Tool plugin: **Content → Import → HTML…** converts a page into a Gum screen via Chromium’s computed box tree (Playwright), then imports the resulting `.gusx` and assets into the open project.

Chromium is **not** bundled with Gum. The plugin runs `npm install` (downloading Playwright's Chromium) automatically the first time you import, if it hasn't been run yet.

## Layout

```text
Tool/HtmlToGum/
  HtmlToGumPlugin.csproj     # MEF plugin
  MainHtmlToGumPlugin.cs
  converter/                 # Node convert pipeline (required for Import HTML)
  fidelity/                  # Site/bookmark pixel-gate harness (dev only)
  samples/                   # Optional fixture HTML for try-outs
```

| Path | Role |
|------|------|
| Plugin DLL | Menu + staging import |
| `converter/` | `convert.ts` + map/extract/fonts (+ `dom-quiescence` for stable extract) |
| `fidelity/` | `site-fidelity` crawl → convert → screenshot → pixel diff |
| `samples/` | Example HTML pages (not required for the plugin) |

## Setup

1. Build Gum Tool, then this plugin:

```powershell
dotnet build Tool/HtmlToGum/HtmlToGumPlugin.csproj -c Release
```

Post-build copies `HtmlToGumPlugin.dll` to `Gum/bin/{Config}/Plugins/HtmlToGumPlugin/`.

2. Install Node.js LTS. Converter dependencies (`npm install`, including Playwright's Chromium download) run automatically on first import — or run them yourself ahead of time:

```powershell
cd Tool/HtmlToGum/converter
npm install
```

3. Launch Gum → open a saved `.gumx` → **Content → Import → HTML…**. Choose a local file or a remote `http(s)://` URL, and optionally set a destination subfolder to import under `Screens/<subfolder>/` and avoid name conflicts with existing screens.

### Converter discovery

The plugin looks for `Tool/HtmlToGum/converter` relative to the Gum repo (from the Plugins folder). Override with:

```text
HTMLTOGUM_CONVERTER=<absolute path to converter/>
```

## CLI convert (optional)

```powershell
cd Tool/HtmlToGum/converter
npm run convert -- ../samples/features/inventory.html #panel InventoryScreen 800 600
```

Default output is `Tool/HtmlToGum/.out/` (gitignored). Use `--out=<dir>` to choose another folder (the plugin always passes a temp `--out=`). The output project (`.gumx` + `Standards/`) is bootstrapped via `gumcli new --template empty`, so Standards always match Gum's live defaults rather than a static snapshot.

Useful flags: `--no-responsive`, `--responsive=n,w`, `--tag=name`.

Fonts: `npm run gumcli -- fonts <project.gumx>` (wraps in-repo `Tools/Gum.Cli`).

## Site fidelity (dev loop)

Crawl a live site (same-origin links), convert each page, screenshot via `gumcli screenshot`, and pixel-diff against Chromium until every page is under a threshold (default **5%**). Harness sources live in `Tool/HtmlToGum/fidelity/`; the convert pipeline stays in `converter/`. Output is gitignored under `Tool/HtmlToGum/.site-fidelity/`.

```powershell
cd Tool/HtmlToGum/converter
npm run site-fidelity -- https://www.spacejam.com/1996/jam.htm
# optional: --max-pages=15 --max-pct=5 --width=800 --height=900 --path-prefix=/1996/
# same as: cd ../fidelity && npm run site-fidelity -- <url>
```

Exit code `0` = all pages ≤ `--max-pct`; `1` = one or more over budget (see `report.json` + per-page `diff/` crops). Use that report to patch the converter, then re-run the same command until green.

Frameset hubs (e.g. Space Jam `*frames.html`) are skipped; their `<frame src>` targets are crawled instead so convert always gets a real `<body>`.

**Agent iteration loop:** run site-fidelity → open failing `pages/<Screen>/diff/` crops + `report.json` → fix `converter/map.ts` / `extract.ts` → re-run the same command (optionally `--pages=` for a single URL) until every page is under 5%.

**Rotating media:** convert calls `stabilizeDynamicMedia` before extract (see `converter/dom-quiescence.ts`). If `capture-meta.json` reports `suspectedRotatingMedia`, do not debug carousel timing — convert already pinned the slide. Treat remaining diffs as real converter issues or move on.

To skip crawl and re-test a fixed URL list:

```powershell
npm run site-fidelity -- https://www.spacejam.com/1996/jam.htm --pages=https://www.spacejam.com/1996/jam.htm
```

## Samples

See [`samples/README.md`](samples/README.md). Open any sample via Import HTML, or convert from the CLI as above.

`converter/fixtures.ts` lists sample paths and pixel thresholds for a future MonoGame regress host (`converter/regress.ts`). That host is not shipped here yet — run `npx tsx regress.ts` only after adding it.

## Requirements summary

| Need | For |
|------|-----|
| .NET 8 / Gum Tool build | Plugin |
| Node.js LTS | Import HTML / CLI (`npm install` runs automatically on first Import HTML; run it manually for CLI use) |
| Playwright Chromium (via postinstall) | Box tree + screenshots |
| Python + fonttools (optional) | Variable-font → static TTF (`requirements-fonts.txt`) |
