# Project Files

## Introduction

A Gum project consists of several files and folders created and managed by the Gum tool. This page describes each file type, what it contains, and whether it should be included in version control.

## Folder Structure

A typical Gum project has the following layout:

```
MyProject.gumj
.gumfcs
ProjectCodeSettings.codsj
TextureCoordinateSettings.tcsj
MyProject.user.setj
Screens/
  MainMenu.gusj
  MainMenuAnimations.ganj
Components/
  Controls/
    Button.gucj
    Button.codsj
    ButtonAnimations.ganj
Standards/
  Text.gutj
  Sprite.gutj
Behaviors/
  ButtonBehavior.behj
FontCache/
  Font12Arial.fnt
  Font12Arial.bmfc
  Font12Arial_0.png
EventExport/
  gum_events.json
```

The layout above uses the JSON format (`.gumj`), which new projects use. Projects created in older versions of Gum may use the XML format (`.gumx`) instead. See [JSON and XML Formats](#json-and-xml-formats) below. Not all files and folders are present in every project. For example, `EventExport/` is only created once a change is made in the Gum tool.

## Project File (.gumj/.gumx)

The `.gumj` file is the main project file opened by the Gum tool. It is a JSON file containing project-wide settings such as canvas size, font ranges, and display options. It also contains references to all screens, components, and standard elements in the project. Older projects use an XML project file with the `.gumx` extension, which holds the same data.

For details on project settings, see the [Project Settings](../project-properties.md) page.

## Element Files

Element files define the screens, components, standard elements, behaviors, and animations that make up your project. New projects save element files in JSON format (see [JSON and XML Formats](#json-and-xml-formats) below for the XML counterparts). Either way, element files store paths relative to the project and contain no machine-specific data. These files should always be committed to version control.

### Screen Files (.gusj/.gusx)

Located in the `Screens/` folder. Each screen in your project is saved as a separate `.gusj` file containing instances and their property values organized by state.

### Component Files (.gucj/.gucx)

Located in the `Components/` folder. Each component is saved as a `.gucj` file with the same structure as screen files. Components can be organized in subfolders within `Components/`.

### Standard Element Files (.gutj/.gutx)

Located in the `Standards/` folder. These define default property values for built-in element types such as Text, Sprite, Container, ColoredRectangle, and others.

### Behavior Files (.behj/.behx)

Located in the `Behaviors/` folder. Behaviors define required state categories for components. For example, `ButtonBehavior.behj` requires that a component have Enabled, Disabled, Highlighted, and Pushed states. For more information, see the [Behaviors](../gum-elements/behaviors/README.md) page.

### Animation Files (.ganj/.ganx)

Animation files are saved alongside their parent element. For example, a component named `Button` would have its animations stored in `ButtonAnimations.ganj` in the same folder as `Button.gucj`. These files store animation sequences that reference states by name. For more information, see the [Animation Tutorials](../tutorials-and-examples/animation-tutorials/README.md).

## JSON and XML Formats

Every project and element file type has a JSON form and an XML form:

| Content | JSON | XML |
| --- | --- | --- |
| Project | `.gumj` | `.gumx` |
| Screen | `.gusj` | `.gusx` |
| Component | `.gucj` | `.gucx` |
| Standard element | `.gutj` | `.gutx` |
| Behavior | `.behj` | `.behx` |
| Animation | `.ganj` | `.ganx` |

JSON is the recommended format, and new projects use it. It also works with Native AOT, which `XmlSerializer` does not. The Gum tool picks the format from the file's extension.

To move an XML project to JSON, conversion is explicit and non-destructive. Nothing converts your project automatically, and converting never deletes or modifies the original XML:

* In the Gum tool, use **Content → Convert to JSON…**.
* From the command line, run `gumcli convert-to-json <project.gumx>`.

Both write a `.gumj` sibling of your `.gumx`, plus a JSON sibling for every Screen/Component/StandardElement/Behavior/animation file the project references, in the same folder as the existing XML file.

## Settings Files

### ProjectCodeSettings.codsj

A JSON file storing project-wide code generation configuration including the output library, root namespace, using statements, and generation behavior. These settings are configured through the [Code Tab](../code-tab/README.md). This file should be committed to version control so all team members share the same code generation settings.

Individual elements may also have their own `<ElementName>.codsj` files saved alongside the element file (e.g., `Button.codsj` next to `Button.gucj`). These store per-element code generation settings such as namespace overrides and generation behavior.

### TextureCoordinateSettings.tcsj

A JSON file storing texture coordinate editor preferences such as whether snap-to-grid is enabled and the grid size. This is a project-level setting that can be shared across the team, so it is safe to commit to version control.

### \<ProjectName>.user.setj

A JSON file storing per-user UI state such as which tree nodes are expanded in the project tree. The filename is derived from your project filename (e.g., `GumProject.gumj` produces `GumProject.user.setj`). This file is automatically created for each user and **should not be committed** to version control. Each user has different expanded nodes, so committing this file causes unnecessary churn.

## Font Character Set File (.gumfcs)

A text file containing the set of Unicode characters to include when generating bitmap fonts. This file is automatically created with default characters (ASCII 32-126 and Latin Supplement 160-255) when the project is saved. The **Use Font Character File (.gumfcs)** option on the **Fonts** page of the [Settings window](../fonts.md) controls whether the Gum tool reads this file to determine font character ranges.

If customized, this file should be committed to version control so all team members generate fonts with the same character set.

## FontCache Folder

The `FontCache/` folder contains auto-generated bitmap font files:

* `.fnt` files contain character positions, sizes, and kerning information
* `.bmfc` files are Bitmap Font Generator configuration files used as input to the font generation tool
* `.png` files are font atlas images containing the rendered characters

These files are generated by the `bmfont.exe` tool bundled with the Gum tool. They are regenerated automatically when font settings (font family, size, style, or character ranges) change. For details on the `.fnt` file format and creating custom fonts, see the [Bitmap font generator (.fnt)](../bitmap-font-generator-.fnt.md) page.

{% hint style="info" %}
FontCache files are typically committed to version control because regenerating them requires the same fonts to be installed on each developer's machine. Teams where all members have matching font installations can optionally exclude this folder.
{% endhint %}

## EventExport Folder

The `EventExport/` folder contains `gum_events.json`, an event log that tracks changes made in the Gum tool:

* Element additions, deletions, and renames (screens, components, standard elements)
* Instance additions, deletions, and renames
* State and state category renames

This is an append-only log. Deleting a screen adds a "deleted" event rather than removing earlier entries. Events are grouped by a hashed username for privacy and automatically expire after 14 days when the project is loaded.

This file is transient and user-specific. It **should not be committed** to version control.

## Missing Source Files (GUM0004)

If the Gum tool opens a project whose project file references an element (screen, component, or standard element) but the matching element file is not found on disk, the element still appears in the tree with a red "!" indicator. This can happen when an element file is deleted, renamed, or moved outside the Gum tool — for example by a version-control operation that removes the file while the project is open.

When this happens the Gum tool reports a **GUM0004** error in the [Errors tab](../editor-tab.md), naming the element and the path where its file was expected (for example `Components/Button.gucj`). The element itself is still present in memory, so the project keeps working. To resolve the error, either:

* **Restore the missing file** — undo the deletion or move it back to the expected location, then reload the project.
* **Re-save the element** — saving the element (for example by editing it) recreates the file from the in-memory copy.

GUM0004 is a listed error only; it does not block saving the project.

## Missing Referenced External File (GUM0006)

An instance or element variable that references an external file, such as a Sprite's `Source File` (including `.achx`/`.achj` animation chains), or a `Custom Font File`, can point at a path that does not exist on disk, for example after a typo, a rename, or a moved file.

When this happens the Gum tool reports a **GUM0006** warning in the [Errors tab](../editor-tab.md), naming the instance and the path it referenced. To resolve it, either point the variable at the correct file or restore the missing file to the expected location.

## Invalid Enum Variable Value (GUM0007)

Gum saves variables that pick from a fixed list of options, such as `Width Units`, `X Units`, and `Children Layout`, as plain numbers rather than names. A file that has been hand edited, merged badly, or written by a tool using a different set of options can end up holding a number that matches none of the options.

When this happens the Gum tool reports a **GUM0007** error in the [Errors tab](../editor-tab.md), naming the element, the state, the variable, and the number it found. To resolve it, select the element and pick a valid option for that variable in the **Variables** tab.

Gum treats an unrecognized `Width Units`, `Height Units`, `X Units`, or `Y Units` value as `Absolute`, so the element still renders and the rest of the project keeps working. Other variables holding an unrecognized number may behave unpredictably until you correct them.

{% hint style="info" %}
Available in October 2026, or now if building Gum from source.
{% endhint %}

## File Name Case Mismatch (GUM0008)

A project can reference a file whose name differs from the file on disk only by letter case: a Sprite's `SourceFile` set to `Textures/Hero.png` when the file is saved as `Textures/hero.png`, or a component named `Button` whose file is `Components/button.gucx`. Windows ignores the difference, so the project works there, but on a case-sensitive file system such as Linux the file is not found.

When this happens the Gum tool reports a **GUM0008** entry in the [Errors tab](../editor-tab.md), naming what referenced the file, the name it used, and the name on disk. It is a warning on a file system where the file still loads and an error where it does not. To resolve it, rename the file to match the reference, or change the reference to match the file.

`gumcli check` reports GUM0008 for every file the project loads at runtime. Besides element files and the files elements reference, this covers behavior files, localization files, element animation files, and the textures that a `.fnt` or `.achx` file names. These belong to the whole project rather than one element, so the Errors tab does not list them. A mismatched animation file is always an error, because the game never connects its animations to the element, on any file system.

## Font Range Entry That Matches No Font (GUM0010)

A project file can hold a character range for one specific font, in the `FontRangeEntries` list next to the project-wide `FontRanges` value. Each entry names a font in its `FontFamily` and the characters to include in its `Ranges`. For a font file, `FontFamily` is the project-relative path exactly as a Text names it. For an installed font, it is the family name.

```json
"FontRangeEntries": [
  { "FontFamily": "Fonts/Icons.ttf", "Ranges": "57344-57400" }
]
```

An entry applies only when a Text uses the font it names. A font file matches by its whole path, so `Fonts/Icon.ttf` does not match a Text that uses `Fonts/Icons.ttf`, and an absolute path does not match a relative one. The direction of the slashes does not matter. An installed font matches by family name, ignoring letter case.

When no Text uses the font an entry names, the Gum tool reports a **GUM0010** warning in the [Errors tab](../editor-tab.md), naming the entry. The entry has no effect until it matches a font, so a typo looks the same as the setting doing nothing. To resolve it, correct the `FontFamily` to match the name or path the Text uses, or remove the entry. The [Fonts](../fonts.md#ranges-for-one-font) page edits these entries and shows the same warning on the row. `gumcli check` reports GUM0010 too.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

## Invalid Font Range (GUM0011)

A font range is a comma separated list of characters and ranges, such as `32-126,160-255`. Each item is either a single character code or a `start-end` pair, written as numbers with no spaces. A value like `50-40`, where the end comes before the start, `abc`, or `32-126, 160-255` with a space is not valid.

When a range is not valid, the font is still created but without the characters you asked for. The BMFont font generator uses the default range instead, and the KernSmith font generator skips each part it cannot read, which can leave the font with no characters from that entry. The Gum tool reports a **GUM0011** warning in the [Errors tab](../editor-tab.md), naming the font and the range it found. The project-wide range is checked as well as each font's own entry. To resolve it, correct the range on the [Fonts](../fonts.md#character-ranges) page or in the project file. `gumcli check` reports GUM0011 too.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

## Text Outside Its Font Range (GUM0012)

A font only has the characters in its ranges. When a Text's value, in any state, has a character its font's ranges leave out, the Gum tool reports a **GUM0012** warning in the [Errors tab](../editor-tab.md), naming the Text and the character codes. At runtime the character draws as `?`. To resolve it, add the codes to the font's range on the [Fonts](../fonts.md#character-ranges) page. Line breaks and tabs are not checked, and neither is a Text that uses a pre-built `.fnt` file. `gumcli check` reports GUM0012 too.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

## Value That Does Not Fit Its Type (GUM0019)

A variable saved as `float` or `int` must hold a number, and a `bool` variable must hold `true` or `false`. A hand-written file can save `Width` as a `float` holding `wide`. Nothing loads that as a number, so the variable does not do what the file says. `gumcli check` reports **GUM0019** as an error, naming the variable, the state, and the value it found. To resolve it, set the variable to a number (or `true`/`false`).

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

## Duplicate Names (GUM0020, GUM0021, GUM0022)

Names inside one element must be unique, because Gum finds an item by name and takes one of the matches. `gumcli check` reports an error when an element has:

* two instances with the same name (**GUM0020**)
* two states with the same name, in the element or within one category, or two categories with the same name (**GUM0021**)
* the same variable twice in one state (**GUM0022**)

To resolve it, rename or delete all but one of them.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

## Element Referenced Twice (GUM0023)

The project file lists each Screen, Component, Standard Element, and Behavior once. When the same name is listed twice, the file loads as two items with one name. `gumcli check` reports **GUM0023** as an error, and `gumcli codegen` skips that element until the extra reference is deleted from the `.gumx` file.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

## Parents That Form a Loop (GUM0024)

Following the `Parent` of one instance to the next must end at an instance with no parent. When instance A has B as its parent and B has A (or an instance is its own parent), none of them can be placed. `gumcli check` reports **GUM0024** as an error naming the loop, for example `A -> B -> A`. To resolve it, clear or change the `Parent` of one instance in the loop.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

## Screen Used as a Type (GUM0025)

A Screen is a whole page and cannot be used inside another element. `gumcli check` reports **GUM0025** as an error when an instance's type is a Screen, or when a component's `Base Type` is a Screen. The Gum tool does not allow either; a hand-written file can. To resolve it, use a Component instead.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

## State the Category Does Not Have (GUM0026)

A variable that selects a state, such as `Switch.ModeState`, must name a state that exists in that category (or in the instance type's uncategorized states for `State`). When it names a state that is not there, such as `Nope` in a `Mode` category that has only `On` and `Off`, the selection does nothing and generated code does not compile. `gumcli check` reports **GUM0026** as an error listing the states that do exist. To resolve it, correct the name or add the state to the category.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

## Variable of an Instance That Does Not Exist (GUM0027)

A variable such as `Ghost.Text` sets a property on the instance `Ghost`, so the element, or one of its base elements, must contain an instance with that name. When the file holds such a variable for an instance that does not exist, the value is ignored. `gumcli check` reports **GUM0027** as a warning and does not block code generation. The Gum tool can leave these variables behind, for example after an instance is deleted, and does not show them, so its Errors tab does not list GUM0027. If you edit the file by hand, delete the variable or add the instance.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

## Variable With an Unknown Type (GUM0028)

Each variable stores the name of its type, such as `float`, `HorizontalAlignment`, or the name of a state category. When that name is none of those, for example `Blorp`, the value cannot be converted and generated code that uses it does not compile. `gumcli check` reports **GUM0028** as a warning. To resolve it, correct the type name in the element file.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

## Version Control (.gitignore)

The following `.gitignore` entries are recommended for Gum projects:

```
# Gum - user-specific settings (expanded tree nodes)
*.user.setj

# Gum - transient event log
**/EventExport/gum_events.json
```

{% hint style="info" %}
If all team members have the same fonts installed, you can optionally also exclude the FontCache folder since it can be regenerated by the Gum tool.
{% endhint %}
