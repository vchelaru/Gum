# Styling Using States

{% hint style="info" %}
This document assumes using V3 styles, which were introduced at the end of November 2025. If your project is using V2 visuals, you need to upgrade to V3 before the styling discussed on this document can be used.

For information on upgrading, see the [Migrating to 2025 November](../../../gum-tool/upgrading/migrating-to-2025-november.md) page.
{% endhint %}

## Introduction

Gum controls respond to cursor and input actions by changing their appearance. States can be used to customize how controls react to these actions.

Each control provides states through its Visual object. Once a Visual is casted to its specific type, its states can be customized.

For more information about Visuals, see the [Styling Individual Controls](styling-individual-controls.md) page.

## Accessing Strongly-Typed States

The following table lists the states available for each control type:

<table><thead><tr><th width="276.727294921875">Visual Type</th><th>States</th></tr></thead><tbody><tr><td>ButtonVisual</td><td><ul><li>Disabled</li><li>DisabledFocused</li><li>Enabled</li><li>Focused</li><li>Highlighted</li><li>HighlightedFocused</li><li>Pushed</li></ul></td></tr><tr><td>CheckBoxVisual</td><td><ul><li>DisabledOn / Off / Indeterminate</li><li>DisabledFocusedOn / Off / Indeterminate</li><li>EnabledOn / Off / Indeterminate</li><li>FocusedOn / Off / Indeterminate</li><li>HighlightedOn / Off / Indeterminate</li><li>HighlightedFocusedOn / Off / Indeterminate</li><li>PushedOn / Off / Indeterminate</li></ul></td></tr><tr><td>ComboBoxVisual</td><td><ul><li>Disabled</li><li>DisabledFocused</li><li>Enabled</li><li>Focused</li><li>Highlighted</li><li>HighlightedFocused</li><li>Pushed</li></ul></td></tr><tr><td>ItemsControlVisual</td><td><ul><li>Enabled</li><li>Focused</li></ul></td></tr><tr><td>LabelVisual</td><td>&#x3C;No States></td></tr><tr><td>ListBoxItemVisual</td><td><ul><li>Disabled</li><li>Enabled</li><li>Focused</li><li>Highlighted</li><li>Selected</li></ul></td></tr><tr><td>ListBoxVisual</td><td><ul><li>Disabled</li><li>DisabledFocused</li><li>Enabled</li><li>Focused</li><li>Highlighted</li><li>HighlightedFocused</li><li>Pushed</li></ul></td></tr><tr><td>MenuItemVisual</td><td><ul><li>Disabled</li><li>Enabled</li><li>Focused</li><li>Highlighted</li><li>Selected</li></ul></td></tr><tr><td>MenuVisual</td><td>&#x3C;No States></td></tr><tr><td>PasswordBoxVisual</td><td><ul><li>Disabled</li><li>Enabled</li><li>Focused</li><li>Highlighted</li></ul><p>PasswordBoxVisual also includes states for single and multi-line layouts, but these states are not covered in this document.</p></td></tr><tr><td>RadioButtonVisual</td><td><ul><li>DisabledOn / Off</li><li>DisabledFocusedOn / Off</li><li>EnabledOn / Off</li><li>FocusedOn / Off</li><li>HighlightedOn / Off</li><li>HighlightedFocusedOn / Off</li><li>Pushed On / Off</li></ul></td></tr><tr><td>ScrollBarVisual</td><td>&#x3C;No Color-related States><br>ScrollBar includes states for Vertical and Horizontal alignment, but these states are not covered in this document.</td></tr><tr><td>ScrollViewerVisual</td><td><ul><li>Enabled</li><li>Focused</li></ul></td></tr><tr><td>SliderVisual</td><td><ul><li>Disabled</li><li>DisabledFocused</li><li>Enabled</li><li>Focused</li><li>Highlighted</li><li>HighlightedPushed</li><li>Pushed</li></ul></td></tr><tr><td>SplitterVisual</td><td>&#x3C;No States></td></tr><tr><td>TextBoxVisual</td><td><ul><li>Disabled</li><li>Enabled</li><li>Focused</li><li>Highlighted</li></ul><p>TextBoxVisual also includes states for single and multi-line layouts, but these states are not covered in this document.</p></td></tr><tr><td>WindowVisual</td><td>&#x3C;No States></td></tr></tbody></table>

{% hint style="info" %}
Each control provides states for common situations. If your game requires additional state support, please let us know on GitHub or Discord.
{% endhint %}

## Code Example: Setting States on a ButtonVisual

To change how a control displays itself using states, the following steps must be performed:

1. Cast the Visual of the control to the appropriate Visual type
2. Clear the state that you would like to change
3. Assign the behavior of the state by setting its Apply or filling its Variables

The following code shows how to change a button so that its colors are explicitly set when highlighted, pressed, or in its default (Enabled) state.

```csharp
var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);
var buttonVisual = (ButtonVisual)button.Visual;

var enabledState = buttonVisual.States.Enabled;
enabledState.Clear();
enabledState.Apply = () =>
{
    buttonVisual.Background.Color = Color.Green;
};

var pushedState = buttonVisual.States.Pushed;
pushedState.Clear();
pushedState.Apply = () =>
{
    buttonVisual.Background.Color = Color.DarkBlue;
};

var highlightedState = buttonVisual.States.Highlighted;
highlightedState.Clear();
highlightedState.Apply = () =>
{
    buttonVisual.Background.Color = Color.Yellow;
};

// forcably apply the states to see the effect immediately
button.UpdateState();
```

<figure><img src="../../../.gitbook/assets/26_08 33 09.gif" alt=""><figcaption><p>Button applying color states</p></figcaption></figure>

Although the default states only modify color, custom states are free to modify any property on the Visual. For example, the following code shows how to also modify the button's text size and increase the background size.

```csharp
var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);
var buttonVisual = (ButtonVisual)button.Visual;

var enabledState = buttonVisual.States.Enabled;
enabledState.Clear();
enabledState.Apply = () =>
{
    buttonVisual.TextInstance.FontScale = 1;
    buttonVisual.Background.Width = 0;
    buttonVisual.Background.Height = 0;
};

var pushedState = buttonVisual.States.Pushed;
pushedState.Clear();
pushedState.Apply = () =>
{
    buttonVisual.TextInstance.FontScale = 1;
    buttonVisual.Background.Width = -4;
    buttonVisual.Background.Height = -4;
};

var highlightedState = buttonVisual.States.Highlighted;
highlightedState.Clear();
highlightedState.Apply = () =>
{
    buttonVisual.TextInstance.FontScale = 1.3f;
    buttonVisual.Background.Width = 2;
    buttonVisual.Background.Height = 2;
};
```

<figure><img src="../../../.gitbook/assets/26_08 37 40.gif" alt=""><figcaption><p>Button changing its background and text size through states</p></figcaption></figure>

Any property on a control's Visual can be changed through state. For more information on working with the standard visual types which make up controls, see the [Standard Visuals](../../standard-visuals/) page.



