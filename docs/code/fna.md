# FNA

## Introduction

Gum can be used in FNA projects. All FNA code is syntactically identical to MonoGame, so it's best to follow the MonoGame documentation.

## Setup

To use FNA Gum, you must already have a working FNA project. At the time of this writing, FNA does not provide an official NuGet package. Therefore, to create an FNA project, you have one of the following options:

* Follow the FNA setup guide [https://fna-xna.github.io/docs/1%3A-Setting-Up-FNA/](https://fna-xna.github.io/docs/1%3A-Setting-Up-FNA/)
* Create a copy of the FNA sample project [https://github.com/vchelaru/Gum/tree/master/Samples/FnaGum](https://github.com/vchelaru/Gum/tree/master/Samples/FnaGum)

Note that the FNA team recommends linking your project against FNA source. The sample project linked above requires that you have the FNA.Core cproj linked to compile your project.

Also, note that the Gum.FNA NuGet package requires that you have FNA linked in your game. The Gum.FNA package does not automatically link the FNA libraries since there is no official FNA NuGet package.

### Adding Gum to an Existing FNA Project

To add Gum to an existing FNA project, first link the Gum NuGet package

[https://www.nuget.org/packages/Gum.FNA](https://www.nuget.org/packages/Gum.FNA)

Next, you can modify your Game project so that it initializes and loads Gum. A simple game project might look like this:

```csharp
protected override void Initialize()
{
    MonoGameGum.GumService.Default.Initialize(this);
    base.Initialize();
}

protected override void Update(GameTime gameTime)
{
    MonoGameGum.GumService.Default.Update(this, gameTime);
    base.Update(gameTime);
}

protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.CornflowerBlue);
    MonoGameGum.GumService.Default.Draw();
    base.Draw(gameTime);
}
```

Note that the code uses the `MonoGameGum` class, despite this being FNA instead of MonoGame. This is because FNA and MonoGame are nearly syntactically identical, so the same Gum syntax for MonoGame works for FNA projects as well, including Gum.

Once your project has Gum linked and initialized, you can begin using Gum either purely in code or by loading a Gum (gumx) project.

For more information, see the setup and tutorial sections in the [MonoGame](monogame/) page.
