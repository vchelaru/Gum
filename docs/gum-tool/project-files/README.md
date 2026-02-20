# Project Files

## Introduction

A Gum project consists of several files and folders created and managed by the Gum tool. This page describes each file type, what it contains, and whether it should be included in version control.

## Folder Structure

A typical Gum project has the following layout:

```
MyProject.gumx
.gumfcs
ProjectCodeSettings.codsj
TextureCoordinateSettings.tcsj
MyProject.user.setj
Screens/
  MainMenu.gusx
  MainMenuAnimations.ganx
Components/
  Controls/
    Button.gucx
    Button.codsj
    ButtonAnimations.ganx
Standards/
  Text.gutx
  Sprite.gutx
Behaviors/
  ButtonBehavior.behx
FontCache/
  Font12Arial.fnt
  Font12Arial.bmfc
  Font12Arial_0.png
EventExport/
  gum_events.json
```

Not all files and folders are present in every project. For example, `EventExport/` is only created once a change is made in the Gum tool.

## Project File (.gumx)

The `.gumx` file is the main project file opened by the Gum tool. It is an XML file containing project-wide settings such as canvas size, font ranges, and display options. It also contains references to all screens, components, and standard elements in the project.

For details on project settings, see the [Project Properties](../project-properties.md) page.

## Element Files

Element files define the screens, components, standard elements, behaviors, and animations that make up your project. All element files use XML format, store paths relative to the project, and contain no machine-specific data. These files should always be committed to version control.

### Screen Files (.gusx)

Located in the `Screens/` folder. Each screen in your project is saved as a separate `.gusx` file containing instances and their property values organized by state.

### Component Files (.gucx)

Located in the `Components/` folder. Each component is saved as a `.gucx` file with the same structure as screen files. Components can be organized in subfolders within `Components/`.

### Standard Element Files (.gutx)

Located in the `Standards/` folder. These define default property values for built-in element types such as Text, Sprite, Container, ColoredRectangle, and others.

### Behavior Files (.behx)

Located in the `Behaviors/` folder. Behaviors define required state categories for components. For example, `ButtonBehavior.behx` requires that a component have Enabled, Disabled, Highlighted, and Pushed states. For more information, see the [Behaviors](../gum-elements/behaviors/README.md) page.

### Animation Files (.ganx)

Animation files are saved alongside their parent element. For example, a component named `Button` would have its animations stored in `ButtonAnimations.ganx` in the same folder as `Button.gucx`. These files store animation sequences that reference states by name. For more information, see the [Animation Tutorials](../tutorials-and-examples/animation-tutorials/README.md).

## Settings Files

### ProjectCodeSettings.codsj

A JSON file storing project-wide code generation configuration including the output library, root namespace, using statements, and generation behavior. These settings are configured through the [Code Tab](../code-tab/README.md). This file should be committed to version control so all team members share the same code generation settings.

Individual elements may also have their own `<ElementName>.codsj` files saved alongside the element file (e.g., `Button.codsj` next to `Button.gucx`). These store per-element code generation settings such as namespace overrides and generation behavior.

### TextureCoordinateSettings.tcsj

A JSON file storing texture coordinate editor preferences such as whether snap-to-grid is enabled and the grid size. This is a project-level setting that can be shared across the team, so it is safe to commit to version control.

### \<ProjectName>.user.setj

A JSON file storing per-user UI state such as which tree nodes are expanded in the project tree. The filename is derived from your `.gumx` filename (e.g., `GumProject.gumx` produces `GumProject.user.setj`). This file is automatically created for each user and **should not be committed** to version control. Each user has different expanded nodes, so committing this file causes unnecessary churn.

## Font Character Set File (.gumfcs)

A text file containing the set of Unicode characters to include when generating bitmap fonts. This file is automatically created with default characters (ASCII 32-126 and Latin Supplement 160-255) when the project is saved. The **Use Font Character File (.gumfcs)** option in [Project Properties](../project-properties.md) controls whether the Gum tool reads this file to determine font character ranges.

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
