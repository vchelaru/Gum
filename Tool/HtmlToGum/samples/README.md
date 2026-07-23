# Samples

Example HTML for trying Import HTML and exercising the converter. **Not** required at runtime — the plugin converts whatever file you pick.

| Folder | Contents |
|--------|----------|
| `features/` | One CSS/layout concern per page |
| `composites/` | Multi-feature HUDs |
| `clones/` | Genre-style stress pages |
| `third-party/` | Harder layouts (Tabler, Cerberus, AdminKit) |
| `assets/` | Shared icons + RPGUI kit |

Paths used by `converter/fixtures.ts` are relative to this folder.

```powershell
cd Tool/HtmlToGum/converter
npm run convert -- ../samples/features/inventory.html #panel InventoryScreen 800 600
```
