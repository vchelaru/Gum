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

## Porting a new theme (#3527): do the landmines up front, not reactively

Each gotcha below is cheap to prevent up front and expensive to discover one at a time via manual
testing after the fact. Sequence a new theme port like this:

1. Namespace the whole tree and scan physical files on disk (not just the `.gumx`'s reference
   list) for orphans, before anything else.
2. Build `Styles.gucx` matching the code-only `*Colors`/`*Text` class names first — every control
   wires against it, so getting names right early avoids a rename pass later.
3. Wire every control's Default state and every categorized state as you build each one; run
   `AllColorVariables_ShouldBeStylesWired` from the start, not as a final audit.
4. After every batch of edits: `gumcli check`/`check-references`/`diff-standards`, **and** a
   rebuild plus fresh-project Add Forms import in the actual tool — `gumcli` never exercises the
   postbuild-copy path where duplicate-import bugs live.

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
landing on disk at `Components/Bubblegum/Controls/Button.gucx`. Apply this to every Component/Screen
reference surface: `.gumx` `ComponentReference`/`ScreenReference` `Name` attributes, each file's own
`<Name>` tag, `BaseType="..."` instance attributes, `VariableReferences` strings
(`Components/Styles.X` → `Components/Bubblegum/Styles.X`), and a `.behx`'s
`<DefaultImplementation>` (points at a themed *component*, so it gets the prefix even though the
behavior file itself doesn't — see below). `StandardElementReference` and `BehaviorReference` stay
unprefixed. Without this, importing a second theme into the same project silently overwrites the
first theme's entire Components/Screens tree (`GetSourceDestinations` maps every theme file to an
identical unnamespaced destination with `overwrite: true`).

**A file that exists on disk but isn't referenced in the theme's own `.gumx` still gets copied and
imported** — `GetSourceDestinations` walks every `.gucx`/`.behx` file under the theme folder, not
just the ones listed as a `ComponentReference`/`BehaviorReference`. A rename pass built only from
the `.gumx`'s reference list misses these orphans, leaving a stale un-prefixed `<Name>` that
duplicates or collides with the correctly-renamed content. Verify by scanning physical files
directly (`Directory.GetFiles(..., SearchOption.AllDirectories)`), not `project.AllElements`, which
only reflects what's registered.

## Behaviors are shared infrastructure, not per-theme content

Unlike Components (a theme's look) and Screens (a theme's demo content), Behaviors declare the
generic category-state/`FormsProperty` contract every theme's visuals plug into — the same
`ButtonBehavior`, `CheckBoxBehavior`, etc. every theme and project needs. They live flat at
`Behaviors/*.behx`, unprefixed, matching how `Standards/` is shared rather than namespaced. A
`.behx`'s own `<Name>` and a `.gucx`'s `<ElementBehaviorReference><BehaviorName>` stay unprefixed;
only `<DefaultImplementation>` (which names a themed *component*) gets the theme prefix. Each
theme still ships its own copy of every behavior file (same reason as Standards, above) — those
copies are expected to be identical to every other theme's, so importing one theme after another
just re-overwrites the shared file rather than duplicating it. There's no mechanism yet to
guarantee the copies actually stay identical as Forms controls gain new properties — see #4070.

## Standard elements: only the default theme may touch them

`GetSourceDestinations` skips `.gutx` files for any theme except `DefaultThemeName` ("Standard") —
a theme's own styling must live entirely in its Components, never in shared Standards, or
importing it stomps the destination project's real defaults (and any other theme already
imported). A theme's `Standards/` folder still needs to exist on disk with real content, though —
without it the theme's `GumProject.gumx` isn't a loadable, standalone Gum project, which is how a
theme is meant to be opened/edited/previewed directly in the tool.

## A theme's Styles component must mirror its code-only palette by name, not just value

A theme's centralized styling component (e.g. `Components/Bubblegum/Styles.gucx`) should expose
exactly the color/text tokens its code-only counterpart defines (`Themes/Gum.Themes.<Name>.*`'s
`*Colors`/`*Text` classes — see `gum-theming`), under the *same names*, with no extra swatch that
class doesn't define. A swatch can hold the correct value under the wrong name — verify by diffing
the tool content's swatch/text-instance names against the code-only class's property names
directly, not by eyeballing rendered colors.

## "Wire the controls to Styles" means every state, not just Default

A control can look fully wired at a glance — its Default state references `Styles.*` — while every
one of its categorized states (Enabled/Disabled/Highlighted/Pushed/...) still hardcodes the same
colors directly, since Default and category states are separate `StateSave`s with independent
`VariableReferences`. `Tests/Gum.ProjectServices.Tests/BubblegumTemplateTests.cs`'s
`AllColorVariables_ShouldBeStylesWired` is the authoritative check-all for this — a fully
transparent fill (`FillAlpha` set to `0`) is exempted, since its RGB is never visible.

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

None of this exercises the actual runtime copy path, though: `gumcli` reads straight from
`Templates/FormsThemes/<Theme>/`, but Add Forms reads from `Gum/bin/<Config>/Content/FormsThemes/<Theme>/`,
populated by `GumFormsPlugin`'s postbuild `xcopy`. `xcopy` never deletes — a rename or removal in
the template leaves the stale old file sitting in any already-built output, so importing the theme
pulls in both the old and new copy. The postbuild step deletes the theme's output folder before
`xcopy`-ing to prevent this; verify a source-side rename actually lands clean by planting a
throwaway file in the built output and confirming a rebuild removes it.
