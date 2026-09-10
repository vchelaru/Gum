using System.Diagnostics;
using Gum;
using Gum.DataTypes;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameAndGum.Renderables;
using RenderingLibrary;

namespace AvaloniaCanvasSpike;

/// <summary>
/// Runs the XNA-family backend as a <see cref="Game"/> with a 1x1 off-screen window, exactly as
/// the CLI's screenshot service does, but stepped one frame at a time from the host's thread
/// through <see cref="RenderFrame"/> instead of owning a loop. Everything happens on the calling
/// thread, which keeps SDL and the host's UI toolkit on the main thread on macOS.
/// </summary>
public sealed class GumCanvasGame : Game, ICanvasRenderer
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly string _projectPath;
    private readonly string? _elementName;
    private readonly Stopwatch _frameStopwatch;

    private RenderTarget2D? _renderTarget;
    private GraphicalUiElement? _element;
    private float _zoom;
    private bool _hasRunFirstFrame;
    private byte[] _pixelBuffer;

    /// <summary>
    /// Creates the game but does not initialize the device or load the project until the first
    /// <see cref="RenderFrame"/> or <see cref="Resize"/>.
    /// </summary>
    /// <param name="projectPath">Absolute path to a .gumx or .gumj file.</param>
    /// <param name="elementName">Screen or component to show; the first screen when null.</param>
    public GumCanvasGame(string projectPath, string? elementName)
    {
        _projectPath = projectPath;
        _elementName = elementName;
        _frameStopwatch = new Stopwatch();
        _pixelBuffer = Array.Empty<byte>();
        _zoom = 1f;
        PixelWidth = 1;
        PixelHeight = 1;
        LoadedElementName = "";

        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1,
            PreferredBackBufferHeight = 1,
            // Apos.Shapes uses an SM4 effect that the default Reach profile can't load (#4403).
            GraphicsProfile = GraphicsProfile.HiDef,
            SynchronizeWithVerticalRetrace = false,
        };
        IsFixedTimeStep = false;
        IsMouseVisible = true;
    }

    /// <inheritdoc/>
    public int PixelWidth { get; private set; }

    /// <inheritdoc/>
    public int PixelHeight { get; private set; }

    /// <inheritdoc/>
    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = Math.Clamp(value, 0.1f, 16f);
            ApplyCanvasSize();
        }
    }

    /// <inheritdoc/>
    public byte[] PixelBuffer => _pixelBuffer;

    /// <inheritdoc/>
    public double LastFrameMilliseconds { get; private set; }

    /// <inheritdoc/>
    public string LoadedElementName { get; private set; }

    /// <inheritdoc/>
    public void Resize(int pixelWidth, int pixelHeight)
    {
        EnsureInitialized();

        PixelWidth = Math.Max(1, pixelWidth);
        PixelHeight = Math.Max(1, pixelHeight);

        _renderTarget?.Dispose();
        _renderTarget = new RenderTarget2D(
            GraphicsDevice,
            PixelWidth,
            PixelHeight,
            mipMap: false,
            SurfaceFormat.Color,
            DepthFormat.None,
            preferredMultiSampleCount: 0,
            RenderTargetUsage.PreserveContents);

        _pixelBuffer = new byte[PixelWidth * PixelHeight * 4];
        ApplyCanvasSize();
    }

    /// <inheritdoc/>
    public void RenderFrame()
    {
        EnsureInitialized();
        _frameStopwatch.Restart();
        // Tick runs Update then Draw on this thread; Draw renders into the target and reads back.
        Tick();
        _frameStopwatch.Stop();
        LastFrameMilliseconds = _frameStopwatch.Elapsed.TotalMilliseconds;
    }

    /// <inheritdoc/>
    public GraphicalUiElement? HitTest(float worldX, float worldY)
    {
        if (_element == null)
        {
            return null;
        }

        return HitTestRecursive(_element, worldX, worldY);
    }

    /// <inheritdoc/>
    protected override void Initialize()
    {
#if !KNI_BACKEND
        // Off-screen so the SDL window never flashes; the host shows the pixels, not this window.
        // KNI's GameWindow has no Position, so its 1x1 window stays where SDL puts it (see README).
        Window.Position = new Point(-10000, -10000);
#endif
        base.Initialize();

        GumService gumService = GumService.Default;
        GumProjectSave? project = gumService.Initialize(this, _projectPath);
        if (project == null)
        {
            throw new InvalidOperationException($"Failed to load Gum project: {_projectPath}");
        }

        // Circle and rounded-rectangle rendering is Apos.Shapes-backed (#4403).
        if (!ShapeRenderer.Self.IsInitialized)
        {
            ShapeRenderer.Self.Initialize();
        }

        ElementSave? elementSave = _elementName != null
            ? project.AllElements.FirstOrDefault(e => e.Name == _elementName)
            : project.Screens.FirstOrDefault();
        if (elementSave == null)
        {
            throw new InvalidOperationException(
                $"Element '{_elementName ?? "(first screen)"}' not found in {_projectPath}");
        }

        _element = elementSave.ToGraphicalUiElement(SystemManagers.Default);
        _element.AddToManagers(SystemManagers.Default, layer: null);
        LoadedElementName = elementSave.Name;
        ApplyCanvasSize();
    }

    /// <inheritdoc/>
    protected override void Update(GameTime gameTime)
    {
        // Input comes from the host, not from this window's mouse; GumService.Update is skipped.
        _element?.UpdateLayout();
    }

    /// <inheritdoc/>
    protected override void Draw(GameTime gameTime)
    {
        if (_renderTarget == null)
        {
            return;
        }

        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(new Color(40, 40, 40, 255));
        GumService.Default.Draw();
        GraphicsDevice.SetRenderTarget(null);

        _renderTarget.GetData(_pixelBuffer);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderTarget?.Dispose();
            _renderTarget = null;
            try
            {
                GumService.Default.Uninitialize();
            }
            catch
            {
                // Best-effort teardown of shared static state.
            }
        }
        base.Dispose(disposing);
    }

    private void EnsureInitialized()
    {
        if (_hasRunFirstFrame)
        {
            return;
        }

        // Initializes the platform, creates the device, calls Initialize, and runs one tick.
        RunOneFrame();
        _hasRunFirstFrame = true;
    }

    private void ApplyCanvasSize()
    {
        if (_element == null)
        {
            return;
        }

        GumService gumService = GumService.Default;
        gumService.CanvasWidth = PixelWidth / _zoom;
        gumService.CanvasHeight = PixelHeight / _zoom;
        SystemManagers.Default.Renderer.Camera.Zoom = _zoom;
        _element.UpdateLayout();
    }

    private static GraphicalUiElement? HitTestRecursive(GraphicalUiElement element, float x, float y)
    {
        if (!element.Visible || !Contains(element, x, y))
        {
            return null;
        }

        // Later children draw on top, so walk them last-to-first for the topmost hit.
        for (int i = element.Children.Count - 1; i >= 0; i--)
        {
            if (element.Children[i] is GraphicalUiElement child)
            {
                GraphicalUiElement? hit = HitTestRecursive(child, x, y);
                if (hit != null)
                {
                    return hit;
                }
            }
        }

        return element;
    }

    private static bool Contains(GraphicalUiElement element, float x, float y)
    {
        // AbsoluteX/Y are the origin, which is the centre for centred elements; use the edges.
        float left = element.AbsoluteLeft;
        float top = element.AbsoluteTop;
        return x >= left && y >= top
            && x <= left + element.AbsoluteWidth
            && y <= top + element.AbsoluteHeight;
    }
}
