---
name: gum-forms-theme-import
description: Add Forms dialog's tool-content theme system (Templates/FormsThemes/*). Triggers: FormsFileService, AddFormsViewModel, ThemeRequirements, theme.txt, GetSourceDestinations, Bubblegum theme content, adding a new Forms theme.
---

# Add Forms Theme Import

Distinct from code-only themes (`gum-theming` skill — C# `*Visual` subclasses, NuGet
packages). This is the **tool-content** side: a theme is a self-contained Gum project under
`Tools/Gum.ProjectServices/Templates/FormsThemes/<Name>/` (its own `GumProject.gumx`,
`Components/`, `Behaviors/`, `Screens/`, `Standards/`) that Add Forms copies into the user's
project. `Bubblegum` is the only one with parity today (#3527 tracks porting the rest).

## Key files

| File | Role |
|------|------|
| `Gum/GumFormsPlugin/Services/FormsFileService.cs` | `GetAvailableThemes`, `GetThemeDirectory`, `GetSourceDestinations` — computes what gets copied where |
| `Tools/Gum.Presentation/GumForms/ViewModels/AddFormsViewModel.cs` | Add Forms dialog: theme selection, save/import |
| `Tools/Gum.Presentation/GumForms/Services/ThemeRequirements.cs` | Parses optional `theme.txt` (font generator, Skia shapes) — project-level prerequisites, not content |

## A theme's qualified names must be self-prefixed — there's no folder-nesting shortcut

`Components/`, `Screens/`, `Standards/` are hardcoded literal subfolders appended directly to
the project root (`GumDataTypes/ElementReference.cs` `Subfolder`; `Behaviors/` the same way in
`BehaviorReference.cs`) — an element's file path is always `<projectRoot>/<Subfolder>/<Name>.<ext>`.
So a theme can't be namespaced by nesting its whole tree one level deeper (e.g. under
`<Theme>/Components/...`) — that path segment never resolves.

Instead, prefix the theme name into every qualified `Name` itself, the same way `Controls/` and
`Elements/` already subfolder within `Components/`: `Controls/Button` → `Bubblegum/Controls/Button`,
landing on disk at `Components/Bubblegum/Controls/Button.gucx`. Apply this to every reference
surface: `.gumx` `ComponentReference`/`ScreenReference`/`BehaviorReference` `Name` attributes,
each file's own `<Name>` tag, `BaseType="..."` instance attributes, `VariableReferences` strings
(`Components/Styles.X` → `Components/Bubblegum/Styles.X`), and a `.behx`'s
`<DefaultImplementation>`/`<BehaviorName>`. `StandardElementReference` stays unprefixed — Standards
aren't per-theme (see below). Without this, importing a second theme into the same project
silently overwrites the first theme's entire Components/Behaviors/Screens tree
(`GetSourceDestinations` maps every theme file to an identical unnamespaced destination with
`overwrite: true`).

## Standard elements: only the default theme may touch them

`GetSourceDestinations` skips `.gutx` files for any theme except `DefaultThemeName` ("Standard") —
a theme's own styling must live entirely in its Components, never in shared Standards, or
importing it stomps the destination project's real defaults (and any other theme already
imported). A theme's `Standards/` folder still needs to exist on disk with real content, though —
without it the theme's `GumProject.gumx` isn't a loadable, standalone Gum project, which is how a
theme is meant to be opened/edited/previewed directly in the tool.

## Verifying theme content changes

There's no C#-unit-test surface for a theme's XML content. Use `gumcli diff-standards` (theme's
Standards vs. `StandardElementsManager` canonical defaults — should always read "No drift found"
for a non-default theme), `gumcli check` (structural errors), and `gumcli check-references`
(unmaterialized `VariableReferences`) — see the `gum-cli` skill. Run before and after a change;
identical output (same pre-existing warnings, nothing new) is the proof a mechanical edit didn't
regress anything.

**Hand-editing a `VariableReferences` block needs the correct LHS property name for the
*target* instance's type, not the swatch's.** `Text` instances use `Red`/`Green`/`Blue`; `Rectangle`/
`Circle` fills use `FillRed`/`FillGreen`/`FillBlue`; strokes use `StrokeRed`/`StrokeGreen`/`StrokeBlue`
regardless of instance type. Getting this wrong doesn't error — if the pre-existing hardcoded
`<Variable>` value already happens to match the swatch, `gumcli check` stays clean and the visual
is unchanged, so the mistake is invisible until `gumcli check-references` reports "has
VariableReferences but missing materialized scalars" (it matches by property *name*, not value).
Run `gumcli check-references --fix` after any hand-authored reference to materialize it for real.
