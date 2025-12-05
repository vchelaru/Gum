# MonoGame/KNI/FNA

## Introduction

Before you begin working with Gum, you need a MonoGame/KNI project. This could be a completely empty MonoGame/KNI project, or it could be an existing game.

## Creating an New Project

To create a new MonoGame project, follow the steps in the MonoGame Getting Started tutorial: [https://docs.monogame.net/articles/tutorials/building\_2d\_games/02\_getting\_started/index.html?tabs=windows](https://docs.monogame.net/articles/tutorials/building_2d_games/02_getting_started/index.html?tabs=windows)

To create a new KNI project, see the KNI project page: [https://github.com/kniEngine/kni](https://github.com/kniEngine/kni)

To create a new FNA project, see the FNA setup guide: [https://fna-xna.github.io/docs/1%3A-Setting-Up-FNA/](https://fna-xna.github.io/docs/1%3A-Setting-Up-FNA/) . Alternatively if you prefer, you can create a copy of the FNA sample project: [https://github.com/vchelaru/Gum/tree/master/Samples/FnaGum](https://github.com/vchelaru/Gum/tree/master/Samples/FnaGum) . Note that the FNA team recommends linking your project against FNA source. The sample project linked above requires that you have the FNA.Core cproj linked to compile your project.

Once you have your project set up, you should have a Game class similar to the following block of code:

```csharp
namespace MonoGameGum1;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        base.Draw(gameTime);
    }
}
```
