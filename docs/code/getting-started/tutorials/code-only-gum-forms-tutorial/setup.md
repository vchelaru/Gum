# Setup

## Introduction

This tutorial walks you through turning an empty MonoGame project into a code-only Gum project, which acts as a starting point for the rest of the tutorials.

This tutorial covers:

* Adding Gum NuGet packages
* Modifying your Game class to support Gum and Gum Forms
* Adding your first Gum control (Button)

## Adding Gum NuGet Packages

Before writing any code, we must add the Gum NuGet package. Add the Gum.MonoGame package to your game. For more information see the [Setup page](https://docs.flatredball.com/gum/code/monogame/setup).

Once you are finished, your game project should reference the `Gum.MonoGame` project.

<figure><img src="../../../../.gitbook/assets/NuGetGum (1).png" alt=""><figcaption><p>Gum.MonoGame NuGet package</p></figcaption></figure>

## Adding Gum to Your Game

Gum requires a few lines of code to get started. A simplified Game class with the required calls would look like the following code:

```csharp
using Gum.Forms;
using Gum.Forms.Controls;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    
    GumService GumUI => GumService.Default;
    
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GumUI.Initialize(this);
            
        var mainPanel = new StackPanel();
        mainPanel.AddToRoot();
        
        base.Initialize();
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
        base.Draw(gameTime);
    }
}
```

The code above includes the following sections:

* Initialize - The Initialize method prepares Gum for use. It must be called one time for every Gum project.
* Once Gum is initialized, we can create controls such as the `StackPanel` which contains all other controls. By calling `AddToRoot`, the `mainPanel` is drawn and receives input. All items added to the `StackPanel` will also be drawn and receive input, so we only need to call `AddToRoot` on the `StackPanel`.

```csharp
// Initialize
GumUI.Initialize(this);
var mainPanel = new StackPanel();
mainPanel.AddToRoot();
```

* Update - this updates the internal keyboard, mouse, and gamepad instances and applies default behavior to any forms components. For example, if a `Button` is added to the `StackPanel`, this code is responsible for checking if the cursor is overlapping the `Button` and adjusting the highlight/pressed state appropriately.

```csharp
// Update
GumUI.Update(gameTime);
```

* Draw - this method draws all Gum objects to the screen. This method does not yet perform any drawing since `StackPanels` are invisible, but we'll be adding controls later in this tutorial.

```csharp
// Draw
GumUI.Draw();
```

We can run our project to see a blank (cornflower blue) screen.

<figure><img src="../../../../.gitbook/assets/image (16).png" alt=""><figcaption><p>Empty MonoGame project</p></figcaption></figure>

### Adding Controls

Now that we have Gum running, we can add controls to our `StackPanel` (`mainPanel`). The following code in Initialize adds a `Button` which responds to being clicked by modifying its `Text` property:

<pre class="language-csharp"><code class="lang-csharp">protected override void Initialize()
{
    GumUI.Initialize(this);

    var mainPanel = new StackPanel();
    mainPanel.Visual.AddToRoot();

<strong>    // Creates a button instance
</strong><strong>    var button = new Button();
</strong><strong>    // Adds the button as a child so that it is drawn and has its
</strong><strong>    // events raised
</strong><strong>    mainPanel.AddChild(button);
</strong><strong>    // Initial button text before being clicked
</strong><strong>    button.Text = "Click Me";
</strong><strong>    // Makes the button wider so the text fits
</strong><strong>    button.Width = 350;
</strong><strong>    // Click event can be handled with a lambda
</strong><strong>    button.Click += (_, _) =>
</strong><strong>        button.Text = $"Clicked at {System.DateTime.Now}";
</strong>
    base.Initialize();
}
</code></pre>

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA12NSwvCMBCE_8oSPFSQUBQvSg--EA-KaNGLINEsdGmagN34xP9uH6LibWfmm9mHmOVTn4kenzy2BFliUobuKHrirE6QKbJLZdFABBYvsGZ1TCsjaPZ39hPLDeVeGTnQOnYr57iKy4WDZ3b2XR9W4q9adEYJGR3UaBnWl4zxykVz58Ow3R4ZOqYwx1p9oS1pTgqq0w2_Zg1X6DCCYN-CfROiUncm__ONn33UoBge61vOmMmxYowpQ7lwl-f7r3i-AEs5ZmU0AQAA)

<figure><img src="../../../../.gitbook/assets/13_07 03 09.gif" alt=""><figcaption></figcaption></figure>

## Conclusion

Now that we have a basic project set up with a single `Button`. The next tutorial covers the most common forms controls.
