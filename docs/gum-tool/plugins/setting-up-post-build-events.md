---
title: Setting Up Post Build Events
---

# Setting Up Post Build Events

## Introduction

dll files created for plugins are files which are not required for Gum to function. In other words, if the file is there Gum will use it. If not, Gum can operate normally. This means that in your Visual Studio solution the project that you have created to hold your plugin will not automatically be copied to the build output folder. Fortunately Visual Studio supports post-build events which allow you to copy the built .dll to the plugins folder.

## Creating a post-build event

To access the post build event on a project:

1. Right-click on your project in the Solution Explorer
2. Select "Properties"
3. Select the "Build Events" tab on the properties page
4. Notice the "Post-build event command line:" section

To add a post build event, you will need to add command line commands to copy the .dll and .pdb to the plugin folder. Post-build events are simply run in a command line environment after the build succeeds. Therefore you can put any logic in here, including running other .exe or .bat files.

For this example we will assume that the project is called PluginProject. You will want to change the following text to match your project's name. To copy the files, paste \(and modify\) the following:

```text
if not exist $(SolutionDir)Gum\bin\Debug\Data\Plugins\ md $(SolutionDir)Gum\bin\Debug\Data\Plugins\
if not exist $(SolutionDir)Gum\bin\Debug\Data\Plugins\PluginProject\ md $(SolutionDir)Gum\bin\Debug\Data\Plugins\PluginProject\
copy $(TargetDir)PluginProject.dll $(SolutionDir)Gum\bin\Debug\Data\Plugins\PluginProject\PluginProject.dll
copy $(TargetDir)PluginProject.pdb $(SolutionDir)Gum\bin\Debug\Data\Plugins\PluginProject\PluginProject.pdb
```

Notice that "PluginProject" appears 8 times in the text above, so be sure to replace all instances with your project name.

### Why do we include the .pdb file?

The copy script copies the .pdb file to allow you to debug your plugin. Specifically the PDB enables breakpoints to trigger and to break on exceptions.

