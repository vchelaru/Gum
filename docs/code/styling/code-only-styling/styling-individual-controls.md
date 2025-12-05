# Styling Individual Controls

{% hint style="info" %}
This document assumes using V3 styles, which were introduced at the end of November 2025. If your project is using V2 visuals, you need to upgrade to V3 before the styling discussed on this document can be used.

For information on upgrading, see the [Migrating to 2025 November](../../../gum-tool/upgrading/migrating-to-2025-november.md) page.
{% endhint %}

## Introduction

Individual controls can be styled through their Visual property. By casting the Visual property to the control-specific type, color values can be assigned on a control.

## Accessing Strongly-Typed Visual

Every control includes a Visual type which can be casted to access control-specific values. The type of each visual is the same name as the control, with the word `Visual` appended.

The following table shows which visuals and properties are available for each type of control:

<table><thead><tr><th>Control</th><th width="216.272705078125">Visual</th><th>Styling Properties</th></tr></thead><tbody><tr><td>Button</td><td>ButtonVisual</td><td><ul><li>BackgroundColor</li><li>FocusedIndicatorColor</li><li>ForegroundColor</li></ul></td></tr><tr><td>CheckBox</td><td>CheckBoxVisual</td><td><ul><li>BackgroundColor</li><li>FocusedIndicatorColor</li><li>ForegroundColor</li><li>CheckColor</li></ul></td></tr><tr><td>ComboBox</td><td>ComboBoxVisual</td><td><ul><li>BackgroundColor</li><li>FocusedIndicatorColor</li><li>ForegroundColor</li><li>DropdownIndicatorColor</li></ul></td></tr><tr><td>ItemsControl</td><td>ItemsControlVisual</td><td><ul><li>BackgroundColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>Label</td><td>LabelVisual</td><td><ul><li>Color</li></ul></td></tr><tr><td>ListBoxItem</td><td>ListBoxItemVisual</td><td><ul><li>HighlightedBackgroundColor</li><li>SelectedBackgroundColor</li><li>ForegroundColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>ListBox</td><td>ListBoxVisual</td><td><ul><li>BackgroundColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>Menuitem</td><td>MenuItemVisual</td><td><ul><li>HighlightedBackgroundColor</li><li>SelectedBackgroundColor</li><li>ForegroundColor</li><li>SubmenuIndicatorColor</li></ul></td></tr><tr><td>Menu</td><td>MenuVisual</td><td><ul><li>BackgroundColor</li></ul></td></tr><tr><td>PasswordBox</td><td>PasswordBoxVisual</td><td><ul><li>BackgroundColor</li><li>ForegroundColor</li><li>SelectionBackgroundColor</li><li>PlaceholderColor</li><li>CaretColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>RadioButton</td><td>RadioButtonVisual</td><td><ul><li>BackgroundColor</li><li>ForegroundColor</li><li>RadioColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>ScrollBar</td><td>ScrollBarVisual</td><td><ul><li>TrackBackgroundColor</li><li>ScrollArrowColor</li></ul></td></tr><tr><td>ScrollViewer</td><td>ScrollViewerVisual</td><td><ul><li>BackgroundColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>Slider</td><td>SliderVisual</td><td><ul><li>BackgroundColor</li><li>TrackBackgroundColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>Splitter</td><td>SplitterVisual</td><td><ul><li>BackgroundColor</li></ul></td></tr><tr><td>TextBox</td><td>TextBoxVisual</td><td><ul><li>BackgroundColor</li><li>ForegroundColor</li><li>SelectionBackgroundColor</li><li>PlaceholderColor</li><li>CaretColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>Window</td><td>WindowVisual</td><td><ul><li>BackgroundColor</li></ul></td></tr></tbody></table>

## Code Example: Changing BackgroundColor

The following code shows how to access the Visual on a Button and TextBox to change the background color of each control:

```csharp
var button = new Button();
button.AddToRoot();
var buttonVisual = (ButtonVisual)button.Visual;
buttonVisual.BackgroundColor = Color.Red;

var textBox = new TextBox();
textBox.AddToRoot();
textBox.Y = 32;
var textBoxVisual = (TextBoxVisual)textBox.Visual;
textBoxVisual.BackgroundColor = Color.Blue;
```

