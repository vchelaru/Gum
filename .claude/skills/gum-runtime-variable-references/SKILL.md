---
name: gum-runtime-variable-references
description: Runtime variable reference propagation and the optional Gum.Expressions NuGet. Triggers: ApplyAllVariableReferences, GumExpressionService, runtime styling/theming, GumExpressions project.
---

# Runtime Variable References

## Overview

Variable references defined in the Gum tool can be re-evaluated at runtime. The primary use case is theming: modify centralized style values in code, propagate them across all elements, then create UI.

## Key API

### ApplyAllVariableReferences (GumRuntime)

Extension method on `GumProjectSave` in `ElementSaveExtensions.GumRuntime.cs`. Iterates all elements (standards, components, screens) and applies variable references on every state including category states (e.g., `ColorCategory`). Uses topological sort so dependencies are applied first — if B references A, A is applied before B. Handles circular dependencies gracefully (appends them at the end).

No Roslyn dependency — lives in GumRuntime/GumCommon, available to all platforms.

### GumExpressionService (Gum.Expressions NuGet)

Located in `Runtimes/GumExpressions/`. Provides Roslyn-based expression evaluation for arithmetic in variable references (`Width + 10`, `Width * 2`). Optional — without it, only simple dot-path lookups work (`OtherInstance.Width`).

`GumExpressionService.Initialize()` sets `ElementSaveExtensions.CustomEvaluateExpression` to a Roslyn-based evaluator. The evaluator is `EvaluatedSyntax`, which was extracted from the Gum tool into this project. Conditional (ternary), comparison (`==`, `!=`, `<`, `>`, `<=`, `>=`), and logical (`&&`, `||`, `!`) operators all flow through this same path — they work at runtime when `Gum.Expressions` is wired.

### Two Apply Overloads (ElementSaveExtensions)

- `ApplyVariableReferences(ElementSave, StateSave)` — writes hard values into the StateSave. Use before creating UI.
- `ApplyVariableReferences(GraphicalUiElement, StateSave)` — sets properties on live runtime visuals via `SetProperty`.

### Values the saved type cannot hold (#6414)

A reference carries what code set, not only what a project can store. Reference reads use `TryGetPropertyExact` (internal; the public `TryGetProperty` stays save-safe for the snapshot serializer): X/Y units with no `PositionUnitType` value (`PercentageOfFile`, `PercentageOfOtherDimension`, `MaintainFileAspectRatio`, X `PixelsFromMiddleInverted`) read as the `GeneralUnitType`, and `EvaluatedSyntax.CastTo` leaves a `GeneralUnitType` alone instead of reinterpreting its number. `SetProperty("FontSize", ...)` accepts a float. Known gap: with Gum.Expressions a fractional FontSize reaches a reference as an int, because every result is cast to the saved variable type (FontSize is `int`; fixing it needs a saved-format change). `TypedSetterSweep` asserts exact values for every variable and enum value and names that gap (`IsKnownExpressionGap`).

### Variables of different types without Gum.Expressions

Without `Gum.Expressions` nothing converts the right side, so a value of another declared type would throw in a setter (or blank a string). `TryReadReference` and `ReportedMismatch` (`ElementSaveExtensions.GumRuntime.cs`) compare the declared types of both variables for every row, at creation, live and in `ApplyAllVariableReferences`. A mismatch keeps the target's value and reports through `CustomSetPropertyOnRenderable.RaisePropertyAssignmentError` (the `[Gum]` `Console.Error` line plus `PropertyAssignmentError`), reached from GumRuntime by the internal hook `ElementSaveExtensions.ReportPropertyAssignmentError` that each platform wires; each owner reports a given row once (`IsFirstReport`), so a row that changes every frame does not flood the console; the text says to add `Gum.Expressions` only when it converts that pair (int/float/double among themselves, anything into a string, a string into an enum). What the setters already take is not a mismatch: any int or float into `FontSize`, an int into an enum. Tests: `ReferenceBetweenMismatchedTypesTests`. The check judges declared types, not values, because a live read can hand a float to an int variable (`FontSize`), which would pass a value check at update time and fail at creation. Live evaluation seeds untyped values, so `FindSourceType` looks the declared type up on the element and caches it; a valid row must allocate nothing extra (`ReferenceReevaluationAllocationTests`).

### A cast Gum.Expressions refuses

