# Forms Controls

## Introduction

Forms controls are a collection of classes which provide common UI behavior. You will probably be familiar with many controls. The most common controls include:

* Button&#x20;
* CheckBox
* ComboBox
* Image
* Label
* ListBox
* RadioButton
* ScrollViewer
* Slider
* StackPanel
* TextBox

All controls are in the `MonoGameGum.Forms.Controls` namespace.

{% hint style="info" %}
Forms naming is based on naming from WPF. If you are familiar with WPF or similar libraries like Avalonia, you may find many of the names and concepts familiar.
{% endhint %}

## Common Control Properties

All controls share a few common properties and characteristics. The following list provides a high-level introduction to forms control similarities. If the list doesn't make sense yet don't worry, we'll cover each topic in this and following tutorials.

* All controls can be added to a StackPanel. Technically, any control can be added to any other control, but for this tutorial we'll keep things simple by adding only to a StackPanel.
* All controls have a Visual property which can be used to position, size, and perform advance layout behavior. This Visual property is ultimately a GraphicalUiElement which provides access to all Gum layout properties.
* All controls can receive input from the mouse (usually through the Gum Cursor object). Most controls can also receive focus and input from gamepads.
* All controls support binding by assigning their BindingContext property. Children controls inherit BindingContext from their parents if the BindingContext is not explicitly assigned.

For the rest of this tutorial we'll add a few of the most common controls to our project and show how to work with them. Note that for this tutorial I've removed the Button control from the previous tutorial.

We also assume that your project has a `mainPanel` to hold all controls.

## Forms Control vs Visual

As we'll see below, each forms control has a specific purpose. Buttons are clicked, Labels display read-only strings, and TextBoxes can be used to input text. Each control provides properties and events specific to its purpose, standardizing the way each works.&#x20;

However, each control also wraps a Visual object which gives you full layout control. The Visual property is of type GraphicalUiElement, and it has access to the full Gum layout engine. For example, a button could be made to be as wide as its parents using the following code:

```csharp
// Initialize
// assuming MyButton is a valid Button:
MyButton.Visual.Width = 0;
MyButton.Visual.WidthUnits = DimensionUnitType.RelativeToContainer;
```

For more information about all of the properties available to GraphicalUiElement, see the [General Properties](../../../../gum-tool/gum-elements/general-properties/) section of the Gum tool - all properties in the tool are also available in code.

{% hint style="info" %}
Some Visual properties are also provided at the Forms Control level for convenience. These are:

* `X`
* `Y`
* `Width`
* `Height`

For example, the following two lines of code are equivalent:

```csharp
// Initialize
MyButton.Width = 100;
MyButton.Visual.Width = 100;
```
{% endhint %}

## Code Organization

For the sake of brevity, we will add all of our controls in the game's Initialize method after `mainPanel` has been created. Of course a full game may require more advanced organization but we'll keep everything in the Game Initialize for simplicity.

## Label

Labels are text objects which can display a string. Labels do not have any direct interaction such as responding to clicks. The following code adds a label to the project:

```csharp
// Initialize
var label = new Label();
mainPanel.AddChild(label);
label.Text = $"I was created at {System.DateTime.Now}";
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAEAGWLzQqCUBBGX2UYWhjEfQClRRSEIBHp0s3kHejS_QEdsxLfPb0LN-0O5_vOiHl37h2m0va8Q-ONGLLmy5jii1pwZPyVPFvYg-cBSqHmGUWyzWq_zuqgdRVuIUj0S2rpvmbFwv_F8WGsTuJx2SKoit8yZ5sacxiog6ZlEtZAAmP56YSdOs2iMo7VJQxTjRlOP54RijvIAAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_08 21 48.png" alt=""><figcaption><p>Label displaying when it was created</p></figcaption></figure>

The rest of this tutorial assumes that the Label is not removed. It is used to show when events have occurred.

## Button

Button controls are usually added when a user needs to perform a command. Buttons can be clicked with the mouse and gamepad, and their click event can be manually invoked for custom support such as pressing Enter on the keyboard.

The following code creates two buttons. One is disabled so it does not respond to click events:

```csharp
// Initialize
var button = new Button();
mainPanel.AddChild(button);
button.Text = "Click Me";
button.Click += (_, _) =>
    label.Text = $"Button clicked @ {System.DateTime.Now}";

