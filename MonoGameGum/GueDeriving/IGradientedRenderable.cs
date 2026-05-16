#if XNALIKE
using Gum.Converters;
using Gum.DataTypes;
using RenderingLibrary.Graphics;

namespace Gum.GueDeriving;

/// <summary>
/// Opt-in gradient surface for fill/stroke renderables in the two-slot shape runtime model
/// (issue #2791). Implemented by the optional MonoGameGumShapes <c>Circle</c> /
/// <c>RoundedRectangle</c> (which inherit the full property bag from
/// <c>RenderableShapeBase</c>) so <see cref="CircleRuntime"/> and friends can push gradient
/// state through to whichever slots support it. Core defaults like
/// <see cref="MonoGameGum.Renderables.DefaultStrokedCircleRenderable"/> deliberately do NOT
/// implement this interface — gradient setters round-trip on the runtime but render as a
/// no-op without the optional package (graceful degradation, matching the
/// <see cref="CircleRuntime.FillColor"/> pattern).
/// </summary>
/// <remarks>
/// Property names mirror <c>SkiaShapeRuntime</c> so the runtime APIs match across backends.
/// All gradient coordinates are interpreted relative to the renderable's local origin by
/// the implementation (Apos.Shapes' <c>GetGradient</c> does the unit resolution); the
/// interface itself is a pure pass-through.
/// </remarks>
public interface IGradientedRenderable
{
    bool UseGradient { get; set; }
    GradientType GradientType { get; set; }

    int Alpha1 { get; set; }
    int Red1 { get; set; }
    int Green1 { get; set; }
    int Blue1 { get; set; }

    int Alpha2 { get; set; }
    int Red2 { get; set; }
    int Green2 { get; set; }
    int Blue2 { get; set; }

    float GradientX1 { get; set; }
    GeneralUnitType GradientX1Units { get; set; }
    float GradientY1 { get; set; }
    GeneralUnitType GradientY1Units { get; set; }

    float GradientX2 { get; set; }
    GeneralUnitType GradientX2Units { get; set; }
    float GradientY2 { get; set; }
    GeneralUnitType GradientY2Units { get; set; }

    float GradientInnerRadius { get; set; }
    DimensionUnitType GradientInnerRadiusUnits { get; set; }

    float GradientOuterRadius { get; set; }
    DimensionUnitType GradientOuterRadiusUnits { get; set; }
}
#endif
