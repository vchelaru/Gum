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

var panelVisual = panel.Visual;

panelVisual.Height = 400;
panelVisual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

panelVisual.Width = 400;
panelVisual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

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

```
```
