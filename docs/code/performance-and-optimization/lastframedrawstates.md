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

### Summarizing by cause

Reading individual draw states is precise but tedious when a frame has dozens or hundreds of begins. `Renderer.GetDrawStateSummary` rolls the same data up into a count per cause, so you can see at a glance whether your begins come from clipping, non-clip render-state changes (blend/color/wrap), or texture sets within batches.

```csharp
// Draw
DrawStateSummary summary = GumUI.SystemManagers.Renderer.GetDrawStateSummary();
System.Diagnostics.Debug.WriteLine(summary);
```

This prints a breakdown similar to:

```
Draw State Summary: 120 SpriteBatch.Begin(s)
  Initial:       1
  Clip changes:  118
  State changes: 1
  Texture sets within batches:     40
  Apos.Shapes ShapeBatch.Begin(s): 0
```

Use the breakdown to decide where to spend effort:

* **Clip changes dominate** — each `ClipsChildren` container forces a begin on entry and another on exit. Forms controls add clipping containers freely, so a list or grid with many clipping items drives this number up. Reduce it by removing `ClipsChildren` where it isn't needed.
* **Texture sets are high** — pack sprites, fonts, and the single-pixel texture onto shared PNGs (see [SinglePixelTexture](singlepixeltexture.md)).
* **`Apos.Shapes` is high** — your scene mixes the `SpriteBatch` and Apos.Shapes batchers, which is the case [BatchKeyGroupedOrderer](batchkeygroupedorderer.md) targets. `SpriteBatch.Begin` counts (the other rows, and the total reported by `LastFrameDrawStates`) do not include Apos.Shapes begins, so this row is the only one the grouped orderer can reduce.

{% hint style="info" %}
The clip-versus-state split is a heuristic. Clip-exit begins are identified exactly; clip-enter begins are inferred from whether the begin's renderable clips its children. A renderable that both clips and changes a non-clip state in the same begin is counted as a clip change. The totals are exact; only the clip/state attribution is approximate.
{% endhint %}
