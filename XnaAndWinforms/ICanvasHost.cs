using System;
using InputLibrary;

namespace XnaAndWinforms;

/// <summary>
/// What a framework-neutral canvas (the wireframe editor, the texture-region selector) needs
/// from the control that hosts it: the shared device and content services, the polled input
/// host, whether the pointer is over the control, and the frame rate knob. The WPF and Avalonia
/// graphics-device controls both implement it, so one canvas implementation runs under either.
/// </summary>
public interface ICanvasHost
{
    /// <summary>The device and content service provider the canvas renders with.</summary>
    IRenderDeviceHost RenderDeviceHost { get; }

    /// <summary>The service provider content managers look the device up through.</summary>
    IServiceProvider Services { get; }

    /// <summary>The polled input host the canvas's cursor and keyboard read.</summary>
    IInputHostControl InputHost { get; }

    /// <summary>Whether the pointer is currently over the control.</summary>
    bool IsPointerOver { get; }

    /// <summary>The frame rate the host aims for.</summary>
    float DesiredFramesPerSecond { get; set; }
}
