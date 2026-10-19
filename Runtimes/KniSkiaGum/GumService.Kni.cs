// KNI's GumService, as a subclass of GumServiceSkiaInteractiveBase. It overrides the
// CreateCursor/CreateKeyboard/ApplyGamePadState capability hooks with KNI-backed implementations;
// the per-frame Forms input pump comes from the base, and the typed Default/Cursor/Keyboard
// accessors from Runtimes/SkiaHost/GumService.SkiaHost.cs. Rendering, .gumx loading, hot reload and
// the rest come from GumServiceSkiaBase. The caller draws Skia into its own canvas (typically a
// SkiaGameRendering SkiaRenderTarget2D) and passes that SKCanvas in.
#if KNISKIA
#nullable enable

using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaSharp;
using XnaGame = Microsoft.Xna.Framework.Game;
using XnaGameWindow = Microsoft.Xna.Framework.GameWindow;
using ICursor = Gum.Wireframe.ICursor;

namespace Gum;

public partial class GumService : GumServiceSkiaInteractiveBase, IGumService
{
    private XnaGameWindow? _window;

    /// <inheritdoc/>
    ICursor? IGumService.CreateCursor() => new Cursor();

    /// <inheritdoc/>
    IInputReceiverKeyboard? IGumService.CreateKeyboard() => new Keyboard(_window);

    /// <inheritdoc/>
    void IGumService.ApplyGamePadState(Gum.Input.GamePad gamepad, int index, double time) =>
        GamePadDriver.Apply(gamepad, index, time);

    /// <summary>
    /// Initializes Gum for a KNI desktop game, optionally loading a Gum project. The canvas size is
    /// read from <see cref="SKCanvas.DeviceClipBounds"/>; use the explicit-size overload if that does
    /// not match the window.
    /// </summary>
    /// <param name="game">The game, used for its window's typed-text events.</param>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public void Initialize(XnaGame game, SKCanvas canvas, string? gumProjectFile = null)
    {
        var bounds = canvas.DeviceClipBounds;
        Initialize(game, canvas, bounds.Width, bounds.Height, gumProjectFile);
    }

    /// <summary>
    /// Initializes Gum for a KNI desktop game with an explicit canvas size, optionally loading a Gum
    /// project.
    /// </summary>
    /// <param name="game">The game, used for its window's typed-text events.</param>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="width">The width to use for the root container and canvas coordinate space.</param>
    /// <param name="height">The height to use for the root container and canvas coordinate space.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public void Initialize(XnaGame game, SKCanvas canvas, int width, int height, string? gumProjectFile = null) =>
        InitializeCore(game.Window, canvas, width, height, gumProjectFile);

    /// <summary>
    /// Initializes without a game window, so no typed text arrives. A KNI <c>Game</c> needs a platform
    /// package, so tests that have none initialize through this.
    /// </summary>
    internal void InitializeWithoutWindow(SKCanvas canvas, int width, int height, string? gumProjectFile = null) =>
        InitializeCore(null, canvas, width, height, gumProjectFile);

    private void InitializeCore(XnaGameWindow? window, SKCanvas canvas, int width, int height, string? gumProjectFile)
    {
        // Stored before base.Initialize, which calls FormsUtilities.InitializeDefaults, which
        // dispatches back to this instance's CreateCursor/CreateKeyboard overrides above.
        _window = window;
        Clipboard = new global::Gum.Clipboard.MonoGameGumClipboard();

        base.Initialize(canvas, width, height, gumProjectFile);
    }
}
#endif
