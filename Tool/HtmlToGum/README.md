# HtmlToGum

Gum Tool plugin: **Content → Import HTML…** converts a page into a Gum screen via Chromium’s computed box tree (Playwright), then imports the resulting `.gusx` and assets into the open project.

Chromium is **not** bundled with Gum. It is downloaded locally when you `npm install` the converter.

## Layout

```text
Tool/HtmlToGum/
  HtmlToGumPlugin.csproj     # MEF plugin
  MainHtmlToGumPlugin.cs
  converter/                 # Node convert pipeline (required for Import HTML)
    scaffold/                # Minimal .gumx + Standards template used when emitting
  samples/                   # Optional fixture HTML for try-outs
```

| Path | Role |
|------|------|
| Plugin DLL | Menu + staging import |
| `converter/` | `convert.ts` + map/extract/fonts |
| `converter/scaffold/` | Blank Gum project template copied into the convert output |
| `samples/` | Example HTML pages (not required for the plugin) |

## Setup

1. Build Gum Tool, then this plugin:

```powershell
dotnet build Tool/HtmlToGum/HtmlToGumPlugin.csproj -c Release
```

Post-build copies `HtmlToGumPlugin.dll` to `Gum/bin/{Config}/Plugins/HtmlToGumPlugin/`.

2. Install Node.js LTS, then converter deps (downloads Playwright Chromium):

```powershell
cd Tool/HtmlToGum/converter
npm install
```

3. Launch Gum → open a saved `.gumx` → **Content → Import HTML…**.

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

Default output is `Tool/HtmlToGum/.out/` (gitignored). Use `--out=<dir>` to choose another folder (the plugin always passes a temp `--out=`).

Useful flags: `--no-responsive`, `--responsive=n,w`, `--tag=name`.

Fonts: `npm run gumcli -- fonts <project.gumx>` (wraps in-repo `Tools/Gum.Cli`).

## Samples

See [`samples/README.md`](samples/README.md). Open any sample via Import HTML, or convert from the CLI as above.

`converter/fixtures.ts` lists sample paths and pixel thresholds for a future MonoGame regress host (`converter/regress.ts`). That host is not shipped here yet — run `npx tsx regress.ts` only after adding it.

## Requirements summary

| Need | For |
|------|-----|
| .NET 8 / Gum Tool build | Plugin |
| Node.js LTS + `npm install` in `converter/` | Import HTML / CLI |
| Playwright Chromium (via postinstall) | Box tree + screenshots |
| Python + fonttools (optional) | Variable-font → static TTF (`requirements-fonts.txt`) |