var disabledButton = new Button();
mainPanel.AddChild(disabledButton);
disabledButton.Text = "Disabled Button";
disabledButton.IsEnabled = false;
disabledButton.Click += (_, _) =>
    label.Text = "This never happens";
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAEAI2PYUvDMBCG_8oR_NDh6A-YVHSbyEBFXD8GRtqc9FiajCbd1LH_bpN0G7MK5tPlvfe5927PFvaxrdnENS2OGWlyJBR9IZuwrWigFqRfhUYFGWjcwdKJch2EZHTD9amd3kuZmzdjXNA9qkRxwp58PSRmFSmZBOORKlrnjO6xafj8xUWrb8YqzfHDdSRnM0XlGp6Rs3MzatcZJKsxrEaQ3QLX0L0Qf2SvOIuhUHo_SriD_fLTOqzTuXCYU43pi9kdwui4siQrCoVy-v_VLxFvulTOp8x7vR8YYn94F_ZBR08G70JZHFqGx_9yO2d5RbbbfosNVGKzQW27PHb4BhMOatYlAgAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_07 14 20.gif" alt=""><figcaption><p>Buttons only respond to Click if IsEnabled is set to true (default)</p></figcaption></figure>

{% hint style="info" %}
Notice that the Label and two Buttons are stacked top-to-bottom. This is the default behavior layout behavior of StackPanels.

As mentioned earlier, layout-related properties can be accessed through a control's Visual property.

These tutorials focus on the Forms controls themselves but for more information you can look at the different properties available in the [General Properties](../../../../gum-tool/gum-elements/general-properties/) pages.
{% endhint %}

## CheckBox

CheckBox controls allow the user to toggle a bool value by clicking on it. Just like Button, the CheckBoxes support clicking with mouse and gamepad and changing the IsChecked property in code.

The following code creates a CheckBox with two method handlers (Checked/Unchecked):

```csharp
// Initialize
var checkBox = new CheckBox();
mainPanel.AddChild(checkBox);
checkBox.Text = "Click Me";
checkBox.Checked += (_, _) => label.Text = "CheckBox checked";
checkBox.Unchecked += (_, _) => label.Text = "CheckBox unchecked";
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAEAJWOUQuCMBSF_8rlPimJP8AwKB8iKIiyN0HWNmg4N7BZUvTfy-nMiB56O_ee8x3OHVfnZV1iZKqaByiUMIJIceMY4YVUUBKhtkRxCTEofoW9IbSwD8-fZmqwwzljqd5pbey_RSU5Dti61d9EchKSeTboKHritFjopgeT_vzFunhrOx2mvDEvPsNEClrAhmc4tm0nZzCJwcsDyH2IZ93aEelm0C782XBQ9J-OWr1b8PEEDSmh0XMBAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_07 17 43.gif" alt=""><figcaption><p>CheckBox responds to clicks</p></figcaption></figure>

## ComboBox

ComboBox provides a collapsible way to display and select from a list of options.

The following code creates a ComboBox which raises an event whenever an item is selected.

```csharp
// Initialize
var comboBox = new ComboBox();
for (int i = 0; i < 20; i++)
{
    comboBox.Items.Add($"Item {i}");
}
comboBox.SelectionChanged += (_, _) =>
{
    label.Text = "Selected: " + comboBox.SelectedObject;
};
mainPanel.AddChild(comboBox);
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAEAG2PTUvEMBCG_8oQPKR0KeKxtYL2IAuC4u4xsKTN6I6mE-hmdbH0v5vEbQ9qLu98Pe9MRrE-3B97UfrhiCtBTJ60pS8UpfjQA_Sa-EkzWqiB8RM2XnfvqSCzSvHSLm6N2bpn53yqR9TqdsEeYvyXaPZkjUyDM9W5vnV37nQGm3Oa2Bc3gCT2QKF7WQW5hquoeZ4pHhVDeLNBsfbYH-IWeaFETGCkSYloNClexjZosfPkuNlrfkUDeQ1yt4JdBvXN4ppuLLZ48mG1Ej8QmjLEkMMvMzSP7VvQuOn_L89AVonpGyQzeFCDAQAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_07 34 57.gif" alt=""><figcaption><p>ComboBox responding to items being selected</p></figcaption></figure>

