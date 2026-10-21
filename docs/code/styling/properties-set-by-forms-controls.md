# Properties Set by Forms Controls

## Introduction

Forms controls change how they look by applying states on their Visual. A few controls also assign properties on the Visual directly, because a value like a thumb size or a caret position depends on runtime data that a state cannot express.

If you build your own Visual in code or in the Gum tool, this page lists what each control writes, so you know which values the control will overwrite and which states the control expects to find.

For example, a `ScrollBar` computes the thumb's size along the scroll direction and assigns it directly. It never touches the thumb's size across that direction. A custom `Horizontal` state therefore needs to set the thumb's cross-axis size itself:

```csharp
// Initialize
var scrollBar = new ScrollBar();
var scrollBarVisual = (ScrollBarVisual)scrollBar.Visual;

var horizontal = scrollBarVisual.States.OrientationStates.Horizontal;
horizontal.Apply = () =>
{
    // ScrollBar sets the thumb's Width, never its Height
    scrollBarVisual.ThumbInstance.Height = 0;
    scrollBarVisual.ThumbInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
    // ...the rest of your Horizontal layout
};
```

If the Horizontal state leaves the thumb's height alone, a bar that was vertical first keeps the thumb height the vertical layout produced.

## How Controls Change a Visual

A control changes its Visual in three ways:

* **States.** The control applies a state by name, such as `ButtonCategoryState` set to `Pushed`. If the Visual has no such category or state, Gum skips it without an error. States are covered in [Styling Using States](code-only-styling/styling-using-states.md).
* **Forwarded properties.** Setting `button.Width`, `label.Text`, or `listBox.IsVisible` assigns the matching property on the Visual or on a named child. The control writes the value you gave it and nothing else.
* **Computed writes.** The control calculates a value and assigns it, such as a scroll thumb's position or a text box caret's `X`. The control assigns these on its own schedule, so a state or a value you set in code on the same property gets replaced.

Computed writes are what a custom Visual needs to plan around. The tables below list them for each control, along with what triggers the write.

Controls find the parts of a Visual by name, such as `TextInstance` or `ThumbInstance`. Most controls skip a missing child without an error, and the tables mark the exceptions (children the control requires).

{% hint style="info" %}
`RefreshStyles` re-applies the Visual's states and then restores some control values. `ComboBox` restores its text, `ScrollBar` and `Slider` restore the thumb position, and `ScrollViewer` restores its scroll offset. `Button`, `Label`, `ToggleButton`, `Expander`, `MenuItem`, `TextBox`, and `PasswordBox` restore their text only when code supplied it, as described in [Text After RefreshStyles](runtime-variable-references.md#text-after-refreshstyles).
{% endhint %}

### Scroll Step Sizes Survive a Visual Swap

`ScrollBar`, `ScrollViewer`, and `Slider` keep `SmallChange` and `LargeChange` when their Visual is replaced. A value set in code, authored on the Visual, or applied from a behavior is not overwritten.

A value you did not assign keeps its default. `SmallChange` and `LargeChange` are 5 and 25 on a `Slider`, and `LargeChange` follows the viewport size on a `ScrollBar` and `ScrollViewer`.

## Properties Every Control Forwards

Every Forms control forwards these properties straight to its Visual. The control assigns them only when your code sets them.

* `X`, `Y`, `XUnits`, `YUnits`, `XOrigin`, `YOrigin`
* `Width`, `Height`, `WidthUnits`, `HeightUnits`, `MinWidth`, `MaxWidth`, `MinHeight`, `MaxHeight`
* `IsVisible` (assigns `Visual.Visible`)
* `Name`, `BindingContext`
* `IsEnabled` (assigns `Visual.IsEnabled`)
* `Anchor(...)` and `Dock(...)`, which call the same methods on the Visual
* `AddChild` and `RemoveChild`, which set the child Visual's `Parent`

## How State Names Are Chosen

