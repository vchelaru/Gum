using System;
using System.Linq;
using Gum;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using RenderingLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameAndGum.Renderables;

namespace MultiGumServiceSample;

/// <summary>
/// Two GumService instances sharing one GraphicsDevice. The game service loads Content/Game
/// (ForestGlade theme) and the editor service loads Content/Editor (DarkPro theme). The editor's
/// ListBox lists the game project's screens; selecting one shows that screen in the game service.
/// </summary>
public class Game1 : Game
{
    // Leaves room for the editor panel docked top-left (the game screens start at x=20).
    const float InitialGameCameraX = -250;

    private GraphicsDeviceManager _graphics;

    private readonly GumService _gameUi = new GumService();
    private readonly GumService _editorUi = new GumService();

    private GumProjectSave _gameProject = null!;
    private GumProjectSave _editorProject = null!;

    private GraphicalUiElement? _currentGameScreen;
    private MouseState _previousMouse;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1024,
            PreferredBackBufferHeight = 768
        };
        // Apos.Shapes (behind the slider thumb and other rounded shapes) needs the HiDef profile.
        _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // The first service to initialize becomes IGumService.Default; the second leaves it alone.
        // Paths are relative to the Content folder.
        _gameProject = _gameUi.Initialize(this, "Game/Game.gumj")!;

        // One ShapeRenderer serves both services. It reads the device from the current service,
        // which is the game here, and must run before the editor initializes so the editor
        // registers the shape runtimes too.
        ShapeRenderer.Self.Initialize();

        _editorProject = _editorUi.Initialize(this, "Editor/Editor.gumj")!;

        _gameUi.SystemManagers.Renderer.Camera.X = InitialGameCameraX;

        CreateEditorScreen();

        base.Initialize();
    }

    private void CreateEditorScreen()
    {
        ListBox screenListBox = RunAs(_editorUi, () =>
        {
            ScreenSave editorScreen = _editorProject.Screens.First(item => item.Name == "EditorScreen");
            GraphicalUiElement screen = editorScreen.ToGraphicalUiElement(_editorUi.SystemManagers);
            _editorUi.Root.Children.Add(screen);

            var listBox = screen.FindFormsControl<ListBox>("ScreenListBox")!;
            foreach (ScreenSave gameScreen in _gameProject.Screens)
            {
                listBox.Items!.Add(gameScreen.Name);
            }
            return listBox;
        });

        // Raised from inside the editor service's Update, so the editor is the current service here.
        screenListBox.SelectionChanged += (_, _) =>
        {
            if (screenListBox.SelectedObject is string screenName)
            {
                ShowGameScreen(screenName);
            }
        };
    }

    private void ShowGameScreen(string screenName)
    {
        // The game's elements must be built and removed while the game service is current.
        RunAs(_gameUi, () =>
        {
            if (_currentGameScreen != null)
            {
                _currentGameScreen.Parent = null;
                _currentGameScreen.RemoveFromManagers();
            }

            ScreenSave screenSave = _gameProject.Screens.First(item => item.Name == screenName);
            _currentGameScreen = screenSave.ToGraphicalUiElement(_gameUi.SystemManagers);
            _gameUi.Root.Children.Add(_currentGameScreen);
            return 0;
        });
    }

    protected override void Update(GameTime gameTime)
    {
        PanGameWithRightMouse();

        _gameUi.Update(gameTime);
        _editorUi.Update(gameTime);

        base.Update(gameTime);
    }

    private void PanGameWithRightMouse()
    {
        MouseState mouse = Mouse.GetState();
        if (mouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Pressed)
        {
            var camera = _gameUi.SystemManagers.Renderer.Camera;
            camera.X -= (mouse.X - _previousMouse.X) / camera.Zoom;
            camera.Y -= (mouse.Y - _previousMouse.Y) / camera.Zoom;
        }
        _previousMouse = mouse;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(24, 32, 28));

        _gameUi.Draw();
        _editorUi.Draw();

        base.Draw(gameTime);
    }

    // Update and Draw make their own service current for the call. Anything else that builds or
    // changes another service's elements has to set IGumService.Default itself.
    private static T RunAs<T>(GumService service, Func<T> action)
    {
        IGumService? previous = IGumService.Default;
        IGumService.Default = service;
        try
        {
            return action();
        }
        finally
        {
            IGumService.Default = previous;
        }
    }
}
