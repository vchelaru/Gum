#if XNALIKE
using Microsoft.Xna.Framework;

namespace Gum.GueDeriving;

/// <summary>
/// Backing state and fill/stroke push logic for the Dropshadow property block shared by
/// <see cref="CircleRuntime"/> and <see cref="RectangleRuntime"/> (issues #2797 / #2818 — the
/// two runtimes' Dropshadow regions were near-byte-identical). A pure data struct, mirroring
/// <see cref="ShapeGradientState"/>: callers pass the current fill/stroke slots (cast to
/// <see cref="IDropshadowRenderable"/>) on every call rather than the struct caching them, so a
/// stale reference can never survive a <c>Clone()</c> rebuild of those slots.
/// </summary>
/// <remarks>
/// Unlike gradient (pushed to BOTH slots so a single gradient covers fill and stroke at once),
/// a dropshadow is drawn once per renderable — pushing to both would render the shadow twice
/// and visibly double up. <see cref="GetTarget"/> picks a single slot: fill when
/// <c>isFilled</c>, stroke otherwise, falling back to whichever slot is non-null.
/// </remarks>
struct ShapeDropshadowState
{
    public bool HasDropshadow;
    public Color DropshadowColor;
    public float DropshadowOffsetX;
    public float DropshadowOffsetY;
    public float DropshadowBlur;

    /// <summary>
    /// The slot that owns the dropshadow: fill when <paramref name="isFilled"/> (the disc/disk
    /// casts the shadow), stroke otherwise (the fill is gated to transparent and can't cast a
    /// visible shadow). Falls back to whichever slot is non-null.
    /// </summary>
    public IDropshadowRenderable? GetTarget(IDropshadowRenderable? fill, IDropshadowRenderable? stroke, bool isFilled)
    {
        return isFilled ? (fill ?? stroke) : (stroke ?? fill);
    }

    /// <summary>
    /// Pushes every backing field onto the active target (see <see cref="GetTarget"/>) and
    /// clears the inactive slot's flag. Re-run whenever <c>isFilled</c> toggles so the previous
    /// target releases its shadow and the new target picks it up — otherwise toggling
    /// <c>IsFilled</c> either ghosts the previous target or never wakes the new one up.
    /// </summary>
    public void SyncTarget(IDropshadowRenderable? fill, IDropshadowRenderable? stroke, bool isFilled)
    {
        var target = GetTarget(fill, stroke, isFilled);
        var other = ReferenceEquals(target, fill) ? stroke : fill;

        if (other != null) other.HasDropshadow = false;
        if (target != null)
        {
            target.HasDropshadow = HasDropshadow;
            target.DropshadowColor = DropshadowColor;
            target.DropshadowOffsetX = DropshadowOffsetX;
            target.DropshadowOffsetY = DropshadowOffsetY;
            target.DropshadowBlurX = DropshadowBlur;
            target.DropshadowBlurY = DropshadowBlur;
        }
    }

    public void SetHasDropshadow(bool value, IDropshadowRenderable? fill, IDropshadowRenderable? stroke, bool isFilled)
    {
        HasDropshadow = value;
        var target = GetTarget(fill, stroke, isFilled);
        if (target != null) target.HasDropshadow = value;
    }

    public void SetDropshadowColor(Color value, IDropshadowRenderable? fill, IDropshadowRenderable? stroke, bool isFilled)
    {
        DropshadowColor = value;
        var target = GetTarget(fill, stroke, isFilled);
        if (target != null) target.DropshadowColor = value;
    }

    public void SetDropshadowOffsetX(float value, IDropshadowRenderable? fill, IDropshadowRenderable? stroke, bool isFilled)
    {
        DropshadowOffsetX = value;
        var target = GetTarget(fill, stroke, isFilled);
        if (target != null) target.DropshadowOffsetX = value;
    }

    public void SetDropshadowOffsetY(float value, IDropshadowRenderable? fill, IDropshadowRenderable? stroke, bool isFilled)
    {
        DropshadowOffsetY = value;
        var target = GetTarget(fill, stroke, isFilled);
        if (target != null) target.DropshadowOffsetY = value;
    }

    /// <summary>
    /// Isotropic blur radius — a single scalar pushed to both the target's X and Y blur fields
    /// (Apos.Shapes approximates falloff via one <c>antiAliasSize</c> parameter; no per-axis
    /// blur). Mirrors industry convention (CSS <c>box-shadow</c> blur-radius, Figma, Photoshop).
    /// </summary>
    public void SetDropshadowBlur(float value, IDropshadowRenderable? fill, IDropshadowRenderable? stroke, bool isFilled)
    {
        DropshadowBlur = value;
        var target = GetTarget(fill, stroke, isFilled);
        if (target != null)
        {
            target.DropshadowBlurX = value;
            target.DropshadowBlurY = value;
        }
    }

    /// <summary>
    /// Pushes every backing field onto freshly-rebuilt fill/stroke slots. Called from
    /// <c>Clone()</c> for symmetry with <see cref="ShapeGradientState.PushAll"/> (which closes a
    /// real gap there). Unlike gradient, dropshadow state already reached the clone's slots
    /// before this existed: <c>Clone()</c> re-fires <c>IsFilled</c> onto the freshly-built slots,
    /// and that setter unconditionally calls <see cref="SyncTarget"/> — so this call is
    /// defensive/idempotent here, not fixing a bug.
    /// </summary>
    public void PushAll(IDropshadowRenderable? fill, IDropshadowRenderable? stroke, bool isFilled)
    {
        SyncTarget(fill, stroke, isFilled);
    }
}
#endif
