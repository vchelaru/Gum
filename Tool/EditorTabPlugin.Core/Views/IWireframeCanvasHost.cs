using System;
using InputLibrary;
using XnaAndWinforms;

namespace Gum.Plugins.InternalPlugins.EditorTab.Views;

/// <summary>
/// What <see cref="WireframeCanvasCore"/> needs from the control that hosts it: the shared
/// device and content services, the polled input host, whether the pointer is over the control,
/// and the frame rate knob. The WPF <c>WireframeControl</c> and the Avalonia canvas control each
/// implement this over their own render surface.
/// </summary>
public interface IWireframeCanvasHost
{
    /// <summary>The device and content service provider the canvas renders with.</summary>
    IRenderDeviceHost RenderDeviceHost { get; }

    /// <summary>The service provider content managers look the device up through.</summary>
    IServiceProvider Services { get; }

    /// <summary>The polled input host the canvas's cursor reads.</summary>
    IInputHostControl InputHost { get; }

    /// <summary>
    /// Whether the pointer is currently over the control. While it is not, the selection manager
    /// is told to drop its hover highlight, so scrolling with the bars does not highlight objects.
    /// </summary>
    bool IsPointerOver { get; }

    /// <summary>The frame rate the host aims for.</summary>
    float DesiredFramesPerSecond { get; set; }
}
