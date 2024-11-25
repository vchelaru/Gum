# Multiple Screens

### Introduction

So far we've been working with a single Screen. This tutorial covers how to work with multiple screens, including how to add and destroy screens in response to UI events.

### Creating Multiple Screens

Before we write any code, we'll create two screens. A real game might have screens like a TitleScreen, OptionsScreen, GameScreen (which includes hud and a pause menu), and a GameOverScreen. For this tutorial we'll create two simple screens, each with a single button and a Text.

<figure><img src="../../../.gitbook/assets/image (118).png" alt=""><figcaption><p>Screen1 with a Text and Button</p></figcaption></figure>

<figure><img src="../../../.gitbook/assets/image (119).png" alt=""><figcaption><p>Screen2 with a Text and Button</p></figcaption></figure>

### Modifying Game1

Next we'll make the following modifications to Game1:

* Change the first screen to Screen1
* Make the Root property `public static` so that it can be accessed by the Screens

A full game may keep the Root in a dedicated object which provides access to the Screens, but we're making it `public static` to keep the tutorial simple.

```csharp
public class Game1 : Game
{
    ...
    public static GraphicalUiElement Root;
    ...
    protected override void Initialize()
    {
        ...
        var screen = gumProject.Screens.Find(item => item.Name == "Screen1");
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
            Game1.Root.RemoveFromManagers();
            var screen = ObjectFinder.Self.GumProjectSave.Screens.Find(
                item => item.Name == "Screen2");
            Game1.Root = screen.ToGraphicalUiElement(
                RenderingLibrary.SystemManagers.Default, addToManagers: true);
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
            Game1.Root.RemoveFromManagers();
            var screen = ObjectFinder.Self.GumProjectSave.Screens.Find(
                item => item.Name == "Screen1");
            Game1.Root = screen.ToGraphicalUiElement(
                RenderingLibrary.SystemManagers.Default, addToManagers: true);
        };

    }
}
```

Each screen removes itself from managers when its button is clicked, then creates and adds the next screen to managers.

<figure><img src="../../../.gitbook/assets/24_18 29 52.gif" alt=""><figcaption><p>The Go to Screen button destroys the current Screen and shows the next Screen</p></figcaption></figure>

### Conclusion

This tutorial showed how to switch between two screens by removing the old screen with RemoveFromManagers and creating a new screen with ToGraphicalUiElement.
