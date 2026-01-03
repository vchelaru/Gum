# Plugins

## Introduction

This page discusses how to write plugins for Gum. Plugins are a useful way to modify Gum because they allow you to customize Gum without making project-specific, technology-specific, or organization-specific modifications to the core source code. This means that you can customize the Gum experience while still maintaining ties to the core source code.

## Setup

To begin writing a plugin:

1. Obtain the Gum source code. You can download the .zip or get the source through a version control client
2. Create a copy of Gum.sln. You will want to work with your own .sln file so that the project containing your plugin can be debugged easily. For example, you might want to call your solution GumWithPlugins.sln
3. Open your new .sln file in Visual Studio

Now that you have created a .sln which will contain your plugin project, you can add this project:

1. Right-click on your solution
2. Select "Add ->New Project..."
3. Select "Class Library" as the type
4. Verify that you are targeting .NET 4.5
5. Enter the name of your project, such as MyPluginProject
6. Click OK

Next you'll need to reference the Gum libraries. To do this:

1. Right-click on your project's References
2. Select "Add Reference"
3. Verify that "Solution" is selected (Assuming Visual Studio 2012)
4. Check the following projects:
   1. Gum
   2. GumDataTypes
   3. InputLibrary
   4. RenderingLibrary
   5. ToolsUtilities
5. Click OK

Since Gum references XNA, and XNA doesn't have 64 bit libraries, you will need to modify your project's build configuration. To do this:

1. Select the "Build"->"Configuration Manager" menu item
2. Change the Active solution platform" from "Mixed Platforms" to "x86" if it isn't already set to x86. If your Active solution is set to "Any CPU", change that to "x86"
3. If your plugin project's Platform is set to "Any CPU", use the drop down to pick "x86" if it exists. If not, select ""
   1. Change the New platform to "x86"
   2. Click "OK"
4. Make sure your plugin project is set to build
5. Click Close to close the Configuration Manager
6. Adding a Plugin class

Next you'll want to add a Plugin class. This is a class that inherits from PluginBase. To do this:

1. Right-click on your project
2. Select "Add->Class..."
3. Name your class "MyPlugin" or whatever you want your plugin to be called
4. Add the following using:
5. Modify your plugin so it's public and inherits from PluginBase:
6. Add the following implementation into your plugin class:

```csharp
public override string FriendlyName
{
    get { return "My Plugin Name"; }
}

public override Version Version
{
    get { return new Version(0, 0, 0, 0); }
}

public override void StartUp()
{
    // Add startup logic here:
}

public override bool ShutDown(Gum.Plugins.PluginShutDownReason shutDownReason)
{
    return true;
}
```

## Getting your plugin into Gum

Now that you have a simple plugin, you need to do two things to test this plugin out. The first is to mark the plugin as a class that is exported using Microsoft Extension Framework. To do this:

1. Right-click on your project's References item in the Solution Explorer
2. Select "Add Reference..."
3. Click the "Assemblies->Framework" tab
4. Check "System.ComponentModel.Composition"
5. Click OK
6. Add the following using statement to your plugin class: `using System.ComponentModel.Composition;`
7. Modify your class definition so it looks like this (we're making it public and also adding the Export attribute:

```csharp
[Export(typeof(PluginBase))](Export(typeof(PluginBase)))
public class MyPlugin : PluginBase
```

Now you can right-click on your project and select Build and it will generate a .dll for your project. This .dll file needs to be in the Plugins folder under the Gum binary. That is, from the location of your .sln file, you can go to . Once there, you will need to create a folder for your plugin (like MyPlugin) and place your .dll there.

To verify that your plugin is working correctly:

1. Run Gum
2. Click Plugins->"Manage Plugins"

![](<../../.gitbook/assets/MyPluginName (1) (1).png>)

## Troubleshooting

### The type or namespace name 'PluginBase' could not be found

This may happen if you are targeting different versions of the .NET framework. At the time of this writing Gum targets .NET 4.5. Therefore, you'll want to make sure that your Plugin project also targets .NET 4.5. To do this:

1. Right-click on your project
2. Select "Properties"
3. Select the "Application" tab
4. Verify that the "Target Framework:" is set to .NET Framework 4.5 (or whatever version Gum is currently on)
