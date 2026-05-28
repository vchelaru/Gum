---
name: gum-icons
description: Umbrella for icons in Gum. Triggers: GumIcon, GumIconKind, GumFigmaIconRipper, GumIcons.xaml, FluentIcon usage in the tool, replacing/adding icons in WPF chrome, tree view, or Forms runtime. Read this first before adding an icon anywhere — it routes you to the right pipeline.
type: skill
---

# Gum Icons Reference

There are **three independent icon pipelines** in Gum. Pick the right one before authoring or adding code.

## At a glance

| Where | Format on disk | Runtime form | Theming | Detail skill |
|---|---|---|---|---|
| **Tool WPF chrome** (variable grid, dock/anchor/alignment, toggle-button option displays) | SVG in `Gum/Content/Svg/` | `PathGeometry` resources in `Gum/Themes/GumIcons.xaml`, consumed via `<controls:GumIcon Icon="…"/>` | `Fill` follows the control's `Foreground` (theme brush) | this file |
| **Tool tree view** (Screens/Components/Behaviors panel, '!' overlay, sizing/origin badges) | PNG in `Gum/Content/Icons/UpdatedTreeViewIcons/` | `Image` cached in `ElementTreeViewCreator._originalImages`, indexed by `ImageIndex` constants | White-on-transparent source, multiplicatively tinted from `Frb.Colors.Icon.*` per a key→color map | [gum-tool-tree-view](../gum-tool-tree-view/SKILL.md) |
| **Forms runtime default visuals** (in-game UI on the user's MonoGame/Skia/etc. surface — not the tool itself) | Sprite sheet (PNG atlas) shipped with `Styling.ActiveStyle` | Sprite coords from `Icons` table, drawn through the runtime sprite system | Style-driven (V2/V3 styling); no DynamicResource concept | [gum-forms-default-visuals](../gum-forms-default-visuals/SKILL.md) |

Don't mix them. A tree-view PNG is not a `GumIcon`. A `GumIcon` `PathGeometry` cannot be drawn into the MonoGame viewport. The Forms sprite sheet has nothing to do with the tool's WPF chrome.

## Pipeline 1 — Tool WPF chrome (`GumIcon`)

The standard for any icon in the WPF tool **outside the tree view**. Replaces ad-hoc `{wpf:FluentIcon …}` usage from the FluentIcons.Wpf NuGet.

These icons live *inside* WPF displayer controls (e.g. the variable grid's origin/alignment/dock toggles). For how such a displayer gets attached to a variable, see [gum-tool-variable-grid](../gum-tool-variable-grid/SKILL.md).

**Components:**
- `Gum/Content/Svg/*.svg` — authored sources.
- `Gum/GumFigmaIconRipper/Program.cs` — console tool. Uses SharpVectors at build time only (no runtime dependency). Walks the SVG drawing tree; flattens into a primary geometry (≥95% opacity fills/strokes) and an optional `.Secondary` geometry (lower-opacity detail). Clips to a 2px live area inside a 32×32 viewBox.
- `Gum/Themes/GumIcons.xaml` — generated `ResourceDictionary` of `PathGeometry` keyed by SVG filename. Merged in `Frb.Styles.Defaults.xaml`, so it's globally available.
- `Gum/Themes/GumIconKind.g.cs` — generated enum + `GumIconKindMap.GetResourceKey`, plus a `TypeConverter` that accepts `Icon="folder-star"` or `Icon="FolderStar"` in XAML.
- `Gum/Controls/GumIcon.cs` + style/template in `Frb.Styles.Defaults.xaml` — templated `Control` with `PART_Path`/`PART_PathSecondary`/`PART_Image`/`PART_Box`. Both paths fill from `{TemplateBinding Foreground}`, so the icon inherits theme tint from its parent button/style. `SecondaryOpacity` defaults to 0.32.

**Adding a new icon:**

1. Author SVG following the format spec below. Drop into `Gum/Content/Svg/YourIconName.svg`.
2. Run `GumFigmaIconRipper` **from its own directory** (it computes input/output paths relative to `Directory.GetCurrentDirectory()`, expecting cwd = `Gum/GumFigmaIconRipper`):
   ```
   cd Gum/GumFigmaIconRipper && dotnet run
   ```
   Do **not** use `dotnet run --project Gum/GumFigmaIconRipper` from the repo root — the ripper will look in the wrong place. The ripper rewrites `Gum/Themes/GumIcons.xaml` and `Gum/Themes/GumIconKind.g.cs` in full.
3. Commit the SVG, the regenerated `GumIcons.xaml`, and the regenerated `GumIconKind.g.cs` together.
4. In XAML, use `<controls:GumIcon Icon="YourIconName"/>` (where `controls` is `clr-namespace:Gum.Controls`). `GumIcon` is a `Control`, not a markup extension — set the button's child element rather than its `Content` attribute when you have other content:
   ```xaml
   <Button Click="LeftButton_Click" ToolTip="Dock Left">
       <controls:GumIcon Icon="DockLeft" />
   </Button>
   ```

**Migrating from FluentIcons.Wpf:**
- Replace `Content="{wpf:FluentIcon Icon=Foo}"` with `Content="{controls2:GumIcon Icon=YourEquivalent}"` (or a nested `<controls:GumIcon Icon="…"/>` element).
- The icon library is yours, not Microsoft's — names are whatever the SVG filename was.

**Author format (passable to a designer or icon-producing AI):**

> SVG, 32×32 `viewBox="0 0 32 32"`. Keep all visible content within a 2px margin (live area 2..30; the ripper hard-clips outside this). Single solid color (any — color is replaced by theme `Foreground` at runtime). Use `fill-opacity` ≥0.95 for the primary tone and `<0.95` for any secondary detail (the ripper auto-routes secondary geometry into a `.Secondary` resource that draws at 0.32 opacity, giving free two-tone). Pure path geometry only — no gradients, filters, masks, or embedded raster; text must be converted to outlines.

## Pipeline 2 — Tool tree view (PNG `ImageList`)

Used by the main element tree (`MultiSelectTreeView`) and the flat search list. Uses `WinForms`-era PNGs because the tree view itself is a WinForms-derived control. White/grayscale PNGs are runtime-tinted from theme colors.

**To add a tree-view icon, see [gum-tool-tree-view](../gum-tool-tree-view/SKILL.md).** The short version: drop a white-on-transparent PNG into `Gum/Content/Icons/UpdatedTreeViewIcons/`, append a `TryInjectIcon` call in `InjectDynamicIcons()` matching the next `ImageIndex` constant, and add a color-map entry in `GetCurrentColorMap()` if the icon should pick up a theme color other than `Frb.Colors.Primary`.

Do not migrate tree-view icons to `GumIcon` — the tree view's `MultiSelectTreeView`/`ImageList` model expects `Image` instances, not WPF `Control`s, and the index-based lookup is wired through `ElementTreeViewManager`.

## Pipeline 3 — Forms runtime default visuals

In-game UI icons (Forms controls' check marks, arrows, etc.) come from a sprite sheet bundled with the active styling. This is a runtime concern (MonoGame/Skia/Raylib), not WPF chrome, and is handled by the Forms styling system rather than any of the above.

**To add an in-game icon, see [gum-forms-default-visuals](../gum-forms-default-visuals/SKILL.md).** Coordinates live in the `Icons` table and are referenced by sprite name from styling.

## Notable existing third-party usage to be aware of

- `wpf:FluentIcon` (FluentIcons.Wpf NuGet) — still used in `StateAnimationPlugin`, `AnchorControl.xaml`, `DockControl.xaml`, and a few ToggleButtonOptionDisplay templates. Treat as legacy: prefer `GumIcon` for any new chrome icon, and migrate existing ones opportunistically.
- `MaterialDesignThemes` `PackIcon` — used for tree-view collapse buttons (sized via `UpdateCollapseButtonSizes`). Distinct from the tree's image-list icons; left as-is for now.
- `gumcli svg <project> <element>` — exports a Gum **project element** to an SVG file (via SkiaGum's `SKSvgCanvas`). Unrelated to icon authoring; do not confuse with this pipeline.

## Gotchas

- **The ripper is not wired into MSBuild.** If you edit an SVG and forget to run it (`cd Gum/GumFigmaIconRipper && dotnet run`), `GumIcons.xaml` and `GumIconKind.g.cs` will be stale and the new icon won't appear. The CI build won't catch this — only a visual inspection will.
- **The ripper writes `GumIconKind.g.cs` and `GumIcons.xaml` externally.** Editors and agent file caches that don't re-stat after a sub-process write may show stale content. If a new `GumIconKind.YourIcon` member appears missing, re-read the file fresh (e.g. via shell `grep`) before assuming the ripper failed.
- **Live-area clip is silent.** Anything in the SVG outside `2..30` on either axis is clipped without warning. Designers used to authoring at the full 32×32 will see edges chopped.
- **Two-tone is opacity-driven, not class- or layer-driven.** SharpVectors flattens the SVG drawing tree; tone routing happens by the resolved fill/stroke alpha at draw time, not by `<g class="secondary">` or layer name. Use `fill-opacity` (or RGBA) to mark secondary detail.
- **Theme tint is `Foreground`, not a custom brush.** `GumIcon`'s template binds path `Fill` to `TemplateBinding Foreground`. Tinting a single icon differently means setting `Foreground` on the `GumIcon` (or its containing button). There's no separate `IconBrush` resource.
- **Tree-view PNG source must be white.** A pre-colored tree-view PNG will multiply with the theme color and look wrong. The tree-view pipeline does not support two-tone.
