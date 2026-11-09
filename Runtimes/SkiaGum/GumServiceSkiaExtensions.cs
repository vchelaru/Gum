using System;

namespace Gum;

/// <summary>
/// The built-in <c>Use...</c> calls of every Skia host. Each is generic over the service type and returns
/// it, so the host's own <c>GumService</c> flows through a chain
/// (<c>GumUI.UseKeyboardDefaults().UseGamepadDefaults().UseShadowDusk()</c>) and the add-on calls that
/// follow still bind. They are extensions, not instance members, because an instance member declared on
/// <see cref="GumServiceSkiaBase"/> could only return that base type.
/// </summary>
public static class GumServiceSkiaExtensions
{
    /// <summary>
    /// Registers the service's keyboard (created during <c>Initialize</c>) as one of
    /// <c>FrameworkElement.KeyboardsForUiControl</c> so Forms controls respond to it.
    /// </summary>
    /// <returns><paramref name="gumService"/>, so calls chain.</returns>
    /// <exception cref="InvalidOperationException">Gum has not been initialized, so there is no keyboard.</exception>
    public static T UseKeyboardDefaults<T>(this T gumService) where T : GumServiceSkiaBase
    {
        ((global::RenderingLibrary.IGumService)gumService).UseKeyboardDefaults();
        return gumService;
    }

    /// <summary>
    /// Registers the service's gamepads as <c>FrameworkElement.GamePadsForUiControl</c> so Forms controls
    /// respond to gamepad input.
    /// </summary>
    /// <returns><paramref name="gumService"/>, so calls chain.</returns>
    public static T UseGamepadDefaults<T>(this T gumService) where T : GumServiceSkiaBase
    {
        ((global::RenderingLibrary.IGumService)gumService).UseGamepadDefaults();
        return gumService;
    }

    /// <summary>
    /// Installs a single-threaded synchronization context on the calling thread so <c>await</c>
    /// continuations resume on the host's primary thread. Call once, after <c>Initialize</c>; later calls
    /// are no-ops. Skip it when you have installed your own context.
    /// </summary>
    /// <returns><paramref name="gumService"/>, so calls chain.</returns>
    public static T UseSingleThreadedAsync<T>(this T gumService) where T : GumServiceSkiaBase
    {
        ((global::RenderingLibrary.IGumService)gumService).UseSingleThreadedAsync();
        return gumService;
    }
}
