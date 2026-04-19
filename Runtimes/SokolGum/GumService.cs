using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using ToolsUtilities;
using static Sokol.SApp;

namespace SokolGum;

/// <summary>
/// High-level entry point for SokolGum. Mirrors the shape of
/// <c>MonoGameGum.GumService</c> so the Sokol samples look like the
/// documented MonoGame usage. Handles SystemManagers construction,
/// content-root wiring, optional .gumx loading, and a <see cref="Root"/>
/// container that elements get parented to via the <c>AddToRoot</c> extension.
/// </summary>
public sealed class GumService
{
    #region Default

    static GumService _default = default!;

    public static GumService Default => _default ??= new GumService();

    #endregion

    private GumService()
    {
        Root = new Gum.GueDeriving.ContainerRuntime
        {
            Width = 0,
            WidthUnits = DimensionUnitType.RelativeToParent,
            Height = 0,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Name = "Main Root",
            HasEvents = false,
        };
    }

    private SystemManagers? _systemManagers;

    public SystemManagers SystemManagers
    {
        get => _systemManagers ?? throw new InvalidOperationException(
            "GumService has not been initialized. Call GumService.Default.Initialize() first.");
        private set => _systemManagers = value;
    }

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
    public InteractiveGue Root { get; }

    /// <summary>Gets or sets the logical canvas width.</summary>
    public float CanvasWidth
    {
        get => GraphicalUiElement.CanvasWidth;
        set => GraphicalUiElement.CanvasWidth = value;
    }

    /// <summary>Gets or sets the logical canvas height.</summary>
    public float CanvasHeight
    {
        get => GraphicalUiElement.CanvasHeight;
        set => GraphicalUiElement.CanvasHeight = value;
    }

    public bool IsInitialized { get; private set; }

    #region Initialize