## ListBox

ListBox provides a way to display a list of items. Each item can be selected.

The following code creates a ListBox which raises an event whenever an item is selected.

```csharp
// Initialize
var listBox = new ListBox();
listBox.Visual.Width = 150;
listBox.Visual.Height = 300;

for (int i = 0; i < 20; i++)
{
    listBox.Items.Add($"Item {i}");
}
listBox.SelectionChanged += (_, _) =>
{
    label.Text = 
        $"Selected item is {listBox.SelectedObject} at index {listBox.SelectedIndex}";
};
mainPanel.AddChild(listBox);
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAEAHWQUUvDMBSF_8ol-NDSUariy2YF3YMWBMUN9xIY2RLXq2kCbarF0v9ubus6pJiXm5xzv5uTtCyr7uuCzV1ZqxlDgw6Fxm_F5uxTlFAINM_CKA0pGPUFKyf2H70QhAtuRju-lXJtX6x1vU6oFrsRe6T9lFjmqGXQN44UVu7ONkduOPXkrxO_YlULHW9Quty3nV8lU_NB4SF33r1MyOXmzZYQoHGAXkwWvlzDBdUoCrlpuQG_jkMyp4qK8gVnnNEBWuw4oxDd6aqV0mrv0JplLsxBSYhSCLYz2IaQ3pxm0uPitWoozSDR8oMH3nNIN2AF7d_JSj7t3n3tQPjYRqpm2pGR7KNRsn8-dyDCBet-AF6amxDsAQAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_07 37 21.gif" alt=""><figcaption><p>ListBox responding to items being selected</p></figcaption></figure>

## RadioButton

RadioButton controls allow the user to view a set of options and pick from one of the available options. Radio buttons are mutually exclusive within their group. Radio buttons can be grouped together by putting them in common containers, such as StackLayouts.

The following creates six radio buttons in two separate groups.

```csharp
// Initialize
var group1 = new StackPanel();
mainPanel.AddChild(group1);

var group2 = new StackPanel();
// move group 2 down slightly:
group2.Y = 10;
mainPanel.AddChild(group2);

var radioButtonA = new RadioButton();
radioButtonA.Text = "Option A";
group1.AddChild(radioButtonA);

var radioButtonB = new RadioButton();
radioButtonB.Text = "Option B";
group1.AddChild(radioButtonB);

var radioButtonC = new RadioButton();
radioButtonC.Text = "Option C";
group1.AddChild(radioButtonC);


var radioButton1 = new RadioButton();
radioButton1.Text = "Option 1";
group2.AddChild(radioButton1);

var radioButton2 = new RadioButton();
radioButton2.Text = "Option 2";
group2.AddChild(radioButton2);

var radioButton3 = new RadioButton();
radioButton3.Text = "Option 3";
group2.AddChild(radioButton3);
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAEAI3SvQrCMBQF4FcJmSpIMelmcWgyiJOiLkKXYIMGayI1VVF8d_ujtpDgdb3nnvsRyAPPztPyiMe2KOUQK62sErm6SzzGF1Ggo1B6IbTM0QRpeUUrK7aHZhAM4lR_4zDJsrVZGmObeV3dFaY8kT96fK_yLGjX67BXp_56m4WbKiWjH-dod64QmTKstNbo5H102Y2aq_2VcC1vttpL8fxkldEoSfEHJp3Sr3gtBlvMsRhkMa_FYYs7Focs3lqORmCNOBrpNOrViPdlFLaoY1HI8v-OCLYix4ogKxrE-PkCGTo7X20DAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_07 39 16.gif" alt=""><figcaption><p>RadioButtons responding to clicks in two different groups</p></figcaption></figure>

