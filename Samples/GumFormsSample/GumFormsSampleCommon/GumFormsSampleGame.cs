﻿using Gum.DataTypes;
using Gum.Wireframe;
using GumFormsSample.Screens;
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
        var gumProject = GumService.Default.Initialize(_graphics.GraphicsDevice, "FormsGumProject/GumProject.gumx");

        const int screenNumber = 0;

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
        var screen = new FromFileDemoScreen();
        GraphicalUiElement root = null;
        screen.Initialize(gumProject, ref root);
        Roots.Add(root);
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

        GamePadState gamePadState = default;
        try { gamePadState = GamePad.GetState(PlayerIndex.One); }
        catch (NotImplementedException) { /* ignore gamePadState */ }


        if (FormsUtilities.Keyboard.KeyDown(Keys.Escape) ||
            gamePadState.Buttons.Back == ButtonState.Pressed)
        {
            // Put a breakpoint here if you want to pause the app when the user presses ESC
            int m = 3;
        }


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
        GraphicsDevice.Clear(Color.CornflowerBlue);

        SystemManagers.Default.Draw();

        base.Draw(gameTime);
    }
}
