# Loading a Gum Project (Optional)

Gum projects can be loaded in a game project. Gum projects are made up of multiple filesincluding:

* .gumx - the main Gum project
* .gusx - Gum screen files
* .gucx - Gum component files
* .gutx - Gum standard element files
* .png - image files
* .fnt - font files

{% hint style="info" %}
You are not required to use the Gum tool or .gumx projects - you are free to do everything in code if you prefer. Of course using the Gum tool can make it much easier to iterate quickly and experiment so its use is recommended.
{% endhint %}

### Creating a Gum Project

Before creating a Gum project, it is recommended that you already have a functional MonoGame project with a Content folder.

If you haven't already downloaded it, you should down the Gum tool. See the [Introduction page](../../#downloading-gum) for information on downloading Gum.

To create a Gum project:

1. Open the gum tool
2. Select File->Save Project
3. Navigate to the Content folder of your game. If desired, create a sub-folder under the Content folder
4. Save the project - this will save multiple files under the Content folder

### Adding the Gum Project Files to Your .csproj

To add the files to your .csproj:

1. Open your .csproj file in a text editor
2.  Add a line to copy all files in the Gum project folder including the .gumx file itself. For an example, see the .csproj file for the MonoGameGumFromFile project: [https://github.com/vchelaru/Gum/blob/0e266942560e585359f019ac090a6c1010621c0b/Samples/MonoGameGumFromFile/MonoGameGumFromFile/MonoGameGumFromFile.csproj#L37](https://github.com/vchelaru/Gum/blob/0e266942560e585359f019ac090a6c1010621c0b/Samples/MonoGameGumFromFile/MonoGameGumFromFile/MonoGameGumFromFile.csproj#L37)\
    Your .csproj may look like this:\


    <figure><img src="../../.gitbook/assets/29_12 07 52.png" alt=""><figcaption><p>Copy all files to output</p></figcaption></figure>
3.  Verify that all gum files (see the extension list above) are marked as Copy if newer in Visual Studio\


    <figure><img src="../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Gum project set to Copy if newer</p></figcaption></figure>

The example above copies the entirety of the content folder to the output folder by using wildcards. If you do not want every file copied over, you can be more selective in what you copy by including only certain file types. For more information about wildcard support in .csproj files, see this page on how to include wildcards in your .csproj:

[https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-select-the-files-to-build?view=vs-2022#specify-inputs-with-wildcards](https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-select-the-files-to-build?view=vs-2022#specify-inputs-with-wildcards)

### Loading a Gum Project

To load a Gum Project:

1. Open Game1.cs
2. Modify the Initialize method so that it has the following lines **after** initializing SystemManagers:

```csharp
protected override void Initialize()
{
    var gumProject = MonoGameGum.GumService.Default.Initialize(
        this.GraphicsDevice, 
        "GumProject.gumx");

    // This assumes that your project has at least 1 screen
    gumProject.Screens.First().ToGraphicalUiElement(
        SystemManagers.Default, addToManagers:true);
    
    base.Initialize();
}




```

Note that the ToGraphicalUiElement method has an `addToManage` parameter which determines whether the GraphicalUiElement is added to managers. If a GraphicalUiElement is not added to managers, it will not appear on screen.

Alternatively, the AddToManagers method can be explicitly called as shown in the following code:

```csharp
var gumScreen = gumProject.Screens.First().ToGraphicalUiElement(
    SystemManagers.Default, addToManagers:true);

// AddToManagers provides more control, such as delaying addition, or adding 
// a GraphicalUiElement to a Layer
gumScreen.AddToManagers(SystemManagers.Default, layer:null);
```

For an example of a Game1.cs file which loads a project file, see the MonoGameGumFromFile: [https://github.com/vchelaru/Gum/blob/0e266942560e585359f019ac090a6c1010621c0b/Samples/MonoGameGumFromFile/MonoGameGumFromFile/Game1.cs#L76-L82](https://github.com/vchelaru/Gum/blob/0e266942560e585359f019ac090a6c1010621c0b/Samples/MonoGameGumFromFile/MonoGameGumFromFile/Game1.cs#L76-L82)

Note that calling ToGraphicalUiElement creates a [GraphicalUiElement](../gum-code-reference/graphicaluielement/) (Gum object) from the first screen. You can inspect the gumProject.Screens file and select which screen you would like to create if your project has mutliple Screens.

You can access elements within the screen by accessing the GraphicalUiElement that is created, as shown in the following code:

```csharp
// Load the gum project (see code above)
var screen = gumProject.Screens.First().ToGraphicalUiElement(
  SystemManagers.Default, 
  addToManagers:true);

// Items in the screen can be accessed using the GetGraphicalUiElementByName method:
var child = screen.GetGraphicalUiElementByName("TitleInstance");

// All GraphicalUiElements have common properties, like X:
child.X += 30;

// you can also set properties which may not be common to all GraphicalUiElements,
// like Text:
child.SetProperty("Text", "Hello world");
```

A full Game1 class which loads a project might look like this:

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
    }

    protected override void Initialize()
    {
        var gumProject = MonoGameGum.GumService.Default.Initialize(this.GraphicsDevice, "GumProject.gumx");
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

### Troubleshooting Gum Project Loading

If your Gum project load results in an exception, you can inspect the exception message for information about the failure. The most common type of failure is a missing file reference.

If you are missing files, you may have not set up the file to copy to the output folder. The following screenshot shows an incorrect setup - the CardInstance.gucx file is not copied, but it probably should be:

<figure><img src="../../.gitbook/assets/image (49).png" alt=""><figcaption><p>Incorrect setting on .gucx</p></figcaption></figure>

This can be changed to copy in Visual Studio, or the .csproj can be modified to include wildcards for copying files over, which can make maintenance easier as the project grows. See the section above for information and examples on setting up your project loading.
