# Shapes (Apos.Shapes)

## Introduction

The Apos.Shapes library can be used with Gum to add support for the following additional standard types:

* ArcRuntime
* ColoredCircleRuntime
* RoundedRectangleRuntime

The interface for working with these types is the same as working with these types in Skia.

## Adding Shapes to a MonoGame Gum Project

Currently this library is not distributed as a NuGet package, so games must link to source. To do this:

1. Clone the Gum repository locally
2. Add the following csproj to your game's .sln: \<Gum Root>/Runtimes/MonoGameGumShapes/MonoGameGumShapes.csproj
3. Link this project in your main game's csproj as a dependency

Your game now has access to the shape runtimes mentioned above.

