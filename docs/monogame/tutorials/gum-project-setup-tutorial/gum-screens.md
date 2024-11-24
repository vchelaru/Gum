# Gum Screens

### Introduction

Gum screens are _top level_ items which can contain instances of Gum objects. We'll be creating our first Gum screen in this tutorial. We'll also load this screen in code and work with gum objects.

### Creating a Screen in the Gum Tool

To add a new Screen:

1. Open the project in the Gum tool
2.  Right-click on the Screens folder and select Add Screen

    <figure><img src="../../../.gitbook/assets/image (99).png" alt=""><figcaption><p>Add Screen right-click item</p></figcaption></figure>


3.  Name the screen TitleScreen and click OK

    <figure><img src="../../../.gitbook/assets/image (100).png" alt=""><figcaption><p>Enter the new screen name and click OK</p></figcaption></figure>



The newly created is now in the Screens folder.

<figure><img src="../../../.gitbook/assets/image (101).png" alt=""><figcaption><p>TitleScreen in Gum</p></figcaption></figure>

### Adding Instances

We can add instances to Gum Screen by drag+dropping the files onto the Game screen. For example, we can add a Text instance by dropping the Standard/Text onto TitleScreen.

<figure><img src="../../../.gitbook/assets/24_09 53 51.gif" alt=""><figcaption><p>Drag+drop Text instance onto TitleScreen to create a Text instance</p></figcaption></figure>

Instances can also be created by selecting the TitleScreen, then drag+dropping the item in the editor window.

<figure><img src="../../../.gitbook/assets/24_09 55 46.gif" alt=""><figcaption><p>Drag+drop a component on the wireframe editor to add it to the TitleScreen</p></figcaption></figure>

Be sure to select the TitleScreen first, then drag+drop. If you click the component instead, then it will be selected, so you must re-select the TitleScreen.

The Gum tool includes lots of functionality for creating and customizing UI. For a more complete tutorial covering the Gum tool, see the [Gum Tool Intro Tutorials](../../../intro-tutorials/). Feel free to spend some time creating your TitleScreen.

### Showing a Gum Screen in Your Game

To show the screen in game, modify the Initialize method as shown in the following snippet:

<pre class="language-csharp" data-line-numbers><code class="lang-csharp">protected override void Initialize()
{
    var gumProject = MonoGameGum.GumService.Default.Initialize(
        this.GraphicsDevice,
        // This is relative to Content:
        "GumProject/GumProject.gumx");

    // the Screens list contains all screens. Find the screen you want
    var screen = gumProject.Screens.Find(item => item.Name == "TitleScreen");
    // Calling GraphicalUiElement creates the visuals for the screen
<strong>    screen.ToGraphicalUiElement(
</strong><strong>        RenderingLibrary.SystemManagers.Default, addToManagers: true);
</strong>
    base.Initialize();
}
</code></pre>

The game now displays the Gum screen.

<figure><img src="../../../.gitbook/assets/image (103).png" alt=""><figcaption><p>Gum Screen loaded and displayed in a MonoGame project</p></figcaption></figure>

### Keeping a Screen Reference (Root)

Games usually need to interact with Gum screens in code. By convention the Screen is stored in a variable named `Root`. We can modify the Game project by:

1. Adding a Root member to Game
2. Assigning Root when calling ToGraphicalUiElement
3. Adding the Root to the Update call

The modified code is shown below in the following code snippet:

```csharp
public class Game1 : Game
{
    ...
    GraphicalUiElement Root;
    ...
    protected override void Initialize()
    {
        ...
        Root = screen.ToGraphicalUiElement(RenderingLibrary.SystemManagers.Default, addToManagers: true);
        ...
    }

    protected override void Update(GameTime gameTime)
    {
        MonoGameGum.GumService.Default.Update(this, gameTime, Root);
        ...
    }
```

Notice that we've added the Root as a parameter to the Update call. By doing this, we get the built-in behavior for Forms control such as Button.

<figure><img src="../../../.gitbook/assets/24_10 16 55.gif" alt=""><figcaption><p>Button with built-in highlight and click styling</p></figcaption></figure>

{% hint style="info" %}
This tutorial uses Game1 as the container for all Gum members and logic. You may want to move this code into other classes to fit the rest of your game's code structure.
{% endhint %}

### Accessing Instances in Code

Now that we have our screen stored in the Root object, we can access objects.

We can modify the displayed string by getting an instance of the Text and modifying its properties as shown in the following code:

```csharp
protected override void Initialize()
{
    ...
    var textInstance = Root.GetGraphicalUiElementByName("TextInstance")
        as TextRuntime;
    textInstance.Text = "I am set in code";
    ...
}

```

<figure><img src="../../../.gitbook/assets/image (104).png" alt=""><figcaption><p>Text modified in code</p></figcaption></figure>

The code above casts TextInstance to a TextRuntime. Each standard type in Gum (such as Text, Sprite, and Container) has a corresponding _runtime_ type (such as TextRuntime, SpriteRuntime, and ContainerRuntime). Therefore, if we wanted to interact with a Sprite in code, we would cast it to a SpriteRuntime.

We can also interact with Forms objects in code. The base type for all Forms objects is FrameworkElement, so we can use the GetFrameworkElementByName extension method as shown in the following code:

```csharp
protected override void Initialize()
{
    ...
    var button = Root.GetFrameworkElementByName<Button>("ButtonStandardInstance");
    button.Click += (_, _) => textInstance.Text = "Button clicked at " + DateTime.Now;
    ...
}

```

<figure><img src="../../../.gitbook/assets/24_11 04 23.gif" alt=""><figcaption><p>Clicking on the Button changes the Text</p></figcaption></figure>

Notice that the code above uses the GetFrameworkElementByName. This code returns an instance of a FrameworkElement (Forms instance). As we'll cover in the next tutorial, only some Components can be used as Forms instances.

### Conclusion

This tutorial showed how to load a Gum screen in code and how to interact with objects. The next tutorial discusses Forms controls and explains the difference between GraphicalUielements and Forms controls.
