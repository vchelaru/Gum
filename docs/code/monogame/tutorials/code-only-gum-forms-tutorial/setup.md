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

<figure><img src="../../../../.gitbook/assets/NuGetGum.png" alt=""><figcaption><p>Gum.MonoGame NuGet package</p></figcaption></figure>

## Adding Gum to Your Game

Gum requires a few lines of code to get started. A simplified Game class with the required calls would look like the following code:

{% tabs %}
{% tab title="Full Code" %}
```csharp
using MonoGameGum.Forms;
using MonoGameGum.Forms.Controls;

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
        GumUI.Initialize(this, DefaultVisualsVersion.V2);
            
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
{% endtab %}

{% tab title="Diff" %}
```diff
+using MonoGameGum.Forms;
+using MonoGameGum.Forms.Controls;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    
+   GumService GumUI => GumService.Default;
    
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
+       GumUI.Initialize(this, DefaultVisualsVersion.V2);
            
+       var mainPanel = new StackPanel();
+       mainPanel.Visual.AddToRoot();
        
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
+       GumUI.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
+       GumUI.Draw();
        base.Draw(gameTime);
    }
}
```
{% endtab %}
{% endtabs %}

The code above includes the following sections:

* Initialize - The Initialize method prepares Gum for use. It must be called one time for every Gum project. Note that `DefaultVisualsVersion.V2` is passed as a second parameter, indicating that Version 2 of visuals are used. All new projects should use Version 2 rather than Version 1 as of July 2025.
* Once Gum is initialized, we can create controls such as the `StackPanel` which contains all other controls.  By calling `AddToRoot`, the `mainPanel` is drawn and receives input. All items added to the `StackPanel` will also be drawn and receive input, so we only need to call `AddToRoot` on the `StackPanel`.

```csharp
GumUI.Initialize(this, DefaultVisualsVersion.V2);
var mainPanel = new StackPanel();
mainPanel.AddToRoot();
```

* Update - this updates the internal keyboard, mouse, and gamepad instances and applies default behavior to any forms components. For example, if a `Button` is added to the `StackPanel`, this code is responsible for checking if the cursor is overlapping the `Button` and adjusting the highlight/pressed state appropriately.

```csharp
GumUI.Update(gameTime);
```

* Draw - this method draws all Gum objects to the screen. This method does not yet perform any drawing since `StackPanels` are invisible, but we'll be adding controls later in this tutorial.

```csharp
GumUI.Draw();
```

We can run our project to see a blank (cornflower blue) screen.

<figure><img src="../../../../.gitbook/assets/image (2) (1).png" alt=""><figcaption><p>Empty MonoGame project</p></figcaption></figure>

### Adding Controls

Now that we have Gum running, we can add controls to our `StackPanel` (`mainPanel`). The following code in Initialize adds a `Button` which responds to being clicked by modifying its `Text` property:

{% tabs %}
{% tab title="Full Code" %}
```csharp
protected override void Initialize()
{
    GumUI.Initialize(this, DefaultVisualsVersion.V2);

    var mainPanel = new StackPanel();
    mainPanel.Visual.AddToRoot();

    // Creates a button instance
    var button = new Button();
    // Adds the button as a child so that it is drawn and has its
    // events raised
    mainPanel.AddChild(button);
    // Initial button text before being clicked
    button.Text = "Click Me";
    // Makes the button wider so the text fits
    button.Width = 350;
    // Click event can be handled with a lambda
    button.Click += (_, _) =>
        button.Text = $"Clicked at {System.DateTime.Now}";

    base.Initialize();
}
```
{% endtab %}

{% tab title="Diff" %}
```diff
protected override void Initialize()
{
    GumUI.Initialize(this, DefaultVisualsVersion.V2);

    var mainPanel = new StackPanel();
    mainPanel.Visual.AddToRoot();

+   // Creates a button instance
+   var button = new Button();
+   // Adds the button as a child so that it is drawn and has its
+   // events raised
+   mainPanel.AddChild(button);
+   // Initial button text before being clicked
+   button.Text = "Click Me";
+   // Makes the button wider so the text fits
+   button.Width = 350;
+   // Click event can be handled with a lambda
+   button.Click += (_, _) =>
+       button.Text = $"Clicked at {System.DateTime.Now}";

    base.Initialize();
}
```
{% endtab %}
{% endtabs %}

<figure><img src="../../../../.gitbook/assets/13_07 03 09.gif" alt=""><figcaption></figcaption></figure>

## Conclusion

Now that we have a basic project set up with a single `Button`. The next tutorial covers the most common forms controls.
