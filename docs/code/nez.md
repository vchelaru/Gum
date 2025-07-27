# Nez

### Introduction

Nez is a framework which adds additional functionality on top of MonoGame and FNA. Gum can be used with Nez since Nez ultimately exposes the MonoGame/FNA GraphicsDevice. Aside from setup, using Gum in Nez is identical to using Gum in any MonoGame project. Therefore, after setup you can reference the [MonoGame section](monogame/) for usage details.

### Setup

Nez projects use a game class which must inherit from [Nez Core](https://github.com/prime31/Nez/blob/master/FAQs/Nez-Core.md). This class ultimately inherits from the standard MonoGame Game class, so you can include overrides for Update and Draw just like any standard MonoGame project.

A typical game may look like is shown in the following code snippet:

```csharp
public class Game1 : Core
{
    protected override void Initialize()
    {
        base.Initialize();

        MonoGameGum.GumService.Default.Initialize(Core.GraphicsDevice);
        
        // Optional - adding a colored rectangle to make sure it all works:
        var rectangle = new ColoredRectangleRuntime();
        rectangle.Width = 100;
        rectangle.Height = 100;
        rectangle.Color = Color.White;
        rectangle.AddToManagers(SystemManagers.Default, null);

        Window.AllowUserResizing = true;
        Scene = new BasicScene();
    }

    protected override void Update(GameTime gameTime)
    {
        MonoGameGum.GumService.Default.Activity(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
        // Add System.Default.Draw after base.Draw or else graphics won't show up
        MonoGameGum.GumService.Default.Draw();
    }
}
```

If you have been able to get Gum working in a Nez project without any runtime errors, you are ready to start adding more complex UI to your game. Head on over to the [MonoGame section](monogame/) for more information.

### Troubleshooting

Could not load file or assembly 'MonoGame.Framework, Version=3.8.1.303

If you add the Gum code to your project, you may experience this exception internally from Nez:

<figure><img src="../.gitbook/assets/image (71).png" alt=""><figcaption></figcaption></figure>

The reason this is happening is because currently (as of July 2024) Nez links MonoGame 3.8.0 instead of 3.8.1 (the latest).

To solve this problem, your project must explicitly link MonoGame 3.8.1 or else you will have this exception.

To do this:

1. Open your project in Visual Studio
2. Expand the Dependencies item
3.  Right-click on Packages and select Manage NuGet Packages\


    <figure><img src="../.gitbook/assets/image (73).png" alt=""><figcaption><p>Right-click Manage NuGet Packages... option</p></figcaption></figure>
4. Click on the Browse tab
5. Search for MonoGame.Framework
6.  Select the MonoGame.Framework NuGet package for your particular project type. This is most likely MonoGame.Framework.DesktopGL, but it may be different if you are targeting another platform.\


    <figure><img src="../.gitbook/assets/image (74).png" alt=""><figcaption><p>MonoGame.Framework NuGet packages</p></figcaption></figure>
7. Click the Install button to add the NuGet package

After adding MonoGame, your NuGet packages should similar to the following image:

<figure><img src="../.gitbook/assets/image (72).png" alt=""><figcaption></figcaption></figure>
