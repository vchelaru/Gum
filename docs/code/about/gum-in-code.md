# Gum in Code

## Introduction

Gum can be used purely in code (no editor), or by referencing a Gum project. Both approaches are well-supported so you can use Gum to fit your development style.

## Gum Code-Only

Gum can be used fully in code. A code-only setup requires minimal code. The following shows a simple MonoGame Game1 class with a few functional Gum controls. All lines of code related to Gum are highlighted:

<pre class="language-csharp"><code class="lang-csharp"><strong>using Gum.Forms.Controls;
</strong><strong>using Gum.Wireframe;
</strong>using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
<strong>using MonoGameGum;
</strong>using System;

namespace MonoGameAndGum;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
<strong>    GumService GumUI => GumService.Default;
</strong>    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
<strong>        GumUI.Initialize(this, Gum.Forms.DefaultVisualsVersion.V2);
</strong><strong>
</strong><strong>        var stackPanel = new StackPanel();
</strong><strong>        stackPanel.AddToRoot();
</strong><strong>        stackPanel.Spacing = 6;
</strong><strong>        stackPanel.Anchor(Anchor.Center);
</strong><strong>
</strong><strong>        var button = new Button();
</strong><strong>        stackPanel.AddChild(button);
</strong><strong>        button.Text = "Click Me";
</strong><strong>        button.Click += (s, e) => 
</strong><strong>        {
</strong><strong>            button.Text = DateTime.Now.ToString();
</strong><strong>        };
</strong><strong>
</strong><strong>        var textBox = new TextBox();
</strong><strong>        textBox.Width = 150;
</strong><strong>        stackPanel.AddChild(textBox);
</strong><strong>
</strong><strong>        var listBox = new ListBox();
</strong><strong>        stackPanel.AddChild(listBox);
</strong><strong>        for(int i = 0; i &#x3C; 10; i++)
</strong><strong>        {
</strong><strong>            listBox.Items.Add("Item " + i);
</strong><strong>        }
</strong>
        base.Initialize();
    }


    protected override void Update(GameTime gameTime)
    {
<strong>        GumUI.Update(gameTime);
</strong>        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
<strong>        GumUI.Draw();
</strong>        base.Draw(gameTime);
    }
}


</code></pre>

<figure><img src="../../.gitbook/assets/15_07 52 36.gif" alt=""><figcaption><p>Code-only Gum Controls</p></figcaption></figure>

## Gum Tool Projects in Code

The Gum Tool can be used to create projects visually. These projects can be loaded into your project and accessed in a type-safe way using Gum's generated code.

The following image shows a screen similar to the one above created in the Gum tool:

<figure><img src="../../.gitbook/assets/15_08 47 22.png" alt=""><figcaption></figcaption></figure>

The following code can be used to load and interact with the controls:

<pre class="language-csharp"><code class="lang-csharp">public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
<strong>    GumService GumUI => GumService.Default;
</strong>    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
<strong>        GumUI.Initialize(this, "GumProject/GumProject.gumx");
</strong><strong>
</strong><strong>        var screen = new ExampleScreen();
</strong><strong>        screen.AddToRoot();
</strong><strong>
</strong><strong>        screen.ButtonStandardInstance.Click += (s,e) =>
</strong><strong>        {
</strong><strong>            screen.ButtonStandardInstance.Text = DateTime.Now.ToString();
</strong><strong>        };
</strong><strong>
</strong><strong>        for(int i = 0; i &#x3C; 10; i++)
</strong><strong>        {
</strong><strong>            screen.ListBoxInstance.Items.Add("Item " + (i + 1));
</strong><strong>        }
</strong>
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
<strong>        GumUI.Update(gameTime);
</strong>        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
<strong>        GumUI.Draw();
</strong>        base.Draw(gameTime);
    }
}

</code></pre>

Generated code is optional - code can access Gum objects without any generated code as shown in the following block:

<pre class="language-csharp"><code class="lang-csharp">public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
<strong>    GumService GumUI => GumService.Default;
</strong>    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
<strong>        GumUI.Initialize(this, "GumProject/GumProject.gumx");
</strong><strong>
</strong><strong>        var screen = ObjectFinder.Self.GumProjectSave.Screens
</strong><strong>            .First().ToGraphicalUiElement();
</strong><strong>        screen.AddToRoot();
</strong><strong>
</strong><strong>        var button = screen.GetFrameworkElementByName&#x3C;Button>("ButtonStandardInstance");
</strong><strong>        button.Click += (s,e) =>
</strong><strong>        {
</strong><strong>            button.Text = DateTime.Now.ToString();
</strong><strong>        };
</strong><strong>
</strong><strong>        var listBox = screen.GetFrameworkElementByName&#x3C;ListBox>("ListBoxInstance");
</strong><strong>        for (int i = 0; i &#x3C; 10; i++)
</strong><strong>        {
</strong><strong>            listBox.Items.Add("Item " + (i + 1));
</strong><strong>        }
</strong>
        base.Initialize();
    }


    protected override void Update(GameTime gameTime)
    {
<strong>        GumUI.Update(gameTime);
</strong>        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
<strong>        GumUI.Draw();
</strong>        base.Draw(gameTime);
    }
}
</code></pre>
