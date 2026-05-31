namespace Gum.Logic;

/// <summary>
/// Adds the Skia-backed Standard elements to the currently-loaded project: Arc,
/// Canvas, Line, LottieAnimation, Svg always, plus the legacy ColoredCircle /
/// RoundedRectangle on pre-v3 projects only. On v3
/// (<see cref="Gum.DataTypes.GumProjectSave.GumxVersions.ShapeVariableExpansion"/>)
/// or later the plain Circle / Rectangle supersede those two, so they are skipped.
/// </summary>
/// <remarks>
/// Historically this lived inside the SkiaPlugin, since Skia was an add-on.
/// As Gum has grown more native vector support the project-authoring side
/// (writing the .gutx files and registering them on the gumx) is no longer
/// Skia-specific — only the rendering side is. This service is the
/// project-authoring half; SkiaPlugin still owns the rendering half.
/// </remarks>
public interface ISkiaShapeStandardsLogic
{
    /// <summary>
    /// Writes the bundled <c>.gutx</c> files to <c>Standards/</c> and adds any
    /// missing <c>StandardElementReference</c>s to the project. Idempotent —
    /// shapes already present are left untouched. Saves the project at the end.
    /// </summary>
    void AddAllStandards();
}
