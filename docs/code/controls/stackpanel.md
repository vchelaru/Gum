# StackPanel

## Introduction

StackPanel is a container used to for controls which should stack vertically or horizontally, and which supports wrapping. StackPanels do not include any visual so they are always invisible.

## Code Example: Adding Buttons to a StackPanel

The following code shows how to add Button instances to a StackPanel. Notice that each button is automatically stacked vertically.

```csharp
// Initialize
var stackPanel = new StackPanel();
stackPanel.AddToRoot();
stackPanel.X = 50;
stackPanel.Y = 50;
stackPanel.Width = 200;

var random = new System.Random();
for (int i = 0; i < 10; i++)
{
    var button = new Button();
    stackPanel.AddChild(button);
    button.Text = "Button " + i;
    button.Height = 36;
    button.Click += (_, _) =>
        button.Text = DateTime.Now.ToString();
}
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACm2PUUvDMBSF3wv9D5c-tXSEquiDs4JOUF9EtoIOCiOucb2svYH21qlj_90kdY6qeUnud07OSba-BxDct7ddHZwDN50aOYKEjLLCT2Vw8CYbaFku14-SVAUpkNrA7AeE0Tingy6uiiLTU635t_Bsrp4mQzb_hz1hwaXhx4kVcrL9jaRC1_vuj5ZVLaaOuZZX3UCIxIDGkozNdgFHdo_jKKdtTmCWzXnpmDV951y7wQVYffiHSYlVEfb-vaOfRKbe2UTkQR9gDhADDj13CleldZ2cDYVJhcs1xCmEixEsIkgvodf_FtxIVhnWSjzojcj0jBuklXvuLvC9ne99ARnmnl_AAQAA)

## Stack Panel Sizing

By default `StackPanels` contain a `Visual` with the following properties:

* WidthUnits = Absolute
* HeightUnits = RelativeToChildren

This means that as more children are added to a StackPanel, the StackPanel grows vertically.

The following code creates a main StackPanel with two internal StackPanels. Each internal StackPanel contains a Button which can be used to add labels to the respective internal StackPanel.

```csharp
// Initialize
var mainStackPanel = new StackPanel();
mainStackPanel.AddToRoot();
mainStackPanel.X = 50;
mainStackPanel.Y = 50;
mainStackPanel.Width = 200;

var firstInternalStackPanel = new StackPanel();
mainStackPanel.AddChild(firstInternalStackPanel);
var firstButton = new Button();
firstButton.Text = "Add to first stack panel";
firstButton.Width = 250;
firstInternalStackPanel.AddChild(firstButton);
firstButton.Click += (_, _) =>
{
    var label = new Label();
    label.Text = $"Added at {DateTime.Now}";
    firstInternalStackPanel.AddChild(label);
};

var secondInternalStackPanel = new StackPanel();
mainStackPanel.AddChild(secondInternalStackPanel);
var secondButton = new Button();
secondButton.Text = "Add to second stack panel";
secondButton.Width = 250;
secondInternalStackPanel.AddChild(secondButton);
secondButton.Click += (_, _) =>
{
    var label = new Label();
    label.Text = $"Added at {DateTime.Now}";
    secondInternalStackPanel.AddChild(label);
};
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACr2R3UrDQBCF7wN5h2HxIsUSiuCNEkErSEFENKDCQlm7K11MdiWZWDHk3d3d2DQ_hiKCuUrmzJz5Jqf0PQCyyK-KlJwAZoWYuopUEiVL5KcwZfLOMkiZVPfIVq-3TIkEIlBiA7tCMDmlqtsTnnMe6zut8Sfx0Vgcz4b1p5H6g-S4NtrRzIpUWaYXmeW4UCgyxZLfw83XMuHBiIkdaXZcFIhaffvWH86zJYax-EDTQYlxBtT1IOTWEN6sIyW9ieYkd-8IRw-0Hu3vnifSbDmMIFhOYTmB6Iyqkiowj70hYc_NX7m27w7eqk7Zoh84dsGBIZSXDEUsUxHe6E3l0G3_XkhnaN2rJqVcrLTif41pzGWbU62PBdVWB0nV4iCqzkw3qzGYPu4urY7Zv8W1n7OVF_G9yve-AA6oIAUSBAAA)

<figure><img src="../../.gitbook/assets/13_09 51 57.gif" alt=""><figcaption></figcaption></figure>

## Orientation

StackPanel Orientation controls whether items in a StackPanel are positioned top-to-bottom or left-to-right. Changing the StackPanel Orientation property changes the internal visual's ChildrenLayout property. For more information on ChildrenLayout see the [ChildrenLayout page](../../gum-tool/gum-elements/container/children-layout.md).

WidthUnits and HeightUnits are not changed when changing Orientation. If you want the StackPanel instance to grow horizontally when changing Orientation to Horizontal, you need to modify the WidthUnits and HeightUnits.

The following code creates a horizontal StackPanel with buttons stacked left-to-right.

```csharp
// Initialize
var stackPanel = new StackPanel();
stackPanel.AddToRoot();
stackPanel.X = 50;
stackPanel.Y = 50;
stackPanel.Orientation = Orientation.Horizontal;
stackPanel.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
stackPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
stackPanel.Height = 50;

for (int i = 0; i < 5; i++)
{
    var button = new Button();
    stackPanel.AddChild(button);
    button.Text = "Button " + i;
}
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACo1QTU_DMAy9T9p_sHrqNBTtsguDw2AS4wQa5UvqJaOGWmQOatwBm_bfcVoNKNqBXJz3nv0Sv22_B5Bchot6lRyDVDUeNQwxCVlHG1Q6WdsKgtin12vL6OAUGN_h5ptIB5Ocf3QzLYrML7yXv8KDjo5HXe7xAHdVEbJYIc-q_kJm7ivaeEWuO3FHobbO3FMh5a3-Peic7mRmVmz2-YbBzGiFHNQjypEyC3RqusbMn5fkigr5oOcc6aWU_5pOl8G7WrBr1XrsN8352VeQEguQcqOJlhMYaxkOBzlvcwY9MfRlLdJkEAM_a0CTadS7gTcrpG3_vqNFJsOP-HSetAZ6gSGQ9uySfm_3BWmf8YcCAgAA" target="_blank">Try on XnaFiddle.NET</a>
