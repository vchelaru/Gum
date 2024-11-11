# Kni

### Introduction

The Kni Engine is an XNA-like library deriving from MonoGame. Compared to MonoGame it offers a number of benefits including more frequent fixes and improvements, and perhaps most importantly support for WebGL builds. In other words, by using Kni you can create games in C# using the XNA syntax which can be played in the browser.

Working with Gum in a Kni project is identical to MonoGame with the exception of how files are handled. Therefore, once you have a basic project working, you can follow the [MonoGame documentation](monogame/) for detailed discussion of Gum's features.

### Creating a Kni Project

If you already have an existing Kni project, then you can skip this section. If you are interested in working with Kni and Gum and you do not already have a project, then you have two options:

1. Convert an existing MonoGame project so it uses Kni
2. Create a brand new project using Kni

If you have an existing MonoGame project, you can convert it to Kni by swapping out the nuget packages. If you are interested in targeting WebGL specifically, this blog post discusses the process:

{% embed url="https://darkgenesis.zenithmoon.com/monogame-on-the-web-No-really!.html" %}

If you would like to create a new Kni project from scratch, you can follow these steps:

1. Go to the Kni Releases
2.  Download and run the latest KniSdkSetup exe\
    &#x20;

    <figure><img src=".gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>KniSdkSetup in GitHub</p></figcaption></figure>
3. After installing the SDK, open Visual Studio
4. Select the option to create a new project
5. Search for Kni
6.  Select one of the project templates, such as **Web browser**\


    <figure><img src=".gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Web browser project in Kni</p></figcaption></figure>
7. Click Next and proceed through the remainder of the steps to create a project

You should test your project to make sure it runs prior to adding Gum.

### Adding Gum to a Kni Project

Once you have verified that your project runs, you can add Gum to your project by following these steps:

1. Right-click on your project's dependencies and select to **Manage NuGet packages...**
2. Search for **Gum.Kni** and add the Gum.Kni package to your project
3. Navigate to your Game class. Modify your code so it is similar to the following code. Note that your game class may differ from WebGlTest1Game:

```csharp
public class WebGlTest1Game : Game
{
    GraphicsDeviceManager graphics;

    public WebGlTest2Game()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        SystemManagers.Default = new SystemManagers();
        SystemManagers.Default.Initialize(graphics.GraphicsDevice, fullInstantiation: true);


        var rectangle = new ColoredRectangleRuntime();
        rectangle.Width = 100;
        rectangle.Height = 100;
        rectangle.Color = Color.White;
        rectangle.AddToManagers(SystemManagers.Default, null);

        base.Initialize();

    }

    protected override void Update(GameTime gameTime)
    {
        SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        SystemManagers.Default.Draw();

        base.Draw(gameTime);
    }
}

```

<figure><img src=".gitbook/assets/image (2) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>KniGum drawing a rectangle</p></figcaption></figure>
