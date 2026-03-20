# Layout Engine — Deep Dive

File: `GumRuntime/GraphicalUiElement.cs`

## UpdateLayout Flow

### Entry Points

```
UpdateLayout()                                         // full update
UpdateLayout(updateParent, updateChildren)             // bool flags
UpdateLayout(updateParent, childrenUpdateDepth, xOrY)  // granular
UpdateLayout(parentUpdateType, childrenUpdateDepth, xOrY)  // conditional parent
```

### Calculation Order

1. **Resolve dimension dependencies** — if width depends on height
   (PercentageOfOtherDimension, MaintainFileAspectRatio), calculate height
   first, and vice versa.
2. **UpdateDimensions** → calls `UpdateWidth` / `UpdateHeight` based on
   dependency order.
3. **UpdatePosition** → calculates X/Y considering origin, units, parent
   stacking, and wrapping.
4. **UpdateChildren** → recursively updates children to the specified depth.
5. **Conditional parent update** — if this element changed and the parent
   depends on children (RelativeToChildren, stacking, ratio), the parent
   is re-updated.

### ParentUpdateType Flags

Controls when to propagate layout upward:
- `None` — never
- `IfParentStacks` — parent uses TopToBottomStack / LeftToRightStack
- `IfParentWidthHeightDependOnChildren` — parent uses RelativeToChildren
- `IfParentIsAutoGrid` — parent uses AutoGrid
- `IfParentHasRatioSizedChildren` — parent has Ratio-sized children
- `All` — always propagate

### ChildType Categorization

Children are bucketed for update ordering:
- **Absolute** — no parent/sibling dependency
- **Relative** — depends on parent dimensions
- **StackedWrapped** — positioned by stacking/wrapping logic

## Layout Suspension

For bulk changes (loading screens, batch property sets), suspend layout to
avoid O(n^2) recalculations:

```csharp
GraphicalUiElement.IsAllLayoutSuspended = true;  // global, thread-static
// ... set many properties ...
GraphicalUiElement.IsAllLayoutSuspended = false;
root.ResumeLayoutUpdateIfDirtyRecursive();
```

Or per-instance:
```csharp
element.SuspendLayout(recursive: true);
// ... changes ...
element.ResumeLayout(recursive: true);
```

### DirtyState Tracking

When layout is suspended, `MakeDirty()` records what needs updating:
- `ParentUpdateType` — what parent conditions to check
- `ChildrenUpdateDepth` — how deep to recurse
- `XOrY` — which axis changed (null = both)
- `IsFontDirty` — font-related property changed

On resume, only the accumulated dirty state is processed.

## Stacking & Wrapping

When a parent has `ChildrenLayout = TopToBottomStack` or `LeftToRightStack`:
- Children are positioned sequentially along the stack axis
- `StackSpacing` adds gaps between children
- `WrapsChildren = true` wraps overflow to the next row/column
- `StackedRowOrColumnDimensions` allows per-row/column sizing

Wrap detection: after initial positioning, if a child's trailing edge exceeds
the parent's bounds, it wraps to the next line.

## Performance Considerations

- **xOrY parameter** — pass `XOrY.X` or `XOrY.Y` when only one axis changed
  to skip recalculating the other.
- **childrenUpdateDepth** — limit recursion depth when only direct children
  are affected.
- **UpdateLayoutCallCount** / **ChildrenUpdatingParentLayoutCalls** — static
  counters for diagnosing layout storms.
- **Partial child updates** — children are categorized by type so only
  relevant subsets are recalculated.
