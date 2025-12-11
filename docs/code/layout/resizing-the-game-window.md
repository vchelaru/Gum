# Resolution and Resizing the Game Window

## Introduction

Gum provides a number of ways to react to game resolution changes. This page discusses how to react to resolution changes.

## Default Behavior

By default Gum uses `GraphicsDevice.Viewport` to set its internal resolution on initialization. This value can be assigned in the Game's constructor by assigning `_graphics.PreferredBackBufferWidth` and `_graphics.PreferredBackBufferWidth`. Most likely your game is already doing this to set the initial Window size. The following shows an example game with Gum.

<pre class="language-csharp"><code class="lang-csharp">public class Game1 : Game
{
    GraphicsDeviceManager _graphics;

    GumService GumUi => GumService.Default;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    
<strong>        // This sets the initial size:
</strong><strong>        // If not explicitly set, then size values
</strong><strong>        // default to 800x480 on desktop platforms
</strong><strong>        _graphics.PreferredBackBufferWidth = 800;
</strong><strong>        _graphics.PreferredBackBufferHeight = 600;
</strong>    }

    protected override void Initialize()
    {
<strong>        // Internally this will initialize using the viewport values
</strong><strong>        GumUi.Initialize(this, DefaultVisualsVersion.V3);
</strong>        ...
</code></pre>

Keep in mind that the `Preferred` properties are not guaranteed, and MonoGame can choose to use these values internally. For example, if MonoGame runs on a Mac with UI scaling, then the actual resolution may not match the preferred values. This can be handled by subscribing to the `ClientSizeChanged` event in the constructor, as shown in the following code:

<pre class="language-csharp"><code class="lang-csharp">public class Game1 : Game
{
    GraphicsDeviceManager _graphics;

    GumService GumUi => GumService.Default;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    
<strong>        Window.ClientSizeChanged += OnClientSizeChanged;
</strong>    
        // This sets the initial size:
        // If not explicitly set, then size values
        // default to 800x480 on desktop platforms
        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 600;
    }

<strong>    private void OnClientSizeChanged(object sender, EventArgs e)
</strong><strong>    {
</strong><strong>        // Updating the canvas width and height when client size changes.
</strong><strong>        GumUi.CanvasWidth = 
</strong><strong>            GraphicsDevice.PresentationParameters.BackBufferWidth;
</strong><strong>        GumUi.CanvasHeight = 
</strong><strong>            GraphicsDevice.PresentationParameters.BackBufferHeight;
</strong><strong>    }
</strong>
    protected override void Initialize()
    {
        // Internally this will initialize using the viewport values
        GumUi.Initialize(this, DefaultVisualsVersion.V3);
        ...
</code></pre>

Of course, this assumes that you would like the Gum canvas to occupy the entirety of the game window.

## Default Resize Behavior

By default resizing your Game does not adjust Gum. The following animation shows how Gum behaves by default. Note that the objects in the following animation are properly docked to the corners and center as indicated by the displayed text:

<figure><img src="../../.gitbook/assets/20_06 44 11.gif" alt=""><figcaption></figcaption></figure>

## Handling Resizing with No Zoom

This section discusses how react to the window resizing by adjusting the canvas sizes and performing a layout. The Screen contains a Container which is sized to the entire screen with a small border around the edges. The Container has a ColoredRectangle which fills the entire Container. Each Text object is docked as indicated by its displayed string.

The following code shows how to handle a resize:

<pre class="language-csharp"><code class="lang-csharp">protected override void Initialize()
{
    // other initialization goes here:
    
    // This allows you to resize:
    Window.AllowUserResizing = true;
    // This event is raised whenever a resize occurs, allowing
    // us to perform custom logic on a resize
<strong>    Window.ClientSizeChanged += HandleClientSizeChanged;
</strong>}

<strong>private void HandleClientSizeChanged(object sender, EventArgs e)
</strong><strong>{
</strong><strong>    GumUI.CanvasWidth = _graphics.GraphicsDevice.Viewport.Width;
</strong><strong>    GumUI.CanvasHeight = _graphics.GraphicsDevice.Viewport.Height;
</strong><strong>
</strong><strong>    // Update layout starting at the Root object. All contained objects
</strong><strong>    // will update their layouts recursively:
</strong><strong>    GumUI.Root.UpdateLayout();
</strong><strong>}
</strong></code></pre>

<figure><img src="../../.gitbook/assets/20_07 01 46.gif" alt=""><figcaption><p>Resizing the window resulting in Gum layout updates</p></figcaption></figure>

## Handling Resizing with Zoom

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
    
    GumUI.Renderer.Camera.Zoom = zoom;

    GumUI.CanvasWidth = _graphics.GraphicsDevice.Viewport.Width/zoom;
    GumUI.CanvasHeight = _graphics.GraphicsDevice.Viewport.Height/zoom;

    // Update layout starting at the Root object. All contained objects
    // will update their layouts recursively:
    GumUI.Root.UpdateLayout();
}
```

<figure><img src="../../.gitbook/assets/20_07 03 01.gif" alt=""><figcaption><p>Gum responding to resizes by zooming and adjusting canvas sizes</p></figcaption></figure>

## FrameworkElement PopupRoot and ModalRoot

The FrameworkElement object has two InteractiveGues: PopupRoot and ModalRoot. These are typically created automatically by FormsUtilities but can be assigned manually. In either case, the size of these two containers is automatically managed by FormsUtilities in its Update call so you do not need to update these manually.
