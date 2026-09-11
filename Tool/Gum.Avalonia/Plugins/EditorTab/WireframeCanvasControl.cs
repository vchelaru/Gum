using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Services;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.EditorTab.Views;
using Gum.Services.Dialogs;
using InputLibrary;

namespace Gum.Avalonia.Plugins.EditorTab;

/// <summary>
/// The Avalonia wireframe canvas: an <see cref="AvaloniaGraphicsDeviceControl"/> that hosts a
/// <see cref="WireframeCanvasCore"/>, forwarding its frames and translating its Avalonia input
/// into the neutral events the core handles. The counterpart of the WPF <c>WireframeControl</c>.
/// </summary>
public sealed class WireframeCanvasControl : AvaloniaGraphicsDeviceControl, IWireframeCanvasHost
{
    private readonly AvaloniaInputHostAdapter _inputHost;

    /// <summary>The framework-neutral canvas this control renders.</summary>
    public WireframeCanvasCore Core { get; }

    /// <summary>Creates the control and its core.</summary>
    public WireframeCanvasControl(IDialogService dialogService, IOutputManager outputManager, IPluginManager pluginManager)
    {
        // Ctrl+= / Ctrl+- zoom this canvas's camera, not the app-wide font size.
        CameraZoomScope.SetOwnsCameraZoom(this, true);

        _inputHost = new AvaloniaInputHostAdapter(this);
        Core = new WireframeCanvasCore(this, dialogService, outputManager, pluginManager);
    }

    /// <inheritdoc/>
    public IInputHostControl InputHost => _inputHost;

    /// <inheritdoc/>
    public bool IsPointerOver => base.IsPointerOver;

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        // The hotkey manager gets first refusal, ahead of Avalonia's own handling (focus
        // navigation on Tab/arrows in particular), the way the WPF control's preview pass does.
        GumKeyEventArgs keyArgs = e.ToGumKeyEventArgs();
        if (Core.HandlePreviewKeyDown(keyArgs))
        {
            e.Handled = true;
            return;
        }
        Core.HandleKeyDown(keyArgs);
        e.Handled = keyArgs.Handled || keyArgs.SuppressKeyPress;
        base.OnKeyDown(e);
    }

    /// <inheritdoc/>
    protected override void OnKeyUp(KeyEventArgs e)
    {
        Core.HandleKeyUp();
        base.OnKeyUp(e);
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Core.HandleMouseDown(ToGumMouseEventArgs(e, e.GetCurrentPoint(this).Properties.PointerUpdateKind));
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        Core.HandleMouseMove(ToGumMouseEventArgs(e, PointerUpdateKind.Other));
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        Core.HandleMouseUp(ToGumMouseEventArgs(e, e.GetCurrentPoint(this).Properties.PointerUpdateKind));
    }

    /// <inheritdoc/>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        GumMouseEventArgs args = ToGumMouseEventArgs(e, PointerUpdateKind.Other);
        // WPF reports 120 per notch; Avalonia reports 1.
        args.Delta = (int)(e.Delta.Y * 120);
        Core.HandleMouseWheel(args);
        e.Handled = args.Handled;
    }

    private GumMouseEventArgs ToGumMouseEventArgs(PointerEventArgs e, PointerUpdateKind updateKind)
    {
        Point position = e.GetPosition(this);
        PointerPointProperties properties = e.GetCurrentPoint(this).Properties;
        return new GumMouseEventArgs
        {
            X = (int)position.X,
            Y = (int)position.Y,
            Button = ToGumMouseButton(updateKind, properties),
            Handled = e.Handled,
        };
    }

    // A button event names the button that changed; a move/wheel event doesn't, so report whichever
    // button is currently held - what a drag needs.
    private static GumMouseButton ToGumMouseButton(PointerUpdateKind updateKind, PointerPointProperties properties) => updateKind switch
    {
        PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased => GumMouseButton.Left,
        PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => GumMouseButton.Right,
        PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => GumMouseButton.Middle,
        _ when properties.IsLeftButtonPressed => GumMouseButton.Left,
        _ when properties.IsRightButtonPressed => GumMouseButton.Right,
        _ when properties.IsMiddleButtonPressed => GumMouseButton.Middle,
        _ => GumMouseButton.None,
    };

    /// <inheritdoc/>
    protected override void PreDrawUpdate() => Core.PreDrawUpdate();

    /// <inheritdoc/>
    protected override void Draw() => Core.Draw();
}
