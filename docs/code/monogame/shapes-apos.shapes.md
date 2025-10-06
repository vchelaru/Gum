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

## Setup in Code

Whether you are using code-only or the Gum tool, you must add the following line of code **before initializing Gum**:

```csharp
ShapeRenderer.Self.Initialize(GraphicsDevice, Content);
// initialize Gum now:
```

## Setup in Gum Tool

Shapes can be used in the Gum tool. To add shapes:

1. Launch the Gum tool
2. Select Plugins ⇒ Add Skia Standard Elements
3. Add instances of Arc, ColoredCircle, or RoundedRectangleRuntime to your Screens or Components

{% hint style="warning" %}
Shapes only supports the shapes listed above. Adding other Skia instanes, such as SVG or Lottie, will result in compile time or runtime errors.
{% endhint %}

