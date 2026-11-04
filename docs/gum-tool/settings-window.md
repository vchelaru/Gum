# Settings Window

## Introduction

The Settings window holds every setting of the Gum tool in one place. To open it, select **Edit** -> **Settings...**.

<figure><img src="../.gitbook/assets/25_08 00 01.png" alt=""><figcaption><p>Settings window showing the Application > General page</p></figcaption></figure>

The list on the left has two sections:

* **Application** pages apply to this machine, whatever project is open: **General**, **Appearance**, **Keyboard shortcuts**, and **Plugins**.
* **Project** pages apply to the loaded project and are saved in its project file: **General**, **Canvas**, **Fonts**, **Localization**, and **Code generation**. The window lists only the Application pages while no project is loaded.

The **Search** box above the list narrows the list to the pages that match the text you type. On the selected page, the settings whose name or description match are highlighted; nothing is hidden. Highlighting is available on Canvas, Code generation, and the other pages built from rows of settings, and is available in December 2026, or now if building Gum from source.

{% hint style="info" %}
The Settings window is available in November 2026, or now if building Gum from source. Before then, these settings were spread across the Project Properties, Font Settings, Hotkeys, and Code tabs, and the Theming and Manage Plugins dialogs.
{% endhint %}

## Application Pages

### General

<figure><img src="../.gitbook/assets/25_08 00 01.png" alt=""><figcaption><p>Application > General page</p></figcaption></figure>

The page has three sections. **Saving** holds **Auto save**, which saves the project whenever something changes. **Project panel** holds **Standards palette**, which shows the standard types as a palette at the bottom of the Project tab instead of as a **Standard** folder. **Settings files** holds **Settings folder**, which shows where Gum keeps its application settings; click **Open folder** under it to open that folder.

{% hint style="info" %}
The grouped layout, hints under each setting, and highlighting of search matches on this page are available in December 2026, or now if building Gum from source. Before then the page was a plain list with an **Open settings folder** button.
{% endhint %}

### Appearance

<figure><img src="../.gitbook/assets/25_08 00 02.png" alt=""><figcaption><p>Application > Appearance page</p></figcaption></figure>

The page has three sections. **Theme** holds **Theme mode** (**System**, **Light**, or **Dark**) and **Accent color**. **Editor background** holds **Editor background** and **Checker color**. **Outlines and guides** holds **Outline color**, **Guide line color**, and **Guide text color**.

Each color row has a swatch that opens a color picker and a field for typing a hex value. A color you have changed also shows a reset button at the end of its row, which returns just that color to its default. **Reset All** below the rows returns every changed color to its default, and is disabled while none differ.

{% hint style="info" %}
The grouped layout, hints under each setting, and highlighting of search matches on this page are available in December 2026, or now if building Gum from source. Before then the page had a single **Reset All** button and no per-color reset.
{% endhint %}

### Keyboard shortcuts

<figure><img src="../.gitbook/assets/25_08 00 03.png" alt=""><figcaption><p>Application > Keyboard shortcuts page</p></figcaption></figure>

This page lists every shortcut and its key combination, such as **Duplicate: Ctrl+D** and **Undo: Ctrl+Z**. Pressing **Ctrl+/** opens the Settings window on this page. The page lists the shortcuts but does not change them.

The shortcuts are grouped by area (**Editing**, **Navigation**, **Canvas keys**, **Nudge**, and **Camera**), one line per command with its keys in a chip beside it. Searching matches the command name or its keys, so searching for `Ctrl+Z` highlights **Undo**.

{% hint style="info" %}
The grouped one-line layout and highlighting of search matches on this page are available in December 2026, or now if building Gum from source. Before then the page was a plain list of lines such as **Undo: Ctrl+Z**.
{% endhint %}

### Plugins

<figure><img src="../.gitbook/assets/25_08 00 04.png" alt=""><figcaption><p>Application > Plugins page</p></figcaption></figure>

The page has three sections. **Installed plugins** has one line per plugin you can turn off, with a checkbox to turn it on or off. A chip beside the checkbox names its state: **Built-in** for a plugin that is part of Gum, **Failed** for one that crashed and was turned off (turning it on asks first), and **Not loaded** for a plugin found in the `Plugins` folder that this version of Gum cannot run. A failed or not-loaded plugin also shows the reason under its line.

**Gum internals** is collapsed. It lists the plugins Gum needs to run, marked **Required**; their checkboxes cannot be changed. Searching for one of them opens the section.

**Folder scan** shows the `Plugins` folder Gum looked in and, under it, each file that holds plugins and whether it loaded. Click **Copy scan** to copy the full scan result so you can paste it into a bug report. To write your own plugin, see [Plugins](plugins/README.md).

{% hint style="info" %}
The row layout, state chips, and highlighting of search matches on this page are available in December 2026, or now if building Gum from source. Before then the page had a **Plugins** tab with a plain checkbox list and a **Folder scan** tab with the scan as text.
{% endhint %}

## Project Pages

* **General**, **Canvas**, and **Localization**: see [Project Settings](project-properties.md).
* **Fonts**: see [Fonts](fonts.md).
* **Code generation**: see the [Code tab](code-tab/README.md) section.
