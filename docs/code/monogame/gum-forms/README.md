# Gum Forms

### Introduction

Gum Forms is a set of classes which provide fully-functional forms controls which you can use in your game. Examples of forms controls include:

* Button
* Checkbox
* ListBox
* TextBox
* Slider

Gum Forms can be used purely in code (no .gumx project required), or you can fully style your forms objects in a Gum project.&#x20;

### Quick Setup - Forms in code

The easiest way to add Forms to your project is to use the `FormsUtilities` class. Keep in mind that Forms is not a replacement for Gum; rather, it adds objects on top of Gum which provide common UI interaction. In other words, if you are using Forms, you are still using Gum as well. Therefore, when using Forms you must also initialize Gum.

The following code snippet shows how to initialize Forms and Gum, and how to add a single Button to your project:

```csharp
public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;

    // Gum renders and updates using a hierarchy. At least
    // one object must have its AddToManagers method called.
    // If not loading from-file, then the easiest way to do this
    // is to create a ContainerRuntime and add it to the managers.
    ContainerRuntime Root;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        var gumProject = MonoGameGum.GumService.Default.Initialize(
            this);

        Root = new ContainerRuntime();
        Root.Width = 0;
        Root.Height = 0;
        Root.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        Root.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        Root.AddToManagers();


        var button = new Button();
        Root.Children.Add(button.Visual);
        button.X = 50;
        button.Y = 50;
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

The code above produces a single button which can be clicked to increment the click count.

<figure><img src="../../../.gitbook/assets/24_06 36 41.gif" alt=""><figcaption><p>Gum button reacting to clicks by incrementing click count</p></figcaption></figure>

### Quick Setup - Forms in the Gum Tool

Forms can be used with the Gum tool, allowing you to create fully functional UI layouts visually.

To use Forms in a project that loads a Gum project (.gumx) the following steps are needed:

1. Components must be added to the Gum project which have the necessary behaviors to be used as Forms visuals
2. The Gum project must be loaded in code, just like any other Gum project

#### Adding Forms Components to the Gum Project

To add components which are used for Forms visuals:

1. Open your project in Gum, or create a new project
   1. If creating a new project, save the project in a subfolder of your game's Content folder
2.  Select Content ->  **Add Forms Components**\


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

    // Gum renders and updates using a hierarchy. At least
    // one object must have its AddToManagers method called.
    // If not loading from-file, then the easiest way to do this
    // is to create a ContainerRuntime and add it to the managers.
    GraphicalUiElement Root;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        var gumProject = MonoGameGum.GumService.Default.Initialize(
            this,
            // This is relative to Content:
            "GumProject/GumProject.gumx");

        // This assumes that your project has at least 1 screen
        Root = gumProject.Screens.First().ToGraphicalUiElement(
            SystemManagers.Default, addToManagers: true);

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

