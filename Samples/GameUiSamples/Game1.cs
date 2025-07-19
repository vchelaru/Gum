using GameUiSamples.Screens;
using GameUiSamples.Services;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.Forms.Controls;
using RenderingLibrary;
using System;

namespace GameUiSamples;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    GumService GumUI => MonoGameGum.GumService.Default;

    public static GameServiceContainer ServiceContainer { get; private set; }

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreparingDeviceSettings += HandlePrepareDeviceSettings;
        Content.RootDirectory = "Content";
        _graphics.PreferredBackBufferWidth = 1366;
        _graphics.PreferredBackBufferHeight = 768;
        IsMouseVisible = true;
    }

    private void HandlePrepareDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
    {
        // This allows Gum to use render targets without wipling whatever was previously rendered
        // Without this, the background becomes black, and many of the rendered objects disappear
        e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
    }

    protected override void Initialize()
    {
        ServiceContainer = Services;

        var gumProject = GumUI.Initialize(this, "GumProject/GameUiSamplesgumProject.gumx");
        // This allows the keyboard to control the game (tabbing)
        FrameworkElement.KeyboardsForUiControl.Add(GumUI.Keyboard);
        FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = Keys.Down });
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo { PushedKey = Keys.Up });

        Services.AddService<InventoryService>(new InventoryService());

        var startScreen = new MainMenu();
        startScreen.AddToRoot();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // TODO: use this.Content to load your game content here
    }

    protected override void Update(GameTime gameTime)
    {
        GumUI.Update(gameTime);

        foreach(var item in GumService.Default.Root.Children)
        {
            if(item is InteractiveGue asInteractiveGue)
            {
                (asInteractiveGue.FormsControlAsObject as IUpdateScreen)?.Update(gameTime);
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        GumUI.Draw();

        base.Draw(gameTime);
    }
}
