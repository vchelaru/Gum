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

namespace Gum.Avalonia.Plugins.EditorTab;

/// <summary>
/// The Avalonia wireframe canvas: an <see cref="AvaloniaGraphicsDeviceControl"/> that hosts a
/// <see cref="WireframeCanvasCore"/>, forwarding its frames and translating its Avalonia input
/// into the neutral events the core handles. The counterpart of the WPF <c>WireframeControl</c>.
/// </summary>
public sealed class WireframeCanvasControl : AvaloniaGraphicsDeviceControl
{
    /// <summary>The framework-neutral canvas this control renders.</summary>
    public WireframeCanvasCore Core { get; }

    /// <summary>Creates the control and its core.</summary>
    public WireframeCanvasControl(IDialogService dialogService, IOutputManager outputManager, IPluginManager pluginManager,
        ICanvasRedrawScheduler redrawScheduler)
        : base(redrawScheduler)
    {
        // Ctrl+= / Ctrl+- zoom this canvas's camera, not the app-wide font size.
        CameraZoomScope.SetOwnsCameraZoom(this, true);

        Core = new WireframeCanvasCore(this, dialogService, outputManager, pluginManager);
    }

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
        Core.HandleKeyUp(e.ToGumKeyEventArgs());
        base.OnKeyUp(e);
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Core.HandleMouseDown(e.ToGumMouseEventArgs(this, e.GetCurrentPoint(this).Properties.PointerUpdateKind));
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        Core.HandleMouseMove(e.ToGumMouseEventArgs(this, PointerUpdateKind.Other));
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        Core.HandleMouseUp(e.ToGumMouseEventArgs(this, e.GetCurrentPoint(this).Properties.PointerUpdateKind));
    }

    /// <inheritdoc/>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        GumMouseEventArgs args = e.ToGumMouseEventArgs(this, PointerUpdateKind.Other);
        // WPF reports 120 per notch; Avalonia reports 1.
        args.Delta = (int)(e.Delta.Y * 120);
        Core.HandleMouseWheel(args);
        e.Handled = args.Handled;
    }

    /// <inheritdoc/>
    protected override void PreDrawUpdate() => Core.PreDrawUpdate();

    /// <inheritdoc/>
    protected override void Draw() => Core.Draw();
}
