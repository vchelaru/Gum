# Multiple Screens

## Introduction

A full project may include multiple screens such as a TitleScreen, LevelSelectScreen, and even GameScreen which might include a HUD or PauseMenu.

This tutorial introduces how to work with multiple Gum screens. For this tutorial we will focus on two new Screens: Screen1 and Screen2. It's okay if your project has more screens, but we'll be using just these two for this tutorial.

## Creating Multiple Screens

Before we write any code, we'll create two screens. As mentioned above, a full game might have many screens.  For this tutorial we'll create two simple screens, each with a single button and a label.

First we'll create Screen1:

1. Right-click on the Screens folder to add a new screen named Screen1
2.  Drag+drop a Components/Controls/Label component into Screen1\


    <figure><img src="../../../../.gitbook/assets/10_06 06 33.png" alt=""><figcaption><p>Add a Label to Screen1</p></figcaption></figure>
3. Set the **Label Text** property on the newly created LabelInstance to **Screen 1** so that you can tell that you are on Screen1 when running your game
4. Drag+drop a **ButtonStandard** component into Screen1
5. Set the Text property on the newly created **ButtonStandardInstance** to **Go to Screen 2**
6. Arrange the two instances so they are not overlapping

<figure><img src="../../../../.gitbook/assets/10_06 08 46.png" alt=""><figcaption><p>Label and Button in Screen1</p></figcaption></figure>

Next we'll create Screen2:

1. Add a new Screen named Screen2
2. Drag+drop a Component/Controls/Label into Screen2
3. Set the Text property on the newly created LabelInstance to **Screen 2** so that you can tell that you are in Screen2 when running your game
4. Drag+drop a ButtonStandard component into Screen2
5. Set the Text property on the newly created ButtonStandardInstance to **Go to Screen 1**
6. Arrange the two instances so they are not overlapping

<figure><img src="../../../../.gitbook/assets/10_06 11 14.png" alt=""><figcaption><p>Label and Button in Screen2</p></figcaption></figure>

## Modify Your Game (Game1) Class

Next we'll modify our Game class so it loads Screen1.

Make sure you have already setup code generation for your project. You can verify that it is set up correctly by selecting one of your screens. You should see the Code tab appear, and it should reference your .csproj location.

<figure><img src="../../../../.gitbook/assets/10_06 20 11.png" alt=""><figcaption><p>Gum referencing .csproj folder</p></figcaption></figure>

You can also click the Generate Code button on each screen file to make sure that code has been properly generated.

By generating code for both screens, you can access each screen as a strongly-typed class in your project: `Screen1` and `Screen2` .

You can load Screen1 by adding the following code to your Game class.

{% tabs %}
{% tab title="Full Code" %}
<pre class="language-csharp"><code class="lang-csharp"><strong>public class Game1 : Game
</strong>{
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
        GumUI.Initialize(this,
            // This is relative to Content:
            "GumProject/GumProject.gumx");

        var screen = new Screen1();
        screen.AddToRoot();

        base.Initialize();
    }
// ...
</code></pre>
{% endtab %}

{% tab title="Diff" %}
<pre class="language-diff"><code class="lang-diff"><strong>public class Game1 : Game
</strong>{
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
        GumUI.Initialize(this,
            // This is relative to Content:
            "GumProject/GumProject.gumx");

+       var screen = new Screen1();
+       screen.AddToRoot();

        base.Initialize();
    }
// ...
</code></pre>
{% endtab %}
{% endtabs %}

<figure><img src="../../../../.gitbook/assets/10_06 23 17.png" alt=""><figcaption><p>Screen1 loaded in game</p></figcaption></figure>

## Switching Between Screens

When we generated our screens earlier, Gum created code files for each, including a _custom code_ file where we can add our own code.

<figure><img src="../../../../.gitbook/assets/10_06 27 23.png" alt=""><figcaption><p>Custom code Screen files in our project</p></figcaption></figure>

We can modify Screen1.cs and Screen2.cs to include code to move from one screen to the other.

Add a Click handler to our button which removes the existing screen and creates the new screen so your two screens look like the code shown in the following blocks.

{% tabs %}
{% tab title="Full Code" %}
```csharp
using MonoGameGum;

partial class Screen1
{
    partial void CustomInitialize()
    {
        ButtonStandardInstance.Click += (_, _) =>
        {
            GumService.Default.Root.Children.Clear();
            var screen = new Screen2();
            screen.AddToRoot();
        };
    }
}
```
{% endtab %}

{% tab title="Diff" %}
```diff
using MonoGameGum;

partial class Screen1
{
    partial void CustomInitialize()
    {
+       ButtonStandardInstance.Click += (_, _) =>
+       {
+           GumService.Default.Root.Children.Clear();
+           var screen = new Screen2();
+           screen.AddToRoot();
+       };
    }
}
```
{% endtab %}
{% endtabs %}

{% tabs %}
{% tab title="Full Code" %}
```csharp
using MonoGameGum;

partial class Screen2
{
    partial void CustomInitialize()
    {
        ButtonStandardInstance.Click += (_, _) =>
        {
            GumService.Default.Root.Children.Clear();
            var screen = new Screen1();
            screen.AddToRoot();
        };
    }
}
```
{% endtab %}

{% tab title="Diff" %}
<pre class="language-diff"><code class="lang-diff">using MonoGameGum;

partial class Screen2
{
    partial void CustomInitialize()
    {
<strong>+       ButtonStandardInstance.Click += (_, _) =>
</strong>+       {
+           GumService.Default.Root.Children.Clear();
+           var screen = new Screen1();
+           screen.AddToRoot();
+       };
    }
}
</code></pre>
{% endtab %}
{% endtabs %}

Each screen clears the root (removes the previous screen) when its button is clicked, then creates and adds the next screen to the root.

<figure><img src="../../../../.gitbook/assets/10_06 34 29 (1).gif" alt=""><figcaption><p>Switching Screens with Buttons</p></figcaption></figure>

### Showing No Screen

This tutorial assumes that a Gum screen is always displayed.

Games can also completely remove Gum screens altogether. To do this, do not create a new screen after calling `GumService.Default.Root.Children.Clear();`

## Conclusion

This tutorial showed how to switch between two screens by removing the old screen and creating a new screen. Although this is a simple example, the same concepts could be applied to a full game to switch between multiple screens.
