# Setup

This page assumes you have an existing MonoGame project. This can be an empty project or an existing game.

Note that as of the time of this writing, only MonoGame Desktop GL projects are supported. If you are working on a different platform, please contact us on discord or file an issue on Github requesting the addition of your project.

### Adding Gum Nuget Packages

1. Open your MonoGame project in your preferred IDE.
2.  Add the `Gum.MonoGame` NuGet package\


    <figure><img src="../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption></figcaption></figure>

### Initializing Gum

1. Open your Game class (usually Game1.cs)
2. Add the following code to your Initialize method:\
   `SystemManagers.Default = new SystemManagers(); SystemManagers.Default.Initialize(_graphics.GraphicsDevice, fullInstantiation: true);`
3. Add the following to your Update method:\
   `SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);`
4. Add the following to your Draw method after you clear the graphics device:\
   `SystemManagers.Default.Draw();`

### Testing Gum

To test that you have successfully added Gum to the project, add the following code to your Initialize method after SystemManagers.Default.Initialize:

```csharp
var rectangle = new ColoredRectangleRuntime();
rectangle.Width = 100;
rectangle.Height = 100;
rectangle.Color = Color.White;
rectangle.AddToManagers(SystemManagers.Default, null);
```

<figure><img src="../.gitbook/assets/image (25).png" alt=""><figcaption></figcaption></figure>

If everything is intiailized correctly, you should see a white rectangle at the top-left of the screen.

