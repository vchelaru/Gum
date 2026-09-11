using Gum.Input;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services.Dialogs;
using System;
using System.Windows.Input;
using XnaAndWinforms;

namespace Gum.Plugins.InternalPlugins.EditorTab.Views;

/// <summary>
/// The WPF wireframe canvas: a <see cref="WpfGraphicsDeviceControl"/> that hosts a
/// <see cref="WireframeCanvasCore"/>, forwarding its frames and translating its WPF input into the
/// neutral events the core handles.
/// </summary>
public class WireframeControl : WpfGraphicsDeviceControl
{

    /// <summary>The framework-neutral canvas this control renders.</summary>
    public WireframeCanvasCore Core { get; }

    public WireframeControl(IDialogService dialogService, IOutputManager outputManager, IPluginManager pluginManager)
    {
        // Ctrl+= / Ctrl+- zoom this canvas's camera, not the app-wide font size.
        CameraZoomScope.SetOwnsCameraZoom(this, true);

        Core = new WireframeCanvasCore(this, dialogService, outputManager, pluginManager);

        KeyDown += HandleKeyDown;
        KeyUp += (_, _) => Core.HandleKeyUp();
        // Focus is taken by WpfGraphicsDeviceControl.OnMouseDown, which runs before these.
        MouseDown += (_, e) => Core.HandleMouseDown(e.ToGumMouseEventArgs(this));
        MouseMove += (_, e) => Core.HandleMouseMove(e.ToGumMouseEventArgs(this));
        MouseUp += (_, e) => Core.HandleMouseUp(e.ToGumMouseEventArgs(this));
        MouseWheel += HandleMouseWheel;
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        GumKeyEventArgs keyArgs = e.ToGumKeyEventArgs();
        Core.HandleKeyDown(keyArgs);
        // WPF has no SuppressKeyPress equivalent; marking the event handled stops both the key
        // event and the text input it would otherwise produce.
        e.Handled = keyArgs.Handled || keyArgs.SuppressKeyPress;
    }

    private void HandleMouseWheel(object? sender, MouseWheelEventArgs e)
    {
        GumMouseEventArgs gumMouseArgs = e.ToGumMouseEventArgs(this);
        Core.HandleMouseWheel(gumMouseArgs);
        // Read the neutral Handled back to suppress a containing scroll viewer's default scroll.
        e.Handled = gumMouseArgs.Handled;
    }

    /// <summary>
    /// Gives the hotkey manager first refusal on every key, ahead of WPF's own handling (focus
    /// navigation on Tab/arrows in particular). The WinForms counterpart was ProcessCmdKey.
    /// </summary>
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (Core.HandlePreviewKeyDown(e.ToGumKeyEventArgs()))
        {
            e.Handled = true;
            return;
        }
        base.OnPreviewKeyDown(e);
    }

    protected override void PreDrawUpdate() => Core.PreDrawUpdate();

    protected override void Draw() => Core.Draw();
}
