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

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACp2NuwrCMBSG90Lf4ZCpgvQBFAcVUQdBascswQQ82CbQnnjFdzcp0pbUqev3X753HAGwfb21JZsBVVZNG4IaCUWBL-Uwu4kKahLn61FoVcACtLrDqQXJZM51l6dLKXOTGUNNwLWfK1E_MyHRrCyR0b-PHvlzsr5gIZNg6WsBSnP1IPfI2cYFnLXSUkm05TjtYOuLA9ipD03Uk19EJcepg6WvBajT7lzgpCyOPnH0BaJP-5HOAQAA)

<figure><img src="../../.gitbook/assets/13_09 42 01.gif" alt=""><figcaption></figcaption></figure>

## Grouping by Container

RadioButtons automatically group themselves based on their container. If RadioButtons are added directly to a Screen or Component, then they all use the same group and will all be mutually exclusive.

Alternatively, RadioButton instances can be grouped into separate containers (such as StackPanels) to control their grouping. the following example shows two StackPanels, each with three RadioButton instances.

<figure><img src="../../.gitbook/assets/31_06 17 29.png" alt=""><figcaption><p>StackPanels grouping RadioButtons</p></figcaption></figure>

At runtime, the RadioButtons in each StackPanel are mutually exclusive.

<figure><img src="../../.gitbook/assets/31_06 18 57.gif" alt=""><figcaption><p>Each RadioButton is mutually exclusive with the other RadioButtons in the same column</p></figcaption></figure>

The following code creates two independent RadioButton groups side by side, one for difficulty and one for game mode. Selecting a RadioButton in one group does not affect the other group.

```csharp
// Initialize
var outerPanel = new StackPanel();
outerPanel.Orientation = Orientation.Horizontal;
outerPanel.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
outerPanel.Spacing = 20;
outerPanel.AddToRoot();

var difficultyPanel = new StackPanel();
outerPanel.AddChild(difficultyPanel);

var difficultyLabel = new Label();
difficultyPanel.AddChild(difficultyLabel);
difficultyLabel.Text = "Difficulty:";

var easyRadioButton = new RadioButton();
difficultyPanel.AddChild(easyRadioButton);
easyRadioButton.Text = "Easy";

var mediumRadioButton = new RadioButton();
difficultyPanel.AddChild(mediumRadioButton);
mediumRadioButton.Text = "Medium";

var hardRadioButton = new RadioButton();
difficultyPanel.AddChild(hardRadioButton);
hardRadioButton.Text = "Hard";

var modePanel = new StackPanel();
outerPanel.AddChild(modePanel);

var modeLabel = new Label();
modePanel.AddChild(modeLabel);
modeLabel.Text = "Mode:";

var soloRadioButton = new RadioButton();
modePanel.AddChild(soloRadioButton);
soloRadioButton.Text = "Solo";

var coopRadioButton = new RadioButton();
modePanel.AddChild(coopRadioButton);
coopRadioButton.Text = "Co-op";

var pvpRadioButton = new RadioButton();
modePanel.AddChild(pvpRadioButton);
pvpRadioButton.Text = "PvP";
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACqWSzU7DMBCE75X6DlFORYIIcSziAC2iSCCqNsAlFxO7dIXjjRIn0CLenbUPieNG_LS3enZmvnXqz-EgCMLb8qbKwnGgi0ocWwUUaGAStoLksGZFgJUWxZwpIYOLQIn3YKlZ-maF0dF5otp59FCAUJppQEVe5xTNsIAt0kl2E09QVkxGz8D1-pHQJeVopWjKNIs3uSijKWRCldRhxkaKFkJSaS1inKxB8kKobucyZymoV2o6O-1OLjmPcYGo7eKJMtfjsFpBWkm9-dsdqcNiR16wr_GOvTSN9rct84J9jdbc9VopisWHpsIknDaDcRI2aMHKzYJxwKtKa_snGLSj_LyAFzdeT2oXuKaBQ84Ehyo7gL1TYNw7Ysu_tyNngzUr-AF8L268ntSyZzRw745c_PPxNJH22Rip_8E05m6-eSTNwfk6JLkPo0SJv3-cHpAXNC5PaqFLGjjMFDHfi-kFjcuTWuYETzB3oHm9H7ObM6au0hLn9Zx44XDw9Q1Cki1wRAUAAA" target="_blank">Try on XnaFiddle.NET</a>

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

var label = new Label();
stackPanel.AddChild(label);

easyRadioButton.Checked += (sender, _) =>
{
    label.Text = "Easy selected";
};
hardRadioButton.Checked += (sender, _) =>
{
    label.Text = "Hard selected";
};
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA6WQzQrCMBCEX2XJqQUpUm9KDypFBQ-iHgsSm4UGYwJN6i--u01brK31ojktM7vzZfdOFnqWHcnQpBn2CJfccCr4DcmQnGgK2tD4sKISBQQg8Qybl-C4o0jWvjdmbKvWSpnCsMNI9XVNGVeTzBglq4Q3pSNimnDBnNakbWtJ3hYvJk-Msn7f98PcLKsKndCU_YZuTdq2ltREz3OzgRZ0_7rW0tZfUUVn127TBOMDspIwCcDRKBmmPdi5EFhxEEbyHknIXxHyeQ3QKDA2WP_t0bHJnyC7exeIPJ5Cn-U6WAIAAA)

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

var label = new Label();
stackPanel.AddChild(label);

var button = new Button();
stackPanel.AddChild(button);
button.Text = "Check selection";
button.Click += (_, _) =>
{
    if(easyRadioButton.IsChecked == true)
        label.Text = "Easy is selected";
    else if(hardRadioButton.IsChecked == true)
        label.Text = "Hard is selected";
    else
        label.Text = "Nothing selected";
};
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA51SXWvCMBT9K5c8tSBF3JuSh1lkE8YYzseCpM3dejFLoEn3Jf73NbWrGp1jy1Ny7zn3nJxkw-b2pn5hY1fVOGCkyZFQ9IlszF5FBdaJYv0gNCrgoPENHvtCFE8yve8n11IuzcIY1zY8GYX9WAhJZlo7Z3Q34aByZkRakpJRwPSwoJQs8d01E7N6OByNZk1zt-ukS1HJ_0kHTA8LSsfSt03zSFqJvE_rzu9_lGqR32Hlh0Z_8Zj31vIzjtISizVYVFg4Mro312FTRU27rU45RKsBrGLg_nw1y_Qm09AsegrfIJnbdjBK4Bz8d4l3UL_am5w-CZDtfOA-I49HZdFrhMn-UcNnf1HjIvveuJL08yl7O2HbL25P9ikZAwAA)
