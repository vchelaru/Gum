# Visual Studio Code and Linking Source

## Introduction

This page explains how to modify your project so that the Gum source appears in Visual Studio Code.

## VS Code Visibility (Multi-root Workspace)

If you are using Visual Studio Code, you may notice that even after linking the projects, the `MonoGameGum` source code does not appear in your File Explorer. This is because VS Code defaults to showing only the folder you have opened.

To make the source project visible and editable alongside your game, you can convert your setup into a Multi-root Workspace:

1. Open your game folder in VS Code.
2. Go to File > Add Folder to Workspace...
3. Navigate to and select the `MonoGameGum` folder.
4. Go to File > Save Workspace As... and save the file (e.g., `MyGame.code-workspace`) in your project root.

Both folders will now appear in your sidebar as separate "roots," allowing you to browse, edit, and step into the Gum source code while debugging.

## **Alternative: Using a Solution File**

If you have the C# Dev Kit extension installed, VS Code can also behave more like "normal" Visual Studio using a `.sln` file:

1. Open a terminal in your game's root.
2. Create a solution if you don't have one: `dotnet new sln`
3.  Add your game and the Gum source to it:

    Bash

    ```
    dotnet sln add YourGame.csproj
    dotnet sln add ../Path/To/MonoGameGum/MonoGameGum.csproj
    ```
4. The projects will now appear in the Solution Explorer pane (the icon with the three stacked boxes), even if the folders are in different locations on your hard drive.
