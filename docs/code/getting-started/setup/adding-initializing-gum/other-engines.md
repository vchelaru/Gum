# Other Engines (Custom Renderer)

## Introduction

Gum's layout, project loading and variables live in the `FlatRedBall.GumCommon` NuGet package, which has no rendering code. To use Gum in an engine that has no Gum runtime, reference GumCommon and write the drawing yourself.

The [Sdl3Gum sample](https://github.com/vchelaru/Gum/tree/main/Samples/Sdl3Gum) is the starting point. It draws Sprites and NineSlices with SDL3's 2D renderer in about 300 lines, comments included, from both code and a `.gumx` project. Porting it means replacing the SDL calls that load a texture and draw a source rectangle into a destination rectangle.

## What a Backend Provides

* **Renderables.** A class per drawn type, usually inheriting `RenderableBase`. Gum's layout sets its `X`, `Y`, `Width` and `Height`. A textured renderable implements `ITextureCoordinate`, so Gum can size it from the texture and set its `SourceRectangle`.
* **A renderable factory.** Assign `ElementSaveExtensions.CustomCreateGraphicalComponentFunc` to return a renderable for each standard type (`"Sprite"`, `"NineSlice"`, `"Container"`). Gum calls it when it builds an element from a `.gumx` project.
* **Property assignment.** Variables Gum does not handle itself are set on the renderable's public properties by name, so a Sprite renderable only needs a `SourceFile` property that loads its texture.
* **A draw walk.** Each frame, walk the tree from the root and draw each visible renderable. A renderable's `Children` holds the child `GraphicalUiElement`s; draw each one's `RenderableComponent`.

Set `GraphicalUiElement.CanvasWidth` and `CanvasHeight` to the window size, and call `UpdateLayout` on the root when the size changes.

## Next Steps

The sample draws no color, alpha, rotation, text or shapes. For a full renderer, see `Runtimes/RaylibGum` in the Gum repository, which draws every standard type.
