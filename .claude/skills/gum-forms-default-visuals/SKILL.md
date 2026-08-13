---
name: gum-forms-default-visuals
description: Forms DefaultVisuals — code-only visual classes backing Forms controls. Triggers: ButtonVisual, any *Visual class in DefaultVisuals/, Styling, DefaultFormsTemplates registration, custom code-only Forms visuals.
---

# Forms Default Visuals

## What They Are

Default visuals are `InteractiveGue` subclasses that procedurally build a complete visual tree in their constructor — no Gum project file needed. Each one backs a specific Forms control (e.g., `ButtonVisual` backs `Button`). They live in `MonoGameGum/Forms/DefaultVisuals/`.

**These are *one* implementation, not *the* structure.** A control can be backed by any visual — a tool-authored component or a custom `InteractiveGue` subclass with a completely different tree — so structural features (a Window's Fill `InnerPanelInstance`, or sizing it to children via `WindowVisual.MakeSizedToChildren()`) live in the visual, never in the control. See the Visual/FrameworkElement split in **gum-forms-controls**.

## Generation

All default visuals live under `DefaultVisuals/V3/*Visual` and use `DefaultVisualsVersion.V3`/`.Newest` — the only generation; the enum has a single member.

> The Forms sprite-sheet icons are one of three icon pipelines in Gum (this one for the runtime; `GumIcon`/`PathGeometry` for tool WPF chrome; PNG `ImageList` for the tool tree view). For the umbrella overview and routing, see [gum-icons](../gum-icons/SKILL.md).

## Construction Pattern

Every `*Visual` constructor does four things in order:

1. **Build child runtimes** — `NineSliceRuntime` for background, `TextRuntime` for label, etc. Children are added via `Children.Add()`.
2. **Create a `StateSaveCategory`** — Populated with `StateSave` objects for each interaction state (Enabled, Disabled, Highlighted, Pushed, Focused, etc.). States are applied by the Forms control via `SetProperty`.
3. **Pull styling from `Styling.ActiveStyle`** — Colors, texture coordinates, font config.
4. **Attach the Forms control** — `FormsControlAsObject = new Button(this)` (or whichever control type). This triggers `ReactToVisualChanged` on the Forms side.

## Initialization — Two Paths

`GumService.Initialize()` always calls `FormsUtilities.InitializeDefaults()` first, which populates `FrameworkElement.DefaultFormsTemplates` with code-only default visuals.

If a `.gumx` project file is also passed to `Initialize()`, it then calls `FormsUtilities.RegisterFromFileFormRuntimeDefaults()`, which overrides the code-only defaults with project-defined Forms visuals (components with Forms behaviors). This is the path used when the Gum tool has authored the UI.

**Code-only projects** — call `Initialize(DefaultVisualsVersion)` with no project file. Controls get their visuals from the `*Visual` classes.

**Project-based** — call `Initialize(gumProjectFile)`. The code-only defaults are registered first, then project components replace them via `RegisterFromFileFormRuntimeDefaults()`.

## Styling.cs

Centralized style constants consumed by default visuals:

- `Colors` — Primary, Danger, Warning, Success palettes
- `NineSlice` — Texture coordinate presets (Solid, Bordered, Outlined, etc.)
- `Icons` — Coordinates for 70+ icon sprites on the shared sprite sheet
- `Text` — Font configuration (Normal, Strong, Emphasis)
- Loads embedded `UISpriteSheet.png` by default via `UseDefaults()`

`Styling.ActiveStyle` is read at **construction time only** — set it before creating controls; existing controls don't retroactively restyle.

Every `*Visual` seeds `BackgroundColor`/`ForegroundColor` from `Styling.ActiveStyle.Colors.*`, but those setters don't paint — they call `FormsControl?.UpdateState()`, which re-runs the active state's `StateSave.Apply` lambda, which derives the real color via `ColorExtensions.Adjust`/`.ToGrayscale()` off the two base colors. **Never set `visual.Background.Color` directly** — the next state transition overwrites it. To override one state's look, clear and reassign its `Apply` lambda, then call `UpdateState()`.

## Named Children Convention

Forms controls locate children by name (e.g., `"TextInstance"`, `"FocusIndicator"`, `"InnerPanel"`). If a visual omits an expected named child, the Forms control silently skips it (or throws under `FULL_DIAGNOSTICS`). When building custom visuals, match the names the Forms control looks up in its `ReactToVisualChanged`.

## Key Files

| Path | Purpose |
|------|---------|
| `MonoGameGum/Forms/DefaultVisuals/V3/*Visual.cs` | Default visual classes |
| `MonoGameGum/Forms/DefaultVisuals/V3/Styling.cs` | Centralized colors, textures, fonts |
| `MonoGameGum/Forms/FormsUtilities.cs` | `InitializeDefaults()` — registers visuals in `DefaultFormsTemplates` |
| `MonoGameGum/Forms/Controls/FrameworkElement.cs` | `DefaultFormsTemplates` dictionary and Forms-first construction |

## Cross-references

- Restyling these visuals as a distributable, palette-driven package: [gum-theming](../gum-theming/SKILL.md).
- `ColorExtensions.Adjust`/`.ToGrayscale()` live alongside `Styling` in the same file.
