using Gum.DataTypes;
using Gum.Wireframe;
using GumFormsSample.Screens;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.Forms;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GumFormsSample;

public class GumFormsSampleGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    RenderTarget2D renderTarget;

    float scale = 1f;

    public GumFormsSampleGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // This sets the initial size:
        _graphics.PreferredBackBufferWidth = (int)(1024*scale);
        _graphics.PreferredBackBufferHeight = (int)(768 * scale);

        _graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
#if (ANDROID || iOS)
    graphics.IsFullScreen = true;
#endif
    }


    protected override void Initialize()
    {
        renderTarget = new RenderTarget2D(GraphicsDevice, 1024, 768);
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        GumService.Default.Initialize(this, "FormsGumProject/GumProject.gumx");
        FormsUtilities.Cursor.TransformMatrix = Matrix.CreateScale(1/scale);

        const int screenNumber = 3;

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
            case 3:
                InitializeComplexListBoxItemScreen();
                break;
            case 4:
                {
                    var screen = new ListBoxBindingScreen();
                    screen.AddToRoot();
                }
                break;
            case 5:
                {
                    var screen = new TestScreenRuntime();
                    screen.AddToRoot();
                }
                break;
        }

        base.Initialize();
    }


    private void InitializeFromFileDemoScreen()
    {
        var screen = new DemoScreenGumRuntime();
        screen.AddToManagers();
        screen.Initialize();
    }

    private void InitializeFormsCustomizationScreen()
    {
        var screen = new FormsCustomizationScreen();
        screen.Initialize();
        screen.AddToRoot();
    }

    private void InitializeFrameworkElementExampleScreen()
    {
        var screen = new FrameworkElementExampleScreen();
        screen.AddToRoot();
        screen.Initialize();
    }


    private void InitializeComplexListBoxItemScreen()
    {
        var screen = new ComplexListBoxItemScreen();
        screen.AddToRoot();
        screen.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        var cursor = FormsUtilities.Cursor;

        GumService.Default.Update(this, gameTime);

        foreach(var item in GumService.Default.Root.Children)
        {
            (item as IUpdateScreen)?.Update(gameTime);

        }

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


        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(renderTarget);

        GraphicsDevice.Clear(Color.CornflowerBlue);

        SystemManagers.Default.Draw();

        GraphicsDevice.SetRenderTarget(null);

        _spriteBatch.Begin();
        _spriteBatch.Draw(renderTarget, new Rectangle(0, 0, (int)(1024*scale), (int)(768*scale)), Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
