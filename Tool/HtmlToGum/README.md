# HtmlToGum

The HtmlToGum plugin adds **Content → Import HTML…** to Gum. It converts a selected HTML file into a Gum screen using Playwright/Chromium to inspect the rendered box tree.

## Build

Build the Gum Tool first, then build the plugin:

```powershell
dotnet build Tool/HtmlToGum/HtmlToGumPlugin.csproj -c Release
```

## Converter setup

Install Node.js LTS, then install the converter dependencies:

```powershell
cd Tool/HtmlToGum/converter
npm install
```

The postinstall step downloads Playwright Chromium locally. Chromium is not bundled with Gum.

## Converter override

Set `HTMLTOGUM_CONVERTER` to an absolute converter directory to override automatic discovery.
