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

    List<GraphicalUiElement> Roots = new List<GraphicalUiElement>();

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

        var gumProject = GumService.Default.Initialize(this, "FormsGumProject/GumProject.gumx");
        FormsUtilities.Cursor.TransformMatrix = Matrix.CreateScale(1/scale);

        const int screenNumber = 1;

        switch (screenNumber)
        {
            case 0:
                InitializeFromFileDemoScreen(gumProject);
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
        }

        base.Initialize();
    }


    private void InitializeFromFileDemoScreen(GumProjectSave gumProject)
    {
        var screenSave = gumProject.Screens.Find(item => item.Name == "DemoScreenGum");

        var screen = screenSave.ToGraphicalUiElement(
            SystemManagers.Default, addToManagers: true) as DemoScreenGumRuntime;
        screen.Initialize();
        Roots.Add(screen);
    }

    private void InitializeFormsCustomizationScreen()
    {
        var root = CreateRoot();
        var screen = new FormsCustomizationScreen();
        screen.Initialize(root);
        Roots.Add(root);
    }

    private void InitializeFrameworkElementExampleScreen()
    {
        var root = CreateRoot();
        var screen = new FrameworkElementExampleScreen();
        Roots.Add(root);
        screen.Initialize(Roots);
    }


    private void InitializeComplexListBoxItemScreen()
    {
        var root = CreateRoot();
        var screen = new ComplexListBoxItemScreen();
        screen.Initialize(root);
        Roots.Add(root);
    }

    private GraphicalUiElement CreateRoot()
    {
        var root = new ContainerRuntime();

        root.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        root.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        root.AddToManagers(SystemManagers.Default, null);
        return root;
    }

    protected override void Update(GameTime gameTime)
    {
        var cursor = FormsUtilities.Cursor;

        GumService.Default.Update(this, gameTime, Roots);

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
