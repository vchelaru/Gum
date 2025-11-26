# Styling Using ActiveStyles

{% hint style="info" %}
This document assumes using V3 styles, which were introduced at the end of November 2025. If your project is using V2 visuals, you need to upgrade to V3 before the styling discussed on this document can be used.

For information on upgrading, see the [Migrating to 2025 November](../../../gum-tool/upgrading/migrating-to-2025-november.md) page.
{% endhint %}

## Introduction

Gum can be restyled using an ActiveStyles object. Changes to ActiveStyles result in style changes for all controls created after the change is made. ActiveStyles can make it easy to restyle all controls without needing to make changes to each individual control.

## Styling.ActiveStyle.Colors

Gum includes a Styling object which contains multiple Color values for default styling. For example the primary color can be changed using the following code:

```csharp
Styling.ActiveStyle.Colors.Primary = Color.DarkGreen;
```

The following properties exist:

<table><thead><tr><th width="212.18182373046875">Property</th><th>Used By</th></tr></thead><tbody><tr><td>Colors.Primary</td><td><ul><li>Button Background</li><li>CheckBox Background</li><li>ComboBox DropdownIndicator</li><li>ListBoxItem Background (Selected)</li><li>MenuItem Background (Selected)</li><li>RadioButton Background</li><li>Slider Thumb Background</li><li>TextBox Caret</li><li>Window Background (Default only border)</li></ul></td></tr><tr><td>Color.Warning</td><td><ul><li>FocusIndicators on all controls</li></ul></td></tr><tr><td>Color.Accent</td><td><ul><li>ListBoxItem Background (Highlight)</li><li>MenuItem Background (Highlight)</li></ul></td></tr><tr><td>Color.InputBackground</td><td><ul><li>ComboBox Background</li><li>ListBox Background</li><li>Menu Background</li><li>PasswordBox Background</li><li>ScrollViewer Background</li><li>Slider Track Background</li><li>Splitter Background</li><li>TextBox Background</li></ul></td></tr><tr><td>Color.SurfaceVariant</td><td><ul><li>ScrollBar Track Background</li></ul></td></tr><tr><td>Color.IconDefault</td><td><ul><li>CheckBox Check</li><li>RadioButton Radio</li><li>ScrollBar UpButton/DownButton Icon</li></ul></td></tr><tr><td>Color.TextPrimary</td><td><ul><li>Button Text</li><li>CheckBox Text</li><li>ComboBox Text</li><li>Label</li><li>ListBoxItem Text</li><li>MenuItem Text</li><li>MenuItem SubmenuIndicator </li><li>PasswordBox Text</li><li>RadioButton Text</li><li>TextBox Text</li></ul></td></tr><tr><td>Color.TextMuted</td><td><ul><li>PasswordBox PlaceholderTextInstance</li><li>TextBox PlaceholderTextInstance</li></ul></td></tr></tbody></table>

## Code Example: Applying Styles Before Creating Controls

The `Styling.ActiveStyle.Color` property includes styles that are used by controls when they are created. We can see how these affect controls by creating a sample project:

```csharp
protected override void Initialize()
{
    GumUI.Initialize(this, DefaultVisualsVersion.V3);

    var window = new Window();
    window.AddToRoot();
    window.Anchor(Anchor.Center);
    window.Width = 270;
    window.MinWidth = 270;
    window.Height = 400;
    window.MinHeight = 400;

    var windowVisual = (WindowVisual)window.Visual;

    var stackPanel = new StackPanel();
    window.AddChild(stackPanel);
    stackPanel.Dock(Dock.Top);
    stackPanel.Y = window.TitleHeight;
    stackPanel.Width = -16;

    var button = new Button();
    var buttonVisual = (Gum.Forms.DefaultVisuals.V3.ButtonVisual)button.Visual;
    stackPanel.AddChild(button);

    var checkBox = new CheckBox();
    stackPanel.AddChild(checkBox);

    var comboBox = new ComboBox();
    stackPanel.AddChild(comboBox);
    for(int i = 0; i < 10; i++)
    {
        comboBox.Items.Add($"ComboBox Item {i}");
    }

    var listBox = new ListBox();
    stackPanel.AddChild(listBox);
    listBox.Height = 200;
    for(int i = 0; i < 20; i++)
    {
        listBox.Items.Add($"Item {i}");
    }

    var radioButton1 = new RadioButton();
    stackPanel.AddChild(radioButton1);
    radioButton1.Text = "Option 1";

    var radioButton2 = new RadioButton();
    stackPanel.AddChild(radioButton2);
    radioButton2.Text = "Option 2";

    var textBox = new TextBox();
    stackPanel.AddChild(textBox);
}
```

This code produces a set of controls which can be used to check how styling is applied.

<figure><img src="../../../.gitbook/assets/25_21 23 15.png" alt=""><figcaption><p>Controls using default styles</p></figcaption></figure>

We can prefix the following code before creating all of our controls:

```csharp
protected override void Initialize()
{
    GumUI.Initialize(this, DefaultVisualsVersion.V3);

    Styling.ActiveStyle.Colors.Primary = Color.DarkGreen;
    Styling.ActiveStyle.Colors.InputBackground = Color.Black;
    Styling.ActiveStyle.Colors.TextPrimary = Color.LimeGreen;
    Styling.ActiveStyle.Colors.Accent = Color.Yellow;

    // Create controls here:
```

By changing these colors, controls are created using the new colors:

<figure><img src="../../../.gitbook/assets/25_21 34 56.png" alt=""><figcaption></figcaption></figure>

## Styling and Creation Order

Styling only applies after it has been set. Controls which are created before styling is set do not automatically update to the new style. The following code shows how order can impact how styling is assigned:

```csharp
var stackPanel = new StackPanel();
stackPanel.AddToRoot();
stackPanel.Anchor(Anchor.Center);

var button1 = new Button();
stackPanel.AddChild(button1);

Styling.ActiveStyle.Colors.Primary = Color.Red;

var button2 = new Button();
stackPanel.AddChild(button2);

Styling.ActiveStyle.Colors.Primary = Color.Purple;

var button3 = new Button();
stackPanel.AddChild(button3);

```

<figure><img src="../../../.gitbook/assets/26_06 27 42.png" alt=""><figcaption></figcaption></figure>

{% hint style="info" %}
This behavior may change in future versions of Gum.
{% endhint %}