Most controls choose a state name from the same inputs. A control that follows this pattern, such as `Button`, `ComboBox`, `Slider`, and `ScrollViewer`, applies the first match:

1. Not enabled and focused: `DisabledFocused`
2. Not enabled: `Disabled`
3. Focused and pushed: `Pushed`
4. Focused and the cursor is over it: `HighlightedFocused`
5. Focused: `Focused`
6. Pushed: `Pushed`
7. The cursor is over it: `Highlighted`
8. Otherwise: `Enabled`

A Visual that is not connected to a cursor, such as one in a render-only host, receives only `Enabled`, `Disabled`, `Focused`, and `DisabledFocused`.

Checkable controls append a suffix to that name. `CheckBox` and `RadioButton` add `On`, `Off`, or `Indeterminate` (`HighlightedFocusedOn`, `DisabledOff`).

Controls with a different pattern are described in their own sections.

## Buttons, Labels, and Other Simple Controls

### Button

| Child | Property | When |
|---|---|---|
| `TextInstance` | `Text` | You set `Text`. The control writes it through the localization path. `SetTextNoTranslate` writes `TextNoTranslate`. |

States: category `ButtonCategory`, names from [How State Names Are Chosen](#how-state-names-are-chosen). `Thumb` and the scroll bar's up and down buttons use the same category.

`TextInstance` is optional. Without it, `Text` does nothing.

### ToggleButton, CheckBox, and RadioButton

| Child | Property | When |
|---|---|---|
| `TextInstance` | `Text` | You set `Text`, the same as `Button`. |

States:

* `ToggleButton` uses category `ToggleCategory`. The names are `Enabled`, `Highlighted`, `Pushed`, or `Disabled`, followed by `On` or `Off`. A `ToggleButton` never uses the focused names, and an indeterminate value displays as `Off`.
* `CheckBox` uses category `CheckBoxCategory` with the full set of names, followed by `On`, `Off`, or `Indeterminate`.
* `RadioButton` uses category `RadioButtonCategory`, with the same names as `CheckBox`. Checking a `RadioButton` unchecks every sibling in the same `GroupName`, which applies their states too.

### Label

| Target | Property | When |
|---|---|---|
| `Visual` itself, if it is named `TextInstance` or its renderable is text. Otherwise the child `TextInstance`. | `Text` | You set `Text`. |

`Label` applies no states.

### Image

| Target | Property | When |
|---|---|---|
| `Visual` | `Width` and `Height` set to 100, with units `PercentageOfSourceFile` | Construction. `Image` creates its own Visual and does not look up a registered template. |
| `Visual` | `SourceFile` | You set `Source`. |

`Image` applies no states.

### Tooltip

| Target | Property | When |
|---|---|---|
| `TextInstance` | `Text` | You set `Content` to a string. A null `Content` writes an empty string. |
| `Visual` | `Z` set to infinity, `XUnits` and `YUnits` set to `PixelsFromSmall`, `XOrigin` set to `Left`, `YOrigin` set to `Top`, `Parent` cleared | `Show`. |
| `Visual` | `X` set to the cursor `X` plus 12, `Y` set to the cursor `Y` plus 20 | `Show`, and then `RepositionToKeepInScreen` can adjust `X` and `Y` again. |

`Show` also adds the Visual to the popup root. `Tooltip` applies no states and never writes the Visual's width, height, or `Visible`.

### Expander

| Child | Property | When |
|---|---|---|
| `ContentContainer` | `Visible` | `IsExpanded` changes, and every time the control updates its state. The control assigns it after applying the state, so a `Visible` value inside an Expanded or Collapsed state is replaced. |
| `TextInstance` | `Text` | You set `Header`. |
| `HeaderContainer` | Click handler, only if it is an `InteractiveGue` | Visual assignment. A click toggles `IsExpanded`. |

States: category `ExpanderCategory` with `Expanded` and `Collapsed`. A disabled expander uses `DisabledExpanded` and `DisabledCollapsed`.

The control starts collapsed, so `ContentContainer` is hidden as soon as the Visual is assigned, whatever `Visible` value it was authored with.

## Containers and Layout

### Panel and StackPanel

| Target | Property | When |
|---|---|---|
| `Visual` | `HasEvents` and `ExposeChildrenEvents` set to true | Every Visual assignment. |
| `Visual` | `Dock(SizeToChildren)` | `Panel()` and `StackPanel()` construction only. A Panel built around your own Visual leaves size and units alone. |
| `Visual` | `ChildrenLayout` set to `LeftToRightStack` for horizontal, `TopToBottomStack` for vertical | `StackPanel` only. It runs on every Visual assignment and when `Orientation` changes, and it replaces the `ChildrenLayout` the Visual was authored with. `Orientation` defaults to vertical. |
| `Visual` | `StackSpacing` | You set `Spacing`. |

Neither control applies states.

### Grid

| Target | Property | When |
|---|---|---|
| `Visual` | `IsVisible` true, `WidthUnits` and `HeightUnits` set to `RelativeToParent` with `Width` and `Height` set to 0, `ChildrenLayout` set to `TopToBottomStack` | `Grid()` construction only. A Grid built around your own Visual leaves these alone. |
| Row and cell containers the Grid creates | `ChildrenLayout`, `Width`, `Height`, their units, and min and max values | Whenever the Grid lays out its rows and columns. These containers are children of `Visual`. |
| Each child you add | `Parent` only | `AddChild(child, row, column)`. The Grid moves the child into its cell and leaves its position and size alone. |

The Grid adds its own row containers to the Visual's children and moves your children into them. `Grid` applies no states.

### UserControl

`UserControl` writes nothing and applies no states.

## Text Input

`TextBox` and `PasswordBox` share the same Visual requirements and the same writes.

Required children: `TextInstance` (its renderable must be text) and `CaretInstance`. The control throws if either one is missing. Optional children: `SelectionInstance` and `PlaceholderTextInstance`.

| Child | Property | When |
|---|---|---|
| `TextInstance` | `Text` | You set `Text` on a `TextBox`. On a `PasswordBox`, the control writes a string of `PasswordChar` characters, one per password character, and the Visual never holds the real password. |
| `TextInstance` | `XUnits` set to `PixelsFromSmall` | Visual assignment, and each time the control keeps the caret in view. |
| `TextInstance` | `X` | The caret moves outside the visible area of a no-wrap text box, and during touch drags. `X` never exceeds the value the Visual had when it was assigned. |
| `TextInstance` | `Y` | The caret moves outside the visible area of a multi-line text box. |
| `TextInstance` | `MaxLettersToShow` and `MaxNumberOfLines` | You set the same-named properties on a `TextBox`. |
| `CaretInstance` | `XUnits` set to `PixelsFromSmall` | Visual assignment, and each caret calculation. |
| `CaretInstance` | `X` | Every change to `CaretIndex`, to the text's `HorizontalAlignment`, and every edit. It is the measured text width up to the caret, plus the text's `X`. |
| `CaretInstance` | `Y` | Multi-line text only. In single-line mode the control never assigns the caret's `Y`. |
| `CaretInstance` | `Visible` | Focus changes and selection changes. It is true when the control is focused (or `IsCaretVisibleWhenNotFocused` is true) and nothing is selected. |
| `SelectionInstance` | `Visible`, `X`, `Y`, `Width`, `XUnits` | The selection changes. `Y` adds the `Y` the instance had when the Visual was assigned. `WidthUnits` is never written. |
| `PlaceholderTextInstance` | `Text` | You set `Placeholder`. |
| `PlaceholderTextInstance` | `Visible` | The text changes. It is true when the text is empty. |

The caret math assumes `CaretInstance` and `TextInstance` share a parent. The control never writes the caret's or the selection's `Height`, `Color`, or `Alpha`.

For multi-line selections, the control clones `SelectionInstance` and inserts one clone after it for each wrapped line. The clones are removed again when the template refreshes, so the number of children under the selection's parent changes at runtime.

States:

* Category `TextBoxCategory` (`PasswordBoxCategory` for `PasswordBox`) with `Disabled`, `Enabled`, `Highlighted`, and `Focused`. A focused control applies a `Selected` state first and then `Focused`, so a Visual that defines `Selected` sees it applied.
* Category `LineModeCategory` with `Single`, `Multi`, and `MultiNoWrap`. The control applies it when `TextWrapping` or `AcceptsReturn` changes, but not when the Visual is first assigned. A Visual defines its initial line mode itself. The control falls back to `Multi` if the category has no `MultiNoWrap` state.

## Range and Scrolling

### ScrollBar

Children: `ThumbInstance` (the track is the thumb's parent), plus optional `UpButtonInstance` and `DownButtonInstance`.

| Child | Property | When |
|---|---|---|
| `ThumbInstance` | Vertical: `YUnits` set to `PixelsFromSmall`, `HeightUnits` set to `Absolute`, `Height` set to the larger of `MinimumThumbSize` and the track height times `ViewportSize / (Maximum - Minimum + ViewportSize)`. Horizontal: the same with `XUnits`, `WidthUnits`, and `Width`. | `ViewportSize`, `Minimum`, `Maximum`, or `Orientation` changes, and when the track's size changes. |
| `ThumbInstance` | Vertical: `YUnits`, `HeightUnits`, `YOrigin` set to `Top`, and `Y` set to the position that matches `Value`. Horizontal: the same with `XUnits`, `WidthUnits`, `XOrigin` set to `Left`, and `X`. | The same triggers, plus `Value` changes and thumb drags. |
| `ThumbInstance` | `IsEnabled` | The scroll bar's `IsEnabled` changes. |

The control never writes the thumb's size across the scroll direction (`Width` for a vertical bar, `Height` for a horizontal one). The thumb's cross-axis size and units come from your Visual's states. The default `Horizontal` state resets the thumb's `Height` to 0 with `RelativeToParent` units for this reason.

The control writes `HeightUnits` (vertical) or `WidthUnits` (horizontal) as `Absolute` in both routines, so the thumb's scroll-direction size cannot use relative units.

States: category `OrientationCategory` with `Vertical` and `Horizontal`. The control applies `Vertical` whenever a Visual is assigned and `Horizontal` when `Orientation` is set to horizontal. The scroll bar has no `Enabled` or `Highlighted` states of its own. The thumb and the up and down buttons apply `ButtonCategory`.

### Slider

Children: `ThumbInstance` and `TrackInstance`. Without a `TrackInstance`, the thumb's parent serves as the track.

| Child | Property | When |
|---|---|---|
| `ThumbInstance` | `XUnits` set to `Percentage`, `X` set to 0 through 100 | `Value`, `Minimum`, or `Maximum` changes, and thumb drags. The value is a percentage of the track, not pixels. |
| `ThumbInstance` | `IsEnabled` | The slider's `IsEnabled` changes. |
| `TrackInstance` | `RaiseChildrenEventsOutsideOfBounds` set to true | Visual assignment. |

The control never writes the thumb's `Y`, `Width`, `Height`, or `XOrigin`. A slider is horizontal only, and `Orientation` has no effect.

States: category `SliderCategory`, names from [How State Names Are Chosen](#how-state-names-are-chosen).

### ScrollViewer

Children: `InnerPanelInstance` and `ClipContainerInstance` are needed for scrolling. `VerticalScrollBarInstance`, `HorizontalScrollBarInstance`, and `StickyHeaderOverlayInstance` are optional.

| Child | Property | When |
|---|---|---|
| `InnerPanelInstance` | `YUnits` set to `PixelsFromSmall`, `Y` set to the negative scroll value | The vertical scroll bar's value changes. |
| `InnerPanelInstance` | `XUnits` set to `PixelsFromSmall`, `X` set to the negative scroll value | The horizontal scroll bar's value changes. |
| Scroll bars | `IsVisible` | `VerticalScrollBarVisibility` or `HorizontalScrollBarVisibility` changes, and when the inner panel or the ScrollViewer changes size. `Auto` shows the bar when the inner panel is larger than `ClipContainerInstance`. |
| Scroll bars | `Minimum` set to 0, `ViewportSize` set to the clip container size, `Maximum` set to the overflow | The same triggers as visibility. |
| Scroll bars | `SmallChange` and `LargeChange` | Visual assignment, only for a value you did not assign. `LargeChange` follows the viewport size until you assign it. |
| Horizontal scroll bar | `Orientation` set to `Horizontal` | Visual assignment. |
| Sticky headers | `Parent`, `YUnits`, `YOrigin`, `Y`, and placeholder children added to `InnerPanelInstance` | You register a sticky header, and then while scrolling. Only when `StickyHeaderOverlayInstance` exists. |

`ClipContainerInstance` is read for its size and never written. The control does not write the scroll bars' position, size, or docking, so your Visual lays them out.

`AddChild` adds a child to `InnerPanelInstance`, not to the ScrollViewer's own Visual.

States:

* Category `ScrollViewerCategory`, with names from [How State Names Are Chosen](#how-state-names-are-chosen).
* Category `ScrollBarVisibility` with `BothScrollVisible`, `VerticalScrollVisible`, `HorizontalScrollVisible`, and `NoScrollBar`. The control applies one after each visibility update, based on which bars are showing.
* Categories `VerticalSizeMode` and `HorizontalSizeMode`, with an `Auto` state. The control applies the state only when the matching `SizeMode` is `Auto`.

### Splitter

A Splitter has no children of its own and applies no states. It writes to the two siblings next to it in its parent.

| Target | Property | When |
|---|---|---|
| Sibling before the splitter | `Height` (rows) or `Width` (columns) | The splitter is dragged, or `ApplyResizeChangeInPixels` is called. |
| Sibling after the splitter | The same property, with the opposite change | The same. |

Siblings with `Absolute`, `RelativeToParent`, or `RelativeToChildren` units change by the pixel amount. Siblings with `PercentageOfParent` or `Ratio` units change in those units. The units themselves never change. Dragging is limited by each sibling's `MinWidth` or `MinHeight`.

### Window

Children: `InnerPanelInstance` is required. `TitleBarInstance` and the eight border children (`BorderTopLeftInstance`, `BorderTopInstance`, `BorderTopRightInstance`, `BorderLeftInstance`, `BorderRightInstance`, `BorderBottomLeftInstance`, `BorderBottomInstance`, `BorderBottomRightInstance`) are optional.

| Target | Property | When |
|---|---|---|
| `TitleBarInstance` | `Height` | You set `TitleHeight`. |
| `Visual` | `ExposeChildrenEvents` set to true | Visual assignment. |
| `Visual` | `X` and `Y` | The title bar is dragged. |
| `Visual` | `Width`, `Height`, and, depending on the origin, `X` and `Y` | A border is dragged and `ResizeMode` is `CanResize`. |

Dragging changes values but never units. The result depends on the Visual's `WidthUnits` and `HeightUnits`. `Window` applies no states.

## Lists, Dropdowns, and Menus

### ItemsControl and ListBox

`ItemsControl` is the base of `ListBox`. A plain `ItemsControl` uses the `ScrollViewer` writes and states above. Children: `InnerPanelInstance`, and for `ListBox`, `ClipContainerInstance`.

| Target | Property | When |
|---|---|---|
| `InnerPanelInstance` children | Each item Visual's `Parent` is set to the inner panel | Items are added, or you set `Items`, `VisualTemplate`, or `FrameworkElementTemplate`. |
| `InnerPanelInstance` children | All children are cleared | You assign `Items` or either template. Anything you authored inside the inner panel is removed. |
| `InnerPanelInstance` | `ChildrenLayout` set to `LeftToRightStack` or `TopToBottomStack` | You set `Orientation` on the control. |
| Each `ListBoxItem` | `TextInstance` raw text | An item is created, `DisplayMemberPath` changes, or an item is replaced. See [ListBoxItem](#listboxitem). |
| Vertical scroll bar | `Value` | `ScrollIntoView`, `ScrollIndexIntoView`, or a selection change. This moves the inner panel as described under [ScrollViewer](#scrollviewer). |

States: `ListBox` uses category `ListBoxCategory` with `Enabled`, `Focused`, `Disabled`, and `DisabledFocused`. A plain `ItemsControl` uses `ScrollViewerCategory`.

### ListBoxItem

| Child | Property | When |
|---|---|---|
| `TextInstance` | Raw text (not localized, and no layout refresh) | The `ListBox` creates the item, `DisplayMemberPath` changes, or the item is replaced. |

States: category `ListBoxItemCategory`. The control chooses the first match:

1. `Disabled`
2. `Focused`
3. `Selected`, or `SelectedHighlighted` when the item is selected and highlighted and the category defines that state
4. `Highlighted`, applied only when the cursor moved
5. `Enabled`

### ComboBox

Required children: `ListBoxInstance` (the dropdown) and `TextInstance`. The control throws if either is missing.

| Target | Property | When |
|---|---|---|
| `TextInstance` | Raw text (not localized) | You set `Text`, or the selection changes, unless `Text` is data-bound. |
| `ListBoxInstance` | Removed from the ComboBox's children, `Visible` set to false | Visual assignment. |
| `ListBoxInstance` | `Visible` set to true, then `X`, `Y`, `Width`, `Height` set to the absolute position and size it had inside the ComboBox, with `XUnits`, `YUnits` as `PixelsFromSmall` and `WidthUnits`, `HeightUnits` as `Absolute`, `Z` set to infinity, and `Parent` moved to the popup root | The dropdown opens. |
| `ListBoxInstance` | `Visible` set to false, `Parent` cleared, and the saved `X`, `Y`, `Width`, `Height` and their units restored | The dropdown closes. |

The open routine assumes the dropdown's origin is the top left. The dropdown is sized to the ComboBox's width at the moment it opens.

States: category `ComboBoxCategory`, names from [How State Names Are Chosen](#how-state-names-are-chosen).

### Menu and MenuItem

`Menu` writes nothing of its own and uses the `ItemsControl` behavior above. States: category `MenuCategory` with `Enabled`, `Focused`, `Disabled`, and `DisabledFocused`.

`MenuItem` children are all optional: `TextInstance`, `SubmenuIndicatorInstance`, `SubItemContainerInstance`, and `InnerPanelInstance`.

| Target | Property | When |
|---|---|---|
| `TextInstance` | `Text` | You set `Header`. |
| `TextInstance` | Raw text | `UpdateToObject` runs for a non-`MenuItem` item. |
| `SubmenuIndicatorInstance` | `Visible`, true when the item has sub-items and is not top level | Visual assignment, `Items` changes, or the parent item changes. |
| `SubItemContainerInstance` | Each child's `Parent` cleared and moved into `Items`, then the container's `Parent` cleared | Visual assignment, and when children are added later. |
| Sub-item popup | Created on demand: `X`, `Y`, `Z`, `XUnits`, `YUnits`, `Parent`, and the popup's contents cleared | The item becomes selected and has sub-items. |

The default popup also gets `HorizontalScrollBarVisibility` and `VerticalScrollBarVisibility` set to `Hidden`, both size modes set to `Auto`, and every Visual from the inner panel up to the popup set to `RelativeToChildren` width and height. A custom `ScrollViewerVisualTemplate` skips that auto-sizing.

States: category `MenuItemCategory`. The control chooses the first match: `Disabled`, `Focused`, `Selected`, `Highlighted` (only when the cursor moved, or the call comes from selection changing), then `Enabled`.

## Other Controls

### ColorPicker

All children are optional: `SaturationValueContainer`, `HueContainer`, `SaturationValueDisplay`, `HueDisplay`, `SaturationValueIndicator`, and `HueIndicator`.

| Child | Property | When |
|---|---|---|
| `SaturationValueIndicator` | `X` and `Y` | The color changes or the user drags on the container. The values are fractions of the container's absolute size, and the control does not set `XUnits` or `YUnits`. |
| `HueIndicator` | `Y` | The same triggers. |
| `SaturationValueDisplay` | Texture pixels (256 by 256) | The color changes. |
| `HueDisplay` | Texture pixels | Visual assignment. |

`ColorPicker` applies no states.

### DialogBox

Required child: `TextInstance` (its renderable must be text). Optional: `ContinueIndicatorInstance`.

| Child | Property | When |
|---|---|---|
| `TextInstance` | `Text`, and `MaxLettersToShow` | A page shows. `MaxLettersToShow` grows each frame when `LettersPerSecond` is above 0. |
| `ContinueIndicatorInstance` | `Visible` | A page starts typing (false) and finishes (true). |
| `Visual` | `Visible` | `Show` and `Dismiss`. |

`DialogBox` applies no states.

## State Names by Control

This table lists each control's state category and the states it applies. The [Styling Using States](code-only-styling/styling-using-states.md) page lists the states each default Visual defines.

| Control | Category | States the control applies |
|---|---|---|
| Button, ComboBox, Slider | `ButtonCategory`, `ComboBoxCategory`, `SliderCategory` | `Enabled`, `Highlighted`, `Pushed`, `Focused`, `HighlightedFocused`, `Disabled`, `DisabledFocused` |
| CheckBox | `CheckBoxCategory` | The seven names above, each followed by `On`, `Off`, or `Indeterminate` |
| RadioButton | `RadioButtonCategory` | The seven names above, each followed by `On`, `Off`, or `Indeterminate` |
| ToggleButton | `ToggleCategory` | `Enabled`, `Highlighted`, `Pushed`, `Disabled`, each followed by `On` or `Off` |
| TextBox | `TextBoxCategory`, `LineModeCategory` | `Enabled`, `Highlighted`, `Focused` (after `Selected`), `Disabled`. `Single`, `Multi`, `MultiNoWrap`. |
| PasswordBox | `PasswordBoxCategory`, `LineModeCategory` | The same as TextBox |
| ScrollBar | `OrientationCategory` | `Vertical`, `Horizontal` |
| ScrollViewer | `ScrollViewerCategory`, `ScrollBarVisibility`, `VerticalSizeMode`, `HorizontalSizeMode` | Seven names, `BothScrollVisible`, `VerticalScrollVisible`, `HorizontalScrollVisible`, `NoScrollBar`, `Auto` |
| ListBox | `ListBoxCategory` | `Enabled`, `Focused`, `Disabled`, `DisabledFocused` |
| ListBoxItem | `ListBoxItemCategory` | `Enabled`, `Highlighted`, `Selected`, `SelectedHighlighted`, `Focused`, `Disabled` |
| Menu | `MenuCategory` | `Enabled`, `Focused`, `Disabled`, `DisabledFocused` |
| MenuItem | `MenuItemCategory` | `Enabled`, `Highlighted`, `Selected`, `Focused`, `Disabled` |
| Expander | `ExpanderCategory` | `Expanded`, `Collapsed`, `DisabledExpanded`, `DisabledCollapsed` |
| Label, Image, Panel, StackPanel, Grid, UserControl, Tooltip, Splitter, Window, ColorPicker, DialogBox | None | None |
