using Gum.Wireframe;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Forms;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using System.Diagnostics;
using MonoGameGum.Input;
using GumFormsSample.Screens;
using Gum.DataTypes;
using Gum.Managers;
using System.Linq;
using GumRuntime;
using ToolsUtilities;

namespace GumFormsSample;

public class GumFormsSampleGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    GraphicalUiElement Root;
public GumFormsSampleGame()
{
    _graphics = new GraphicsDeviceManager(this);
    Content.RootDirectory = "Content";
    IsMouseVisible = true;

    // This sets the initial size:
    _graphics.PreferredBackBufferWidth = 1024;
    _graphics.PreferredBackBufferHeight = 768;

    _graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
#if (ANDROID || iOS)
    graphics.IsFullScreen = true;
#endif
}


    protected override void Initialize()
    {
        SystemManagers.Default = new SystemManagers();
        SystemManagers.Default.Initialize(_graphics.GraphicsDevice, fullInstantiation: true);
        FormsUtilities.InitializeDefaults();

        // The initialize methods here create one of the available screens
        // Uncomment the one you want to see
        //InitializeFrameworkElementExampleScreen();
        //InitializeFormsCustomizationScreen();
        InitializeFromFileDemoScreen();
        base.Initialize();

    }

    private void InitializeFromFileDemoScreen()
    {
        var screen = new FromFileDemoScreen();
        screen.Initialize(ref Root);
    }

    private void InitializeFormsCustomizationScreen()
    {
        CreateRoot();
        var screen = new FormsCustomizationScreen();
        screen.Initialize(Root);
    }

    private void InitializeFrameworkElementExampleScreen()
    {
        CreateRoot();
        var screen = new FrameworkElementExampleScreen();
        screen.Initialize(Root);
    }

    private void CreateRoot()
    {
        Root = new ContainerRuntime();

        Root.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        Root.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        Root.AddToManagers(SystemManagers.Default, null);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // TODO: Use this.Content to load your game content here
    }

    protected override void UnloadContent()
    {
        // TODO: Unload any non ContentManager content here
    }

    protected override void Update(GameTime gameTime)
    {
        var cursor = FormsUtilities.Cursor;



        MouseState mouseState = Mouse.GetState();
        KeyboardState keyboardState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        GamePadState gamePadState = default;
        try { gamePadState = GamePad.GetState(PlayerIndex.One); }
        catch (NotImplementedException) { /* ignore gamePadState */ }


        if (keyboardState.IsKeyDown(Keys.Escape) ||
            keyboardState.IsKeyDown(Keys.Back) ||
            gamePadState.Buttons.Back == ButtonState.Pressed)
        {
            int m = 3;
        }



        FormsUtilities.Update(gameTime, Root);

        string windowOver = "<null>";
        if(cursor.WindowOver != null)
        {
            windowOver = $"{cursor.WindowOver.GetType().Name}" ;
        }

        // Uncomment this to see the current window over every frame
        //System.Diagnostics.Debug.WriteLine($"Window over: {windowOver} @ x:{cursor.WindowOver?.X}");

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
