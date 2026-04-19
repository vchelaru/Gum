using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using ToolsUtilities;
using static Sokol.SApp;

namespace SokolGum;

/// <summary>
/// High-level entry point for SokolGum. Mirrors the shape of
/// <c>MonoGameGum.GumService</c> so the Sokol samples look like the
/// documented MonoGame usage (see
/// https://docs.flatredball.com/gum/code/getting-started/tutorials/gum-project-forms-tutorial/setup).
/// Intentionally minimal: handles SystemManagers construction, content-root
/// wiring, optional .gumx loading, and a <see cref="Root"/> container that
/// elements get parented to via the <c>AddToRoot</c> extension. Does NOT
/// yet cover input, update loops, Forms wiring, or the deferred queue —
/// those are tracked in .claude/designs/runtime-refactoring.md as a future
/// full port of GumService to Sokol.
/// </summary>
public sealed class GumService
{
    public static GumService Default { get; } = new();

    private GumService() { }

    public SystemManagers SystemManagers { get; private set; } = null!;

    /// <summary>
    /// The loaded project, or null if <see cref="Initialize"/> was called
    /// without a .gumx path (code-only mode).
    /// </summary>
    public GumProjectSave? Project { get; private set; }

    /// <summary>
    /// The root container. Elements added via <c>AddToRoot</c> become
    /// top-level visuals and participate in layout and draw. Conceptually
    /// matches <c>MonoGameGum.GumService.Root</c>.
    /// </summary>
    public InteractiveGue Root { get; private set; } = null!;

    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Initializes SokolGum and (optionally) loads a .gumx project. Call
    /// once at startup before showing any screens.
    /// </summary>
    /// <param name="gumProjectFile">
    /// Optional path to a .gumx project file. Relative paths are resolved
    /// against the current working directory. When a project is loaded,
    /// <see cref="FileManager.RelativeDirectory"/> is set to the directory
    /// containing the .gumx so relative <c>SourceFile</c> / <c>Font</c>
    /// references inside the project resolve as the Gum editor sees them
    /// (project root = asset root). Pass null (the default) for code-only
    /// scenarios where screens are built programmatically instead of
    /// loaded from disk.
    /// </param>
    /// <returns>The loaded project, or null if no path was supplied.</returns>
    public GumProjectSave? Initialize(string? gumProjectFile = null)
    {
        SystemManagers = new SystemManagers();
        SystemManagers.Default = SystemManagers;
        SystemManagers.Initialize();

        if (gumProjectFile != null)
        {
            var gumxPath = Path.IsPathRooted(gumProjectFile)
                ? gumProjectFile
                : Path.Combine(Directory.GetCurrentDirectory(), gumProjectFile);
            Project = LoadProject(gumxPath);

            // Anchor relative asset lookups at the .gumx's directory —
            // mirrors MonoGameGum.GumService. CustomSetPropertyOnRenderable
            // prepends RelativeDirectory to any SourceFile / Font value
            // before handing it to the ContentLoader.
            FileManager.RelativeDirectory = FileManager.GetDirectory(gumxPath);
        }

        // Root spans the canvas so RelativeToParent children resolve.
        // Matches the Raylib branch of MonoGame's GumService: Root is
        // registered with the layer via AddToManagers, then its renderable
        // is moved to index 0 of MainLayer so anything added to
        // Root.Children draws above (not under) it.
        Root = new Gum.GueDeriving.ContainerRuntime
        {
            Width = 0,
            WidthUnits = DimensionUnitType.RelativeToParent,
            Height = 0,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Name = "Main Root",
            HasEvents = false,
        };
        Root.AddToManagers(SystemManagers, layer: null);
        Root.UpdateLayout();

        var mainLayer = SystemManagers.Renderer.MainLayer;
        if (Root.RenderableComponent is IRenderableIpso rootRenderable)
        {
            mainLayer.Remove(rootRenderable);
            mainLayer.Insert(0, rootRenderable);
        }

        // Default the logical canvas to the current window size, matching
        // what the Raylib sample does manually before InitWindow. Callers
        // wanting a fixed design size independent of the window set these
        // explicitly after Initialize returns.
        if (GraphicalUiElement.CanvasWidth == 0f)
        {
            GraphicalUiElement.CanvasWidth = sapp_width();
        }
        if (GraphicalUiElement.CanvasHeight == 0f)
        {
            GraphicalUiElement.CanvasHeight = sapp_height();
        }

        IsInitialized = true;
        return Project;
    }