    /// <summary>
    /// Initializes SokolGum and (optionally) loads a .gumx project. Call
    /// once at startup before showing any screens.
    /// </summary>
    /// <param name="gumProjectFile">
    /// Optional path to a .gumx project file. Relative paths are resolved
    /// against the current working directory. When omitted, Gum operates in
    /// code-only mode and screens are built programmatically.
    /// </param>
    /// <returns>The loaded project, or null if no path was supplied.</returns>
    public GumProjectSave? Initialize(string? gumProjectFile = null)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Initialize has already been called once. It cannot be called again.");
        }
        IsInitialized = true;

        SystemManagers = new SystemManagers();
        SystemManagers.Default = SystemManagers;
        ISystemManagers.Default = SystemManagers;
        SystemManagers.Initialize();

        Root.AddToManagers(SystemManagers, layer: null);
        Root.UpdateLayout();

        var mainLayer = SystemManagers.Renderer.MainLayer;
        if (Root.RenderableComponent is IRenderableIpso rootRenderable)
        {
            mainLayer.Remove(rootRenderable);
            mainLayer.Insert(0, rootRenderable);
        }

        // Default the logical canvas to the current window size.
        // Callers wanting a fixed design size set CanvasWidth/Height after Initialize.
        if (GraphicalUiElement.CanvasWidth == 0f)
        {
            GraphicalUiElement.CanvasWidth = sapp_width();
        }
        if (GraphicalUiElement.CanvasHeight == 0f)
        {
            GraphicalUiElement.CanvasHeight = sapp_height();
        }

        if (!string.IsNullOrEmpty(gumProjectFile))
        {
            var gumxPath = Path.IsPathRooted(gumProjectFile)
                ? gumProjectFile
                : Path.Combine(Directory.GetCurrentDirectory(), gumProjectFile);

            var project = GumProjectSave.Load(gumxPath, out var loadResult);

            if (project == null || !string.IsNullOrEmpty(loadResult.ErrorMessage))
            {
                throw new InvalidOperationException(
                    $"Failed to load {gumxPath}: {loadResult.ErrorMessage}");
            }

            // Anchor relative asset lookups (SourceFile, Font) at the .gumx's directory.
            // Mirrors MonoGameGum.GumService — CustomSetPropertyOnRenderable prepends
            // RelativeDirectory before handing paths to the ContentLoader.
            FileManager.RelativeDirectory = FileManager.GetDirectory(gumxPath);

            ObjectFinder.Self.GumProjectSave = project;

            // Initialize() fixes serialized boxed-int enum values (e.g. XUnits stored
            // as xsd:int) back to their proper typed enums, and hydrates every standard
            // element's DefaultState. Skipping this causes NotImplementedException in
            // UnitConverter.ConvertToGeneralUnit when loading any screen that uses
            // non-default unit values. Matches what MonoGameGum.GumService does.
            project.Initialize();

            Project = project;
        }

        return Project;
    }

    #endregion

    #region Uninitialize

    /// <summary>
    /// Tears down this GumService instance, releasing GPU resources and
    /// resetting static state so that Initialize can be called again.
    /// </summary>
    public void Uninitialize()
    {
        Root.Children.Clear();
        Root.RemoveFromManagers();

        ElementSaveExtensions.ClearRegistrations();

        ObjectFinder.Self.GumProjectSave = null;

        LoaderManager.Self.DisposeAndClear();

        GraphicalUiElement.SetPropertyOnRenderable = null!;
        GraphicalUiElement.AddRenderableToManagers = null;
        GraphicalUiElement.RemoveRenderableFromManagers = null;

        GraphicalUiElement.CanvasWidth = 0;
        GraphicalUiElement.CanvasHeight = 0;

        SystemManagers.Default = null;
        ISystemManagers.Default = null;

        FileManager.RelativeDirectory = "Content/";

        IsInitialized = false;
        _systemManagers = null;
        _default = null!;
    }

    #endregion

    #region Update / Draw

    /// <summary>
    /// Per-frame tick. Advances sprite animation chains on <see cref="Root"/>.
    /// Uses <c>sapp_frame_duration()</c> for the delta.
    /// </summary>
    public void Update() => Update(sapp_frame_duration());

    /// <inheritdoc cref="Update()"/>
    /// <param name="secondsSinceLastFrame">Elapsed seconds — pass this to scale or pause time.</param>
    public void Update(double secondsSinceLastFrame)
    {
        Root.AnimateSelf(secondsSinceLastFrame);
        SystemManagers.Renderer.Update(secondsSinceLastFrame);
    }

    /// <summary>
    /// Renders the current Gum scene. Logical canvas comes from
    /// <c>GraphicalUiElement.CanvasWidth/Height</c>; framebuffer size is
    /// queried from <c>sapp_width()/sapp_height()</c> each frame so window
    /// resizes are self-healing.
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

    #endregion
}

/// <summary>
/// Extension methods matching the shape of MonoGameGum's equivalents so
/// Sokol sample code reads identically to the documented patterns.
/// </summary>
public static class ElementSaveExtensionMethods
{
    /// <summary>
    /// Instantiates a GraphicalUiElement from the supplied ElementSave using
    /// the GumService's SystemManagers. Matches
    /// <c>MonoGameGum.ElementSaveExtensionMethods.ToGraphicalUiElement</c>.
    /// Callers pair this with <see cref="GraphicalUiElementExtensionMethods.AddToRoot"/>.
    /// </summary>
    public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, SystemManagers? systemManagers = null)
    {
        systemManagers ??= GumService.Default.SystemManagers;
        return elementSave.ToGraphicalUiElement(systemManagers, addToManagers: false);
    }
}

/// <summary>
/// Extension methods on <see cref="GraphicalUiElement"/> for adding/removing
/// elements from the GumService root container.
/// </summary>
public static class GraphicalUiElementExtensionMethods
{
    /// <summary>
    /// Adds this element as a child of the GumService root container,
    /// making it a top-level element that will be rendered.
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
