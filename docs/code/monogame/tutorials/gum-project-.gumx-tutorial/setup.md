# Setup

{% hint style="danger" %}
This tutorial series represents the old way to add a .gumx project to your MonoGame project. This tutorial was  retired in April 2025, replaced by the new [Gum Project Forms Tutorial](../gum-project-forms-tutorial/).

This tutorial is still syntactically valid but it is not recommended as of the April 2025 release:

[https://github.com/vchelaru/Gum/releases/tag/Release\_April\_27\_2025](https://github.com/vchelaru/Gum/releases/tag/Release_April_27_2025)
{% endhint %}

## Introduction

This tutorial walks you through creating a brand new Gum project and adding it to an existing MonoGame project. The MonoGame project can be empty or it can be an existing game - the steps are the same either way.

This tutorial covers:

* Adding Gum NuGet packages
* Creating a new Gum project using the Gum tool
* Modifying the game .csproj to include all Gum files
* Loading the Gum project in your game

This tutorial presents the minimum amount of code necessary to work with Gum. You may need to adapt the code to fit in your game project.

## Adding Gum NuGet Packages

Before writing any code, we must add the Gum nuget package. Add the `Gum.MonoGame` package to your game. For more information see the [Setup page](../../setup/).

Once you are finished, your game project should reference the `Gum.MonoGame` project.

<figure><img src="../../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Gum.MonoGame NuGet package</p></figcaption></figure>

## Creating a new Gum Project

Next we'll create a project in the Gum UI tool. If you have not yet run the Gum tool, you can get setup instructions in the Gum [Setup page](../../../../gum-tool/setup/).

Once you have the tool downloaded, run it. You should have an empty project.

<figure><img src="../../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Empty Gum Project</p></figcaption></figure>

We need to save our Gum project in the Content folder of our game. Gum projects include many files. it's best to keep a Gum project and all of its files in a dedicated folder.

Add a new folder to your Game's Content folder which will contain the Gum project, such as GumProject.

<figure><img src="../../../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>GumProject folder in Visual Studio</p></figcaption></figure>

In the Gum tool click File -> Save Project.

<figure><img src="../../../../.gitbook/assets/image (3) (1) (1).png" alt=""><figcaption><p>File -> Save project menu item</p></figcaption></figure>

Select the GumProject folder created earlier as the target location. Give your Gum project a name such as GumProject.

<figure><img src="../../../../.gitbook/assets/image (4) (1) (1).png" alt=""><figcaption><p>Save GumProject in the newly-created GumProject folder</p></figcaption></figure>

After your project is saved it should appear in Visual Studio.

<figure><img src="../../../../.gitbook/assets/image (5) (1).png" alt=""><figcaption><p>Gum project in Visual Studio</p></figcaption></figure>

We can add default Forms components to our project. Forms components are premade components for standard UI elements such as Button, TextBox, and ListBox. We'll use these components in later tutorials.

To add Gum Forms components in Gum, select Content -> Add Forms Components

<figure><img src="../../../../.gitbook/assets/image (6) (1).png" alt=""><figcaption><p>Add Forms Components menu item</p></figcaption></figure>

This tutorial will not use the DemoScreenGum, so leave this option unchecked and press OK.

<figure><img src="../../../../.gitbook/assets/image (7) (1).png" alt=""><figcaption><p>Leave Include DemoScreenGum unchecked</p></figcaption></figure>

If asked, click **Yes** when asked about overwriting the default standards.

<figure><img src="../../../../.gitbook/assets/image (10).png" alt=""><figcaption><p>Click, Yes to modify standards with the default Forms styling</p></figcaption></figure>

Your project now includes Forms components.

<figure><img src="../../../../.gitbook/assets/image (8) (1).png" alt=""><figcaption><p>Forms Components in Gum</p></figcaption></figure>

## Modifying the Game .csproj

Now that we have our Gum project created, we can load it in our game.

First, we'll set up our project so all Gum files are copied when the project is built. To do this:

1. Right-click on any Gum file in your project, such as GumProject.gumx
2.  Select the Properties item\\

    <figure><img src="../../../../.gitbook/assets/image (11).png" alt=""><figcaption><p>Properties right click option</p></figcaption></figure>
3.  Set the file to Copy if Newer. If using Android, see instructions below.

    <figure><img src="../../../../.gitbook/assets/image (12).png" alt=""><figcaption><p>Mark the Gum file as Copy if newer</p></figcaption></figure>
4.  Double click your game's csproj file to open it in the text editor and find the entry for the file that you marked as Copy if newer.\\

    <figure><img src="../../../../.gitbook/assets/image (13).png" alt=""><figcaption><p>Entry for GumProject.gumx in the csproj file.</p></figcaption></figure>
5.  Modify the code to use a wildcard for all files in the Gum project. In other words, change `Content\GumProject\GumProject.gumx` to `Content\GumProject\**\*.*`\\

    <figure><img src="../../../../.gitbook/assets/image (14).png" alt=""><figcaption><p>Wildcard entry for all files in the GumProject folder</p></figcaption></figure>

Now all files in your Gum project will be copied to the output folder whenever your project is built, including any files added later as you continue working in Gum.

<figure><img src="../../../../.gitbook/assets/image (15).png" alt=""><figcaption><p>All Gum files automatically are marked as Copy if newer.</p></figcaption></figure>

{% hint style="info" %}
At the time of this writing, Gum does not use the MonoGame Content Builder to build XNBs for any of its files. This means that referenced image files (.png) will also be copied to the output folder.

Future versions may be expanded to support using either the .XNB file format or _raw_ PNGs.
{% endhint %}

### Android

If you are using Android, then your files must be marked as Android Assets rather than copied files.

The steps to do this are:

1. Open your project file
2. Find the entry for the Gum project if you followed the previous section which copies file using wildcard
3. Change it to an Android Asset. For example your code might look like this:

```xml
<AndroidAsset Include="Content\GumProject\**\*.*" />
```

## Loading the Gum Project

Now that we have a Gum project added to the .csproj, we can load the Gum project. We need to add code to Initialize, Update, and Draw. A simplified Game class with these calls would look like the following code:

```csharp
using MonoGameGum;

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

The code above has the following three calls on GumService:

* Initialize - this loads the argument Gum project and sets appropriate defaults. Note that we are loading a Gum project here, but the gum project is optional. Projects which are using Gum only in code would not pass the second parameter.

```csharp
        var gumProject = GumUI.Initialize(
            this,
            // This is relative to Content:
            "GumProject/GumProject.gumx");
```

* Update - this updates the internal keyboard, mouse, and gamepad instances and applies default behavior to any components which implement Forms. For example, if a Button is added to the Screen, this code is responsible for checking if the cursor is overlapping the Button and adjusting the highlight/pressed state appropriately.

```csharp
GumUI.Update(gameTime);
```

* Draw - this method draws all Gum objects to the screen. Currently this method does not perform any drawing, but in the next tutorial we'll be adding a Gum screen which is drawn in this method.

```csharp
GumUI.Draw();
```

## Conclusion

If you've followed along, your project is now a fully-functional Gum project. We haven't added any screens to the Gum project yet, so if you run the game you'll still see a blank (cornflower blue) screen.

<figure><img src="../../../../.gitbook/assets/image (16).png" alt=""><figcaption><p>Empty MonoGame project</p></figcaption></figure>

The next tutorial adds our first screen.
