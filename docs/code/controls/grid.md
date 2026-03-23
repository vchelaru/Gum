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
// Grid fills its parent by default
grid.AddToRoot();

// Three columns showing the three main sizing types
grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(2, GridUnitType.Star)));
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

// Two rows with different proportions
grid.RowDefinitions.Add(new RowDefinition(new GridLength(2, GridUnitType.Star)));
grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));

// Colors for each column — makes proportions immediately visible
Color[] columnColors = [
    new Color(180, 100, 100), // red-ish
      new Color(100, 150, 180), // blue-ish
      new Color(100, 170, 120), // green-ish
  ];
string[] columnLabels = ["1*", "2*", "Auto"];
string[] rowLabels = ["2*", "1*"];

for (int row = 0; row < 2; row++)
{
    for (int col = 0; col < 3; col++)
    {
        var background = new ColoredRectangleRuntime();

        float brightness = row == 0 ? 1.0f : 0.6f;
        var cellColor = new Color(
            (int)(columnColors[col].R * brightness),
            (int)(columnColors[col].G * brightness),
            (int)(columnColors[col].B * brightness)
        );

        background.Color = cellColor;
        background.Dock(Dock.Fill);
        grid.AddChild(background, row: row, column: col);

        var label = new Label();
        label.Text = $"Col {columnLabels[col]}\nRow {rowLabels[row]}";
        label.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        label.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        label.X = 0;
        label.Y = 0;
        label.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
        label.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
        grid.AddChild(label, row: row, column: col);
    }
}
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA61UUW_aMBD-K1a0h6TK3EC1rQJNE6Uqm9RqU8Y2JuAhJEewauzKcUpbxH-fzwmEAK26Cj-cffZ9n-_Od14637JePndaWuXgO3nGRJo5raFjNukfpmCqojk4Y99hgmkWcfYETsu5jxRJFUvIZyJgQXpm6XrtkcA92kmSvgyl1NVWV_J8Li5halmkyNDIRejuibvmuwaR6pnb8K32y5z2H--A_tSR8rwjEDePRlyR0k6uZcURysUhgtr2f7r1FsrnU2hCkWo4JrENyWqZedLhSBAzymClchvngU8aQSE8n5yeEgXJe5bN9iyt0QcU56XlhOfwvOknFM3SNFUAorAdG_cyrUw5bvy7jibAC__yIGg2GyfF7JNibu7o-BjFqkam5GKXaRe5ZkbcVCriMqERZxBB2y7w_KxLmlazxheF9EZiWUS6ARrvCyAuSuCZ1XaAiCrBOLDJJlF8myqZi3Wr2dxBEkKsI5FyCHOh2Rxsq1XQKZeRJhPF0pkWkGGo1n3jBvlCGjSYkhYJ6Mdpu35dDJzbG7ZvcysbHBiT526XzNAoYxqSk60bPf91qN6bUBd1VAWqp6HKHl1HtYmwfdDsUsa3Lgp6xTj3tozWX1t3xnjiVhAfM9tC4Zd12sK57gjmlmPRlXm1Behu09tT2ocHbUzeFeVn_CTL7dq3wa9GI2H6nSw3hTw0K7NtMXuUA-x7LAD80btS3IPSoDLaAwEq4ptf4Qd7MFRXSs5vWJJw2CP6eyyige2GPfqDu4Pv5pWZMGchiASwha_ZREXqkfZUdDdjcUa_SsWepNAR73CWijkITbtGgNq_5BV0v01cLH6RrF4LlvqFMkDEaiRWzuofTIfR6W4HAAA)

<figure><img src="../../.gitbook/assets/23_10 11 48.png" alt=""><figcaption><p>Grid displaying rows and columns</p></figcaption></figure>



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