    /// <summary>
    /// Per-frame tick. Advances sprite animation chains and runs the
    /// GraphicalUiElement animation hook on <see cref="Root"/>. Call once
    /// per frame, typically just before <see cref="Draw"/>. With no
    /// argument, uses <c>sapp_frame_duration()</c> for the delta.
    /// </summary>
    public void Update() => Update(sapp_frame_duration());

    /// <inheritdoc cref="Update()"/>
    /// <param name="secondsSinceLastFrame">Elapsed seconds override — pass this to scale or pause time.</param>
    public void Update(double secondsSinceLastFrame)
    {
        Root.AnimateSelf(secondsSinceLastFrame);
        SystemManagers.Renderer.Update(secondsSinceLastFrame);
    }

    /// <summary>
    /// Renders the current Gum scene. Logical canvas comes from
    /// <c>GraphicalUiElement.CanvasWidth/Height</c>; framebuffer size is
    /// queried from <c>sapp_width()/sapp_height()</c> each frame (so
    /// window resizes are self-healing). Wraps <c>Renderer.BeginFrame</c>
    /// / <c>Renderer.Draw</c> / <c>Renderer.EndFrame</c>. The native
    /// sokol_gfx pass boundaries (<c>sg_begin_pass</c> / <c>sg_end_pass</c>
    /// / <c>sg_commit</c>) stay in caller code because multi-pass setups
    /// may want to wrap Gum in a pass the service can't know about.
    /// </summary>
    public void Draw()
    {
        var renderer = SystemManagers.Renderer;
        renderer.BeginFrame(
            logicalWidth: (int)GraphicalUiElement.CanvasWidth,
            logicalHeight: (int)GraphicalUiElement.CanvasHeight,
            framebufferWidth: sapp_width(),
            framebufferHeight: sapp_height());
        renderer.Draw(SystemManagers);
        renderer.EndFrame(SystemManagers);
    }

    private static GumProjectSave LoadProject(string gumxPath)
    {
        var project = GumProjectSave.Load(gumxPath, out var result)
            ?? throw new InvalidOperationException(
                $"Failed to load {gumxPath}: {result.ErrorMessage}");

        // GumProjectSave.Load creates bare stubs for any
        // <StandardElementReference> whose companion .gutx file is missing
        // on disk. Hydrate each with its in-memory default state so
        // DefaultState is non-null when Gum walks instances during
        // ToGraphicalUiElement.
        foreach (var std in project.StandardElements)
        {
            if (std.States.Count == 0
                && StandardElementsManager.Self.DefaultStates.TryGetValue(std.Name, out var defaultState))
            {
                std.Initialize(defaultState);
            }
        }
        ObjectFinder.Self.GumProjectSave = project;
        return project;
    }
}

/// <summary>
/// Extensions matching the shape of MonoGameGum's and SkiaGum's equivalents
/// so Sokol sample code reads identically to the documented patterns.
/// </summary>
public static class ElementSaveExtensionMethods
{
    /// <summary>
    /// Instantiates a GraphicalUiElement from the supplied ElementSave using
    /// the GumService's SystemManagers by default. Matches
    /// <c>MonoGameGum.ElementSaveExtensionMethods.ToGraphicalUiElement</c>.
    /// Uses <c>addToManagers: false</c> — callers are expected to pair with
    /// <see cref="GraphicalUiElementExtensionMethods.AddToRoot"/>.
    /// </summary>
    public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, SystemManagers? systemManagers = null)
    {
        systemManagers ??= GumService.Default.SystemManagers;
        return elementSave.ToGraphicalUiElement(systemManagers, addToManagers: false);
    }
}

/// <summary>
/// Extension methods on <see cref="GraphicalUiElement"/> for adding
/// elements to and removing them from the GumService root container.
/// </summary>
public static class GraphicalUiElementExtensionMethods
{
    /// <summary>
    /// Adds this element as a child of the GumService root container,
    /// making it a top-level element that will be rendered and receive
    /// layout updates.
    /// </summary>
    public static void AddToRoot(this GraphicalUiElement element)
    {
        if (!GumService.Default.IsInitialized)
        {
            throw new InvalidOperationException(
                "Cannot call AddToRoot because GumService.Default is not initialized — "
                + "call GumService.Default.Initialize(...) first.");
        }
        GumService.Default.Root.Children.Add(element);
    }

    /// <summary>
    /// Removes this element from its parent, reversing a previous
    /// <see cref="AddToRoot"/> call.
    /// </summary>
    public static void RemoveFromRoot(this GraphicalUiElement element)
    {
        element.Parent = null;
    }
}
