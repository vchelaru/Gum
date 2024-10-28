# Gum Forms

### Introduction

Gum Forms is a set of classes which provide fully-functional forms controls which you can use in your game. Examples of forms controls include:

* Button
* Checkbox
* ListBox
* TextBox
* Slider

Gum Forms can be used purely in code (no .gumx project required), or you can fully style your forms objects in a Gum project.&#x20;

### Quick Setup

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
        SystemManagers.Default = new SystemManagers(); 
        SystemManagers.Default.Initialize(_graphics.GraphicsDevice, fullInstantiation: true);
        FormsUtilities.InitializeDefaults();

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
        if(IsActive)
        {
            FormsUtilities.Update(gameTime, Root);
        }
        
        SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        SystemManagers.Default.Draw();
        base.Draw(gameTime);
    }
}

```

The code above produces a single button which can be clicked to increment the click count.

<figure><img src="../../.gitbook/assets/24_06 36 41.gif" alt=""><figcaption><p>Gum button reacting to clicks by incrementing click count</p></figcaption></figure>

### Gum Forms and the Gum Tool

A fully-functional Gum project can be downloaded which includes components for all Gum Form types. This can be found in the Samples folder of the repository. You can use the Gum project located there to load Gum Forms into your own project, or you can run the sample to see how it works. For more information see the [Samples page](../samples.md).