With `Gum.Expressions` the package only adds evaluation power: a reference it cannot apply is reported like the same pair without it, never dropped silently. `EvaluatedSyntax.CastTo` returns false for a bool or text into a number, an enum into a number, bool or float or double into an enum, a name that is no value of the enum, an enum of another enum type, a float or double the int or float cannot hold, and any value into a type it does not know (a state category, or a variable with no declared type). The service value is null then, so `TryReadReference` asks the internal hook `ElementSaveExtensions.CustomFindRejectedCast` (set by `GumExpressionService.Initialize()`, asked only after `CustomEvaluateExpression` returned null, so wrappers of that delegate still see one call) for the value that was refused and `ReportedRejectedCast` reports it once per row per owner through the shared `ReportNotApplied` wording; the variable keeps its value. A missing variable has no value and stays silent, with or without the package. `ReportedMismatch` now judges only the no-package path. Tests: `ReferenceFailedCastTests` (the source x target matrix).

### Nullable variables

A typed set reports null too (`label.MinWidth = null` reaches `Shadow.MinWidth = MinWidth`). Live re-evaluation (`VariableReferenceGraph.Evaluate`) writes a null result only to a variable whose type ends in `?` and only when the right side is a plain reference to an existing variable of a compatible type (`MinWidth = Other.MinWidth`; a bool or string read into a `float?` is a failed cast); any other null (unresolved name, failed expression, arithmetic on null) means "nothing evaluated" and keeps the value. `EvaluatedSyntax.CastTo` accepts `float?`/`int?` targets. `TypedSetterSweep` (shared by the MonoGame and Skia tests) covers nullable variables with and without Gum.Expressions, including set back to null.

## Architecture

```
GumCommon (no Roslyn)
    ↑
Gum.Expressions (adds Roslyn) — optional NuGet
    ↑           ↑
Gum Tool    Game (opt-in)
```

The decoupling mechanism is `ElementSaveExtensions.CustomEvaluateExpression` — a static `Func<StateSave, string, string, GraphicalUiElement?, object>` delegate. When null, falls back to `RecursiveVariableFinder` (simple lookups only). When set by `GumExpressionService.Initialize()`, uses Roslyn for full expression support. The 4th argument is the live, already-laid-out `GraphicalUiElement` being applied against (when one exists) — it lets a reference resolve the runtime-computed Absolute* properties, which don't exist on authored `StateSave` data at all (see `gum-tool-variable-references` for the resolution mechanism). `ApplyVariableReferences(GraphicalUiElement, StateSave)` supplies its own top-level element automatically; `ApplyVariableReferences(ElementSave, StateSave)` only resolves Absolute* references when called with its optional `liveRoot` argument.

After applying variable references, call `GraphicalUiElement.RefreshStyles()` or
`GumService.Default.RefreshStyles()` to push the updated values to live visuals. For a deep
dive into how this works end-to-end, see the **gum-variable-deep-dive** skill.

### `global::Localization.CurrentLanguage`

Reserved identifier (int, mirrors `ILocalizationService.CurrentLanguage`) resolved in `EvaluatedSyntax` against `Gum.Localization.LocalizationRuntimeState.Current` — a GumCommon-level static so `Gum.Expressions` can read it without depending on any platform runtime. `CustomSetPropertyOnRenderable.LocalizationService` (per-platform-compiled) forwards to it. `VariableReferenceGraph.ScanReads` lists it as the read `VariableReferenceGraph.CurrentLanguageReadName`, and `GraphicalUiElement.RefreshLocalization` (run by `GumService` on `CurrentLanguageChanged`) reports that name to each element with an `ElementSave`, so same-component rows reading the language re-evaluate on a switch with no explicit call. Cross-element rows still need `ApplyAllVariableReferences()` + `RefreshStyles()`. Tests: `CurrentLanguageReferenceTests`.

## Live re-evaluation within a component

