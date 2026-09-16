using System;
using System.IO;
using System.Runtime.InteropServices;
using Gum;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameAndGum.Renderables;
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

    private string _elementName;
    private DateTime _lastSelectionFileWriteTimeUtc;
    private double _secondsSinceLastSelectionPoll;

    /// <param name="contentRootDirectory">
    /// Overrides where relative content (fonts, textures) resolves from after load (issue #4748).
    /// Needed when <paramref name="gumxPath"/> is a temporary JSON copy of a .gumx project elsewhere
    /// (the Native AOT build can't load .gumx directly) - content still lives beside the original
    /// .gumx, not the temp copy. Null for an ordinary project, where the project's own directory is
    /// already correct.
    /// </param>
    public Game1(string gumxPath, string elementName, string? selectionFilePath, string? contentRootDirectory = null)
    {
        _gumxPath = gumxPath;
        _elementName = elementName;
        _selectionFilePath = selectionFilePath;
        _contentRootDirectory = contentRootDirectory;

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

        GumService.Default.EnableHotReload(_gumxPath);

        ApplyCanvasSizeFromProject();
        ShowElement(_elementName);

        if (_selectionFilePath != null && File.Exists(_selectionFilePath))
        {
            _lastSelectionFileWriteTimeUtc = File.GetLastWriteTimeUtc(_selectionFilePath);
        }

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);
        PollSelectionFile(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumService.Default.Draw();
        base.Draw(gameTime);
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

        string[] lines;
        try
        {
            lines = File.ReadAllLines(_selectionFilePath);
        }
        catch (IOException)
        {
            // The tool may still be mid-write; try again on the next poll rather than skipping it.
            return;
        }

        _lastSelectionFileWriteTimeUtc = writeTimeUtc;

        // A second "activate" line means this write came from an explicit re-click of the tool's
        // Preview button (see PreviewLauncher.BuildSelectionFileContent), so this process - not the
        // tool, which has no cross-platform way to foreground another process's window - raises its
        // own window. A passive selection change while browsing the tool's tree omits that line, so
        // the preview updates live without stealing focus (issue #4717 follow-up).
        if (lines.Length > 1 && lines[1].Trim() == "activate")
        {
            ActivateWindow();
        }

        string newElementName = lines.Length > 0 ? lines[0].Trim() : string.Empty;
        if (string.IsNullOrEmpty(newElementName) || newElementName == _elementName)
        {
            return;
        }

        _elementName = newElementName;
        ShowElement(_elementName);
    }

    // SDL_RaiseWindow is the same call MonoGame's own SDL backend uses internally, so it works
    // uniformly across the Windows/X11/Wayland/macOS backends SDL abstracts.
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_RaiseWindow(IntPtr window);

    private void ActivateWindow()
    {
        try
        {
            SDL_RaiseWindow(Window.Handle);
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
    }

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

    private void HandleClientSizeChanged(object? sender, EventArgs e)
    {
        GraphicalUiElement.CanvasWidth = GraphicsDevice.Viewport.Width;
        GraphicalUiElement.CanvasHeight = GraphicsDevice.Viewport.Height;
    }

    private void ShowElement(string elementName)
    {
        GumService.Default.Root.Children.Clear();

        ElementSave? element = ObjectFinder.Self.GetElementSave(elementName);
        if (element == null)
        {
            Console.Error.WriteLine($"GumPreview: no screen or component named '{elementName}' in {_gumxPath}.");
            UpdateWindowTitle(missingElement: true);
            return;
        }

        element.ToGraphicalUiElement().AddToRoot();
        UpdateWindowTitle();
    }

    private void UpdateWindowTitle(bool missingElement = false) =>
        Window.Title = missingElement
            ? $"Gum Preview - element not found: {_elementName}"
            : $"Gum Preview - {_elementName}";
}
