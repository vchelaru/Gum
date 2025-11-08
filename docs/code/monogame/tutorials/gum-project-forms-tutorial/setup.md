# Setup

## Introduction

This tutorial walks you through creating a brand new Gum project and adding it to an existing MonoGame project.

This tutorial covers:

* Adding Gum NuGet packages
* Creating a new Gum project using the Gum tool
* Modifying the game .csproj to include all Gum files
* Adding Forms Components
* Loading the Gum project in your game

This tutorial presents the minimum amount of code necessary to work with Gum. You may need to adapt the code to fit in your game project.

## Adding Gum NuGet Packages

Before writing any code, we must add the Gum NuGet package. Add the `Gum.MonoGame` package to your game. For more information see the [Setup page](../../setup/).

Once you are finished, your game project should reference the `Gum.MonoGame` project.

<figure><img src="../../../../.gitbook/assets/NuGetGum (1).png" alt=""><figcaption><p>Gum.MonoGame NuGet package</p></figcaption></figure>

## Creating a new Gum Project

Next we'll create a project in the Gum UI tool. If you have not yet run the Gum tool, you can get setup instructions in the Gum [Setup page](../../../../gum-tool/setup/).

Once you have the tool downloaded, run it. You should have an empty project.

<figure><img src="../../../../.gitbook/assets/image (176) (1).png" alt=""><figcaption><p>Empty Gum Project</p></figcaption></figure>

We need to save our Gum project in the Content folder of our game. Gum projects include many files. it's best to keep a Gum project and all of its files in a dedicated folder.

Add a new folder to your Game's Content folder which will contain the Gum project, such as GumProject.

<figure><img src="../../../../.gitbook/assets/image (177) (1).png" alt=""><figcaption><p>GumProject folder in Visual Studio</p></figcaption></figure>

In the Gum tool click File -> Save Project.

<figure><img src="../../../../.gitbook/assets/image (178) (1).png" alt=""><figcaption><p>File -> Save project menu item</p></figcaption></figure>

Select the GumProject folder created earlier as the target location. Usually the save location is an empty folder inside your game's Content folder. Give your Gum project a name such as GumProject.

<figure><img src="../../../../.gitbook/assets/image (179) (1).png" alt=""><figcaption><p>Save GumProject in the newly-created GumProject folder</p></figcaption></figure>

After your project is saved it should appear in Visual Studio.

<figure><img src="../../../../.gitbook/assets/image (180) (1).png" alt=""><figcaption><p>Gum project in Visual Studio</p></figcaption></figure>

Next, add default Forms components to the Gum project. Forms components are premade components for standard UI elements such as Button, TextBox, and ListBox. We'll use these components in later tutorials.

To add Gum Forms components in Gum, select **Content** -> **Add Forms Components**.

<figure><img src="../../../../.gitbook/assets/AddForms (1).png" alt=""><figcaption><p>Add Forms Components menu item</p></figcaption></figure>

Later tutorials will reference the demo screen, so check the **Include DemoScreenGum** option and click **OK**. Don't worry, you can delete this screen later as you develop your game.

<figure><img src="../../../../.gitbook/assets/02_06 52 11.png" alt=""><figcaption><p>Include DemoScreenGum</p></figcaption></figure>

If asked, click **Yes** when asked about overwriting the default standards.

<figure><img src="../../../../.gitbook/assets/image (10) (1).png" alt=""><figcaption><p>Click, Yes to modify standards with the default Forms styling</p></figcaption></figure>

Your project now includes Forms components.

<figure><img src="../../../../.gitbook/assets/Components (1).png" alt=""><figcaption><p>Forms Components in Gum</p></figcaption></figure>

## Modifying the Game .csproj

Now that we have our Gum project created, we can load it in our game.

{% tabs %}
{% tab title="Visual Studio" %}
First, we'll set up our project so all Gum files are copied when the project is built. To do this:

1. Right-click on any Gum file in your project, such as GumProject.gumx
2.  Select the Properties item\\

    <figure><img src="../../../../.gitbook/assets/image (11) (1).png" alt=""><figcaption><p>Properties right click option</p></figcaption></figure>
3.  Set the file to Copy if Newer. If using Android, see instructions below.\\

    <figure><img src="../../../../.gitbook/assets/image (12) (1).png" alt=""><figcaption><p>Mark the Gum file as Copy if newer</p></figcaption></figure>
4.  Double click your game's csproj file to open it in the text editor and find the entry for the file that you marked as Copy if newer.\\

    <figure><img src="../../../../.gitbook/assets/image (13) (2).png" alt=""><figcaption><p>Entry for GumProject.gumx in the csproj file.</p></figcaption></figure>
5.  Modify the code to use a wildcard for all files in the Gum project. In other words, change `Content\GumProject\GumProject.gumx` to `Content\GumProject\**\*.*`\\

    <figure><img src="../../../../.gitbook/assets/image (14) (1).png" alt=""><figcaption><p>Wildcard entry for all files in the GumProject folder</p></figcaption></figure>

    Now all files in your Gum project will be copied to the output folder whenever your project is built, including any files added later as you continue working in Gum.\\

    <figure><img src="../../../../.gitbook/assets/image (15) (2).png" alt=""><figcaption><p>All Gum files automatically are marked as Copy if newer.</p></figcaption></figure>
{% endtab %}

{% tab title="Visual Studio Code" %}
First, we'll set up our project so all Gum files are copied when the project is built. To do this:

1. Open your game's .cproj in Visual Studio Code to edit the text
2. Find an ItemGroup where you are loading other content. If you do not have one, you can create a new one (see below).
3. Add an entry to copy all Gum files to the output directory. Use wildcards to include all Gum files including the main Gum project, Screens, Components, Standard Elements, fonts, and any other referenced files.

For example, you may add something like this to your .csproj:

```xml
<ItemGroup>
    <None Update="Content\GumProject\**\*.*">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

Notice that the folder includes the root of the Gum folder. You may need to adjust this path according to where your .gumx is located.
{% endtab %}
{% endtabs %}

{% hint style="info" %}
At the time of this writing, Gum does not use the MonoGame Content Builder to build XNBs for any of its files. This means that referenced image files (.png) will also be copied to the output folder.

As you build your Gum project it's best to keep all referenced PNGs inside your Gum folder to keep it portable.

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

{% tabs %}
{% tab title="Full Code" %}
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
        var gumProject = GumUI.Initialize(this,
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
{% endtab %}

{% tab title="Diff" %}
```diff
using MonoGameGum;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
+    GumService GumUI => GumService.Default;
    
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
+        var gumProject = GumUI.Initialize(this,
+            // This is relative to Content:
+            "GumProject/GumProject.gumx");

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
+        GumUI.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
+        GumUI.Draw();
        base.Draw(gameTime);
    }
}
```
{% endtab %}
{% endtabs %}

The code above has the following three calls on Gum:

* Initialize - this loads the argument Gum project and sets appropriate defaults. Note that we are loading a Gum project here, but the gum project is optional. Projects which are using Gum only in code would not pass the second parameter.

```csharp
        var gumProject = GumUI.Initialize(this,
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

<figure><img src="../../../../.gitbook/assets/image (16) (1).png" alt=""><figcaption><p>Empty MonoGame project</p></figcaption></figure>

The next tutorial adds our first screen.
