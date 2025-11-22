# Grid

## Introduction

Gum supports adding children to a Panel in a grid layout. Unlike wrapped children in a stack, each cell in a grid is always the same size.

## Panels and Grids

Panels have the following behavior by default:

* Sized automatically according to their children (`WidthUnits` and `HeightUnits` both set to `RelativeToChildren`)
* No stacking or grid layout (`ChildrenLayout` set to `Regular`)

We can change this default behavior to create a grid as shown in the following code:

```csharp
var panel = new Panel();
panel.AddToRoot();
panel.Anchor(Gum.Wireframe.Anchor.Center);
panel.Height = 400;
panel.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
panel.Width = 400;
panel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

var panelVisual = panel.Visual;
panelVisual.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridHorizontal;
panelVisual.AutoGridHorizontalCells = 3;
panelVisual.AutoGridVerticalCells = 3;

for(int i = 0; i < 9; i++)
{
    var button = new Button();
    panel.AddChild(button);
    button.Text = $"Button {i}";
}
```

<figure><img src="../../.gitbook/assets/17_19 34 51.png" alt=""><figcaption><p>Buttons in a grid</p></figcaption></figure>

## Grid Cells as Parents

Children in a grid treat their cell as their direct parent. This means that Dock and Anchor calls are performed relative to the cell rather than the entire panel as shown in the following code:

<pre class="language-csharp"><code class="lang-csharp">var panel = new Panel();
panel.AddToRoot();
panel.Anchor(Gum.Wireframe.Anchor.Center);
panel.Height = 400;
panel.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
panel.Width = 400;
panel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

var panelVisual = panel.Visual;
panelVisual.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridHorizontal;
panelVisual.AutoGridHorizontalCells = 3;
panelVisual.AutoGridVerticalCells = 3;

for(int i = 0; i &#x3C; 9; i++)
{
    var button = new Button();
    panel.AddChild(button);
<strong>    button.Dock(Gum.Wireframe.Dock.Fill);
</strong>    button.Text = $"Button {i}";
}
</code></pre>

<figure><img src="../../.gitbook/assets/17_20 23 29.png" alt=""><figcaption></figcaption></figure>

## Grid Overflow

The `AutoGridHorizontalCells` and `AutoGridVerticalCells` properties determine the number of rows and columns in the grid. For example, the following code creates a grid with eight (8) cells:

```csharp
var panel = new Panel();
panel.AddToRoot();
panel.Anchor(Gum.Wireframe.Anchor.Center);
panel.Height = 200;
panel.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
panel.Width = 400;
panel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

var panelVisual = panel.Visual;
panelVisual.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridHorizontal;
panelVisual.AutoGridHorizontalCells = 4;
panelVisual.AutoGridVerticalCells = 2;

for(int i = 0; i < 8; i++)
{
    var button = new Button();
    panel.AddChild(button);
    button.Dock(Gum.Wireframe.Dock.Fill);
    button.Text = $"Button {i}";
}
```

<figure><img src="../../.gitbook/assets/17_20 38 51.png" alt=""><figcaption><p>Buttons in a grid with eight cells</p></figcaption></figure>

If additional buttons are added, they overflow the grid. Notice that the grid's size is not modified by the additional cells so, so additional rows do not shift the grid up.

```csharp
var panel = new Panel();
panel.AddToRoot();
panel.Anchor(Gum.Wireframe.Anchor.Center);
panel.Height = 200;
panel.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
panel.Width = 400;
panel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

var panelVisual = panel.Visual;
panelVisual.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridHorizontal;
panelVisual.AutoGridHorizontalCells = 4;
panelVisual.AutoGridVerticalCells = 2;

for(int i = 0; i < 14; i++)
{
    var button = new Button();
    panel.AddChild(button);
    button.Dock(Gum.Wireframe.Dock.Fill);
    button.Text = $"Button {i}";
}
```

<figure><img src="../../.gitbook/assets/17_20 40 32.png" alt=""><figcaption><p>Grid with 8 cells with overflow</p></figcaption></figure>

## Grid Sized to Children

If a Panel is sized to its children, then the size of the grid is based on the largest cell. The following code creates a grid with nine (9) cells. The largest button decides the size of the cell. All cells in the grid match the largest size.

```csharp
var panel = new Panel();
panel.AddToRoot();
panel.Anchor(Gum.Wireframe.Anchor.Center);
panel.Dock(Gum.Wireframe.Dock.SizeToChildren);

var panelVisual = panel.Visual;
panelVisual.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridHorizontal;
panelVisual.AutoGridHorizontalCells = 3;
panelVisual.AutoGridVerticalCells = 3;

for(int i = 0; i < 9; i++)
{
    var button = new Button();
    panel.AddChild(button);
    button.Width = 50 + (float)Random.Shared.NextDouble() * 50;
    button.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
    button.Height = 50 + (float)Random.Shared.NextDouble() * 50;
    button.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
    button.Text = $"Button {i}";
}
```

<figure><img src="../../.gitbook/assets/17_21 02 44.png" alt=""><figcaption><p>Buttons sized randomly. The largest width and height determine the grid size</p></figcaption></figure>

