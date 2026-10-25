# Settings Window

## Introduction

The Settings window holds every setting of the Gum tool in one place. To open it, select **Edit** -> **Settings...**.

<figure><img src="../.gitbook/assets/25_08 00 01.png" alt=""><figcaption><p>Settings window showing the Application > General page</p></figcaption></figure>

The list on the left has two sections:

* **Application** pages apply to this machine, whatever project is open: **General**, **Appearance**, **Keyboard shortcuts**, and **Plugins**.
* **Project** pages apply to the loaded project and are saved in its project file: **General**, **Canvas**, **Fonts**, **Localization**, and **Code generation**. The window lists only the Application pages while no project is loaded.

The **Search** box above the list narrows the list to the pages that match the text you type.

{% hint style="info" %}
The Settings window is available in November 2026, or now if building Gum from source. Before then, these settings were spread across the Project Properties, Font Settings, Hotkeys, and Code tabs, and the Theming and Manage Plugins dialogs.
{% endhint %}

## Application Pages

### General

<figure><img src="../.gitbook/assets/25_08 00 01.png" alt=""><figcaption><p>Application > General page</p></figcaption></figure>

* **Auto save** saves the project whenever something changes.
* **Standards palette** shows the standard types as a palette at the bottom of the Project tab instead of as a **Standard** folder.
* **Open settings folder** opens the folder where Gum keeps its application settings.

### Appearance

<figure><img src="../.gitbook/assets/25_08 00 02.png" alt=""><figcaption><p>Application > Appearance page</p></figcaption></figure>

**Theme Mode** is **System**, **Light**, or **Dark**. The color pickers below it set the **Accent Color** and the Editor tab colors: **Editor Background**, **Checker Color**, **Outline Color**, **Guide Line Color**, and **Guide Text Color**. **Reset All** returns every color to its default.

### Keyboard shortcuts

<figure><img src="../.gitbook/assets/25_08 00 03.png" alt=""><figcaption><p>Application > Keyboard shortcuts page</p></figcaption></figure>

This page lists every shortcut and its key combination, such as **Duplicate: Ctrl+D** and **Undo: Ctrl+Z**. Pressing **Ctrl+/** opens the Settings window on this page. The page lists the shortcuts but does not change them.

### Plugins

<figure><img src="../.gitbook/assets/25_08 00 04.png" alt=""><figcaption><p>Application > Plugins page</p></figcaption></figure>

The **Plugins** tab lists every plugin Gum loaded, with a checkbox to turn each one on or off. Plugins the tool needs to run have a checkbox that cannot be changed. The **Folder scan** tab lists what Gum found in the `Plugins` folder, including any plugin that could not be loaded and the reason. **Copy Scan** copies that text so you can paste it into a bug report. To write your own, see [Plugins](plugins/README.md).

## Project Pages

* **General**, **Canvas**, and **Localization**: see [Project Settings](project-properties.md).
* **Fonts**: see [Fonts](fonts.md).
* **Code generation**: see the [Code tab](code-tab/README.md) section.
