using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace KniSkiaGumImmediateMode.Screens;

/// <summary>
/// Demonstrates two coordinate spaces coexisting in the same frame, using real Forms controls held in
/// two containers: <see cref="_inGameContainerForRenderTarget"/> is drawn onto a low-resolution
/// <see cref="SKSurface"/> that is drawn to the window canvas at 3x (visibly blurred by linear
/// sampling), and <see cref="_aboveGameUiContainer"/> is drawn straight to the window canvas at 1:1 on
/// top of it. See "Several coordinate spaces at once" in the cursor docs
/// (docs/code/events-and-interactivity/mouse-and-touch-screen-cursor.md).
///
/// Neither container is added to <c>GumService.Default.Root</c>, so this screen exposes them via
/// <see cref="AdditionalUpdateRoots"/>, which Game1 combines with Root into a single per-frame Update
/// call. The game button is clickable despite the surface's scale because its container carries a
/// <see cref="GraphicalUiElement.HitTestTransformMatrix"/> that maps the raw window pixel back into
/// surface space.
/// </summary>
public class RenderTargetScreen : IImmediateModeScreen
{
    // A deliberately low resolution so the 3x blit is obviously blurred, proving the surface really is
    // being scaled up rather than drawn at 1:1.
    private const int GameWidth = 240;
    private const int GameHeight = 180;
    private const float BlitScale = 3f;
    private const float BlitY = 100f;

    private SKSurface _surface = null!;

    private ContainerRuntime _inGameContainerForRenderTarget = null!;
    private ContainerRuntime _aboveGameUiContainer = null!;
    private Menu _menu = null!;

    private readonly List<GraphicalUiElement> _roots = new List<GraphicalUiElement>();

    public void Initialize()
    {
        _surface = SKSurface.Create(new SKImageInfo(GameWidth, GameHeight));

        RectangleRuntime gameBackground = new RectangleRuntime();
        gameBackground.Width = GameWidth;
        gameBackground.Height = GameHeight;
        gameBackground.IsFilled = true;
        gameBackground.FillColor = new SKColor(40, 110, 60);

        Button gameButton = new Button();
        gameButton.Text = "Game Button";
        gameButton.Width = 120;
        gameButton.Height = 40;
        gameButton.Anchor(Anchor.Center);

        _inGameContainerForRenderTarget = new ContainerRuntime();
        // Not docked to fill, because this uses its own internal size.
        _inGameContainerForRenderTarget.Width = GameWidth;
        _inGameContainerForRenderTarget.Height = GameHeight;

        _inGameContainerForRenderTarget.AddChild(gameBackground);
        _inGameContainerForRenderTarget.AddChild(gameButton);

        Menu menu = new Menu();
        _menu = menu;
        menu.Y = 48;
        MenuItem fileMenu = new MenuItem();
        fileMenu.Header = "File";
        fileMenu.Items!.Add("Item under File");
        menu.Items!.Add(fileMenu);
        MenuItem editMenu = new MenuItem();
        editMenu.Header = "Edit";
        editMenu.Items!.Add("Item under Edit");
        menu.Items!.Add(editMenu);

        Window window = new Window();
        window.X = 620;
        window.Y = 160;
        window.Width = 260;
        window.Height = 160;
        Button windowButton = new Button();
        windowButton.Text = "Button in Window";
        windowButton.Anchor(Anchor.Center);
        window.AddChild(windowButton);

        _aboveGameUiContainer = new ContainerRuntime();
        _aboveGameUiContainer.Dock(Dock.Fill);
        _aboveGameUiContainer.HasEvents = false; // so it doesn't eat all game events

        _aboveGameUiContainer.Children.Add(menu.Visual);
        _aboveGameUiContainer.Children.Add(window.Visual);

        // Neither container is ever added to a managed root (they're only drawn through GumBatch), so
        // EffectiveManagers is null throughout their trees by default. The Menu popup's own
        // close-on-outside-click logic gates on EffectiveManagers being set, so without this the popup
        // opens but never closes.
        _inGameContainerForRenderTarget.AttachManagersOnly(SystemManagers.Default);
        _aboveGameUiContainer.AttachManagersOnly(SystemManagers.Default);

        _roots.Add(_inGameContainerForRenderTarget);
        _roots.Add(_aboveGameUiContainer);
    }

    public IEnumerable<GraphicalUiElement> AdditionalUpdateRoots => _roots;

    public void Draw(GumBatch gumBatch, SKCanvas canvas)
    {
        // Begin(canvas) draws onto the surface for this cycle only; End restores the window canvas.
        _surface.Canvas.Clear(SKColors.Transparent);
        gumBatch.Begin(_surface.Canvas);
        gumBatch.Draw(_inGameContainerForRenderTarget);
        gumBatch.End();

        // Center the scaled-up surface horizontally, below the menu.
        float blitWidth = GameWidth * BlitScale;
        float blitHeight = GameHeight * BlitScale;
        float blitX = (canvas.DeviceClipBounds.Width - blitWidth) / 2f;

        // Map a raw window pixel back into surface space: undo the blit offset, then the blit scale.
        // Consumed only by hit-testing (never rendering).
        _inGameContainerForRenderTarget.HitTestTransformMatrix =
            System.Numerics.Matrix3x2.CreateTranslation(-blitX, -BlitY) *
            System.Numerics.Matrix3x2.CreateScale(1f / BlitScale);

        using (SKImage snapshot = _surface.Snapshot())
        {
            canvas.DrawImage(
                snapshot,
                SKRect.Create(blitX, BlitY, blitWidth, blitHeight),
                new SKSamplingOptions(SKFilterMode.Linear));
        }

        // Window-space UI, drawn straight to the window canvas on top of the blit, at 1:1 with no
        // HitTestTransformMatrix.
        gumBatch.Begin();
        gumBatch.Draw(_aboveGameUiContainer);
        gumBatch.End();
    }

    public void Dispose()
    {
        // A Menu's open popup is parented into the shared, always-present PopupRoot rather than this
        // screen's own containers - if left open here it would stay stuck in PopupRoot after navigating
        // away, since nothing else ever removes it.
        foreach (MenuItem menuItem in _menu.MenuItems)
        {
            menuItem.HidePopupRecursively();
        }

        _surface?.Dispose();
    }
}
