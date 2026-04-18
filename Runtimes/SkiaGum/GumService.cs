using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace SkiaGum;
public class GumService
{
    static GumService _default;
    public static GumService Default
    {
        get
        {
            if (_default == null)
            {
                _default = new GumService();
            }
            return _default;
        }
    }

    /// <summary>
    /// Gets whether GumService has been initialized. Used by extension methods
    /// like <see cref="GraphicalUiElementExtensionMethods.AddToRoot(GraphicalUiElement)"/>
    /// to guard against calls made before Initialize.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// The root container that fills the entire canvas. Elements added via
    /// <see cref="GraphicalUiElementExtensionMethods.AddToRoot(GraphicalUiElement)"/>
    /// become children of this container. Null until <c>Initialize</c> is called.
    /// </summary>
    public InteractiveGue Root { get; private set; }

    /// <summary>
    /// Initializes Gum for a Skia canvas, optionally loading a Gum project. The canvas
    /// size is read from <see cref="SKCanvas.DeviceClipBounds"/> to size the root container.
    /// If that does not produce the size you expect (for example if the canvas's clip has not
    /// yet been configured), use the overload that takes explicit width and height instead.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public void Initialize(SKCanvas canvas, string? gumProjectFile = null)
    {
        var bounds = canvas.DeviceClipBounds;
        Initialize(canvas, bounds.Width, bounds.Height, gumProjectFile);
    }

    /// <summary>
    /// Initializes Gum for a Skia canvas with an explicit canvas size, optionally loading
    /// a Gum project.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="width">The width to use for the root container and canvas coordinate space.</param>
    /// <param name="height">The height to use for the root container and canvas coordinate space.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public void Initialize(SKCanvas canvas, int width, int height, string? gumProjectFile = null)
    {
        // SkiaGum relies on ModuleInitializer instead of explicitly registering
        // runtimes.
        SystemManagers.Default = new SystemManagers();
        SystemManagers.Default.Canvas = canvas;
        SystemManagers.Default.Initialize();
        SystemManagers.Default.Renderer.ClearsCanvas = false;

        GumProjectSave gumProject = null;

        if (!string.IsNullOrEmpty(gumProjectFile))
        {

            gumProject = GumProjectSave.Load(gumProjectFile);
            ObjectFinder.Self.GumProjectSave = gumProject;
            gumProject.Initialize();
            //    FormsUtilities.RegisterFromFileFormRuntimeDefaults();

            var absolutePath = FileManager.IsRelative(gumProjectFile)
                ? FileManager.MakeAbsolute(gumProjectFile)
                : gumProjectFile;
            var gumDirectory = FileManager.GetDirectory(absolutePath);

            FileManager.RelativeDirectory = gumDirectory;
        }

        // Size the canvas coordinate space before configuring Root, so the
        // RelativeToParent root has something to resolve against.
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;

        Root = new ContainerRuntime
        {
            Width = 0,
            WidthUnits = DimensionUnitType.RelativeToParent,
            Height = 0,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Name = "Main Root",
            HasEvents = false,
        };

        Root.AddToManagers(SystemManagers.Default);
        Root.UpdateLayout();

        IsInitialized = true;
    }

    /// <summary>
    /// Updates the canvas coordinate space and re-runs layout on the root container.
    /// Call this from your platform's window-resized callback so Gum-layouted elements
    /// reposition to match the new window size.
    /// </summary>
    /// <param name="width">The new canvas width.</param>
    /// <param name="height">The new canvas height.</param>
    public void HandleResize(int width, int height)
    {
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;
        Root?.UpdateLayout();
    }

    public void Draw()
    {
        SystemManagers.Default.Draw();
    }
}
