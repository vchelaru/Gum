using FlatRedBall;
using FlatRedBall.Gum;
using Gum.DataTypes;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.RenderingLibrary;
using RenderingLibrary;

namespace FlatRedBall.GumRendering.Tests.Harness;

/// <summary>
/// A real FlatRedBall game whose only content is Gum, drawn through FRB's <c>GumIdb</c>. FRB state is
/// static, so one host lives for the whole process; scenes are added, drawn, and removed per test.
/// </summary>
internal sealed class FrbGumHost : Game
{
    public const int Width = 128;
    public const int Height = 128;

    private static FrbGumHost? _instance;
    public static FrbGumHost Instance => _instance ??= Create();

    private readonly GraphicsDeviceManager _graphics;
    private bool _capture;
    private Color[]? _captured;

    private FrbGumHost()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = Width,
            PreferredBackBufferHeight = Height,
        };
        Content.RootDirectory = "Content";
    }

    private static FrbGumHost Create()
    {
        FrbGumHost host = new();
        host.RunOneFrame(); // runs Initialize
        host.RunOneFrame(); // lets FRB's instruction queue register the GumIdb
        return host;
    }

    protected override void Initialize()
    {
        FlatRedBallServices.InitializeFlatRedBall(this, _graphics);
        Camera.Main.UsePixelCoordinates();

        // Mirrors what FRB's Glue generates into Game1.Initialize (GumGame1CodeGenerator.cs).
        global::GumRuntime.ElementSaveExtensions.CustomCreateGraphicalComponentFunc =
            (name, managers) => global::Gum.Wireframe.RuntimeObjectCreator.TryHandleAsBaseType(name, managers);
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        GraphicalUiElement.UpdateFontFromProperties = CustomSetPropertyOnRenderable.UpdateToFontValues;
        GraphicalUiElement.ThrowExceptionsForMissingFiles = CustomSetPropertyOnRenderable.ThrowExceptionsForMissingFiles;
        GraphicalUiElement.AddRenderableToManagers = CustomSetPropertyOnRenderable.AddRenderableToManagers;
        GraphicalUiElement.RemoveRenderableFromManagers = CustomSetPropertyOnRenderable.RemoveRenderableFromManagers;
        GraphicalUiElement.CloneRenderableFunction = global::RenderingLibrary.Graphics.RenderableCloneLogic.Clone;

        // GumIdb.StaticInitialize requires a .gumx; an empty project is enough for code-built scenes.
        string directory = Path.Combine(Path.GetTempPath(), "frb-gum-rendering-tests");
        Directory.CreateDirectory(directory);
        string gumx = Path.Combine(directory, "Empty.gumx");
        new GumProjectSave().Save(gumx, false);
        GumIdb.StaticInitialize(gumx);

        // Mirrors Glue's generated GlobalContent setup: Gum loads textures through FRB's content manager.
        global::RenderingLibrary.Content.LoaderManager.Self.ContentLoader = new ContentManagerWrapper
        {
            ContentManagerName = FlatRedBallServices.GlobalContentManager,
        };

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        FlatRedBallServices.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        FlatRedBallServices.Draw();
        if (_capture)
        {
            _captured = new Color[Width * Height];
            GraphicsDevice.GetBackBufferData(_captured);
            _capture = false;
        }
        base.Draw(gameTime);
    }

    /// <summary>Draws the elements through FRB and returns the backbuffer, row-major, Width x Height.</summary>
    public Color[] Render(params GraphicalUiElement[] topLevelElements)
    {
        foreach (GraphicalUiElement element in topLevelElements)
        {
            element.AddToManagers(SystemManagers.Default, null);
            element.UpdateLayout();
        }

        RunOneFrame(); // settles render-target bakes
        _capture = true;
        RunOneFrame();

        foreach (GraphicalUiElement element in topLevelElements)
        {
            element.RemoveFromManagers();
        }
        return _captured!;
    }
}
