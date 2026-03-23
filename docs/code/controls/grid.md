# Grid

## Introduction

Grid is a container control that arranges children in rows and columns with independent sizing per row and per column. It is similar to WPF's `Grid` control and is well-suited for tabular layouts, form layouts, and any UI where rows and columns need different widths or heights.

{% hint style="info" %}
Grid is an experimental control and its API may change in future releases.
{% endhint %}

Rows and columns are defined by adding `RowDefinition` and `ColumnDefinition` instances to the `RowDefinitions` and `ColumnDefinitions` collections. Children are placed into a specific cell by calling `AddChild` with a row and column index.

## Code Example: Creating a Grid

The following code creates a simple 2×2 grid with two rows and two columns, then places a button in each cell.

```csharp
// Initialize
var grid = new Grid();
grid.AddToRoot();
grid.X = 50;
grid.Y = 50;
grid.Width = 400;
grid.Height = 300;

grid.RowDefinitions.Add(new RowDefinition());
grid.RowDefinitions.Add(new RowDefinition());
grid.ColumnDefinitions.Add(new ColumnDefinition());
grid.ColumnDefinitions.Add(new ColumnDefinition());

var topLeft = new Button();
topLeft.Text = "Row 0, Col 0";
grid.AddChild(topLeft, row: 0, column: 0);

var topRight = new Button();
topRight.Text = "Row 0, Col 1";
grid.AddChild(topRight, row: 0, column: 1);

var bottomLeft = new Button();
bottomLeft.Text = "Row 1, Col 0";
grid.AddChild(bottomLeft, row: 1, column: 0);

var bottomRight = new Button();
bottomRight.Text = "Row 1, Col 1";
grid.AddChild(bottomRight, row: 1, column: 1);
```

Add XnaFiddle Here

Add Image Here

## Row and Column Sizing

Each `RowDefinition` has a `Height` property and each `ColumnDefinition` has a `Width` property. Both are of type `GridLength`, which supports three sizing modes controlled by `GridUnitType`:

### Star (Proportional)

`Star` is the default. Each row or column receives a proportion of the available space based on its value relative to the total star weight. A 1-star column and a 2-star column in a 300px grid produce a 100px column and a 200px column.

```csharp
// Initialize
grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
```

The default `ColumnDefinition` and `RowDefinition` constructors produce a 1-star size, so `new ColumnDefinition()` and `new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }` are equivalent.

### Absolute

`Absolute` sets a fixed pixel size regardless of the available space.

```csharp
// Initialize
grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
```

The code above creates a fixed 120px left column and a right column that fills the remaining space.

### Auto

`Auto` sizes a column to the widest content in that column, or sizes a row to the tallest content in that row. All cells in an Auto column are unified to the same width.

```csharp
// Initialize
grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
```

Add Image Here

## Adding Children

`AddChild` places a child element at a specific row and column. Row and column indices are zero-based.

```csharp
// Initialize
grid.AddChild(child, row: 0, column: 1);
```

Calling `AddChild` a second time on the same child moves it to the new position — the child appears in only one cell at a time.

If the row or column index exceeds the number of defined rows or columns, it is clamped to the last valid index. This matches WPF behavior.

```csharp
// Initialize
// Grid has 2 rows (indices 0 and 1). A row of 10 is clamped to row 1.
grid.AddChild(child, row: 10, column: 0);
```

## Removing Children

Call `RemoveChild` to remove a child from the grid entirely.

```csharp
// Initialize
grid.RemoveChild(child);
```

## Supported Ways to Add Children

Grid requires that every child be placed in a specific cell. Only the following patterns work correctly.

**Adding a `FrameworkElement` (Button, Panel, etc.):**

```csharp
// Initialize
grid.AddChild(button, row: 0, column: 1);
```

**Adding a raw `GraphicalUiElement`:**

```csharp
// Initialize
grid.AddChild(gue, row: 0, column: 1);
```

{% hint style="warning" %}
The following patterns bypass the Grid's cell placement system and must not be used.

**Calling `AddChild` without a row and column** throws a `NotSupportedException` at runtime:

```csharp
// Initialize
// This throws NotSupportedException — always supply row and column.
grid.AddChild(child);
```

**Directly manipulating the visual tree** silently produces broken layout because the Grid never records a cell assignment for the child:

```csharp
// Initialize
// Do not do this.
grid.Visual.Children.Add(child.Visual);

// Do not do this either.
child.Visual.Parent = grid.Visual;
```
{% endhint %}

## Constraining Column and Row Sizes

`ColumnDefinition` exposes `MinWidth` and `MaxWidth` properties, and `RowDefinition` exposes `MinHeight` and `MaxHeight` properties. These constraints apply to all three sizing modes (`Absolute`, `Auto`, and `Star`) and are specified in pixels.

```csharp
// Initialize
// Column is 200px but capped at 150px
grid.ColumnDefinitions.Add(new ColumnDefinition
{
    Width = new GridLength(200),
    MaxWidth = 150
});

// Column is Auto but won't shrink below 80px
grid.ColumnDefinitions.Add(new ColumnDefinition
{
    Width = GridLength.Auto,
    MinWidth = 80
});

// Star column that won't grow beyond 300px
grid.ColumnDefinitions.Add(new ColumnDefinition
{
    Width = new GridLength(1, GridUnitType.Star),
    MaxWidth = 300
});

// Row is 50px but won't shrink below 30px
grid.RowDefinitions.Add(new RowDefinition
{
    Height = new GridLength(50),
    MinHeight = 30
});

// Star row that won't shrink below 40px
grid.RowDefinitions.Add(new RowDefinition
{
    Height = new GridLength(1, GridUnitType.Star),
    MinHeight = 40
});
```

`MinWidth` defaults to `0` (no minimum) and `MaxWidth` defaults to no maximum. The same defaults apply to `MinHeight` and `MaxHeight`.
