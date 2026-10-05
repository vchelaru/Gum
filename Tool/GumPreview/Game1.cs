using System;
using System.IO;
using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.Input;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Graphics;
using ToolsUtilities;

namespace GumPreview;

/// <summary>
/// Loads the .gumx/.gumj passed on the command line and shows the requested screen/component, live
/// and interactive (issue #4697). No codegen, no recompile - Forms controls work out of the box via
/// <c>GumService.Initialize</c>'s from-file registration (see the gum-forms-behaviors skill).
/// </summary>
public class Game1 : Game
{
    private const double SelectionPollIntervalSeconds = 0.25;
    private const int FallbackCanvasWidth = 800;
    private const int FallbackCanvasHeight = 600;

    private readonly GraphicsDeviceManager _graphics;
    private readonly string _gumxPath;
    private readonly string? _selectionFilePath;
    private readonly string? _contentRootDirectory;
    private readonly UnattendedPreviewRun? _unattended;
    private readonly string? _screenshotPath;

    private PreviewSelectionMessage _selection;
    private DateTime _lastSelectionFileWriteTimeUtc;
    private double _secondsSinceLastSelectionPoll;
    private bool _isElementMissing;
    private bool _isRetinaBackingEnabled;
    private BackingScale _backingScale;

    /// <summary>The process exit code: 0, or 1 when an unattended run failed.</summary>
    public int UnattendedExitCode { get; private set; }

    /// <param name="contentRootDirectory">
    /// Overrides where relative content (fonts, textures) resolves from after load (issue #4748).
    /// Needed when <paramref name="gumxPath"/> is a temporary JSON copy of a .gumx project elsewhere
    /// (the Native AOT build can't load .gumx directly) - content still lives beside the original
    /// .gumx, not the temp copy. Null for an ordinary project, where the project's own directory is
    /// already correct.
    /// </param>
    /// <param name="unattended">Set for an <c>--exit-after</c> run: the first drawn frame ends it.</param>
    /// <param name="screenshotPath">With <paramref name="unattended"/>, where that frame is saved as a PNG.</param>
    public Game1(string gumxPath, string elementName, string? selectionFilePath, string? contentRootDirectory = null,
        UnattendedPreviewRun? unattended = null, string? screenshotPath = null)
    {
        _gumxPath = gumxPath;
        _selection = new PreviewSelectionMessage(elementName);
        _selectionFilePath = selectionFilePath;
        _contentRootDirectory = contentRootDirectory;
        _unattended = unattended;
        _screenshotPath = screenshotPath;
        _backingScale = new BackingScale(1);

        _graphics = new GraphicsDeviceManager(this);
        // Apos.Shapes (the shape fill/effect renderer behind RectangleRuntime/CircleRuntime/etc.)
        // uses a Shader Model 4 effect, which requires HiDef - Reach cannot load it and shapes
        // silently fail to draw.
        _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += HandleClientSizeChanged;
        UpdateWindowTitle();
    }

    protected override void Initialize()
    {
        GumService.Default.Initialize(this, _gumxPath);

        // GumService.Initialize just set this to _gumxPath's own directory - override it back to the
        // original project's directory when _gumxPath is actually a temporary converted copy
        // elsewhere (issue #4748), so relative content (fonts, textures) still resolves correctly.
        if (!string.IsNullOrEmpty(_contentRootDirectory))
        {
            FileManager.RelativeDirectory = _contentRootDirectory;
        }

        // Must come after GumService.Default.Initialize - ShapeRenderer.Initialize reads the
        // initialized GumService/GraphicsDevice.
        ShapeRenderer.Self.Initialize();

        // The window is sized for the backing scale on the first Update, once it is showing; a resize
        // here is undone when MonoGame shows it.
        _isRetinaBackingEnabled = MacRetinaBacking.TryEnable(Window.Handle);

        GumService.Default.EnableHotReload(_gumxPath);
        // A reload re-applies the default state over the whole tree, undoing the selected state.
        GumService.Default.HotReloadCompleted += ApplySelectedState;

        ApplyCanvasSizeFromProject();

        // The tool writes the full selection (state, orderer) before launching; --element alone is
        // the fallback for a launch with no selection file.
        if (_selectionFilePath != null && File.Exists(_selectionFilePath))
        {
            _lastSelectionFileWriteTimeUtc = File.GetLastWriteTimeUtc(_selectionFilePath);
            PreviewSelectionMessage? initial = TryReadSelectionFile();
            if (initial != null)
            {
                _selection = initial;
            }
        }
        ApplySiblingOrdering();
        ShowElement();

        if (_unattended != null)
        {
            if (_isElementMissing)
            {
                FailUnattended(_unattended.TryFail("the element to show was not found."));
            }
            else
            {
                _unattended.MarkLoaded();
            }
        }

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        UpdateBackingScale();
        GumService.Default.Update(gameTime);
        PollSelectionFile(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_unattended != null && _unattended.TryClaimCapture())
        {
            FinishUnattended();
        }

        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumService.Default.Draw();
        base.Draw(gameTime);
    }

