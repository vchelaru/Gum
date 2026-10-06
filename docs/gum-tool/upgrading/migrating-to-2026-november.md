# Migrating to 2026 November

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 October` to `2026 November`.

## What Changed at a Glance

`2026 November` changes how stacks position and size around their children. A child after the first in a stack now ignores its origin on the stacking axis, the same way it already ignored its units, so it no longer overlaps its previous sibling. A stack sized to its children that has a `Max Height` or `Max Width` and does not wrap now grows to its max when its children need more space, instead of stopping at the last child that fits. A stack with `Wraps Children` checked that is sized to its children and has a max now measures its widest row or column, not only its first. In a stack with `Wraps Children` checked, each row or column is now the parent of its children on the other axis, so a centered or bottom aligned child lines up within its row instead of within the whole stack. A container sized to its children now counts a `Relative to Max of Children or Parent` child at its `Min Width` or `Min Height`. All of these changes affect layouts in the Gum tool and the runtime. It also adds an `AnimationChainFinished` event to sprites and nine slices playing an animation chain, which replaces `AnimationChainCycled` as the way to detect the end of a chain that does not loop.

## Breaking Changes and Migrations

### Stacked Children Ignore Their Origin on the Stacking Axis

A child after the first in a stack now ignores its origin on the stacking axis:

* In a `Top to Bottom Stack`, later children treat `Y Origin` as `Top`.
* In a `Left to Right Stack`, later children treat `X Origin` as `Left`.

Before this version, the origin still applied after the stack positioned the child. A `Center` origin pulled the child back over its previous sibling by half its size, and a `Bottom`, `Baseline`, or `Right` origin pulled it back by its full size, covering the previous sibling.

Stacks already ignored a later child's units on the stacking axis, so a child with `Y Units` of `Pixels From Center` still stacked below its previous sibling. The origin now follows the same rule, and a child's units and origin on the stacking axis have no effect unless it is the first child.

These cases are unchanged:

* The first child in a stack uses its units and origin on both axes.
* The origin on the other axis works normally. A child in a `Top to Bottom Stack` can still use `X Origin` of `Center` to center itself horizontally.
* `X` and `Y` values still add space between a child and its previous sibling.

This affects you only if a stacked child other than the first uses a `Y Origin` other than `Top` in a `Top to Bottom Stack`, or an `X Origin` other than `Left` in a `Left to Right Stack`. Calling `Anchor` with a value such as `Center` or `Bottom` sets these origins, so code that anchors a stacked child is affected too. Such a child now sits right after its previous sibling instead of overlapping it.

To migrate, open your screens and components in the Gum tool and check any stacks whose children use a non-default origin. If you relied on the overlap, for example to pull a child up so it overlaps the one before it, give the child a negative `Y` (or `X`) value instead. A negative value moves the child back toward its previous sibling by that many pixels.

❌ Old (child overlaps the previous sibling by half its height):

```csharp
// Initialize
var child = new ContainerRuntime();
child.Height = 50;
child.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
stackPanel.AddChild(child);
```

✅ New (same overlap, stated explicitly):

```csharp
// Initialize
var child = new ContainerRuntime();
child.Height = 50;
child.Y = -25;
stackPanel.AddChild(child);
```

For more information see the [Children Layout](../gum-elements/container/children-layout.md#stacking-and-children-origin) page.

### Stacks Without Wrapping Grow to Their Max

A stack that sizes itself to its children and has a max on its stacking axis now grows all the way to that max when its children need more space. This applies to a `Top to Bottom Stack` with `Height Units` of `Relative to Children` (or `Relative to Max of Children or Parent`) and a `Max Height`, and to a `Left to Right Stack` with the matching `Width Units` and a `Max Width`.

Before this version, such a stack stopped growing at the last child that fit under its max. For example, a `Top to Bottom Stack` with a `Max Height` of `120` and three children that are each `50` tall measured `100`, and the third child hung below it. It now measures `120`, so it holds as much of the third child as it can. The children stay in the same positions.

These cases are unchanged:

* A stack with `Wraps Children` checked still moves the next child to a new row or column. Its size is its widest row or column, as described in the next section.
* A container with a `Children Layout` of `Regular` already grew to its max.
* A stack whose children fit under its max still sizes to its children.

This affects you only if a stack that does not wrap has more content than its `Max Height` or `Max Width` allows. Such a stack is now larger.

To migrate, open your screens and components in the Gum tool and check any stacks that use a max. If you want the old size, lower the max to the size of the children that fit, or check `Wraps Children` if the extra children should move to a new row or column.

❌ Old (with three children that are each 50 tall, measured 100 tall and now measures 120):

```csharp
// Initialize
var stackPanel = new ContainerRuntime();
stackPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
stackPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
stackPanel.Height = 0;
stackPanel.MaxHeight = 120;
```

✅ New (keeps the old 100 tall size):

```csharp
// Initialize
var stackPanel = new ContainerRuntime();
stackPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
stackPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
stackPanel.Height = 0;
stackPanel.MaxHeight = 100;
```

For more information see the [Children Layout](../gum-elements/container/children-layout.md#stacking-and-container-height-units-and-width-units) page.

### Wrapping Stacks Sized to Their Children Measure Their Widest Row

A stack with `Wraps Children` checked that sizes itself to its children and has a max on its stacking axis is now as large as its widest row or column. This applies to a `Left to Right Stack` with `Width Units` of `Relative to Children` (or `Relative to Max of Children or Parent`) and a `Max Width`, and to a `Top to Bottom Stack` with the matching `Height Units` and a `Max Height`. Rows break where the next child would pass the max, and padding and the max apply after the widest row is measured.

Before this version, such a stack measured only its first row. The rows after it then wrapped against that narrower size, so they used more rows than needed, and a child wider than the first row extended past the stack. For example, a `Left to Right Stack` with a `Max Width` of `200` holding children `100`, `120`, and `60` wide measured `100` wide and placed each child on its own row, with the `120` wide child extending `20` past the stack. It now measures `180` wide, with the `120` and `60` wide children sharing the second row.

A stack whose first child is wider than the max is now as wide as its max. Before this version, such a stack measured `0` wide when it had no `Width` padding.

These cases are unchanged:

* A stack whose first row is its widest row.
* A stack whose children fit in one row.
* Stacks with a size that does not depend on their children.

This affects you only if a wrapping stack sized to its children with a max has a later row wider than its first. Such a stack is now wider and may have fewer rows.

To migrate, open your screens and components in the Gum tool and check any wrapping stacks that use a max. If you want the old size, lower the max to the width of the first row.

For more information see the [Wraps Children](../gum-elements/container/wraps-children.md#relative-to-children-and-max-width-and-max-height) page.

### Wrapping Stacks Position Children Within Their Row or Column

A stack with `Wraps Children` checked that is sized to its children and has a max now measures its widest row or column, not only its first. In a stack with `Wraps Children` checked, each row or column is now the parent of its children on the other axis for positioning:

* In a `Left to Right Stack`, a child's `Y Units` and `Y Origin` position it within its row.
* In a `Top to Bottom Stack`, a child's `X Units` and `X Origin` position it within its column.

Before this version, these values positioned the child within the whole stack. For example, in a `Left to Right Stack` that is `300` tall with rows that are `40`, `80`, and `20` tall, a child with `Y Units` of `Pixels From Center` and `Y Origin` of `Center` sat at the stack's center whichever row it was in, outside its own row. It is now centered in its row. A child with `Y Units` of `Pixels From Bottom` now aligns to the bottom of its row instead of the bottom of the stack, and a `Percentage` value is now a percentage of the row's height.

A row or column is as large as its largest child, and rows and columns stay packed at the start of the stack. A child alone in its row is the same size as the row, so centering it leaves it at the top of the row.

Rows and columns now count their children the same way a container sized to its children does. A child's `Y` (or `X`) value counts from the edge it is measured from, a portion of a child outside its row does not count, and a child positioned with `Percentage` does not count. A child whose size depends on its parent (for example, `Height Units` of `Percentage of Parent`) still counts, unless the stack itself is sized to its children on that axis. Before this version, a row added each child's value to its size whatever its units.

These cases are unchanged:

* Children using `Pixels From Top` in a `Left to Right Stack` (or `Pixels From Left` in a `Top to Bottom Stack`), which is the default.
* Size units. A child with `Height Units` of `Percentage of Parent` still sizes itself from the whole stack.
* Stacks without `Wraps Children` checked.

This affects you only if a child of a wrapping stack uses units other than the default on the axis that does not stack, or a `Percentage` value there. Such a child now sits inside its row or column.

To migrate, open your screens and components in the Gum tool and check any wrapping stacks whose children use these units. If you want a child placed relative to the whole stack, move it out of the stack into a container that does not stack.

For more information see the [Wraps Children](../gum-elements/container/wraps-children.md#positioning-children-within-a-row-or-column) page.

### Containers Count a Child's Min When It Uses Relative to Max of Children or Parent

A container with `Height Units` of `Relative to Children` now counts a child that uses `Relative to Max of Children or Parent` at no less than the child's `Min Height`. The same applies to `Width Units` and `Min Width`.

Before this version, the container measured such a child by its children only. For example, a child with a `Min Height` of `80` whose children are `30` tall was `80` tall, but its container measured `30`, so the child extended past the container. The container now measures `80`.

These cases are unchanged:

* A child whose children are already larger than its `Min Height`.
* Children using other `Height Units`. Their min was already counted.

This affects you only if a container sized to its children holds a `Relative to Max of Children or Parent` child with a `Min Height` or `Min Width` larger than its content. Such a container is now larger.

To migrate, open your screens and components in the Gum tool and check these containers. If you want the old size, lower or clear the child's `Min Height` or `Min Width`.

For more information see the [Height Units](../gum-elements/general-properties/height-units.md#relative-to-max-of-children-or-parent) page.

### AnimationChainFinished Replaces AnimationChainCycled at the End of a Non-Looping Chain

`AnimationChainCycled` is now raised only when a looping animation chain wraps around. A chain that does not loop raises the new `AnimationChainFinished` event once when it reaches its end and `Animate` becomes `false`. `SpriteRuntime`, `Sprite`, `NineSlice`, and `AnimationChainLogic` all have the new event.

Before this version, a chain that did not loop raised `AnimationChainCycled` when it reached its end, even though it never started over. It also returned to its first frame instead of holding its last.

A chain that does not loop now also finishes when it plays backward. With a negative `AnimationChainSpeed`, it stops on its first frame, sets `Animate` to `false`, and raises `AnimationChainFinished`. Before this version, it held the first frame, kept `Animate` set to `true`, and raised no event.

This affects you only if your code subscribes to `AnimationChainCycled` on a sprite or nine slice whose chain does not loop, for example to remove a sprite after its death animation plays. That handler no longer runs.

To migrate, subscribe to `AnimationChainFinished` instead.

❌ Old:

```csharp
// Initialize
sprite.IsAnimationChainLooping = false;
sprite.AnimationChainCycled += () => sprite.RemoveFromRoot();
```

✅ New:

```csharp
// Initialize
sprite.IsAnimationChainLooping = false;
sprite.AnimationChainFinished += () => sprite.RemoveFromRoot();
```

For more information see the [Animation Chains](../../code/files-and-fonts/animation-chains.md#looping-and-end-of-chain-events) page.
