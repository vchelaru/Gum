# ContainerRuntime

### Introduction

ContainerRuntime is a GraphicalUiElement-inheriting object used to organize and perform layout on a specific group of objects. Examples of when to use a container include:

* Providing margins inside the screen or another container
* Aligning or orienting children along a common position
* Changing children layout types, such as stacking children horizontally inside a parent container which stacks its children vertically
* To inject spacing between objects when using ratio width or height

ContainerRuntime instances have no visuals, so they cannot be directly observed in game.

### Example - Creating a ContainerRuntime

To create a ContainerRuntime, instantiate it and add it to the managers as shown in the following code:

```csharp
// Initialize
var container = new ContainerRuntime();
container.Width = 150; // by default, containers use absolute width...
container.Height = 150; // ...and height.
container.AddToManagers(SystemManagers.Default, null);
```

### Children (Containers as Parents)

Containers are usually used as parents for other runtime objects. To add another runtime instance to a container, add it to the Children list as shown in the following code:

```csharp
// Initialize
var parentContainer = new ContainerRuntime();
parentContainer.AddToManagers(SystemManagers.Default, null);

var childText = new TextRuntime();
childText.Text = "I am a child TextRuntime";
parentContainer.Children.Add(childText);
```

Notice that only the parent object needs to have its AddToManagers method called. Any child added to a parent which has been added to managers is automatically added as well. This membership is cascaded through all children, so if your project has a single root object, then only that root object needs to be added (or removed) from managers.

The following code shows a parent container added to managers. The child container and child text do not need to be added to managers:

```csharp
// Initialize
var parentContainer = new ContainerRuntime();
parentContainer.AddToManagers(SystemManagers.Default, null);

var childContainer = new ContainerRuntime();
// By adding the childContainer to the parentContainer, we do not need
// to call childContainer.AddToManagers
parentContainer.Children.Add(childContainer);

var textRuntime = new TextRuntime();
textRuntime.Text = "I do not need my AddToManagers method called either.";
childContainer.Children.Add(textRuntime);
```

### Render Target Shaders (RenderTargetEffect)

A ContainerRuntime can render its entire contents to a render target and then apply a shader (an `Effect`) when that render target is drawn back to the screen. Because the shader runs over the container's whole composited image, it acts as a post-process over everything inside the container — useful for grayscale, blur, glow, tint, and similar full-container effects.

This uses two properties together:

1. `IsRenderTarget` set to `true` — the container composites its children into a render target instead of drawing them directly.
2. `RenderTargetEffect` set to an `Effect` — the shader bound for the single draw that blits that render target to the screen.

{% hint style="info" %}
`RenderTargetEffect` is available only on the XNA-like runtimes (MonoGame, KNI, and FNA), where the effect is a `Microsoft.Xna.Framework.Graphics.Effect`.
{% endhint %}

Gum does not compile or load the shader for you — you supply an already-constructed `Effect`. You can load it however you like: the MonoGame content pipeline, `new Effect(graphicsDevice, bytes)` with precompiled shader bytes (no content pipeline required), or a runtime `.fx` compiler such as ShadowDusk.

#### Example - Applying a shader to a container

```csharp
// Initialize
var container = new ContainerRuntime();
container.Width = 200;
container.Height = 200;
container.IsRenderTarget = true;

// Load the Effect however you prefer - here through the MonoGame content pipeline.
container.RenderTargetEffect = Content.Load<Microsoft.Xna.Framework.Graphics.Effect>("Grayscale");

var bear = new SpriteRuntime();
bear.SourceFileName = "Bear.png";
container.Children.Add(bear);

container.AddToManagers(SystemManagers.Default, null);
```

The shader is applied to the container's render target as a whole, so every child inside the container (sprites, text, nested containers) is affected at once. The container can be nested anywhere in your layout — the effect is applied whether the render-target container is a root object or deeply nested inside other containers.

#### Shader requirements

The effect must follow the standard MonoGame 2D `SpriteBatch` shader convention: a pixel shader that samples the sprite texture, with the vertex transform supplied by `SpriteBatch` through the usual `MatrixTransform` parameter (the same shape used by the standard MonoGame 2D shader examples). An effect that hardcodes its own projection matrix will not position correctly.

Because the `Effect` instance is yours, you can update its parameters each frame - for example animating a blur radius or passing elapsed time - before drawing.

{% hint style="info" %}
To instead display a container's render target as a regular Sprite (so it can be scaled, rotated, or stacked) rather than post-processing it in place, see [RenderTargetTextureSource](spriteruntime/rendertargettexturesource.md).
{% endhint %}