<figure><img src="../../../.gitbook/assets/26_07 17 29.png" alt=""><figcaption></figcaption></figure>

{% hint style="info" %}
V3 Visuals no longer require changing colors on each individual state. By changing values like BackgroundColor, the visual automatically uses the color for other states such as Highlighted and Pushed.
{% endhint %}

## Color Properties vs Visual Element Properties

Each color property listed above ultimately sets the color of one of the parts of a control. These individual parts are also accessible through the casted visual `Visual`, but usually these color values should not be directly changed. Setting a property directly on a visual may only be temporary - colors can be reset in response to actions such as highlight, push, or variable changes such as IsEnabled.

For example, the following code sets the `Background.Color` property on a `Button`, and this seems to change the color; however, the background color resets back when the user hovers over the button.

```csharp
var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);
var buttonVisual = (ButtonVisual)button.Visual;
buttonVisual.Background.Color = Color.Pink;
```

<figure><img src="../../../.gitbook/assets/26_08 47 45.gif" alt=""><figcaption><p>Color is only <strong>temporary</strong> - hover resets the color back to BackgroundColor</p></figcaption></figure>

Since ButtonVisual exposes a `BackgroundColor` property, this should be used rather than directly setting the `Background.Color` value. In general, it's best to check if a color property already exists before making any changes to a Visual's child.

## Changing Background Texture (NineSlice)

Most forms controls use a background which draws a texture using NineSlice. Gum provides a number of built-in styles for backgrounds which can be swapped by accessing the Visual's Background property.

The following table provides the names of the background objects for each control type:

| Visual Type        | Background(s)                                                                                                                                                                                                                                         |
| ------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ButtonVisual       | <ul><li>Background</li></ul>                                                                                                                                                                                                                          |
| CheckBoxVisual     | <ul><li>CheckBoxBackground (NineSlice displaying the check when selected)</li></ul>                                                                                                                                                                   |
| ComboBoxVisual     | <ul><li>Background (main background)</li><li>ListBoxInstance.Background (dropdown background)</li></ul>                                                                                                                                               |
| ItemsControlVisual | <ul><li>Background</li></ul>                                                                                                                                                                                                                          |
| LabelVisual        | \<No Background>                                                                                                                                                                                                                                      |
| ListBoxItemVisual  | <ul><li>Background (only visible when highlighted or selected)</li></ul>                                                                                                                                                                              |
| ListBoxVisual      | <ul><li>Background</li></ul>                                                                                                                                                                                                                          |
| MenuItemVisual     | <ul><li>Background (only visible when highlighted or selected)</li><li>Dropdown background can be controlled through a VisualTemplate as discussed in the <a href="customizing-menu-and-menuitem.md">Customizing Menu and MenuItem</a> page</li></ul> |
| MenuVisual         | <ul><li>Background</li></ul>                                                                                                                                                                                                                          |
| PasswordBoxVisual  | <ul><li>Background</li><li>SelectionInstance</li></ul>                                                                                                                                                                                                |
| RadioButtonVisual  | <ul><li>RadioBackground (NineSlice displaying the Radio when selected)</li></ul>                                                                                                                                                                      |
| ScrollBarVisual    | <ul><li>TrackInstance</li><li>UpButtonInstance.Background</li><li>DownButtonInstance.Background</li><li>ThumbInstance.Background</li></ul>                                                                                                            |
| ScrollViewerVisual | <ul><li>Background</li></ul>                                                                                                                                                                                                                          |
| SliderVisual       | <ul><li>TrackBackground</li></ul>                                                                                                                                                                                                                     |
| SplitterVisual     | <ul><li>Background</li></ul>                                                                                                                                                                                                                          |
| TextBoxVisual      | <ul><li>Background</li><li>SelectionInstance</li></ul>                                                                                                                                                                                                |
| WindowVisual       | <ul><li>Background</li></ul>                                                                                                                                                                                                                          |

