using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SDL;
using ToolsUtilities;

namespace Sdl3GumSample.Backend;

/// <summary>
/// Everything that connects GumCommon to SDL3: one-time wiring, .gumx loading, and a draw walk.
/// </summary>
public static unsafe class SdlGum
{
    /// <summary>
    /// Wires Gum to SDL. <paramref name="contentDirectory"/> is the folder relative file names
    /// (a .gumx project's SourceFile values, for example) resolve against.
    /// </summary>
    public static void Initialize(SDL_Renderer* renderer, string contentDirectory)
    {
        SdlTexture.Renderer = renderer;

        // When Gum builds an element from a .gumx project, it asks this factory for the renderable
        // behind each standard type. Returning null for a type (Text, Rectangle, ...) leaves it
        // undrawn; its layout and children still work.
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = CreateRenderable;

        // Variables with no custom handler are assigned to the renderable's public properties by
        // name (GraphicalUiElement.SetPropertyOnRenderable defaults to reflection), so SdlSprite
        // only has to expose SourceFile.

        FileManager.RelativeDirectory = contentDirectory;
    }

    private static IRenderable? CreateRenderable(string standardTypeName, ISystemManagers managers) =>
        standardTypeName switch
        {
            "Sprite" => new SdlSprite(),
            "NineSlice" => new SdlNineSlice(),
            "Container" => new InvisibleRenderable(),
            _ => null
        };

    /// <summary>
    /// Loads a .gumx project and builds one of its screens. Call after <see cref="Initialize"/>.
    /// </summary>
    public static GraphicalUiElement LoadScreen(string gumxFile, string screenName)
    {
        GumProjectSave? project = GumProjectSave.Load(gumxFile, out GumLoadResult result);
        if (project == null || !string.IsNullOrEmpty(result.ErrorMessage))
        {
            throw new InvalidOperationException($"Could not load {gumxFile}: {result.ErrorMessage}");
        }

        ObjectFinder.Self.GumProjectSave = project;
        project.Initialize();
        // The project's SourceFile values are relative to the .gumx folder.
        FileManager.RelativeDirectory = FileManager.GetDirectory(Path.GetFullPath(gumxFile));

        ScreenSave screen = ObjectFinder.Self.GetScreen(screenName)
            ?? throw new ArgumentException($"{gumxFile} has no screen named {screenName}");
        // No SystemManagers: this backend draws by walking the tree itself (see Draw below).
        return screen.ToGraphicalUiElement(systemManagers: null!, addToManagers: false);
    }

    /// <summary>
    /// Draws <paramref name="root"/> and its descendants in tree order (parents before children,
    /// siblings in order). Layout must be current; <c>GraphicalUiElement</c> keeps it current as
    /// properties change, and <see cref="GraphicalUiElement.UpdateLayout()"/> refreshes it after a resize.
    /// </summary>
    public static void Draw(GraphicalUiElement root, SDL_Renderer* renderer)
    {
        if (root.RenderableComponent is IRenderableIpso renderable)
        {
            Draw(renderable, renderer);
        }
    }

    private static void Draw(IRenderableIpso renderable, SDL_Renderer* renderer)
    {
        if (!renderable.Visible)
        {
            return;
        }

        // A renderable's Children holds the child GraphicalUiElements (layout wrappers), not their
        // renderables, so reach through to the object that actually draws.
        IRenderable? drawn = renderable is IHasRenderableComponent wrapper ? wrapper.RenderableComponent : renderable;
        if (drawn is SdlSprite sprite)
        {
            sprite.Draw(renderer);
        }

        foreach (IRenderableIpso child in renderable.Children)
        {
            Draw(child, renderer);
        }
    }
}
