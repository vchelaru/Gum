# Plugins

## Introduction

This page discusses how to write plugins for Gum. Plugins are a useful way to modify Gum because they allow you to customize Gum without making project-specific, technology-specific, or organization-specific modifications to the core source code. This means that you can customize the Gum experience while still maintaining ties to the core source code.

Gum runs on Avalonia and ships for Windows, macOS, and Linux, so a plugin is a plain .NET class library with no UI framework dependency. Plugins talk to the tool through the services and contracts in the `Gum.Presentation` project: menu entries, tabs, dialogs, and the plugin events.

{% hint style="warning" %}
**Plugins written for the older WPF tool do not load.** Gum used to be a WPF application, and plugins built against it referenced WPF types (`WpfPluginBase`, `AddMenuItem` returning a `MenuItem`, tabs handed over as a WPF `UserControl`). The current tool contains no WPF or Windows Forms, so it skips any plugin assembly that references them and lists it in the **Plugins** dialog as not supported, with the referenced assembly named in the Output tab. See [Migrating a WPF plugin](#migrating-a-wpf-plugin) below.
{% endhint %}

## Setup

To begin writing a plugin:

1. Obtain the Gum source code. You can download the .zip or get the source through a version control client. See [Running from Source](../setup/running-from-source.md).
2. Create a copy of `Gum.slnx`. You will want to work with your own solution file so that the project containing your plugin can be debugged easily. For example, you might want to call your solution `GumWithPlugins.slnx`.
3. Open your new solution file in Visual Studio, Rider, or VS Code.

Now that you have created a solution which will contain your plugin project, you can add this project:

1. Right-click on your solution
2. Select "Add -> New Project..."
3. Select "Class Library" as the type
4. Target `net10.0` (the plain target, not `net10.0-windows`)
5. Enter the name of your project, such as MyPluginProject
6. Click OK

Next you'll need to reference the Gum libraries. Add a project reference to `Tools/Gum.Presentation/Gum.Presentation.csproj`. It brings `GumCommon`, `GumDataTypes`, `ToolsUtilities`, and `Gum.ProjectServices` with it, so no other Gum reference is needed. Your project file should look like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Tools\Gum.Presentation\Gum.Presentation.csproj" />
    <PackageReference Include="System.ComponentModel.Composition" Version="9.0.3" />
  </ItemGroup>
</Project>
```

If you want the compiler to flag Windows-only API calls before you ship, add the `Microsoft.CodeAnalysis.BannedApiAnalyzers` package and the `BannedSymbols.CrossPlatform.txt` file from the Gum repository as an `AdditionalFiles` item, the way `Gum/ConvertToJsonPlugin/ConvertToJsonPlugin.csproj` does.

## Adding a Plugin class

Next you'll want to add a Plugin class. This is a class that inherits from `PluginBase`:

1. Right-click on your project
2. Select "Add -> Class..."
3. Name your class "MyPlugin" or whatever you want your plugin to be called
4. Add the following usings: `using Gum.Plugins.BaseClasses;` and `using System.ComponentModel.Composition;`
5. Modify your plugin so it's public, inherits from `PluginBase`, and is exported for the tool's plugin loader (MEF):

```csharp
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;

[Export(typeof(PluginBase))]
public class MyPlugin : PluginBase
{
    public override string FriendlyName => "My Plugin Name";

    public override Version Version => new Version(1, 0, 0, 0);

    public override void StartUp()
    {
        // Add startup logic here, such as subscribing to events or adding menu entries:
        AddMenuEntry(() => _dialogService.ShowMessage("Hello from My Plugin"), "My Plugin", "Say Hello");
    }

    public override bool ShutDown(Gum.Plugins.PluginShutDownReason shutDownReason)
    {
        return true;
    }
}
```

`PluginBase` gives every plugin the tool's shared services (`_dialogService`, `_fileCommands`, `_guiCommands`, `_tabManager`, and others). A plugin that needs more services declares them as constructor parameters and marks the constructor with `[ImportingConstructor]`; the in-repo `ConvertToJsonPlugin` is a small example.

### Talking to the tool

* **Menus:** [AddMenuEntry](pluginbase.addmenuitem.md) adds an item to the main menu and returns a model whose `Header`, `IsEnabled`, and `IsChecked` you can change later.
* **Dialogs:** use the injected `IDialogService` (`ShowMessage`, `ShowYesNoMessage`, `GetUserString`, `OpenFile`, `SaveFile`). For a custom dialog, put a `DialogViewModel` in your plugin and show it with `Show<TViewModel>()`.
* **Tabs:** `CreateTab(control, title, location)` shows a panel. Hand it an Avalonia control, or a view model that your plugin has registered a view for.
* **Events:** subscribe in `StartUp()` to the events declared on `PluginBase` (`ElementSelected`, `VariableSet`, `ProjectLoad`, and many more).
* **Variables:** [AddAndRemoveVariablesForType](pluginbase.addandremovevariablesfortype.md) adds custom variables to a standard element type.

## Getting your plugin into Gum

Build your project to produce a .dll. That .dll file needs to be in a folder under `Plugins` next to the Gum executable, one folder per plugin. For example, when running from source in Debug:

```
Tool/Gum.Avalonia/bin/Debug/net10.0/Plugins/MyPluginProject/MyPluginProject.dll
```

For a downloaded release, the folder is `Plugins/MyPluginProject/` beside `Gum.exe` (Windows), inside `Gum.app/Contents/MacOS/` (macOS), or beside `Gum` (Linux). The same folder layout works on every operating system.

[Setting up post-build events](setting-up-post-build-events.md) shows how to copy the .dll there automatically on every build.

To verify that your plugin is working correctly:

1. Run Gum
2. Click Plugins -> "Manage Plugins"

![](<../../.gitbook/assets/MyPluginName (1).png>)

## Migrating a WPF plugin

If you maintain a plugin written against the WPF tool, here is what to change.

### Menus

Replace `AddMenuItem`, which returned a WPF `MenuItem`, with `AddMenuEntry`, which takes the click action first and then the menu path:

```csharp
// Before
var item = AddMenuItem(new[] { "My Plugin", "Do Thing" });
item.Click += (_, _) => DoThing();

// After
var entry = AddMenuEntry(DoThing, "My Plugin", "Do Thing");
entry.Header = "Do Thing…";   // the returned model drives the rendered item
```

### Tabs

`CreateTab(object content, string title, TabLocation location)` accepts either a control the tool can show or a view model. Hand it an Avalonia control or a view model that your plugin registers a view for. A WPF `UserControl` no longer works.

### Dialogs

Use the injected `IDialogService` (`ShowMessage`, `GetUserString`, `OpenFile`, `SaveFile`, and `Show<TViewModel>()` for your own dialogs). `System.Windows.MessageBox` and WPF `Window`s do not exist in the tool.

### Project file

Target `net10.0` rather than `net10.0-windows` (or `net8.0-windows`), drop `<UseWPF>` and `<UseWindowsForms>`, and reference `Gum.Presentation` instead of `Gum.csproj`.

### If you cannot migrate yet

The last WPF release, [September 2, 2026](https://github.com/vchelaru/Gum/releases/tag/Release_September_02_2026), remains available for download (`Gum.zip`) and keeps loading WPF plugins. It does not receive new features.

## Troubleshooting

### The type or namespace name 'PluginBase' could not be found

Make sure your project references `Tools/Gum.Presentation/Gum.Presentation.csproj` and targets the same .NET version as Gum (currently `net10.0`).

### The plugin does not appear in Manage Plugins

* Check that the .dll is in its own folder under `Plugins` next to the executable, not directly in `Plugins`.
* Check the Output tab after startup. A plugin that references WPF or Windows Forms is reported there and skipped; so is a plugin whose dependencies are a different version of a Gum assembly than the one the tool loaded.