    private void FinishUnattended()
    {
        if (_screenshotPath != null)
        {
            try
            {
                SaveFrame(_screenshotPath);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"GumPreview: could not write the screenshot {_screenshotPath}: {exception.Message}");
                UnattendedExitCode = 1;
                Exit();
                return;
            }
            Console.WriteLine($"GumPreview: wrote {_screenshotPath}");
        }
        Exit();
    }

    // Draws a frame into an offscreen target of the canvas size and saves it. The window's own
    // back buffer can't be used: on the first frame its drawable may not have been resized to the
    // project's canvas yet, and a window partly off the screen doesn't keep every pixel.
    private void SaveFrame(string path)
    {
        int width = (int)GraphicalUiElement.CanvasWidth;
        int height = (int)GraphicalUiElement.CanvasHeight;
        using RenderTarget2D target = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
        GraphicsDevice.SetRenderTarget(target);
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumService.Default.Draw();
        GraphicsDevice.SetRenderTarget(null);

        // Blending leaves partial alpha where translucent content was drawn; the window shows the
        // frame opaque, so the PNG is too.
        Color[] pixels = new Color[width * height];
        target.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i].A = 255;
        }
        using Texture2D opaque = new Texture2D(GraphicsDevice, width, height);
        opaque.SetData(pixels);

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        using FileStream stream = File.Create(path);
        opaque.SaveAsPng(stream, width, height);
    }

    private void FailUnattended(string? message)
    {
        if (message == null)
        {
            return;
        }
        Console.Error.WriteLine(message);
        UnattendedExitCode = 1;
        Exit();
    }

    // The tool has no live channel to this process, so it hands off new selections by rewriting
    // this file (see PreviewLauncher.PushSelection in Tool/EditorTabPlugin.Core); polling the
    // file's last-write time is simpler and more robust across platforms than a named pipe.
    private void PollSelectionFile(GameTime gameTime)
    {
        if (_selectionFilePath == null)
        {
            return;
        }

        _secondsSinceLastSelectionPoll += gameTime.ElapsedGameTime.TotalSeconds;
        if (_secondsSinceLastSelectionPoll < SelectionPollIntervalSeconds)
        {
            return;
        }
        _secondsSinceLastSelectionPoll = 0;

        if (!File.Exists(_selectionFilePath))
        {
            return;
        }

        DateTime writeTimeUtc = File.GetLastWriteTimeUtc(_selectionFilePath);
        if (writeTimeUtc <= _lastSelectionFileWriteTimeUtc)
        {
            return;
        }

        PreviewSelectionMessage? message = TryReadSelectionFile();
        if (message == null)
        {
            // The tool may still be mid-write; try again on the next poll rather than skipping it.
            return;
        }

        _lastSelectionFileWriteTimeUtc = writeTimeUtc;

        // Activate is set only for an explicit re-click of the tool's Preview button, so this
        // process - not the tool, which has no cross-platform way to foreground another process's
        // window - raises its own window. A passive selection change while browsing the tool's
        // tree leaves it unset, so the preview updates live without stealing focus (issue #4717
        // follow-up).
        if (message.Activate)
        {
            ActivateWindow();
        }

        bool selectionChanged = !message.HasSameSelection(_selection);
        _selection = message;
        ApplySiblingOrdering();
        if (selectionChanged)
        {
            ShowElement();
        }
    }

    private PreviewSelectionMessage? TryReadSelectionFile() => PreviewSelectionFile.TryRead(_selectionFilePath!);

    // Mirrors the tool's Performance panel "Sort by batch" option so the preview renders in the
    // same sibling order the tool's canvas does (issue #4856).
    private void ApplySiblingOrdering() =>
        Renderer.SiblingOrdering = _selection.SortByBatchKey ? BatchKeyGroupedOrderer.Instance : HierarchicalOrderer.Instance;

    private void ActivateWindow() => SdlLibrary.TryRaiseWindow(Window.Handle);

    private void ApplyCanvasSizeFromProject()
    {
        GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
        int width = project != null && project.DefaultCanvasWidth > 0 ? project.DefaultCanvasWidth : FallbackCanvasWidth;
        int height = project != null && project.DefaultCanvasHeight > 0 ? project.DefaultCanvasHeight : FallbackCanvasHeight;

        _graphics.PreferredBackBufferWidth = width;
        _graphics.PreferredBackBufferHeight = height;
        _graphics.ApplyChanges();

        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;
    }

    // Runs every frame: the window can move to a display with a different scale (a Retina laptop
    // and an external monitor), which changes how many pixels each point covers.
    private void UpdateBackingScale()
    {
        if (!_isRetinaBackingEnabled)
        {
            return;
        }
        BackingScale scale = MacRetinaBacking.GetScale(Window.Handle);
        if (scale.Factor != _backingScale.Factor)
        {
            _backingScale = scale;
            FitWindowToBackBuffer();
        }
    }

    // On a Retina display the back buffer stays at the canvas's pixel size and the window shrinks to
    // fit it in points, so Preview draws 1:1 with the tool's canvas (#5571). The cursor arrives in
    // points and is scaled up to pixels.
    private void FitWindowToBackBuffer()
    {
        Cursor? cursor = GumService.Default.Cursor;
        if (cursor != null)
        {
            cursor.TransformMatrix = Matrix.CreateScale((float)_backingScale.Factor);
        }
        PresentationParameters parameters = GraphicsDevice.PresentationParameters;
        (int width, int height) = _backingScale.ToPoints(parameters.BackBufferWidth, parameters.BackBufferHeight);
        MacRetinaBacking.SetWindowSize(Window.Handle, width, height);
    }

    private void HandleClientSizeChanged(object? sender, EventArgs e)
    {
        // MonoGame sets the back buffer to the window's size in points; put it back in pixels.
        if (_isRetinaBackingEnabled)
        {
            (int width, int height) = _backingScale.ToPixels(Window.ClientBounds.Width, Window.ClientBounds.Height);
            GraphicsDevice.PresentationParameters.BackBufferWidth = width;
            GraphicsDevice.PresentationParameters.BackBufferHeight = height;
            GraphicsDevice.Viewport = new Viewport(0, 0, width, height);
        }
        GraphicalUiElement.CanvasWidth = GraphicsDevice.Viewport.Width;
        GraphicalUiElement.CanvasHeight = GraphicsDevice.Viewport.Height;
    }

    private void ShowElement()
    {
        GumService.Default.Root.Children.Clear();

        ElementSave? element = ObjectFinder.Self.GetElementSave(_selection.ElementName);
        if (element == null)
        {
            Console.Error.WriteLine($"GumPreview: no screen or component named '{_selection.ElementName}' in {_gumxPath}.");
            UpdateWindowTitle(missingElement: true);
            _isElementMissing = true;
            return;
        }

        _isElementMissing = false;
        element.ToGraphicalUiElement().AddToRoot();
        ApplySelectedState();
        UpdateWindowTitle();
    }

    // Puts the shown element in the state selected in the tool, on top of its default state - the
    // same thing the tool's own canvas shows for a selected categorized state (issue #4856).
    private void ApplySelectedState()
    {
        if (_selection.StateName == null)
        {
            return;
        }

        foreach (GraphicalUiElement root in GumService.Default.Root.Children)
        {
            if (root.ElementSave != null && root.ElementSave.Name == _selection.ElementName)
            {
                StateSave? state = _selection.FindState(root.ElementSave);
                if (state != null)
                {
                    root.ApplyState(state);
                }
            }
        }
    }

    private void UpdateWindowTitle(bool missingElement = false) =>
        Window.Title = missingElement
            ? $"Gum Preview - element not found: {_selection.ElementName}"
            : $"Gum Preview - {_selection.ElementName}";
}
