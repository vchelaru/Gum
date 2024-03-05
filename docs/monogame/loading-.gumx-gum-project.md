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
2.  Add a line to copy all files in the Gum project folder including the .gumx file itself. For an example, see the .csproj file for the MonoGameGumFromFile project: [https://github.com/vchelaru/Gum/blob/8cde3f76d00cf14c00d68c1aaa4713b9f75e702f/Samples/MonoGameGumFromFile/MonoGameGumFromFile/MonoGameGumFromFile.csproj#L37](https://github.com/vchelaru/Gum/blob/8cde3f76d00cf14c00d68c1aaa4713b9f75e702f/Samples/MonoGameGumFromFile/MonoGameGumFromFile/MonoGameGumFromFile.csproj#L37)\
    Your .csproj may look like this:\


    <figure><img src="../.gitbook/assets/image (50).png" alt=""><figcaption><p>Copy all files to output</p></figcaption></figure>
3.  Verify that all gum files (see the extension list above) are marked as Copy if newer in Visual Studio\


    <figure><img src="../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Gum project set to Copy if newer</p></figcaption></figure>

The example above copies the entirety of the content folder to the output folder by using wildcards. If you do not want every file copied over, you can be more selective in what you copy by including only certain file types. For more information about wildcard support in .csproj files, see this page on how to include wildcards in your .csproj:

[https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-select-the-files-to-build?view=vs-2022#specify-inputs-with-wildcards](https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-select-the-files-to-build?view=vs-2022#specify-inputs-with-wildcards)

### Loading a Gum projecxt

To load a Gum Project:

1. Open Game1.cs
2. Modify the Initialize method so that it has the following lines **after** initializing SystemManagers:

```csharp
var gumProject = GumProjectSave.Load("GumProject.gumx", out GumLoadResult result);
ObjectFinder.Self.GumProjectSave = gumProject;
gumProject.Initialize();

// This assumes that your project has at least 1 screen
gumProject.Screens.First().ToGraphicalUiElement(SystemManagers.Default, addToManagers:true);
```

For an example of a Game1.cs file which loads a project file, see the MonoGameGumFromFile: [https://github.com/vchelaru/Gum/blob/8cde3f76d00cf14c00d68c1aaa4713b9f75e702f/Samples/MonoGameGumFromFile/MonoGameGumFromFile/Game1.cs#L33C1-L37C105](https://github.com/vchelaru/Gum/blob/8cde3f76d00cf14c00d68c1aaa4713b9f75e702f/Samples/MonoGameGumFromFile/MonoGameGumFromFile/Game1.cs#L33C1-L37C105)

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

### Troubleshooting Gum Project Loading

If your Gum project loads incorrectly, you can inspect the GumLoadResult object that is returned from the Load method to see what might be wrong. To inspect this object, you can place a breakpoint after your Load call and look at the `result` object as shown in the following screenshot.

<figure><img src="../.gitbook/assets/image (43).png" alt=""><figcaption><p>Viewing the GumLoadResult in the output window</p></figcaption></figure>

In this case the GumLoadResult is indicating no errors and no missing files.

You may see information about missing files if any files are not found during the loading process. For example, the following screenshot shows that a Component file (.gucx) is missing:

<figure><img src="../.gitbook/assets/image (48).png" alt=""><figcaption><p>Missing component "CardInstance.gucx"</p></figcaption></figure>

If you are missing files, you may have not set up the file to copy to the output folder. The following screenshot shows an incorrect setup - the file is not copied:

<figure><img src="../.gitbook/assets/image (49).png" alt=""><figcaption><p>Incorrect setting on .gucx</p></figcaption></figure>

This can be changed to copy in Visual Studio, or the .csproj can be modified to include wildcards for copying files over, which can make maintenance easier as the project grows.
