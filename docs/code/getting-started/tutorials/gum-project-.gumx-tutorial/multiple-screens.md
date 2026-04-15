# Multiple Screens

{% hint style="danger" %}
This tutorial series represents the old way to add a .gumx project to your MonoGame project. This tutorial was retired in April 2025, replaced by the new [Gum Project Forms Tutorial](../gum-project-forms-tutorial/).

This tutorial is still syntactically valid but it is not recommended as of the April 2025 release:

[https://github.com/vchelaru/Gum/releases/tag/Release\_April\_27\_2025](https://github.com/vchelaru/Gum/releases/tag/Release_April_27_2025)
{% endhint %}

### Introduction

So far we've been working with a single Screen. This tutorial covers how to work with multiple screens, including how to add and destroy screens in response to UI events.

{% hint style="info" %}
This tutorial changes the screen which is loaded, so you must remove the code which accesses any instances in your Game class, such as `textInstance`, `button`, or `listBox`.

This tutorial assumes that you still have a Gum project and that you have set up your Game class to include the necessary Initialize, Draw, and Update calls.

This tutorial also assumes that you have set up code generation for your project.

If you would like a simpler starting point, feel free to delete all content in your TitleScreen in Gum, and feel free to delete all code aside from the bare minimum for your project. Be sure to keep your Root object as we'll be using that in this tutorial.

For a full example of what your Game code might look like, see the start of the [Gum Forms](/broken/pages/eV3NKAEskUjucqKpK8kt#introduction) tutorial.
{% endhint %}

### Creating Multiple Screens

Before we write any code, we'll create two screens. A real game might have screens like a TitleScreen, OptionsScreen, GameScreen (which includes HUD and a pause menu), and a GameOverScreen. For this tutorial we'll create two simple screens, each with a single button and a Text.

First we'll create Screen1:

1. Add a new screen named Screen1
2. Drag+drop a Text standard element into Screen1
3. Set the Text property on the newly created TextInstance to **Screen 1** so that you can tell that you are on Screen1 when it is active in your game
4. Drag+drop a ButtonStandard component into Screen1
5. Set the Text property on the newly created ButtonStandardInstance to **Go to Screen 2**
6. Arrange the two instances so they are not overlapping

<figure><img src="../../../../.gitbook/assets/image (118).png" alt=""><figcaption><p>Screen1 with a Text and Button</p></figcaption></figure>

Next we'll create Screen2:

1. Add a new Screen named Screen2
2. Drag+drop a Text standard element into Screen2
3. Set the Text property on the newly created TextInstance to **Screen 2** so that you can tell that you are in Screen2 when it is active in your game
4. Drag+drop a ButtonStandard component into Screen2
5. Set the Text property on the newly created ButtonStandardInstance to **Go to Screen 1**
6. Arrange the two instances so they are not overlapping

<figure><img src="../../../../.gitbook/assets/image (119).png" alt=""><figcaption><p>Screen2 with a Text and Button</p></figcaption></figure>

### Modifying Game1

Next we'll make the following modifications to Game1:

* Change the first screen to Screen1
* Make the Root property `public static` so that it can be accessed by the Screens

{% hint style="info" %}
A full game may keep the Root in a dedicated object which provides access to the Screens, but we're making it `public static` to keep the tutorial simple.
{% endhint %}

The first part of your Game1 class might look like the following code:

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

// Start of new code
        var screen = gumProject.Screens.Find(item => item.Name == "Screen1");
// End of new code

        var screenRuntime = screen.ToGraphicalUiElement();
        screenRuntime.AddToRoot();

        base.Initialize();
    }
    ...
```

### Modifying Screen1Runtime and Screen2Runtime

Now we can modify both Screen1Runtime.cs and Screen2Runtime.cs to include code that links from one to the other as shown in the following code:

```csharp
partial class Screen1Runtime : Gum.Wireframe.GraphicalUiElement
{
    partial void CustomInitialize()
    {
        this.GetFrameworkElementByName<Button>("ButtonStandardInstance").Click += (_, _) =>
        {
            GumService.Default.Root.Children.Clear();
            var screen = ObjectFinder.Self.GumProjectSave.Screens.Find(
                item => item.Name == "Screen2");
            var screenRuntime = screen.ToGraphicalUiElement();
            screenRuntime.AddToRoot();
        };
    }
}
```

```csharp
partial class Screen2Runtime : Gum.Wireframe.GraphicalUiElement
{
    partial void CustomInitialize()
    {
        this.GetFrameworkElementByName<Button>("ButtonStandardInstance").Click += (_, _) =>
        {
            GumService.Default.Root.Children.Clear();
            var screen = ObjectFinder.Self.GumProjectSave.Screens.Find(
                item => item.Name == "Screen1");
            var screenRuntime = screen.ToGraphicalUiElement();
            screenRuntime.AddToRoot();
        };

    }
}
```

Each screen removes itself from managers when its button is clicked, then creates and adds the next screen to managers.

<figure><img src="../../../../.gitbook/assets/24_18 29 52.gif" alt=""><figcaption><p>The Go to Screen button destroys the current Screen and shows the next Screen</p></figcaption></figure>

### Showing No Screen

This tutorial assumes that a Gum screen is always displayed.

Games can also completely remove Gum screens altogether. To do this, do not create a new screen after calling `GumService.Default.Root.Clear();`

### Conclusion

This tutorial showed how to switch between two screens by removing the old screen with RemoveFromManagers and creating a new screen with ToGraphicalUiElement.
