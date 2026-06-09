---
name: gum-tool-variable-references
description: Gum variable references — Excel-like cross-instance/cross-element binding via Roslyn-parsed assignments. Triggers: VariableReferenceLogic, EvaluatedSyntax, ApplyVariableReferences, VariableChangedThroughReference, VariableReferences VariableListSave.
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
  - Conditional/comparison/logical operators: ternary `cond ? a : b`, `==`, `!=`, `<`, `>`, `<=`, `>=`, `&&`, `||`, `!`
  - Category-state LHS: `<CategoryName>State = "StateName"` assigns the categorical state by name
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

### Author-Time Materialization Is The Model

When the tool resolves a `VariableReferences` row — interactive edit, Make Default, the handful of `ElementCommands` paths — `ApplyVariableReferences` writes the evaluated right-hand-side as a **hard scalar** into the same `StateSave`'s `Variables`. The references row and the materialized scalar are *both* persisted to disk. Lookup never re-evaluates the reference: scalar resolution finds the materialized value directly.

This is the load-bearing fact for reasoning about VariableReferences. A few consequences fall out of it:

- **Files written by paths that bypass `ApplyVariableReferences` are inconsistent.** AI-authored XML, hand edits, programmatic creation, and "delete the scalars to force a reapply" workflows all leave a state with a `VariableReferences` row but no materialized scalars. The Variables tab / scalar lookup then falls through to the default state instead of the reference's resolved value. Gum currently has no load-time repair for this; the only fix is to retrigger a path that runs `ApplyVariableReferences` (e.g. re-edit and re-save the reference).
- **References are *snapshots*, not live bindings.** Once materialized, the scalar is what every reader sees. If the right-hand side changes elsewhere, the snapshot stays stale until propagation runs again. The cascade in `DoVariableReferenceReaction` (described above) is what keeps snapshots fresh when authoring; nothing keeps them fresh on its own.
- **Precedence is decided at author time, not at lookup time.** The materialized scalar lives in `state.Variables` like any other authored value, so the normal "most specific wins" scalar walk decides who wins between a state-reference and a more-local explicit override. There is no separate evaluation pass that re-asserts the reference.
- **Inheritance interacts naturally.** Materialization happens on the element that *authors* the reference. Derived components and instances find the materialized scalar via the existing recursive state walk; they do not need their own copy. (The walk going up the instance type's `BaseType` chain was previously broken in `StateSaveExtensionMethods.cs` — see fix history on `fix/variable-references-inheritance-display`.)

When designing fixes in this area, the question is almost always "did `ApplyVariableReferences` run on the state that owns the reference?" — not "should the lookup do something smarter when it walks past a `VariableReferences` row?"

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

## Behavior-Sourced Tool-Only References

A separate variable-reference flavor lives on `BehaviorSave.ToolOnlyVariableReferences` (a `List<string>`, not a `VariableListSave`). Used by Forms property promotion (#2637 v2): a behavior declares e.g. `ButtonCategoryState = IsEnabled ? "Enabled" : "Disabled"` so the design-time wireframe reflects authored `FormsProperty` values. **Strictly tool-only** — applied by `BehaviorToolOnlyReferencesApplier` invoked from `VariableReferenceLogic.DoVariableReferenceReaction` immediately after the state-level apply. The runtime never traverses this list; the wrapped Forms control's setter (e.g. `FrameworkElement.IsEnabled` → `UpdateState()`) owns the visual at runtime, so applying the reference there would double-write. See `gum-forms-behaviors` for the property-promotion pipeline.

The applier passes a `fallback` resolver into `EvaluatedSyntax.FromSyntaxNode` so identifiers not authored on state fall back to the behavior's `FormsProperty.Value` declarations (mirrors WPF DependencyProperty default values). Plumbed through `RecursiveVariableFinder.Fallback` — any caller that needs the same "default-when-state-empty" shape can use it.

**Evaluating "defaults-only" (footgun):** To ask "what would this resolve to with *nothing* authored?", do **not** pass an empty `StateSave` to `FromSyntaxNode` — `RecursiveVariableFinder` resolves by name through `ParentContainer`, so an empty state owned by a real element still leaks that element's authored values (routes to its `DefaultState`). Call `EvaluatedSyntax.FromSyntaxNodeUsingDefaultsOnly(node, fallback)` instead; it owns the state with a throwaway empty element so every identifier falls through to the `fallback`. The applier uses this for the "skip if equal to resting wireframe" check (issue #3082).

## Known Gaps

- **Font generation:** `CollectRequiredFonts` (in `HeadlessFontGenerationService`) and `RecursiveVariableFinder` do not resolve variable references. If a font property (Font, FontSize, etc.) is set via a variable reference, the font file may not be generated for that value. The tool-time path works because `VariableChangedThroughReference` fires `PluginManager.VariableSet`, but headless/CLI font generation could miss these. (See issue #2414)
- **Runtime support:** The Roslyn expression evaluator has been extracted into `Runtimes/GumExpressions/` (`Gum.Expressions` NuGet). Games can opt in to expression support and use `ApplyAllVariableReferences` to propagate changes at runtime. See the `gum-runtime-variable-references` skill for details.
