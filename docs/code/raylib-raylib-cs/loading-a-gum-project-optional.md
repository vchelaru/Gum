# Loading a Gum Project (Optional)

## Introduction

Gum projects can be loaded in a game project. Gum projects are made up of multiple files including:

* .gumx - the main Gum project
* .gusx - Gum screen files
* .gucx - Gum component files
* .gutx - Gum standard element files
* .png - image files

{% hint style="info" %}
You are not required to use the Gum tool or .gumx projects - you are free to do everything in code if you prefer. Of course using the Gum tool can make it much easier to iterate quickly and experiment so its use is recommended.
{% endhint %}

## Creating a Gum Project

Before creating a Gum project, it is recommended that you already have a functional raylib-cs project. It's best to put your Gum project in its own folder, such as a subfolder of the `resources` folder, so that it stays organized with your other of your content files. Remember, Gum creates lots of files.

<figure><img src="../../.gitbook/assets/18_08 27 17.png" alt=""><figcaption><p>Gum project in a raylib game</p></figcaption></figure>

If you haven't already downloaded it, you should download the Gum tool. See the [Introduction page](https://docs.flatredball.com/gum#downloading-gum) for information on downloading Gum.

To create a Gum project:

1. Open the Gum tool
2. Select File->New Project
3. Navigate to the desired subfolder (such as resources/GumProject/) to select a location for the project

## Adding the Gum Project Files to Your .csproj

To add the files to your .csproj:

1. Open your .csproj file in a text editor
2. Add a line to copy all files in the Gum project folder including the .gumx file itself. For example, your csproj might look like this:

```xml
<ItemGroup>
    <None Update="resources\GumProject\**\*.*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

If using Visual Studio, you can verify that files are set to copy by selecting a random file in your Gum project and looking at the properties to see if it is marked as copied.

<figure><img src="../../.gitbook/assets/18_09 00 16.png" alt=""><figcaption></figcaption></figure>

## Loading a Gum Project

To load a Gum Project:

1. Make sure that your Gum project has at least one Screen
2. Open your file that has your Gum initialization code, such as Project.cs
3. Modify the Initialize method by passing it a Gum project

```csharp
GumUI.Initialize(
    "resources/GumProject/raylibGumProject.gumx");

var screen = ObjectFinder.Self.GumProjectSave.Screens
    .FirstOrDefault()
    .ToGraphicalUiElement();

if(screen == null)
{
    throw new Exception(
        "No screen found in the Gum project, did you add a Screen in the Gum tool?");
}
screen.AddToRoot();

```

The code above loads the Gum project using the file path `"resources/GumProject/raylibGumProject.gumx"`. By default this path is relative to your game's exe folder.

## ToGraphicalUiElement

Once a Gum project is loaded, all of its screens and components can be accessed through the object returned from the Initialize method. The gum project can be obtained by accessing `ObjectFinder.Self.GumProjectSave`. Any screen or component can be converted to a GraphicalUiElement, which is the visual object that displays in game.

The code in the previous section creates a `GraphicalUiElement` from the first screen in the project.

Calling AddToRoot adds the screen to the root Gum object and makes a fully-functional Gum screen, including any Forms instances, such as instances of the Button type.

## Working With Forms Controls

Gum projects with Forms controls can be used in raylib projects. To add Forms controls to your project:

1. Open your Gum project in the Gum tool
2. Select the Content -> Add Forms Components menu. This adds Forms components to the Components folder.
3. Drag+drop controls into your screen. As a test, add at least one of the following:
   1. ButtonStandard
   2. CheckBox
   3. ComboBox
   4. ListBox
   5. Slider

<figure><img src="../../.gitbook/assets/09_17 31 56.png" alt=""><figcaption><p>Forms controls in Gum tool</p></figcaption></figure>

These controls can be interacted with. To do this, add the following code to your project after creating the first screen:

<pre class="language-csharp"><code class="lang-csharp">GumUI.Initialize(
    "resources/GumProject/gumProject.gumx");

var screen = ObjectFinder.Self.GumProjectSave.Screens
    .FirstOrDefault()
    .ToGraphicalUiElement();

if(screen == null)
{
    throw new Exception(
        "No screen found in the Gum project, did you add a Screen in the Gum tool?");
}
screen.AddToRoot();


<strong>var button = screen.GetFrameworkElementByName&#x3C;Button>("ButtonStandardInstance");
</strong><strong>button.Click += (_, _) => button.Text = $"Clicked at {DateTime.Now}";
</strong><strong>
</strong><strong>var checkBox = screen.GetFrameworkElementByName&#x3C;CheckBox>("CheckBoxInstance");
</strong><strong>checkBox.Text = "Button Visible";
</strong><strong>checkBox.Click += (_, _) => button.IsVisible = checkBox.IsChecked == true;
</strong><strong>
</strong><strong>var comboBox = screen.GetFrameworkElementByName&#x3C;ComboBox>("ComboBoxInstance");
</strong><strong>for(int i = 0; i &#x3C; 10; i++)
</strong><strong>{
</strong><strong>    comboBox.Items.Add(i);
</strong><strong>}
</strong><strong>
</strong><strong>var listBox = screen.GetFrameworkElementByName&#x3C;ListBox>("ListBoxInstance");
</strong><strong>for(int i = 0; i &#x3C; 20; i++)
</strong><strong>{
</strong><strong>    listBox.Items.Add($"Item {i}");
</strong><strong>}
</strong><strong>
</strong><strong>var slider = screen.GetFrameworkElementByName&#x3C;Slider>("SliderInstance");
</strong><strong>slider.Minimum = 0;
</strong><strong>slider.Maximum = 100;
</strong><strong>slider.ValueChanged += (_, _) =>
</strong><strong>{
</strong><strong>    listBox.X = (float)slider.Value;
</strong><strong>};
</strong>
</code></pre>
