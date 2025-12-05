# About

## Introduction

Gum is a set of technologies for making game UI including a WYSIWYG tool, runtime libraries for a variety of platforms, and a layout engine which can be included in any .NET environment.

## Gum Tool

The Gum tool is a WYISWYG (what you see is what you get) tool for creating game UI.

<figure><img src="../../.gitbook/assets/image (26).png" alt=""><figcaption><p>Gum UI</p></figcaption></figure>

The Gum tool can be used to create interactive UI and HUDs for your game of any complexity. UI can be created for simple HUD, such as score and health display, or more complex UI such as a lobby for a multi-player game.

The Gum tool can be used in games by loading the resulting XML files or through generated code. Generated code can be used to eliminate the need for _magic strings_ when accessing objects in your screens and components. Generated code can also be used to completely eliminate the need for loading XML files which can improve your game's performance, and even allow using the Gum tool on platforms which may restrict file operations.

For more information on using the Gum tool, see the [Gum Tool Introduction](../../) page, and the [Gum Tool Tutorials and Examples](../../gum-tool/tutorials-and-examples/) section.

## Gum Runtimes

Gum runtimes can be used to load Gum projects or create code-only UI in many popular game platforms including:

* MonoGame/KNI/FNA
* raylib
* FlatRedBall
* SkiaSharp (Silk.NET, WPF, .NET Maui)
* Pygame
* Meadow

The use of the Gum tool is completely optional - you can do everything in code if you prefer.

## Gum Layout Engine

Gum is built on a platform-agnostic layout engine. This engine is a normal .NET NuGet package which can be included in any .NET environment. The Gum layout engine can be used directly in your game if you have special needs beyond what is provided by the existing Gum runtime libraries including

* Rendering on unsupported platforms
* Integration with an existing rendering engine
* Creation of new Gum runtimes