The background for a control's Visual can be modified using the built-in background styles, as shown in the following code:

```csharp
var panel = new Panel();
panel.AddToRoot();
panel.Anchor(Anchor.Center);
panel.Width = 720;
panel.WidthUnits = DimensionUnitType.Absolute;
panel.Height = 220;
panel.HeightUnits = DimensionUnitType.Absolute;
panel.Visual.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
panel.Visual.AutoGridHorizontalCells = 4;
panel.Visual.AutoGridVerticalCells = 4;

AddButton(Styling.ActiveStyle.NineSlice.Solid);
AddButton(Styling.ActiveStyle.NineSlice.Bordered);
AddButton(Styling.ActiveStyle.NineSlice.BracketVertical);
AddButton(Styling.ActiveStyle.NineSlice.BracketHorizontal);
AddButton(Styling.ActiveStyle.NineSlice.Tab);
AddButton(Styling.ActiveStyle.NineSlice.TabBordered);
AddButton(Styling.ActiveStyle.NineSlice.Outlined);
AddButton(Styling.ActiveStyle.NineSlice.OutlinedHeavy);
AddButton(Styling.ActiveStyle.NineSlice.Panel);
AddButton(Styling.ActiveStyle.NineSlice.CircleSolid);
AddButton(Styling.ActiveStyle.NineSlice.CircleBordered);
AddButton(Styling.ActiveStyle.NineSlice.CircleOutlined);
AddButton(Styling.ActiveStyle.NineSlice.CircleOutlinedHeavy);

void AddButton(StateSave backgroundStyle)
{
    var button = new Button();
    button.Text = backgroundStyle.Name;
    button.Width = 168;
    panel.AddChild(button);

    var visual = (ButtonVisual)button.Visual;

    visual.Background.ApplyState(backgroundStyle);
}
```

<figure><img src="../../../.gitbook/assets/27_07 18 42.png" alt=""><figcaption></figcaption></figure>

## Using Custom NineSlice Textures

A control's background NineSliceRuntime can be modified to reference a custom texture. For example, we can create a custom button using this texture:

<figure><img src="../../../.gitbook/assets/input_outline_square.png" alt=""><figcaption></figcaption></figure>

To use this texture on a button, first save the texture in the folder where you keep your content. For example, save the file in your game's Content folder.

You can load this texture however you load other textures in your project, or you can use Gum's built-in content loading.

```csharp
var texture = GumService.Default.ContentLoader.LoadContent<Texture2D>(
    "input_outline_square.png");
```

Once you have the texture loaded, you can use this on any control's background, a shown in the following code:

```csharp
var texture = GumService.Default.ContentLoader.LoadContent<Texture2D>(
    "input_outline_square.png");

var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);

var visual = (ButtonVisual)button.Visual;
visual.Background.Texture = texture;
visual.Background.TextureAddress = TextureAddress.EntireTexture;
```

<figure><img src="../../../.gitbook/assets/27_07 33 25.gif" alt=""><figcaption></figcaption></figure>

Notice that the button still uses its default coloring. This can be changed as shown in the following code:

<pre class="language-csharp"><code class="lang-csharp">var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);

var visual = (ButtonVisual)button.Visual;
visual.Background.Texture = texture;
visual.Background.TextureAddress = TextureAddress.EntireTexture;
<strong>visual.BackgroundColor = Color.Red;
</strong></code></pre>

<figure><img src="../../../.gitbook/assets/27_07 34 03.gif" alt=""><figcaption></figcaption></figure>

For more information on working with NineSliceRuntime, see the [NineSliceRuntime](../../standard-visuals/ninesliceruntime.md) page.

{% hint style="info" %}
Gum colors its controls by multiplying the texture color by the BackgroundColor. Therefore, if you intend to use BackgroundColor, it's best to use a white or grayscale texture.

If you prefer to use a texture that is not grayscale, you should set the BackgroundColor to White so that it is not tinted by Gum.
{% endhint %}

## Advanced styling

For more control over colors and states, see the [Styling Using States](styling-using-states.md) page.
