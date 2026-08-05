# Samples

Example HTML for trying Import HTML and exercising the converter. **Not** required at runtime — the plugin converts whatever file you pick.

| Folder | Contents |
|--------|----------|
| `features/` | One CSS/layout concern per page (includes layout-zoo landmines: `white-canvas`, `fixed-cookie-header`, `bg-size-auto-banner`) |
| `composites/` | Multi-feature HUDs |
| `clones/` | Genre-style stress pages |
| `third-party/` | Harder layouts (Tabler, Cerberus, AdminKit) |
| `assets/` | Shared icons + RPGUI kit |

Paths used by `converter/fixtures.ts` are relative to this folder. Layout-zoo pages also run via `fidelity/canaries.ts --tier=local`.

```powershell
cd Tool/HtmlToGum/converter
npm run convert -- ../samples/features/inventory.html #panel InventoryScreen 800 600
```
