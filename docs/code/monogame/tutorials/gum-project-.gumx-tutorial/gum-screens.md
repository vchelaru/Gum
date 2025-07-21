# Gum Screens

{% hint style="danger" %}
This tutorial series represents the old way to add a .gumx project to your MonoGame project. This tutorial was  retired in April 2025, replaced by the new [Gum Project Forms Tutorial](../gum-project-forms-tutorial/).

This tutorial is still syntactically valid but it is not recommended as of the April 2025 release:

[https://github.com/vchelaru/Gum/releases/tag/Release\_April\_27\_2025](https://github.com/vchelaru/Gum/releases/tag/Release_April_27_2025)
{% endhint %}

## Introduction

Gum screens are _top level_ items which can contain instances of Gum objects. We'll be creating our first Gum screen in this tutorial. We'll also load this screen in code and work with gum objects.

## Creating a Screen in the Gum Tool

To add a new Screen:

1. Open the project in the Gum tool
2.  Right-click on the Screens folder and select Add Screen

    <figure><img src="../../../../.gitbook/assets/image (99).png" alt=""><figcaption><p>Add Screen right-click item</p></figcaption></figure>
3.  Name the screen TitleScreen and click OK

    <figure><img src="../../../../.gitbook/assets/image (100).png" alt=""><figcaption><p>Enter the new screen name and click OK</p></figcaption></figure>

The newly created TitleScreen is now in the Screens folder.

<figure><img src="../../../../.gitbook/assets/image (101).png" alt=""><figcaption><p>TitleScreen in Gum</p></figcaption></figure>

## Adding Instances

We can add instances to Gum Screen by drag+dropping the files onto the Game screen.

Add a Text instance by dropping the Standard/Text onto TitleScreen.

<figure><img src="../../../../.gitbook/assets/24_09 53 51.gif" alt=""><figcaption><p>Drag+drop Text instance onto TitleScreen to create a Text instance</p></figcaption></figure>

Instances can also be created by selecting the TitleScreen, then drag+dropping the item in the editor window.

Add a ButtonStandard instance by dropping Components/Controls/ButtonStandard onto TitleScreen.

<figure><img src="../../../../.gitbook/assets/24_09 55 46.gif" alt=""><figcaption><p>Drag+drop a component on the wireframe editor to add it to the TitleScreen</p></figcaption></figure>

Be sure to select the TitleScreen first, then drag+drop. If you click the component instead, then it will be selected, so you must re-select the TitleScreen.

The Gum tool includes lots of functionality for creating and customizing UI. For a more complete tutorial covering the Gum tool, see the [Gum Tool Intro Tutorials](../../../../gum-tool/tutorials-and-examples/intro-tutorials/). Feel free to spend some time creating your TitleScreen.

## Showing a Gum Screen in Your Game

To show the screen in game, modify the Initialize method as shown in the following snippet:

<pre class="language-diff" data-line-numbers><code class="lang-diff">protected override void Initialize()
{
    var gumProject = GumUI.Initialize(
        this,
        // This is relative to Content:
        "GumProject/GumProject.gumx");

<strong>+    // the Screens list contains all screens. Find the screen you want
</strong>+    var screen = gumProject.Screens.Find(item => item.Name == "TitleScreen");
+    // Calling GraphicalUiElement creates the visuals for the screen
<strong>+    var screenRuntime = screen.ToGraphicalUiElement();
</strong><strong>+    screenRuntime.AddToRoot();
</strong>
    base.Initialize();
}
</code></pre>

The game now displays the Gum screen.

<figure><img src="../../../../.gitbook/assets/image (103).png" alt=""><figcaption><p>Gum Screen loaded and displayed in a MonoGame project</p></figcaption></figure>

The new code has the following calls:

* Initialize - this loads our Gum project (.gumx). This call does not yet load any additional files such as .pngs or font files. This call only needs to happen one time in your game.
* The ToGraphicalUiElement method is responsible for converting the Gum screen into a visual object. It loads all other files referenced by the Screen and its instances such as .png and font files. This method is called whenever you want to show a new GraphicalUiElement, and it may be called multiple times in a game. For example, ToGraphicalUiElement is called whenever transitioning between screens, which we will do in a future tutorial.
* AddToRoot adds the screen so that it is drawn and updated every frame. Updating enables buttons responding to cursor events such as clicks.

<figure><img src="../../../../.gitbook/assets/24_10 16 55.gif" alt=""><figcaption><p>Button with built-in highlight and click styling</p></figcaption></figure>

{% hint style="info" %}
This tutorial uses Game1 as the container for all Gum members and logic. You may want to move this code into other classes to fit the rest of your game's code structure.
{% endhint %}

## Accessing Instances in Code

Now that we have our screen stored in the Root object, we can access objects.

We can modify the displayed string by getting an instance of the Text and modifying its properties as shown in the following code:

<pre class="language-diff"><code class="lang-diff">protected override void Initialize()
{
    var gumProject = MonoGameGum.GumService.Default.Initialize(
        this,
        // This is relative to Content:
        "GumProject/GumProject.gumx");      
        
    var screen = gumProject.Screens.Find(item => item.Name == "TitleScreen");
        
    var screenRuntime = screen.ToGraphicalUiElement();
        
    screenRuntime.AddToRoot();

<strong>+    var textInstance = screenRuntime.GetGraphicalUiElementByName("TextInstance")
</strong>+        as TextRuntime;
+    textInstance.Text = "I am set in code";

    base.Initialize();
}
</code></pre>

<figure><img src="../../../../.gitbook/assets/image (104).png" alt=""><figcaption><p>Text modified in code</p></figcaption></figure>

The code above casts TextInstance to a TextRuntime. Each standard type in Gum (such as Text, Sprite, and Container) has a corresponding _runtime_ type (such as TextRuntime, SpriteRuntime, and ContainerRuntime). Therefore, if we wanted to interact with a Text in code, we would cast it to a TextRuntime.

We can also interact with Forms objects in code. The base type for all Forms objects is FrameworkElement, so we can use the GetFrameworkElementByName extension method as shown in the following code:

```diff
protected override void Initialize()
{
    var gumProject = MonoGameGum.GumService.Default.Initialize(
        this,
        // This is relative to Content:
        "GumProject/GumProject.gumx");      
        
    var screen = gumProject.Screens.Find(item => item.Name == "TitleScreen");
        
    var screenRuntime = screen.ToGraphicalUiElement();
        
    screenRuntime.AddToRoot();

    var textInstance = screenRuntime.GetGraphicalUiElementByName("TextInstance")
        as TextRuntime;
    textInstance.Text = "I am set in code";

+    var button = screenRuntime.GetFrameworkElementByName<Button>("ButtonStandardInstance");
+    button.Click += (_, _) => textInstance.Text = "Button clicked at " + DateTime.Now;

    base.Initialize();
}
```

<figure><img src="../../../../.gitbook/assets/24_11 04 23.gif" alt=""><figcaption><p>Clicking on the Button changes the Text</p></figcaption></figure>

Notice that the code above uses the GetFrameworkElementByName. This code returns an instance of a FrameworkElement (Forms instance). As we'll cover in the next tutorial, only some Components can be used as Forms instances.

## Conclusion

This tutorial showed how to load a Gum screen in code and how to interact with objects. The next tutorial discusses Forms controls and explains the difference between GraphicalUielements and Forms controls.
