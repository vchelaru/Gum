# RenderTargetTextureSource

## Introduction

The RenderTargetTextureSource property allows a Sprite to render a texture produced by a different ContainerRuntime which is drawing to a render target. This feature allows a render target to be scaled, rotated, positioned, stacked, or to use any other Gum layout functionality.

## RenderTargetTextureSource Requirements

To use RenderTargetTextureSource, the following must exist:

1. A container which has its IsRenderTarget set to true. This container can contain any number of children.
2. A Sprite which has its RenderTargetTextureSource set to the container

{% hint style="info" %}
Normally invisible objects are not rendered and do not perform their Layout calls. If a container has its IsRenderTarget set to true, then it will perform layout and rendering calls so even if invisible so that SpriteRuntime instances can reference the resulting texture.
{% endhint %}

## Code Example: Scaling a ListBox

The following code creates a ListBox which is displayed on a render target with wavy scaling:

```csharp
SpriteRuntime sprite;

protected override void Initialize()
{
    GumUI.Initialize(this);

    var container = new ContainerRuntime();
    container.AddToRoot();
    container.Name = "Render target container";
    container.IsRenderTarget = true;
    container.Dock(Dock.SizeToChildren);
    container.Visible = false;

    var listBox = new ListBox();
    container.AddChild(listBox);
    for (int i = 0; i < 20; i++)
    {
        listBox.Items.Add($"Item {i}");
    }

    sprite = new SpriteRuntime();
    sprite.AddToRoot();
    sprite.Anchor(Anchor.Center);
    sprite.RenderTargetTextureSource = container;

    base.Initialize();
}

protected override void Update(GameTime gameTime)
{
    var gameSeconds = (float)gameTime.TotalGameTime.TotalSeconds;
    sprite.Width = 100 + 50 * MathF.Sin(gameSeconds * 4);
    sprite.Height = 100 + 50 * MathF.Sin(gameSeconds * 3);

    GumUI.Update(gameTime);

    base.Update(gameTime);
}

```

<figure><img src="../../../.gitbook/assets/11_05 56 52.gif" alt=""><figcaption></figcaption></figure>

{% hint style="info" %}
Currently the ListBox is not interactive because the original ListBox is invisible so it does not receive events from the Cursor. Future versions of Gum may provide a way to interact with invisible controls rendered to a RenderTarget.
{% endhint %}
