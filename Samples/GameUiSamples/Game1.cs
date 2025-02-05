using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RenderingLibrary;

namespace GameUiSamples;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    GraphicalUiElement Root;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        _graphics.PreferredBackBufferWidth = 1366;
        _graphics.PreferredBackBufferHeight = 768;
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        var gumProject = MonoGameGum.GumService.Default.Initialize(this, "GumProject/GameUiSamplesgumProject.gumx");

        Root = gumProject.Screens
            .Find(item => item.Name == "MainMenu")
            .ToGraphicalUiElement(SystemManagers.Default, addToManagers:true);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // TODO: use this.Content to load your game content here
    }

    protected override void Update(GameTime gameTime)
    {
        MonoGameGum.GumService.Default.Update(this, gameTime, Root);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        MonoGameGum.GumService.Default.Draw();

        base.Draw(gameTime);
    }
}