Rows on a component or screen that read a variable re-evaluate when it changes: see `VariableReferenceGraph` (`GumCommon/Runtime`) and `GraphicalUiElement.NotifyVariablesChanged`. `SetProperty`, `ApplyState` (batched, so animations need no call) and the typed `X`/`Y`/`Width`/`Height`/`Rotation`/`Visible` setters report changes. Any typed setter that calls `NotifyPropertyChanged` (such as `TextRuntime.Text`) reports through it, reading the new value with `TryGetProperty`; `TextRuntime` `Red`/`Green`/`Blue`/`Alpha` call the protected `ReportTypedPropertyChanged(name)` themselves. `TextRuntime` `FontSize`, `IsBold`, `IsItalic` and `OutlineThickness` call `ReportTypedPropertyChanged` too; `TryGetProperty` returns `FontSize` as the int the variable is, since the float would fail the `(int)` cast when written to another element. A typed property that is also a Gum variable reports on the runtimes the sweep covers (MonoGame, Skia, Apos), including the layout ones (`StackSpacing`, the `*Units`, `*Origin`, `ChildrenLayout`, `Min`/`Max` sizes and so on); `TypedSetterSweep` (`Tests/Shared`) lists what is covered and holds the skip list and its conditions, and `VariableReferenceTypedSetterTests` runs it and goes red for a new setter that does not report (a setter that wraps another, like `DropshadowRed` over `DropshadowColor`, must report its own name). A Skia shape runtime whose dispatcher branch in `Runtimes/SkiaGum/CustomSetPropertyOnRenderable.cs` writes a variable straight to the renderable (instead of calling `TrySetPropertyOnRuntime` first) leaves the runtime getter stale, so a reference never sees the value (#6406; the Svg `SourceFile` branch was the same, #6422). `SvgRuntime`/`LottieAnimationRuntime` `SourceFile` report the assigned path with `ReportTypedPropertyChanged(name, string)`, since `TryGetProperty("SourceFile")` recovers a texture name, not the path; the sweep covers them (Sprite's `SourceFile` always reads null and is excluded, `RenderTargetTextureSource` is a renderable assigned by element name and is not swept). A property that changes other variables reports them too (#6410): `GraphicalUiElement.NotifyPropertyChanged` reports `Red`/`Green`/`Blue`/`Alpha` (and the `1`/`2` forms) for `Color`/`Color1`/`Color2`, `ArcRuntime` also reports `Red1`..`Alpha1` for `Red`..`Alpha`/`Color`, and the sweep finds these by setting each property (including non-variable `Color`) on a fresh element and giving every variable that changed its own reference row. It runs for Apos shapes in `MonoGameGum.Shapes.Tests`; `AposShapeRuntime` setters call `NotifyPropertyChanged`. `CurrentChainName` is not swept (it keeps only a name the element has); `CurrentChainNameReferenceTests` covers it. Setting a nullable variable back to null is reported too (see Nullable variables). A property that calls neither `NotifyPropertyChanged` nor `ReportTypedPropertyChanged` needs `RefreshReferences`. Design: `Direction/decisions/0022-reactive-variable-references-within-a-component.md`.

- Live evaluation reads every input at its live value (#6411): `NotifyVariablesChanged` seeds the evaluation state with the changed values plus the live value of each other input the affected rows read (`AddLiveInputs`). Typed values come from `TryGetProperty`; a custom variable (no property on the visual) comes from `_referenceInputs`, the last value reported or written by a row (`RecordReferenceInput`). A null or unreadable input falls back to the authored value, since a null cannot be put in the state. Styling and cross-element references stay explicit.
- `SetVariablesRecursively` marks the element being loaded (`BeginReferenceLoad`): what it writes to that element, and rows reading that element from its container, report to nobody, since the load applies those rows itself. What it writes to the element's children reports as usual, so a screen override (`Label1.Text`), an exposed variable, a nested component's row or a hot reload reaches the child's own rows, which were evaluated at its creation against defaults. Do not wrap a load in `BeginSuppression` again: that silences the children too (#6385).
- `VariableReferenceGraph.BeginSuppression` is for row evaluation only: `Evaluate` writes each result silently, then notifies a child owner's own rows explicitly. A derived component whose base owns rows has them evaluated again after its own values apply (`HasInheritedRows`).
- A new reporting path must check `GetReferenceAudience` before boxing a value; the zero-allocation guard is `ReferenceReevaluationAllocationTests`.

## `@`, `%` and `Index` at runtime

All three work in a game that calls `GumExpressionService.Initialize()`. The `GraphicalUiElement` overload of `ApplyVariableReferencesOnSpecificOwner` rewrites `@` through `ResolveOwnerPrefix`, and `Index` reads the element's own instance order when no live tree is passed. Without Gum.Expressions, `@Width` still resolves as a plain lookup, but `%`, `Index` and every other operator need it (see `gum-tool-variable-references`).

## One analysis of which rows read what

`VariableReferenceGraph.ScanReads` is the only place that decides what a row reads. `ReferenceDependencies` builds on it for cross-element questions, and `ApplyAllVariableReferences`, `ObjectFinder.GetElementReferencesToThis` and the tool's `VariableReferenceLogic.ApplyReferencesToElement` all call it. Match references through it, not by string prefix. `ApplyVariableReferences(ElementSave, StateSave)` runs a state's rows in `VariableReferenceGraph.LoadOrderRows` order, so written order does not matter.

