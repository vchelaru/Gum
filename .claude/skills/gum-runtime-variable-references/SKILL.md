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
