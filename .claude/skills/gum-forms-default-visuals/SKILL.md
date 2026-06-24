---
name: gum-forms-default-visuals
description: Forms DefaultVisuals — code-only visual classes backing Forms controls. Triggers: ButtonVisual, any *Visual class in DefaultVisuals/, Styling, DefaultFormsTemplates registration, custom code-only Forms visuals.
---

# Forms Default Visuals

## What They Are

Default visuals are `InteractiveGue` subclasses that procedurally build a complete visual tree in their constructor — no Gum project file needed. Each one backs a specific Forms control (e.g., `ButtonVisual` backs `Button`). They live in `MonoGameGum/Forms/DefaultVisuals/`.

**These are *one* implementation, not *the* structure.** A control can be backed by any visual — a tool-authored component or a custom `InteractiveGue` subclass with a completely different tree — so structural features (a Window's Fill `InnerPanelInstance`, or sizing it to children via `WindowVisual.MakeSizedToChildren()`) live in the visual, never in the control. See the Visual/FrameworkElement split in **gum-forms-controls**.

## Two Generations

**V1 (legacy `Default*Runtime`)** — Solid-colored rectangles (`ColoredRectangleRuntime`, `RectangleRuntime`). No textures, no centralized styling. Still shipped but superseded.

**V2+ (`*Visual`)** — Nine-slice textured backgrounds via `Styling.ActiveStyle`. Uses a shared sprite sheet for backgrounds, icons, and focus indicators. V3 variants exist under `DefaultVisuals/V3/`.

> The Forms sprite-sheet icons are one of three icon pipelines in Gum (this one for the runtime; `GumIcon`/`PathGeometry` for tool WPF chrome; PNG `ImageList` for the tool tree view). For the umbrella overview and routing, see [gum-icons](../gum-icons/SKILL.md).

Both generations follow the same wiring pattern; they differ only in visual fidelity.

## Construction Pattern (V2+)

Every `*Visual` constructor does four things in order:

1. **Build child runtimes** — `NineSliceRuntime` for background, `TextRuntime` for label, etc. Children are added via `Children.Add()`.
2. **Create a `StateSaveCategory`** — Populated with `StateSave` objects for each interaction state (Enabled, Disabled, Highlighted, Pushed, Focused, etc.). States are applied by the Forms control via `SetProperty`.
3. **Pull styling from `Styling.ActiveStyle`** — Colors, texture coordinates, font config.
4. **Attach the Forms control** — `FormsControlAsObject = new Button(this)` (or whichever control type). This triggers `ReactToVisualChanged` on the Forms side.

## Initialization — Two Paths

`GumService.Initialize()` always calls `FormsUtilities.InitializeDefaults()` first, which populates `FrameworkElement.DefaultFormsTemplates` with code-only default visuals. The `DefaultVisualsVersion` parameter controls which generation (V1/V2/V3/Newest).

If a `.gumx` project file is also passed to `Initialize()`, it then calls `FormsUtilities.RegisterFromFileFormRuntimeDefaults()`, which overrides the code-only defaults with project-defined Forms visuals (components with Forms behaviors). This is the path used when the Gum tool has authored the UI.

**Code-only projects** — call `Initialize(DefaultVisualsVersion)` with no project file. Controls get their visuals from the `*Visual` / `Default*Runtime` classes.

**Project-based** — call `Initialize(gumProjectFile)`. The code-only defaults are registered first, then project components replace them via `RegisterFromFileFormRuntimeDefaults()`.

## Styling.cs

Centralized style constants consumed by V2+ visuals:

- `Colors` — Primary, Danger, Warning, Success palettes
- `NineSlice` — Texture coordinate presets (Solid, Bordered, Outlined, etc.)
- `Icons` — Coordinates for 70+ icon sprites on the shared sprite sheet
- `Text` — Font configuration (Normal, Strong, Emphasis)
- Loads embedded `UISpriteSheet.png` by default via `UseDefaults()`

## Named Children Convention

Forms controls locate children by name (e.g., `"TextInstance"`, `"FocusIndicator"`, `"InnerPanel"`). If a visual omits an expected named child, the Forms control silently skips it (or throws under `FULL_DIAGNOSTICS`). When building custom visuals, match the names the Forms control looks up in its `ReactToVisualChanged`.

## Key Files

| Path | Purpose |
|------|---------|
| `MonoGameGum/Forms/DefaultVisuals/*Visual.cs` | V2+ visual classes |
| `MonoGameGum/Forms/DefaultVisuals/Default*Runtime.cs` | V1 legacy visual classes |
| `MonoGameGum/Forms/DefaultVisuals/Styling.cs` | Centralized colors, textures, fonts |
| `MonoGameGum/Forms/FormsUtilities.cs` | `InitializeDefaults()` — registers visuals in `DefaultFormsTemplates` |
| `MonoGameGum/Forms/Controls/FrameworkElement.cs` | `DefaultFormsTemplates` dictionary and Forms-first construction |
