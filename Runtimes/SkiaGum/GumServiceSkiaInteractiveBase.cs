using System;
using System.Collections.Generic;
using SkiaSharp;
using Gum.Forms;
using Gum.Wireframe;
using RenderingLibrary;

namespace Gum;

/// <summary>
/// The base for Skia hosts that read real input (Silk.NET, Stride, Unity). Adds the per-frame Forms
/// input pump to <see cref="GumServiceSkiaBase"/>, which stays render-only for the WPF, MAUI and
/// bring-your-own-canvas hosts. A host derives from this, imports <c>Runtimes/SkiaInputHost.props</c>
/// (which adds the typed Default, Cursor and Keyboard accessors and owns the cursor and keyboard),
/// implements its <c>CreateHostCursor</c> and <c>CreateHostKeyboard</c> partial methods, initializes
/// through <see cref="InitializeWithDevices"/> (which attaches its devices before they are read), and overrides
/// <see cref="IGumService.ApplyGamePadState"/>.
/// </summary>
public abstract class GumServiceSkiaInteractiveBase : GumServiceSkiaBase
{
    /// <summary>
    /// The Initialize body for a host that reads input devices. <paramref name="attachDevices"/> is
    /// required and runs before the cursor and keyboard are created, so a host stores the devices it
    /// was given (and assigns <see cref="GumServiceSkiaBase.Clipboard"/>) there and cannot get the
    /// order wrong. Its <c>CreateHostCursor</c> and <c>CreateHostKeyboard</c> then read that state.
    /// </summary>
    /// <param name="canvas">The canvas to draw into, or null to supply it on the first draw.</param>
    /// <param name="width">The width to use for the root container and canvas coordinate space.</param>
    /// <param name="height">The height to use for the root container and canvas coordinate space.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    /// <param name="attachDevices">Stores the host's device state and assigns the clipboard.</param>
    /// <param name="visualsVersion">The default Forms visuals to register.</param>
    protected void InitializeWithDevices(
        SKCanvas? canvas, int width, int height, string? gumProjectFile, Action attachDevices,
        DefaultVisualsVersion visualsVersion = DefaultVisualsVersion.Newest)
    {
        if (attachDevices == null)
        {
            throw new ArgumentNullException(nameof(attachDevices));
        }

        InitializeCore(canvas, width, height, gumProjectFile, visualsVersion, attachDevices);
    }

    /// <summary>
    /// Per-frame tick. Call once per frame, before <see cref="GumServiceSkiaBase.Draw()"/>, with total
    /// elapsed seconds since startup. Runs the base's deferred-queue and animation tick, then pumps
    /// Forms input (cursor, keyboard and gamepad activity, control events).
    /// </summary>
    /// <param name="totalSeconds">Total elapsed time in seconds since startup.</param>
    public override void Update(double totalSeconds)
    {
        base.Update(totalSeconds);
        FormsUtilities.Update(totalSeconds, Root);
    }

    private readonly List<GraphicalUiElement> _singleRootList = new();

    /// <summary>
    /// The per-frame tick for a single root other than <see cref="GumServiceSkiaBase.Root"/>, such as a
    /// container drawn on a custom layer. Same as <see cref="Update(double, IEnumerable{GraphicalUiElement})"/>
    /// with one root.
    /// </summary>
    /// <param name="totalSeconds">Total elapsed time in seconds since startup.</param>
    /// <param name="root">The root to animate and pump Forms input for.</param>
    public void Update(double totalSeconds, GraphicalUiElement root)
    {
        _singleRootList.Clear();
        _singleRootList.Add(root);
        Update(totalSeconds, _singleRootList);
    }

    /// <summary>
    /// The per-frame tick for an explicit set of roots, for layered or popup UI that is not a child of
    /// <see cref="GumServiceSkiaBase.Root"/>: animates each root and pumps Forms input (cursor, keyboard,
    /// gamepad, control events) for them. Only the roots passed are updated, so include
    /// <see cref="GumServiceSkiaBase.Root"/> when it should be. Call it at most once per frame, instead of
    /// <see cref="Update(double)"/>.
    /// </summary>
    /// <param name="totalSeconds">Total elapsed time in seconds since startup.</param>
    /// <param name="roots">The roots to animate and pump Forms input for.</param>
    public void Update(double totalSeconds, IEnumerable<GraphicalUiElement> roots) =>
        UpdateRoots(totalSeconds, roots);

    /// <summary>
    /// The per-frame tick for an explicit set of roots, for a host whose engine passes its own time
    /// type: the frame work of <see cref="GumServiceSkiaBase.Update(double)"/> on
    /// <paramref name="roots"/>, then the Forms input pump.
    /// </summary>
    /// <param name="totalSeconds">Total elapsed time in seconds since startup.</param>
    /// <param name="roots">The roots to animate and pump Forms input for.</param>
    protected void UpdateRoots(double totalSeconds, IEnumerable<GraphicalUiElement> roots)
    {
        UpdateFrame(totalSeconds, roots);
        FormsUtilities.Update(totalSeconds, roots);
    }
}
