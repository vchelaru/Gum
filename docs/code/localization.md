# Localization

## Introduction

Gum supports localization using CSV and RESX files. Localization can be performed automatically by linking a localization file in your Gum project, or it can be done by hand in code-only projects. This document explains how to use the `LocalizationManager` to perform localization.

## Localization in Gum Projects (Using the Gum UI Tool)

If you are using the Gum UI Tool to create your project, you can add and test localization in the tool itself. For information on how to set up localization in the Gum UI tool, see the [Localization page](../gum-tool/localization.md).

Once you have a project set up with localization, the only code change needed is to specify the language index. In the Gum UI tool, you select a language by name from a dropdown. At runtime in code, you set `CurrentLanguage` as an integer index: index 0 is the string ID column, index 1 is the first language column, index 2 is the second, and so on. If `CurrentLanguage` is left at its default value of 0, your game will display the raw string IDs.

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

<figure><img src="../.gitbook/assets/image (1) (1).png" alt=""><figcaption><p>Screen with localization</p></figcaption></figure>

## Localization and Font Ranges

Localized games may need an extended font range. If using the Gum tool, see the [Project Properties](../gum-tool/project-properties.md#font-ranges) page for information on font ranges.

## Localization in a Code-Only Project

Code-only projects can use the `LocalizationManager` to enable localization. The steps for localization are:

1. Create a localization CSV or RESX files
2. Add these files to your project in such a way as to obtain a stream to them
3. Call the appropriate method for loading these files
4. Set the language index
5. Assign Text to a string ID

{% hint style="info" %}
How you open the stream depends on your platform. XNA-like platforms (MonoGame, KNI, FNA) bundle content files into the app package and require `TitleContainer.OpenStream` to read them — this is the only approach that works on iOS, Android, consoles, and web. Non-XNA platforms (raylib, SkiaSharp, and plain desktop apps) can read content directly from the filesystem with `File.OpenRead`. The examples below show both.
{% endhint %}

### Code Example: Loading from CSV

This example uses a CSV file with the following contents:

```csv
String ID,English,Spanish
T_OK,OK,OK
T_Cancel,Cancel,Cancelar
T_Submit,Submit,Entregar
T_Greeting,"Welcome, this is a localized project example","Bienvenido, este es un ejemplo de proyecto localizado."

```

{% tabs %}
{% tab title="XNA-like (MonoGame/KNI/FNA)" %}
```csharp
// Initialize
using Gum.Localization; // for extension methods
using Microsoft.Xna.Framework; // for TitleContainer
// ...
protected override void Initialize()
{
    //var project = GumUI.Initialize(this, "GumProject/GumProject.gumx");
    GumUI.Initialize(this);

    var localizationService = GumUI.LocalizationService;

    // TitleContainer opens content bundled into the app package.
    // This is required on iOS, Android, consoles, and web.
    using var stream = TitleContainer.OpenStream("Content/LocalizationDB.csv");
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
{% endtab %}

{% tab title="Non-XNA (raylib, SkiaSharp, etc.)" %}
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
{% endtab %}
{% endtabs %}

<figure><img src="../.gitbook/assets/06_06 21 28.png" alt=""><figcaption><p>Label and Buttons displaying localized UI</p></figcaption></figure>

### Code Example: Loading from RESX

RESX files are the standard .NET resource format and integrate well with tools like Visual Studio's resource editor. Gum loads a base RESX file and automatically discovers satellite files (named by culture code) alongside it.

This example uses a base file `Strings.resx` with English entries:

```xml
<root>
  <data name="T_OK"><value>OK</value></data>
  <data name="T_Cancel"><value>Cancel</value></data>
  <data name="T_Greeting"><value>Welcome, this is a localized project example</value></data>
</root>
```

A satellite `Strings.es.resx` sits next to it with the Spanish translations (same keys, translated values).

{% tabs %}
{% tab title="XNA-like (MonoGame/KNI/FNA)" %}
The path-based overload uses `Directory.GetFiles` to auto-discover satellites, which does not work with bundled content. Open the base file and each satellite as streams via `TitleContainer` and pass them as `(languageName, stream)` pairs:

```csharp
// Initialize
using Gum.Localization; // for extension methods
using Microsoft.Xna.Framework; // for TitleContainer
// ...
protected override void Initialize()
{
    GumUI.Initialize(this);

    var localizationService = GumUI.LocalizationService;

    var streams = new (string languageName, System.IO.Stream stream)[]
    {
        ("Default", TitleContainer.OpenStream("Content/Strings.resx")),
        ("es", TitleContainer.OpenStream("Content/Strings.es.resx")),
    };

    localizationService.AddResxDatabase(streams);

    // index 0 is the string ID, index 1 is "Default" (base file),
    // index 2+ are satellites in the order they are passed
    localizationService.CurrentLanguage = 2; // Spanish

    Label label = new();
    label.AddToRoot();
    label.Text = "T_Greeting";
}
```
{% endtab %}

{% tab title="Non-XNA (raylib, SkiaSharp, etc.)" %}
```csharp
// Initialize
using Gum.Localization; // for extension methods
// ...
protected override void Initialize()
{
    GumUI.Initialize(this);

    var localizationService = GumUI.LocalizationService;

    // pass the path to the base .resx file; satellites are auto-discovered
    localizationService.AddResxDatabase("Content/Strings.resx");

    // index 0 is the string ID, index 1 is "Default" (base file),
    // index 2+ are satellites in the order they are discovered
    localizationService.CurrentLanguage = 2; // Spanish

    Label label = new();
    label.AddToRoot();
    label.Text = "T_Greeting";
}
```
{% endtab %}
{% endtabs %}

{% hint style="info" %}
Satellite discovery uses the file naming convention `BaseName.<culture>.resx` (for example `Strings.es.resx`, `Strings.fr.resx`). The base file is labeled `"Default"`; each satellite is labeled with its culture code.
{% endhint %}

### Multiple RESX Files

Larger projects often split localized strings across several base files — for example, one per feature area. This is the layout used by tools like [ResXResourceManager](https://github.com/dotnet/ResXResourceManager), where strings are organized into files such as `Strings.resx`, `Buttons.resx`, and `Errors.resx`, each with its own satellites:

```
Content/
  Strings.resx
  Strings.es.resx
  Buttons.resx
  Buttons.es.resx
  Errors.resx
  Errors.es.resx
```

Pass the collection of base files (or groups of streams) to `AddResxDatabase`. Gum merges keys across all files into a single database:

{% tabs %}
{% tab title="XNA-like (MonoGame/KNI/FNA)" %}
Build one group per base file. Each group is a collection of `(languageName, stream)` pairs and may optionally be named — the group name appears in collision-warning messages:

```csharp
// Initialize
using Microsoft.Xna.Framework; // for TitleContainer
// ...
var localizationService = GumUI.LocalizationService;

var groups = new (string? groupName, IEnumerable<(string, System.IO.Stream)>)[]
{
    ("Strings", new[]
    {
        ("Default", TitleContainer.OpenStream("Content/Strings.resx")),
        ("es",      TitleContainer.OpenStream("Content/Strings.es.resx")),
    }),
    ("Buttons", new[]
    {
        ("Default", TitleContainer.OpenStream("Content/Buttons.resx")),
        ("es",      TitleContainer.OpenStream("Content/Buttons.es.resx")),
    }),
    ("Errors", new[]
    {
        ("Default", TitleContainer.OpenStream("Content/Errors.resx")),
        ("es",      TitleContainer.OpenStream("Content/Errors.es.resx")),
    }),
};

localizationService.AddResxDatabase(
    groups,
    onWarning: message => System.Diagnostics.Trace.WriteLine(message));

localizationService.CurrentLanguage = 2;
```
{% endtab %}

{% tab title="Non-XNA (raylib, SkiaSharp, etc.)" %}
```csharp
// Initialize
var localizationService = GumUI.LocalizationService;

var baseFiles = new[]
{
    "Content/Strings.resx",
    "Content/Buttons.resx",
    "Content/Errors.resx",
};

localizationService.AddResxDatabase(
    baseFiles,
    onWarning: message => System.Diagnostics.Trace.WriteLine(message));

localizationService.CurrentLanguage = 2;
```
{% endtab %}
{% endtabs %}

{% hint style="warning" %}
If the same key appears in more than one base file, the **last write wins**. When an `onWarning` callback is provided, Gum invokes it once per colliding key with a message listing every source file that defined the key.
{% endhint %}

The set of languages is the union of cultures across all base files. If `Strings.resx` has an `es` satellite but `Buttons.resx` does not, keys from `Buttons.resx` fall back to their string ID when `es` is selected.

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
