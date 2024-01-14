# Loading .gumx (Gum Project)

Gum projects can be loaded in a game project. Gum projects are made up of multiple projects including:

* .gumx - the main Gum project
* .gusx - Gum screen files
* .gucx - Gum component files
* .gutx - Gum standard element files
* .png - image files
* .fnt - font files

### Creating a Gum Project

Before creating a Gum project, it is recommended that you already have a functional MonoGame project with a Content folder.

To create a Gum project:

1. Open the gum tool
2. Select File->Save Project
3. Navigate to the Content folder of your game. If desired, create a sub-folder under the Content folder
4. Save the project - this will save multiple files under the Content folder

### Adding the Gum Project Files to your .csproj

To add the files to your .csproj:

1. Open your .csproj file in a text editor
2. Add a line to copy all files in the Gum project folder including the .gumx file itself. For an example, see the .csproj file for the MonoGameGumFromFile project: [https://github.com/vchelaru/Gum/blob/8cde3f76d00cf14c00d68c1aaa4713b9f75e702f/Samples/MonoGameGumFromFile/MonoGameGumFromFile/MonoGameGumFromFile.csproj#L37](https://github.com/vchelaru/Gum/blob/8cde3f76d00cf14c00d68c1aaa4713b9f75e702f/Samples/MonoGameGumFromFile/MonoGameGumFromFile/MonoGameGumFromFile.csproj#L37)
3.  Verify that all gum files (see the extension list above) are marked as Copy if newer in Visual Studio\


    <figure><img src="../.gitbook/assets/image (1) (1).png" alt=""><figcaption><p>Gum project set to Copy if newer</p></figcaption></figure>

### Loading a Gum projecxt

To load a Gum Project:

1. Open Game1.cs
2. Modify the Initialize method so that it has the following lines **after** initializing SystemManagers:

```csharp
var gumProject = GumProjectSave.Load("GumProject.gumx", out _);
ObjectFinder.Self.GumProjectSave = gumProject;
gumProject.Initialize();

// This assumes that your project has at least 1 screen
gumProject.Screens.First().ToGraphicalUiElement(SystemManagers.Default, addToManagers:true);
```

For an example of a Game1.cs file which loads a project file, see the MonoGameGumFromFile: [https://github.com/vchelaru/Gum/blob/8cde3f76d00cf14c00d68c1aaa4713b9f75e702f/Samples/MonoGameGumFromFile/MonoGameGumFromFile/Game1.cs#L33C1-L37C105](https://github.com/vchelaru/Gum/blob/8cde3f76d00cf14c00d68c1aaa4713b9f75e702f/Samples/MonoGameGumFromFile/MonoGameGumFromFile/Game1.cs#L33C1-L37C105)

Note that calling ToGraphicalUiElement creates a GraphicalUiElement (Gum object) from the first screen. You can inspect the gumProject.Screens file and select which screen you would like to create if your project has mutliple Screens.
