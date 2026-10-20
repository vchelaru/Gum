using System.Collections.Generic;
using Gum.Forms;
using Gum.Wireframe;
using RenderingLibrary;

namespace Gum;

/// <summary>
/// The base for Skia hosts that read real input (Silk.NET, Stride, Unity). Adds the per-frame Forms
/// input pump to <see cref="GumServiceSkiaBase"/>, which stays render-only for the WPF, MAUI and
/// bring-your-own-canvas hosts. A host derives from this, imports <c>Runtimes/SkiaInputHost.props</c>
/// (which adds the typed Default, Cursor and Keyboard accessors and owns the cursor and keyboard),
/// implements its <c>CreateHostCursor</c> and <c>CreateHostKeyboard</c> partial methods, and overrides
/// <see cref="IGumService.ApplyGamePadState"/>.
/// </summary>
public abstract class GumServiceSkiaInteractiveBase : GumServiceSkiaBase
{
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
