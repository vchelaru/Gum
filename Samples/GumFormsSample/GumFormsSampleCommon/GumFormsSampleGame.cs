using Gum.Wireframe;
using GumFormsSample.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Forms;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using System;
using System.Diagnostics;

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

        const int screenNumber = 0;

        switch (screenNumber)
        {
            case 0:
                InitializeFromFileDemoScreen();
                break;
            case 1:
                InitializeFrameworkElementExampleScreen();
                break;
            case 2:
                InitializeFormsCustomizationScreen();
                break;
        }

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

    protected override void Update(GameTime gameTime)
    {
        var cursor = FormsUtilities.Cursor;

        GamePadState gamePadState = default;
        try { gamePadState = GamePad.GetState(PlayerIndex.One); }
        catch (NotImplementedException) { /* ignore gamePadState */ }


        if (FormsUtilities.Keyboard.KeyDown(Keys.Escape) ||
            gamePadState.Buttons.Back == ButtonState.Pressed)
        {
            // Put a breakpoint here if you want to pause the app when the user presses ESC
            int m = 3;
        }

        FormsUtilities.Update(gameTime, Root);

        // Set this to true to see WindowOver information in the output window
        bool printWindowOver = false;
        if (printWindowOver)
        {
            string windowOver = "<null>";
            if (cursor.WindowOver != null)
            {
                windowOver = $"{cursor.WindowOver.GetType().Name}";
            }
            Debug.WriteLine($"Window over: {windowOver} @ x:{cursor.WindowOver?.X}");
        }

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
