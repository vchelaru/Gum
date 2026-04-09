---
name: gum-tool-variable-references
description: Reference guide for Gum's variable reference system — Excel-like cross-instance/cross-element variable binding using Roslyn-parsed assignment syntax. Load this when working on VariableReferenceLogic, EvaluatedSyntax, ApplyVariableReferences, VariableChangedThroughReference, or the VariableReferences VariableListSave.
---

# Variable References

Variable references are Gum's system for keeping variables in sync across instances and elements, like cell references in a spreadsheet. A user writes `X = SomeOtherObject.X` and the left side stays updated whenever the right side changes.

## Storage

Variable references are stored as a `VariableListSave<string>` on a `StateSave`, with `Name` set to `"VariableReferences"` (or `"InstanceName.VariableReferences"` for instance-scoped references). Each string entry is one assignment line.

### Syntax

```
LeftProperty = RightSide
```

- **Left side:** An unqualified property name on the owning instance/element (e.g. `X`, `FontSize`, `Red`).
- **Right side:** A variable path, which can be:
  - Local: `OtherInstance.X` (same element)
  - Cross-element: `Components/MyComp.InstanceName.Width` (slash-separated element path)
  - Expressions: `OtherInstance.Width + 10`, `OtherInstance.Width * 2`, `!OtherInstance.Visible`
  - Literals: `X = 42`
- **Comments:** Lines starting with `//` are skipped. Invalid lines are auto-commented on validation failure.
- **Shorthand:** Writing just `OtherInstance.X` (no left side) auto-expands to `X = OtherInstance.X`.
- **Color expansion:** `Color = OtherInstance.Color` auto-expands to separate `Red`, `Green`, `Blue` assignments.

### Roslyn Parsing

The syntax is parsed as C# via Roslyn. Slashes in element paths are converted to `global::` qualified names before parsing (`Components/Foo` becomes `global::Components.Foo`) and converted back after. The `EvaluatedSyntax` class handles conversion (`ConvertToCSharpSyntax` / `ConvertToSlashSyntax`) and recursive evaluation of the right-side expression tree.

## Architecture

```
SetVariableLogic (variable change entry point)
  ├─ calls VariableReferenceLogic.DoVariableReferenceReaction()
  │    ├─ Validates lines (GetIndividualFailures)
  │    ├─ ElementSaveExtensions.ApplyVariableReferences() — writes hard values to StateSave
  │    ├─ Finds all elements that reference this element (via ObjectFinder.GetElementReferencesToThis)
  │    ├─ Applies references on those elements too (cascade)
  │    └─ DoVariableReferenceReactionOnInstanceVariableSet() — deep propagation for tunneled vars
  └─ calls VariableReferenceLogic.ReactIfChangedMemberIsVariableReference()
       └─ ModifyLines() — auto-expansion and qualification of newly entered references
```

### Key Classes

| Class | Location | Role |
|-------|----------|------|
| `VariableReferenceLogic` | `Gum/Plugins/InternalPlugins/VariableGrid/` | Tool-side orchestration: validation, reaction to changes, line expansion |
| `EvaluatedSyntax` | Same directory | Roslyn-based expression parser/evaluator; resolves right-side values via `RecursiveVariableFinder` |
| `ElementSaveExtensions` (partial) | `GumRuntime/ElementSaveExtensions.GumRuntime.cs` | `ApplyVariableReferences` — two overloads: one for `ElementSave` (save-class, tool-time), one for `GraphicalUiElement` (runtime) |
| `MainVariableGridPlugin` | Same directory as logic | Wires `CustomEvaluateExpression` delegate so the runtime can use Roslyn evaluation |

### Two Apply Paths

`ApplyVariableReferences` has two overloads:

1. **`ElementSave` overload (tool-time):** Iterates `VariableListSave` entries, evaluates right sides, writes hard values into the `StateSave` via `SetValue`. Fires `VariableChangedThroughReference` delegate when a value actually changes, which routes through `PluginManager.Self.VariableSet` — this triggers downstream reactions (font generation, etc.).

2. **`GraphicalUiElement` overload (runtime):** Similar iteration but calls `referenceOwner.SetProperty(left, value)` on the runtime object. Used for wireframe preview in the tool and at game runtime.

### Right-Side Evaluation

`GetRightSideValue` resolves the right side of an assignment:
- In the tool: `CustomEvaluateExpression` is set by `MainVariableGridPlugin` to use `EvaluatedSyntax` (Roslyn parsing with full expression support).
- At runtime (no tool): Falls back to `RecursiveVariableFinder` with simple dot-path lookup — no expression support, just direct variable resolution.

### Hard Values — Runtime Implications

Variable references write **hard values** into the `StateSave`. This means at game runtime (where `ApplyVariableReferences` on the `GraphicalUiElement` runs once at load time), the referenced values are already baked into the save data. References are **not dynamically re-evaluated** at game runtime when the source value changes — they are a tool-time binding mechanism. The runtime `ApplyVariableReferences(GraphicalUiElement)` overload exists primarily for the tool's wireframe preview.

### Cross-Element References and Cascading

When a variable changes, `DoVariableReferenceReaction` finds all elements that reference the changed element via `ObjectFinder.GetElementReferencesToThis` (filtered to `ReferenceType.VariableReference`). It then applies variable references on those elements too, creating a cascade. Modified elements are auto-saved.

### Deep Propagation

`DoVariableReferenceReactionOnInstanceVariableSet` handles a subtler case: when an instance's base element has variable references internally, and the changed variable tunnels through. It walks the reference graph to find which inner-instance variables need updating and writes the values directly into the container's state.

## Validation

`GetIndividualFailures` checks each line for:
- Parseable assignment syntax
- Forbidden left-side names (`Name`, `BaseType`, `DefaultChildContainer`)
- Left-side variable existence
- Right-side evaluability
- Type compatibility (with casting support for numeric types)
- Root variable matching for unit/alignment types (prevents mixing XUnits with YUnits, etc.)

Invalid lines are auto-commented with `//` prefix and a message is shown to the user.

## Known Gaps

- **Font generation:** `CollectRequiredFonts` (in `HeadlessFontGenerationService`) and `RecursiveVariableFinder` do not resolve variable references. If a font property (Font, FontSize, etc.) is set via a variable reference, the font file may not be generated for that value. The tool-time path works because `VariableChangedThroughReference` fires `PluginManager.VariableSet`, but headless/CLI font generation could miss these. (See issue #2414)
- **Runtime support:** The Roslyn expression evaluator has been extracted into `Runtimes/GumExpressions/` (`Gum.Expressions` NuGet). Games can opt in to expression support and use `ApplyAllVariableReferences` to propagate changes at runtime. See the `gum-runtime-variable-references` skill for details.
