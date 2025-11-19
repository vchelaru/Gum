# Measuring Draw Calls

### Introduction

LastFrameDrawStates is an IEnumerable for the draw states used in the previous draw call. This can be used to find performance problems and isolate what may be causing state changes.

## Rendering

Internally Gum uses a SpriteBatch for rendering. It attempts to use as few Begin/End pairs as possible, but it will end a draw batch if necessary. The following changes require a batch to be interrupted:

* Changes in textures. Examples include:
  * Two SpriteRuntimes which use different textures
  * A TextRuntime and a non-TextRuntime (such as a NineSlice) will introduce a break in rendering unless the TextRuntime's font shares a PNG with the non-TextRuntime
  * A ColoredRectangleRuntime and a non-ColoredRectangleRuntime, unless the SinglePixelTexture is on the same PNG
* Changes in clip region by setting ClipsChildren to true
* Changes in render targets by setting IsRenderTarget to true

Gum provides a list of changes which can be inspected to spot where render problems might be occurring.

### Code Example: Checking Performance

The following code shows how to check the performance of a simple project. Note that this code creates visuals instead of Forms controls to intentionally create render breaks.

```csharp
protected override void Initialize()
{
    // Initialize Gum - can load a project, although it isn't used below
    GumUI.Initialize(this);

    // Create a panel to hold all children.
    var panel = new StackPanel();
    panel.AddToRoot();

    for(int i = 0; i < 5; i++)
    {
        var subContainer = new ContainerRuntime();
        // ClipsChildren sets 
        subContainer.ClipsChildren = true;
        panel.AddChild(subContainer);

        var rectangle = new ColoredRectangleRuntime();
        subContainer.AddChild(rectangle);
    }
}

protected override void Update(GameTime gameTime)
{
    GumUI.Update(gameTime);
    base.Update(gameTime);
}

protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.CornflowerBlue);

    GumUI.Draw();

    if(GumUI.Keyboard.KeyPushed(Keys.Space))
    {

        var spriteRenderer = GumUI.SystemManagers.Renderer.SpriteRenderer;
        var lastFrameDrawStates = spriteRenderer.LastFrameDrawStates;
        
        System.Diagnostics.Debug.WriteLine(
            $"Last Frame Draw States ({lastFrameDrawStates.Count()}):");

        foreach(var item in lastFrameDrawStates)
        {
            System.Diagnostics.Debug.WriteLine(item);
        }
    }
    base.Draw(gameTime);
}

```

Pressing space outputs diagnostic information about the rendering similar to the following output:

```
Last Frame Draw States (11):
Begin w/ 0 Textures set(s)
By ContainerRuntime w/ 1 Textures set(s)
By Un-set ContainerRuntime Clip
By ContainerRuntime w/ 1 Textures set(s)
By Un-set ContainerRuntime Clip
By ContainerRuntime w/ 1 Textures set(s)
By Un-set ContainerRuntime Clip
By ContainerRuntime w/ 1 Textures set(s)
By Un-set ContainerRuntime Clip
By ContainerRuntime w/ 1 Textures set(s)
By Un-set ContainerRuntime Clip
```
