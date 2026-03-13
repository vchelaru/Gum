# RadioButton

## Introduction

RadioButtons provide a way to display multiple mutually-exclusive options to the user. Clicking on one RadioButton unchecks all other RadioButtons in a group.

## Code Example: Creating RadioButtons

The following code creates RadioButtons for selecting difficulty:

```csharp
// Initialize
var stackPanel = new StackPanel();
stackPanel.AddToRoot();

var easyRadioButton = new RadioButton();
stackPanel.AddChild(easyRadioButton);
easyRadioButton.Text = "Easy";

var mediumRadioButton = new RadioButton();
stackPanel.AddChild(mediumRadioButton);
mediumRadioButton.Text = "Medium";

var hardRadioButton = new RadioButton();
stackPanel.AddChild(hardRadioButton);
hardRadioButton.Text = "Hard";
```

<figure><img src="../../.gitbook/assets/13_09 42 01.gif" alt=""><figcaption></figcaption></figure>

## Grouping by Container

RadioButtons automatically group themselves based on their container. If RadioButtons are added directly to a Screen or Component, then they all use the same group and will all be mutually exclusive.

Alternatively, RadioButton instances can be grouped into separate containers (such as StackPanels) to control their grouping. the following example shows two StackPanels, each with three RadioButton instances.

<figure><img src="../../.gitbook/assets/31_06 17 29.png" alt=""><figcaption><p>StackPanels grouping RadioButtons</p></figcaption></figure>

At runtime, the RadioButtons in each StackPanel are mutually exclusive.

<figure><img src="../../.gitbook/assets/31_06 18 57.gif" alt=""><figcaption><p>Each RadioButton is mutually exclusive with the other RadioButtons in the same column</p></figcaption></figure>

## Checked and Unchecked Events

`RadioButton` inherits `Checked` and `Unchecked` events from `ToggleButton`. `Checked` fires when a RadioButton becomes selected; `Unchecked` fires when it is deselected because another RadioButton in the same group was chosen.

```csharp
// Initialize
var stackPanel = new StackPanel();
stackPanel.AddToRoot();

var easyRadioButton = new RadioButton();
stackPanel.AddChild(easyRadioButton);
easyRadioButton.Text = "Easy";

var hardRadioButton = new RadioButton();
stackPanel.AddChild(hardRadioButton);
hardRadioButton.Text = "Hard";

easyRadioButton.Checked += (sender, _) =>
{
    System.Console.WriteLine("Easy selected");
};
hardRadioButton.Checked += (sender, _) =>
{
    System.Console.WriteLine("Hard selected");
};
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACqWPQWsCMRCF74L_YchppbI_oGUFuxQVPIgKvQQkbAY2GCeQzNZa8b83saKSempzfG_e-16O_R6AmIVJtxPPwL7D4VkxZNgoa74wyuJDeQismu1CEVqogHAPq6tQDF4k3fxyrPXaLZ3jsyEpxVGFw1Jp4147ZkeXjjvlQUndGquLLJnOMqlc4yfHRineoiHFFdoqr_8GzZLpLJNu0Gk0LtB8WN1is0UNTxUUAUmjH8JmANVI0lESxLc6BMZdWTsKzmL57g3j3BAWP5-BgBYbxghIG04PdvyLkbb_Yoh-7_QNwOpGWxkCAAA" target="_blank">Try on XnaFiddle.NET</a>

## Reading IsChecked

To determine which RadioButton is selected at any point, check the `IsChecked` property on each instance. `IsChecked` is `true` for the selected button and `false` for all others in the group.

```csharp
// Initialize
var stackPanel = new StackPanel();
stackPanel.AddToRoot();

var easyRadioButton = new RadioButton();
stackPanel.AddChild(easyRadioButton);
easyRadioButton.Text = "Easy";

var hardRadioButton = new RadioButton();
stackPanel.AddChild(hardRadioButton);
hardRadioButton.Text = "Hard";

var button = new Button();
button.AddToRoot();
button.Y = 100;
button.Text = "Check selection";
button.Click += (_, _) =>
{
    if(easyRadioButton.IsChecked == true)
        System.Console.WriteLine("Easy is selected");
    else if(hardRadioButton.IsChecked == true)
        System.Console.WriteLine("Hard is selected");
    else
        System.Console.WriteLine("Nothing selected");
};
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACp2RUUvDMBSF3wf7D5c-tTjKfHVUmEV0ICLbQITAiMvVXpYl0KTqHPvvJm1pS1CQ5fHcc8-XnBzHI4BoYe6qfXQFtqxwUiukyBKX9I1Ojj54Ccby7e6JK5SQgcJPWHVCnMyY6ufpXIi1Xmpt6wFTfh25OSy5IH1TWatVmzFQfgnJC5IiDja9LZDSNX5Zl8iiWzdgUQcteCnOgwab3hZIPfTeDQbQ1yFrgGn0oJxWfHH2y-m0F7rwvMDtDgxK3FrSqua0nlySG11kEG8msEkgu2bqyBS4Q29hbenC1FEoIMvqj04aqz-rg7G4T3OtjJaYPpdk8YEUxk2jQKa9AbqH-mv7JZQGPSjs5UyQb_Fv0L8iHrUtSL0HEadZNB6dfgAYHhTD6QIAAA" target="_blank">Try on XnaFiddle.NET</a>
