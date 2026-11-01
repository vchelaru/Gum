# Creating a Project

Select **File** > **New Project** to open the New Project dialog. Gum also shows it when it starts with no project to reopen. The dialog creates a Gum project, and can also create a C# game project that already contains it.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

<figure><img src="../../.gitbook/assets/01_12 20 01.png" alt=""><figcaption><p>The New Project dialog</p></figcaption></figure>

Hover the info icons in the dialog for a short explanation of each option.

## Gum project only

For a Gum project without a game project:

1. Enter a **Project name**.
2. Pick the **Location**, either by typing a folder or by clicking **Browse...**. Gum creates a new folder named after the project inside it. If the location folder already has the project's name, the project goes directly in that folder. The line under **Location** shows the exact folder.
3. Choose a **File format**. **.gumj (JSON)** is recommended; **.gumx (XML)** also works. See [JSON and XML Formats](../project-files/README.md#json-and-xml-formats) for the difference.
4. Click **OK**. Gum creates the project and opens it.

The name must work as a file name on every operating system, so Gum refuses characters such as `<` and `:`, a trailing dot or space, and Windows device names such as `CON`. The reason appears under the name box.

<figure><img src="../../.gitbook/assets/01_12 20 05.png" alt=""><figcaption><p>A name with an invalid character</p></figcaption></figure>

### Forms controls

**Include Forms controls** is checked by default and adds the standard controls (buttons, text boxes, list boxes, and others) to the project. Uncheck it for a project that only has the standard elements.

With Forms controls included, you can also choose:

* **Theme** - how the controls look. The preview beside the dropdown shows the selected theme.
* **Include demo screen** - adds `DemoScreenGum`, a sample screen that uses the controls. You can delete it at any time.

## Including a C# project

Check **Create C# project** to also create a runnable game project with the Gum project inside it, in `Content/GumProject`. Choose the game framework in **Platform**: MonoGame, KNI, KNI with Skia, raylib, Stride (Windows only), or Silk.NET. The project name also names the `.csproj`, so it must be a valid C# name.

<figure><img src="../../.gitbook/assets/01_12 20 02.png" alt=""><figcaption><p>Creating a C# project</p></figcaption></figure>

The options below appear once **Create C# project** is checked:

* **Gum packages** - which Gum packages the project references. **Stable** is the published release. **Preview** uses the newest preview packages, which may be unstable. **Local Gum source** references a Gum source checkout instead of NuGet packages; it is available only when the tool was built from that source code, and is needed for KNI with Skia.
* **Restore NuGet packages** - runs `dotnet restore` after the files are created. The first code generation needs the restored packages.
* **Include recommended packages** - adds packages for shapes, shader compilation, and dynamic fonts, where the platform has them.
* **Generate code** - runs code generation after the restore, creating a C# class for every screen and component in the new project. It is available when the project has Forms controls and the restore is checked. Unchecked, no code is generated and the game loads the demo screen from the Gum project at runtime.

Clicking **OK** runs each step in turn and shows its progress, for example `Step 2 of 4: Running dotnet restore...`. If the restore fails, the dialog offers **Retry**, **Continue without restore**, and **Back**.

### Warnings

If the location is inside a folder that already has a `.csproj`, the dialog shows an **Inside an existing code project** warning. Choose a folder outside the existing project, or uncheck **Create C# project** to add only the Gum project.

<figure><img src="../../.gitbook/assets/01_12 20 04.png" alt=""><figcaption><p>The existing code project warning</p></figcaption></figure>

If the project folder already has files, the dialog warns that files with the same names may be overwritten. If it already holds a project with the same name, the dialog shows **Project already exists** and **OK** is disabled.

## Folders

The **Folders** section lists the folders and the Gum project file that will be created. Click the heading to expand it. **Expand all** and **Collapse all** open and close every folder.

<figure><img src="../../.gitbook/assets/01_12 20 03.png" alt=""><figcaption><p>The Folders section for a C# project without Forms controls</p></figcaption></figure>

## Command line

The **Command line** section shows the [`gumcli new`](../../cli/new.md) command that creates the same project. Click the copy button to copy it, for example to create a project from a script. Some choices have no command line equivalent yet; a note under the command names them.
