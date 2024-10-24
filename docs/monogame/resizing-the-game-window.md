# Resizing the Game Window

### Introduction

If your game includes support for resizing the game window, you may need to write additional code to handle the new width and height. This page discusses the options you have for handling resizing. When a game is resized, the typical response is to adjust GraphicalUiElement.CanvasWidth and GraphicalUiElement.CanvasHeight, but you may also want to zoom your UI depending on the needs of your game.

### Default Behavior

By default resizing your Game does not adjust Gum. The following animation shows how Gum behaves by default. Note that the objects in the following animation are properly docked to the corners and center as indicated by the displayed text:

<figure><img src="../.gitbook/assets/20_06 44 11.gif" alt=""><figcaption></figcaption></figure>

### Handling Resizing with No Zoom

This section discusses how react to the window resizing by adjusting the GraphicalUiElement sizes and performing a layout. The Screen contains a Container which is sized to the entire screen with a small border around the edges. The Container has a ColoredRectangle which fills the entire Container. Each Text object is docked as indicated by its displayed string.

The following code shows how to handle a resize:

<pre class="language-csharp"><code class="lang-csharp">protected override void Initialize()
{
    // other initialization goes here:
    
    
    // This allows you to resize:
    Window.AllowUserResizing = true;
    // This event is raised whenever a resize occurs, allowing
    // us to perform custom logic on a resize
    Window.ClientSizeChanged += HandleClientSizeChanged;

}
<strong>
</strong><strong>private void HandleClientSizeChanged(object sender, EventArgs e)
</strong>{
    GraphicalUiElement.CanvasWidth = _graphics.GraphicsDevice.Viewport.Width;
    GraphicalUiElement.CanvasHeight = _graphics.GraphicsDevice.Viewport.Height;

    // Grab your rootmost object and tell it to resize:
    currentScreenGue.UpdateLayout();
}
</code></pre>

<figure><img src="../.gitbook/assets/20_07 01 46.gif" alt=""><figcaption><p>Resizing the window resulting in Gum layout updates</p></figcaption></figure>

### Handling Resizing with Zoom

You may want to zoom your game rather than provide more visible space to the user when resizing. In this case you need to decide whether to use width or height when deciding how much to zoom your game. For this example we will use height since some users may have monitors with differing aspect ratios.

To properly zoom we need to store the original size in a variable in the Game class so that we can use it to determine the zoom level. The following code is a modified version of the code above with additional logic and variables added to handle zooming:

```csharp
protected override void Initialize()
{
    // other initialization goes here:
    
    
    // This allows you to resize:
    Window.AllowUserResizing = true;
    // This event is raised whenever a resize occurs, allowing
    // us to perform custom logic on a resize
    Window.ClientSizeChanged += HandleClientSizeChanged;

    // store off the original height so we can use it for zooming
    originalHeight = _graphics.GraphicsDevice.Viewport.Height;
}

private void HandleClientSizeChanged(object sender, EventArgs e)
{
    float zoom = _graphics.GraphicsDevice.Viewport.Height / (float)originalHeight;
    
    SystemManagers.Default.Renderer.Camera.Zoom = zoom;

    GraphicalUiElement.CanvasWidth = _graphics.GraphicsDevice.Viewport.Width/zoom;
    GraphicalUiElement.CanvasHeight = _graphics.GraphicsDevice.Viewport.Height/zoom;

    // Grab your rootmost object and tell it to resize:
    currentScreenGue.UpdateLayout();
}
```

<figure><img src="../.gitbook/assets/20_07 03 01.gif" alt=""><figcaption><p>Gum responding to resizes by zooming and adjusting canvas sizes</p></figcaption></figure>

### FrameworkElement PopupRoot and ModalRoot

The FrameworkElement object has two InteractiveGues: PopupRoot and ModalRoot. These are typically created automatically by FormsUtilities but can be assigned manually. In either case, the size of these two containers is automatically managed by FormsUtilities in its Update call so you do not need to update these manually.
