# Localization

## Introduction

Gum supports localization using CSV and RESX files. Localization can be performed automatically by linking a localization file in your Gum project, or it can be done by hand in code-only projects. This document explains how to use the `LocalizationManager` to perform localization.

## Localization in Gum Projects (Using the Gum UI Tool)

If you are using the Gum UI Tool to create your project, you can add and test localization in the tool itself. For information on how to set up localization in the Gum UI tool, see the [Localization page](../gum-tool/localization.md).

Once you have a project set up with localization, the only code change needed is to specify the language index. Keep in mind that index 0 is the string IDs, so if this value is unchanged then your game will display the string IDs.

For example, the following is a screenshot from the Gum UI tool:

<figure><img src="../.gitbook/assets/06_04 52 35.png" alt=""><figcaption><p>Screen displaying string IDs</p></figcaption></figure>

At runtime the string IDs are displayed by default:

<figure><img src="../.gitbook/assets/06_04 53 46.png" alt=""><figcaption><p>Screen displaying string IDs</p></figcaption></figure>

We can select our string IDs before creating our screen:

{% tabs %}
{% tab title="Generated Code" %}
```csharp
protected override void Initialize()
{
    var project = GumUI.Initialize(this, "GumProject/GumProject.gumx");

    // set the language index before instantiating a screen or component:
    GumUI.LocalizationService.CurrentLanguage = 1;
    
    var screen = new MainMenu();
    screen.AddToRoot();

    base.Initialize();
}
```
{% endtab %}

{% tab title="No Generated Code" %}
```csharp
protected override void Initialize()
{
    var project = GumUI.Initialize(this, "GumProject/GumProject.gumx");

    // set the language index before calling ToGraphicalUiElement:
    GumUI.LocalizationService.CurrentLanguage = 1;

    var screen = project.Screens.First().ToGraphicalUiElement();
    screen.AddToRoot();

    base.Initialize();
}
```
{% endtab %}
{% endtabs %}

<figure><img src="../.gitbook/assets/image (2).png" alt=""><figcaption><p>Screen with localization</p></figcaption></figure>

## Localization in a Code-Only Project

Code-only projects can use the `LocalizationManager` to enable localization. The steps for localization are:

1. Create a localization CSV or RESX files
2. Add these files to your project in such a way as to obtain a stream to them
3. Call the appropriate method for loading these files
4. Set the language index
5. Assign Text to a string ID

### Code Example: Loading from CSV

This example uses a CSV file with the following contents:

```csv
String ID,English,Spanish
T_OK,OK,OK
T_Cancel,Cancel,Cancelar
T_Submit,Submit,Entregar
T_Greeting,"Welcome, this is a localized project example","Bienvenido, este es un ejemplo de proyecto localizado."

```

```csharp
// Initialize
using Gum.Localization; // for extension methods
// ...
protected override void Initialize()
{
    //var project = GumUI.Initialize(this, "GumProject/GumProject.gumx");
    GumUI.Initialize(this);

    var localizationService = GumUI.LocalizationService;

    // obtain a stream to your localization data
    using var stream = System.IO.File.OpenRead("Content/LocalizationDB.csv");
    localizationService.AddCsvDatabase(stream);

    // set the language index before creating your UI:
    localizationService.CurrentLanguage = 2;

    // create UI using the string IDs:
    StackPanel panel = new();
    panel.AddToRoot();
    panel.Anchor(Anchor.Center);

    Label label = new();
    panel.AddChild(label);
    label.Text = "T_Greeting";

    Button okButton = new();
    panel.AddChild(okButton);
    okButton.Text = "T_OK";

    Button cancelButton = new();
    panel.AddChild(cancelButton);
    cancelButton.Text = "T_Cancel";
}
```

<figure><img src="../.gitbook/assets/06_06 21 28.png" alt=""><figcaption><p>Label and Buttons displaying localized UI</p></figcaption></figure>

## Forms Control Localization

Forms controls localize text automatically when a `LocalizationService` is active. When you assign a string ID to a control's `Text` property (or `Header` for MenuItem, `Placeholder` for TextBox), Gum translates it at assignment time. Each control that supports localization also provides a no-translate method for setting literal text that should not be translated.

### Localization by Control

The following table shows how each Forms control handles localization:

| Control      | Localized Property | No-Translate Method           | Notes                                                                           |
| ------------ | ------------------ | ----------------------------- | ------------------------------------------------------------------------------- |
| Button       | `Text`             | `SetTextNoTranslate()`        |                                                                                 |
| Label        | `Text`             | `SetTextNoTranslate()`        |                                                                                 |
| CheckBox     | `Text`             | `SetTextNoTranslate()`        |                                                                                 |
| RadioButton  | `Text`             | `SetTextNoTranslate()`        |                                                                                 |
| TextBox      | `Text`             | `SetTextNoTranslate()`        | Setting `Text` in code localizes. User-typed text does not localize. See below. |
| TextBoxBase  | `Placeholder`      | `SetPlaceholderNoTranslate()` | Placeholder text localizes when set in code.                                    |
| MenuItem     | `Header`           | `SetHeaderNoTranslate()`      |                                                                                 |
| PasswordBox  | —                  | —                             | Mask characters are never localized.                                            |
| ComboBox     | —                  | —                             | Text comes from selected item. Pre-translate items before adding.               |
| ListBoxItem  | —                  | —                             | Text comes from data items. Pre-translate items before adding.                  |
| ScrollBar    | —                  | —                             | No text property.                                                               |
| Slider       | —                  | —                             | No text property.                                                               |
| ToggleButton | —                  | —                             | No text property.                                                               |

### TextBox Localization Behavior

TextBox has special behavior because it handles both programmatic text and user-typed input:

* Setting `Text` in code applies localization — use this for initial values that should be translated.
* Text entered by the user through typing, pasting, or deleting is never localized. TextBox internally uses `SetTextNoTranslate` for all user-initiated edits.
* The `Placeholder` property (from TextBoxBase) is localized when set in code.

For example, if you set a TextBox's `Text` to a string ID, it displays the translated text. Once the user begins editing, their input is used as-is without translation.

### Data-Driven Controls

ComboBox and ListBoxItem intentionally bypass localization. Their displayed text comes from data objects (via `ToString()`), so translating would attempt to look up the data value as a string ID.

To localize items in a ComboBox or ListBox, translate the values before adding them to the `Items` collection:

```csharp
// Initialize
var comboBox = new ComboBox();
comboBox.AddToRoot();

var localizationService = GumUI.LocalizationService;

comboBox.Items.Add(localizationService.Translate("T_Option1"));
comboBox.Items.Add(localizationService.Translate("T_Option2"));
comboBox.Items.Add(localizationService.Translate("T_Option3"));
```

### Using SetTextNoTranslate

Every localization-aware control provides a method for setting text without translation. This is useful when displaying dynamic values that should not be treated as string IDs:

```csharp
// Initialize
var label = new Label();
label.AddToRoot();

// This localizes — "T_Score" is looked up in the localization database
label.Text = "T_Score";

// This does NOT localize — the literal string is displayed as-is
label.SetTextNoTranslate("1,250 pts");
```

{% hint style="info" %}
`SetTextNoTranslate` is a method rather than a property because the underlying text component only stores the final string. A `TextNoTranslate` property getter would be misleading since there is no way to distinguish translated from untranslated text after assignment.
{% endhint %}
