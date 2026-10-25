using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Tests.Shared;
using Gum.Wireframe;
using Moq;
using RenderingLibrary.Graphics;
using SkiaSharp;
using Keys = Gum.Forms.Input.Keys;

namespace SkiaGum.Tests.Forms.Linked;

/// <summary>
/// Per-test setup for the Forms tests linked from MonoGameGum.Tests (see <c>MonoGameTestBase</c>).
/// Initializes the shared Skia <see cref="GumService"/> once, installs a mock cursor so
/// <c>FrameworkElement.MainCursor</c> is non-null (the render-only Skia service has none), and
/// resets the Forms statics the linked tests change, mirroring <c>MonoGameGum.Tests.BaseTestClass</c>.
/// </summary>
public class SkiaFormsTestBase : IDisposable
{
    private static readonly SKSurface Surface = SKSurface.Create(new SKImageInfo(800, 600));
    private static bool _isInitialized;

    private readonly FileManagerState _fileManagerState = FileManagerState.Capture();
    private readonly GraphicalUiElementState _graphicalUiElementState = GraphicalUiElementState.Capture();

    public SkiaFormsTestBase()
    {
        if (!_isInitialized)
        {
            GumService.Default.Initialize(Surface.Canvas, 800, 600);
            _isInitialized = true;
        }

        GraphicalUiElement.IsAllLayoutSuspended = false;
        GraphicalUiElement.SuppressFontRegeneration = false;
        GraphicalUiElement.CanvasWidth = 800;
        GraphicalUiElement.CanvasHeight = 600;

        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.PrimaryClick).Returns(true);
        FormsUtilities.SetCursor(cursor.Object);
        cursor.SetupProperty(x => x.VisualOver);
        cursor.SetupProperty(x => x.WindowPushed);
        cursor.Setup(x => x.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(x => x.PrimaryPush).Returns(true);
    }

    public virtual void Dispose()
    {
        GraphicalUiElement.IsAllLayoutSuspended = false;
        GraphicalUiElement.SuppressFontRegeneration = false;
        GraphicalUiElement.CanvasWidth = 800;
        GraphicalUiElement.CanvasHeight = 600;
        GraphicalUiElement.GlobalFontScale = 1;

        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.Clear();
        FrameworkElement.ClickCombos.Clear();
        FrameworkElement.ClickCombos.Add(new KeyCombo { PushedKey = Keys.Enter, HeldKey = null, IsTriggeredOnRepeat = false });
        FrameworkElement.TabKeyCombos.Clear();
        FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = Keys.Tab, HeldKey = null, IsTriggeredOnRepeat = true });
        FrameworkElement.TabReverseKeyCombos.Clear();
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo { PushedKey = Keys.Tab, HeldKey = Keys.LeftShift, IsTriggeredOnRepeat = true });
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo { PushedKey = Keys.Tab, HeldKey = Keys.RightShift, IsTriggeredOnRepeat = true });
        FrameworkElement.UpKeyCombos.Clear();
        FrameworkElement.DownKeyCombos.Clear();
        FrameworkElement.LeftKeyCombos.Clear();
        FrameworkElement.RightKeyCombos.Clear();

        InteractiveGue.CurrentInputReceiver = null;
        InteractiveGue.ClearNextClickActions();
        InteractiveGue.ClearNextPushActions();

        GumService.Default.Root.Children!.Clear();
        GumService.Default.ModalRoot.Children!.Clear();
        GumService.Default.PopupRoot.Children!.Clear();
        FrameworkElement.AdditionalPopupRootPairs.Clear();

        _fileManagerState.Restore();
        _graphicalUiElementState.Restore();
        ObjectFinderState.Reset();
    }
}
