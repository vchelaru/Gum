# Layered Forms

## Introduction

Gum Forms can be added to layers to control ordering, apply offsets, and change zoom. Keep in mind that ordering can be achieved by changing the order that controls are added to the root, so using explicit layers is not necessary for controlling ordering. This document shows how to create and use explicit layers.

## Layers and Update

A typical Gum control must be be drawn and must have its every-frame logic performed. Both of these are handled when a control is added to GumServices.Root, which can be accomplished by calling AddToRoot. For example, the following code creates a fully-functional and drawn Button:

```csharp
var button = new Button();
button.AddToRoot();
```

Although this is convenient, the Button must be drawn on the same layer as the root. Instead, we an create a custom layer and add the Button to the layer. However, controls which are not added to Root will not receive automatic updates, so they will not receive input events such as clicks from a cursor.

At a high level, the steps to creating a layered button are:

1. Create a Layer
2. Set the Layer settings, such as by using LayerCameraSettings. For more information, see the [LayerCameraSettings](../gum-code-reference/layer.md#layercamerasettings) page.
3. Create a Forms control. This could be a simple Button, or it could be a StackPanel which contains many controls
4. Add the Forms control to the layer
5. Create a List that contains all controls which should be updated. This might contain the GumServices.Default.Root as well as the layered Forms Control
6. Change the GumService Update call to take a List of controls.

The following code shows how to do the steps above:

```csharp
public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    GumService GumUI => GumService.Default;

    StackPanel layeredStackPanel;
    List<GraphicalUiElement> itemsToUpdate;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GumUI.Initialize(this);

        var unlayeredStackPanel = new StackPanel();
        unlayeredStackPanel.AddToRoot();

        for(int i = 0; i < 3; i++)
        {
            var button = new Button();
            unlayeredStackPanel.AddChild(button);
            button.Text = "Button " + i;
        }
       
        // 1
        var layer = GumUI.SystemManagers.Renderer.AddLayer();
        // 2
        var layerCameraSettings = new LayerCameraSettings();
        layerCameraSettings.Zoom = 2;
        layer.LayerCameraSettings = layerCameraSettings;

        // 3
        layeredStackPanel = new StackPanel();
        layeredStackPanel.X = 100;
        // 4
        layeredStackPanel.Visual.AddToManagers(GumUI.SystemManagers, layer);

        for (int i = 0; i < 3; i++)
        {
            var button = new Button();
            layeredStackPanel.AddChild(button);
            button.Text = "Button " + i;
        }

        // 5
        itemsToUpdate = new()
        {
            GumUI.Root,
            layeredStackPanel.Visual
        };

        base.Initialize();
    }


    protected override void Update(GameTime gameTime)
    {
        // 6
        GumUI.Update(gameTime, itemsToUpdate);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        GumUI.Draw();

        base.Draw(gameTime);
    }
}

```

The code above creates two stack panels - one layered and one unlayered. To differentiate, the layered panel is plaed on a layer that is drawn at 2x zoom.

<figure><img src="../../.gitbook/assets/18_22 24 31.gif" alt=""><figcaption><p>Layered buttons drawn at 2x zoom</p></figcaption></figure>
