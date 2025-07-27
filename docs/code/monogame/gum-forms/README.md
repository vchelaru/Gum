# Gum Forms

### Introduction

Gum Forms is a set of classes which provide fully-functional forms controls which you can use in your game. Examples of forms controls include:

* Button
* Checkbox
* ListBox
* TextBox
* Slider

Gum Forms can be used purely in code (no .gumx project required), or you can fully style your forms objects in a Gum project.

### Quick Setup - Forms in code

The easiest way to add Forms to your project is to use the `GumService` class.

The following code snippet shows how to initialize Forms and Gum, and how to add a single Button to your project:

```csharp
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

        var stackPanel = new StackPanel();
        stackPanel.AddToRoot();


        var button = new Button();
        stackPanel.AddChild(button);
        stackPanel.X = 50;
        stackPanel.Y = 50;
        
        button.Width = 100;
        button.Height = 50;
        button.Text = "Hello MonoGame!";
        int clickCount = 0;
        button.Click += (_, _) =>
        {
            clickCount++;
            button.Text = $"Clicked {clickCount} times";
        };
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

The code above produces a single button which can be clicked to increment the click count.

<figure><img src="../../../.gitbook/assets/13_08 49 14.gif" alt=""><figcaption><p>Gum button reacting to clicks by incrementing click count</p></figcaption></figure>

### Quick Setup - Forms in the Gum Tool

Forms can be used with the Gum tool, allowing you to create fully functional UI layouts visually.

To use Forms in a project that loads a Gum project (.gumx) the following steps are needed:

1. Components must be added to the Gum project which have the necessary behaviors to be used as Forms visuals
2. The Gum project must be loaded in code, just like any other Gum project

#### Adding Forms Components to the Gum Project

To add components which are used for Forms visuals:

1. Open your project in Gum, or create a new project
   1. If creating a new project, save the project in a subfolder of your game's Content folder
2.  Select Content -> **Add Forms Components**\\

    <figure><img src="../../../.gitbook/assets/17_13 25 25.png" alt=""><figcaption><p>Menu item to add Forms Components</p></figcaption></figure>
3. Check the option to include DemoScreenGum, or alternatively add a new screen to your project and drag+drop the desired components into your screen

Once you have saved your project, modify your Gum file to include the project:

1. Open your .csproj in a text editor (or click on it in Visual Studio)
2. Add the wildcard addition code to copy all of your Gum projects

```markup
  <ItemGroup>
    <None Update="Content\GumProject\**\*.*">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
```

Your project is now referenced in your game. Modify the Game file to initialize the Gum systems and to load the .gumx project. Your code might look like this:

```csharp
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
        var gumProject = GumUI.Initialize(
            this,
            // This is relative to Content:
            "GumProject/GumProject.gumx");

        // This assumes that your project has at least 1 screen
        var screen = gumProject.Screens.First().ToGraphicalUiElement();
        screen.AddToRoot();

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        GumUI.Update(this, gameTime);
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
