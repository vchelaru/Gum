# 0022. Re-evaluate variable references live, within one component

- **Status:** Accepted
- **Date:** 2026-10-05
- **Deciders:** Victor Chelaru, Claude

## Context

Variable references (`Y = Math.Sin(Progress)`) are evaluated once. At runtime that happens in
`SetVariablesRecursively` (`GumRuntime/ElementSaveExtensions.GumRuntime.cs`), in the order the rows
are listed. Nothing re-runs them afterward, so animating `Progress` leaves `Y` at its load-time
value (#5661). Chains such as `A = B`, `B = C` only work if the rows happen to be listed in
dependency order.

The two places that re-evaluate today are both explicit and both coarse:

- **Across components:** `ApplyAllVariableReferences(project)` sorts elements by dependency, then the
  caller runs `RefreshStyles()`. Intended for theming; stays as is.
- **Tool animation preview:** `AnimatedReferenceReevaluator` (`Tools/Gum.Presentation/StateAnimationPlugin`,
  #5662) picks the rows that read an animated variable and applies them to the tick's throwaway
  state. It is internal to the tool, so runtime playback does not use it.

Three facts about the runtime shape the options:

1. **No single write point.** `SetProperty(string, object)` is the string path used by states,
   animations and references. Typed properties such as `X` write their backing field directly.
   Making every write notify would touch dozens of properties across `GraphicalUiElement` and its
   subclasses.
2. **Evaluation re-parses every call.** `GumExpressionService.EvaluateExpression` runs
   `SyntaxFactory.ParseExpression` on the expression text each time. That is acceptable at load and
   too costly per frame; runtime animation is already allocation-tested (`AnimationAllocationTests`).
3. **Animated values already sit in a `StateSave`.** `AnimationRuntime.ApplyAtTimeTo` builds a state
   and applies it, and the evaluator reads from a `StateSave`, so evaluating against that state
   sees animated values without new plumbing.

A component is a closed set of variables, so a dependency graph over one component is small and fully
known. Cross-component propagation is a different problem and stays explicit.

## Decision

We will re-evaluate same-component variable references automatically when a source variable changes,
using a push model scoped to one component.

- **Cache parsed expressions by their text.** Each row is parsed once and keeps the list of variables
  it reads. An edited row is a new string and parses fresh, so nothing goes stale. The tool and the
  runtime share this cache.
- **Build a per-component graph from the rows.** Edges run from the variables a row reads to the
  variable it writes. Evaluation runs in topological order, which replaces list order. A cycle is
  reported and skipped, not looped on. The graph is derived data: built at load at runtime, and
  rebuilt when a component's `VariableReferences` change in the tool.
- **Push, not pull.** When a source variable changes, the graph finds the dependent rows, evaluates
  them in order, and writes results with `SetProperty`, so layout runs the normal way. Pull
  (evaluate on read) is not available because variable reads are plain field reads with no hook.
- **One entry point, `NotifyVariableChanged(name)`.** Everything that changes a variable calls it.
- **Share the logic.** The affected-row selection in `AnimatedReferenceReevaluator` moves into
  `GumCommon` so the tool preview and runtime playback use the same code.

Rollout, one step at a time with each step measured before the next:

1. **Compile-once cache.** Useful alone; removes the per-frame parse.
2. **Graph, and the shared selection logic.** Fixes list-order dependence.
3. **Trigger from animation.** `ApplyAtTimeTo` notifies with the variables it set. Closes #5661. Also
   adds an explicit `RefreshReferences(names)` for code-driven changes.
4. **Trigger from `SetProperty`.** Every string-path write pushes automatically.
5. **Trigger from typed setters, incrementally.** One property at a time, and each setter notifies
   only when its element has a row that reads it, so elements without references pay nothing.

Steps 4 and 5 change who calls `NotifyVariableChanged`, not the graph, the cache or the evaluator.
`RefreshReferences` stays valid as the manual override.

### As built

- The cache holds the parsed syntax tree by expression text (`ParsedExpressionCache` in `Gum.Expressions`).
  The variables a row reads come from a lexical scan in `GumCommon` (`VariableReferenceGraph`), not
  from the tree, so the graph works without Roslyn. The scan may list a path that is not a variable,
  which costs one unneeded evaluation and never misses a read.
- Evaluation is against a small throwaway state holding only the changed values, owned by the element so
  every other name resolves from authored values. Each result is written into that state so a chain
  reads it, then onto the live visual. Live re-evaluation therefore reads unchanged variables as
  authored, not as the live object currently holds them.
- `ApplyState` collects the changes of every `SetProperty` inside it and re-evaluates once at the end.
  Animations need no call of their own.
- Element load applies its rows as before, then evaluates the rows again in dependency order when any row
  reads another's result. Before this, a chain read stale values at load.
- Step 5 covers `X`, `Y`, `Width`, `Height`, `Rotation` and `Visible`. Other typed properties still need
  `RefreshReferences`, or a setter that reports itself the same way.
- A row in a cycle is skipped by live re-evaluation and listed in `VariableReferenceGraph.CyclicRows`.

## Consequences

- Animated variables drive dependent variables, including chains, in the tool preview and at runtime.
- Until step 5, code that sets a typed property (`element.X = 5`) does not update dependents without
  `RefreshReferences`. This is the same contract as `ApplyAllVariableReferences` plus
  `RefreshStyles` today, narrowed to one component.
- Cross-component and styling references stay explicit. A cross-component push model is a separate
  decision, if it is ever wanted.
- Per-frame cost is bounded by the rows the animation touches, not by the component's total
  reference count. This is unmeasured; step 1 includes a measurement before step 3 depends on it.
- The graph and cache are new state to keep correct as rows are edited. Tests should pin rebuild on
  edit, cycle handling, and chain ordering.
- Expression evaluation still needs `Gum.Expressions` (Roslyn). Without it only simple dot-path
  references work, and live updates are limited to those.

## Alternatives considered

- **Re-apply every same-component row after each animation step (the suggestion in #5661).** Simple,
  but cost grows with the component's reference count, it keeps list-order evaluation, and it does
  not extend to non-animation triggers.
- **Pull (lazy) evaluation.** Needs a hook on every variable read, and layout reads fields directly.
- **Make every setter notify now.** Gives full automatic behavior but touches every typed property
  before the graph is proven. Kept as step 5 instead.
- **Express the built-in layout units as references.** Considered as a shader-style foundation. It
  would put expression evaluation on the layout hot path and make units opaque to the editor and
  codegen. References stay an escape hatch above the units, not a replacement for them.