## ScrollViewer

ScrollViewer provides a scrollable panel for controls. ScrollViewers are similar in concept to ListBoxes, but they can contain any type of item rather than only ListBoxItems.

The following code creates a ScrollViewer and adds buttons using AddChild.

```csharp
// Initialize
var scrollViewer = new ScrollViewer();
scrollViewer.Width = 200;
mainPanel.AddChild(scrollViewer);

for(int i = 0; i < 15; i++)
{
    var button = new Button();
    button.Text = "Button " + i;
    scrollViewer.AddChild(button);
}
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAEAF2OTw8BMRDFv8qkp5WVzZK4-HPAQdwE4dJL2cpO1DSpWYT47tousvQy7Xu_N30PMT_PqpPos6t0WyAhozJ416IvLsrBSSEtFGkDIyB9hRWr_TEKSWsg6Wtn46JY26W1HPUQPe-dNWaD-qrdJ92QItdksi0WXHqym-f_q6clmiJp0iEt6WBdgsSAPpYP_BhCp-dnmrYkPSSBP6HLrmK29G4xiY_4f_BrL1vrG3tAitr2F0gB38xPz2-fOhn2PMXzBa3qHJNKAQAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_07 41 52.gif" alt=""><figcaption><p>ScrollViewer containing buttons</p></figcaption></figure>

## Slider

Slider controls allow the user to select a value between a minimum and maximum value.

The following code creates a Slider which raises an event whenever its Value changes.

```csharp
// Initialize
var slider = new Slider();
slider.Width = 200;
slider.Minimum = 0;
slider.Maximum = 100;
slider.ValueChanged += (_,_) => 
    label.Text = $"Slider value: {slider.Value:0.0}";
mainPanel.AddChild(slider);
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAEAG2P0QqCMBSGX-UwulASsS4Ng_AigoIoqRshTm3kaE7QaZL07s1ZJtSutu_83_avIatiWabEV3nJHMIlVxwFfzDikwpzSJHLLUomIADJ7rBXeLkZYNmzWPZjd0FplO2yTBneqgLPvbZu979GmHBBLRP8WIXglOWf18zBeB13j5yqRE-nnveFG107LVONhxDrN5wMswcUJQsTlFdGYRyAdXJONgRziCXoZbq4EauVFkcx6RpA1Vo-NMNLfM_1njH5_6cuaM_I8wW4WNisYgEAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_07 43 36.gif" alt=""><figcaption><p>Slider responding to cursor input</p></figcaption></figure>

## TextBox

TextBox controls allow the user to see and edit string values. TextBoxes support typing with the keyboard, copy/paste, selection, and multiple lines of text.

TextBoxes are automatically focused when clicked, but IsFocused can be explicitly set to give focus.

The following code creates a TextBox which raises an event whenever its text is changed. The text is then copied over to a label.

```csharp
// Initialize
var textBox = new TextBox();
textBox.Placeholder = "Enter text here...";
textBox.Width = 200;
textBox.TextChanged += (_, _) => 
    label.Text = $"Text box text is now: {textBox.Text}";
mainPanel.AddChild(textBox);
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAEAG2Py4oCMRBFf6UoXHQzEsRlSwsqIoILcYTZBCSawgRjAm18oPjvky5baZjJqnLvPfV44Pw0Ox-xiNWZumi9jVY5eycs8KIqOCrrl8qTgxI8XeE7qt2BhSwfSP-xxUjrdViFEFmvUae2H2xR13-JibFOZxx8U5FucRxuDbd-_ZhsHLF0akcmOE1VSkmcJufFgaGKhBASW_Efq6NJwX6v11LrxhOj_J40fJWQbbqwyaEcgvSQHq_EoUR2JHK1TWvxGHsCH64FPNrdnjz1n_OaUD7A5y9_J2gcbgEAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../../.gitbook/assets/13_08 09 13.gif" alt=""><figcaption><p>TextBox responding to text input</p></figcaption></figure>
