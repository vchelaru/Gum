---
description: Reference guide for runtime variable reference propagation and the optional Gum.Expressions NuGet. Load this when working on ApplyAllVariableReferences, GumExpressionService, runtime styling/theming, or the GumExpressions project.
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

`GumExpressionService.Initialize()` sets `ElementSaveExtensions.CustomEvaluateExpression` to a Roslyn-based evaluator. The evaluator is `EvaluatedSyntax`, which was extracted from the Gum tool into this project.

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

The decoupling mechanism is `ElementSaveExtensions.CustomEvaluateExpression` — a static `Func<StateSave, string, string, object>` delegate. When null, falls back to `RecursiveVariableFinder` (simple lookups only). When set by `GumExpressionService.Initialize()`, uses Roslyn for full expression support.

## Known Limitation

Division expressions (`Width / 2`) are broken when routed through `EvaluatedSyntax.ConvertToCSharpSyntax` because it replaces all `/` characters with a Unicode placeholder for path disambiguation. The division operator gets corrupted. Addition, subtraction, and multiplication work correctly.
