# Setup

## Introduction

This tutorial walks you through creating an empty Gum project which acts as a starting point for the rest of the tutorials.&#x20;

This tutorial covers:

* Adding Gum NuGet packages
* Modifying your Game class to support Gum and Gum Forms
* Adding your first Gum control (Button)

## Adding Gum NuGet Packages

Before writing any code, we must add the Gum nuget package. Add the Gum.MonoGame package to your game. For more information see the [Setup page](https://docs.flatredball.com/gum/code/monogame/setup).

Once you are finished, your game project should reference the `Gum.MonoGame` project.

<figure><img src="../../../../.gitbook/assets/NuGetGum.png" alt=""><figcaption><p>Gum.MonoGame NuGet package</p></figcaption></figure>

## Adding Gum to Your Game

Gum requires a few lines of code to get started. A simplified Game class with the required calls would look like the following code:

```csharp
using MonoGameGum.Forms.Controls;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    
    StackPanel Root;
    
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        var gumProject = MonoGameGum.GumService.Default.Initialize(this);
            
        Root = new StackPanel();
        Root.Visual.AddToManagers();
        
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        MonoGameGum.GumService.Default.Update(this, gameTime, Root);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        MonoGameGum.GumService.Default.Draw();
        base.Draw(gameTime);
    }
}
```

The code above includes the following sections:

* StackPanel Root - Games using Gum usually have a Root object which contains all other instances. In this case we create a StackPanel which will hold all of our controls.&#x20;

```csharp
StackPanel Root;
```

* Initialize - The contents of the Initialize method calls GumService.Default.Initialize which prepares Gum. It also creates a Root instance of type StackPanel. Finally, the StackPanel has its AddToManagers method called which tells Gum to draw the StackPanel and its children that we'll add later.

```csharp
        var gumProject = MonoGameGum.GumService.Default.Initialize(this);
            
        Root = new StackPanel();
        Root.Visual.AddToManagers();
```

* Update - this updates the internal keyboard, mouse, and gamepad instances and applies default behavior to any components which implement Forms. For example, if a Button is added to the Screen, this code is responsible for checking if the cursor is overlapping the Button and adjusting the highlight/pressed state appropriately. We pass the Root instance so that it and all of its children can receive input events.

```csharp
MonoGameGum.GumService.Default.Update(this, gameTime, Root);
```

* Draw - this method draws all Gum objects to the screen. This method does not yet perform any drawing since StackPanels are invisible, but we'll be adding controls later in this tutorial.

```csharp
MonoGameGum.GumService.Default.Draw();
```

We can run our project to see a blank (cornflower blue) screen.

<figure><img src="../../../../.gitbook/assets/image.png" alt=""><figcaption><p>Empty MonoGame project</p></figcaption></figure>

### Adding Controls

Now that we have Gum running, we can add controls to our StackPanel (Root). The following code in Initialize adds a button which responds to being clicked by modifying its Text property:

```csharp
protected override void Initialize()
{
    MonoGameGum.GumService.Default.Initialize(this);

    Root = new StackPanel();
    Root.Visual.AddToManagers();

    // Creates a button instance
    var button = new Button();
    // Adds the button as a child so that it is drawn and has its
    // events raised
    Root.AddChild(button);
    // Initial button text before being clicked
    button.Text = "Click Me";
    // Makes the button wider so the text fits
    button.Visual.Width = 350;
    // Click event can be handled with a lambda
    button.Click += (_, _) =>
        button.Text = $"Clicked at {System.DateTime.Now}";

    base.Initialize();
}
```

<figure><img src="../../../../.gitbook/assets/13_07 53 14.gif" alt=""><figcaption></figcaption></figure>
