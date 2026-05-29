using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #2852: when Width != Height the Apos.Shapes Circle previously sized its radius from
// Width alone, which produced two divergent results vs Skia (the tool/viewport renderer):
// the circle could overflow the box on the short axis, and a wide-but-short circle's center
// landed below the box because center.Y was also derived from Width. These tests pin the
// canonical behavior to "fit inside the bounding box, centered" — matching Skia.
public class CircleRenderableTests
{
    [Fact]
    public void Radius_WhenWidthGreaterThanHeight_UsesSmallerDimension()
    {
        Circle sut = new() { Width = 200, Height = 50 };

        sut.Radius.ShouldBe(25f);
    }

    [Fact]
    public void Radius_WhenHeightGreaterThanWidth_UsesSmallerDimension()
    {
        Circle sut = new() { Width = 50, Height = 200 };

        sut.Radius.ShouldBe(25f);
    }

    [Fact]
    public void Radius_WhenSquare_IsHalfDimension()
    {
        Circle sut = new() { Width = 80, Height = 80 };

        sut.Radius.ShouldBe(40f);
    }

    [Fact]
    public void Radius_Setter_KeepsWidthAndHeightSquare()
    {
        Circle sut = new();

        sut.Radius = 30;

        sut.Width.ShouldBe(60f);
        sut.Height.ShouldBe(60f);
    }

    // Issue #2851 — Apos.Shapes previously drew dropshadows at their own DropshadowColor
    // alpha while ignoring the body's alpha, so a shape fading to transparent left an opaque
    // shadow ghost behind. SkiaGum (the tool/viewport) multiplies shadow alpha by body alpha,
    // and the two backends must agree. EffectiveDropshadowColor is the multiplied value the
    // Render path now forwards to Apos.

    [Fact]
    public void EffectiveDropshadowColor_BodyFullyOpaque_PreservesShadowAlpha()
    {
        Circle sut = new()
        {
            Color = new Color(10, 20, 30, 255),
            DropshadowColor = new Color(100, 110, 120, 200),
        };

        sut.EffectiveDropshadowColor.ShouldBe(new Color(100, 110, 120, 200));
    }

    [Fact]
    public void EffectiveDropshadowColor_BodyHalfTransparent_HalvesShadowAlpha()
    {
        Circle sut = new()
        {
            Color = new Color(10, 20, 30, 128),
            DropshadowColor = new Color(100, 110, 120, 200),
        };

        // 200 * 128 / 255 = 100 (integer truncation matches the Render path).
        sut.EffectiveDropshadowColor.ShouldBe(new Color(100, 110, 120, 100));
    }

    [Fact]
    public void EffectiveDropshadowColor_BodyFullyTransparent_ZeroesShadowAlpha()
    {
        Circle sut = new()
        {
            Color = new Color(10, 20, 30, 0),
            DropshadowColor = new Color(100, 110, 120, 255),
        };

        sut.EffectiveDropshadowColor.A.ShouldBe((byte)0);
    }

    [Fact]
    public void EffectiveDropshadowColor_PreservesShadowRgb()
    {
        Circle sut = new()
        {
            Color = new Color(10, 20, 30, 64),
            DropshadowColor = new Color(100, 110, 120, 200),
        };

        Color effective = sut.EffectiveDropshadowColor;
        effective.R.ShouldBe((byte)100);
        effective.G.ShouldBe((byte)110);
        effective.B.ShouldBe((byte)120);
    }

    // Issue #2950 — on a stroke-only Circle (no fill), when DropshadowBlur exceeds StrokeWidth
    // the Apos lineThickness arg (StrokeWidth - DropshadowBlur) goes <= 0 and the Apos shader
    // refuses to draw, so the shadow disappears entirely. Fix: clamp lineThickness to a small
    // positive epsilon so Apos still draws, and scale the shadow's starting alpha by
    // (StrokeWidth / DropshadowBlur) so the visible band represents the tail of the alpha
    // ramp instead of the (impossible) full ramp. Matches the user's expected behavior:
    // "start at a smaller alpha value and advance to 0."

    [Fact]
    public void ComputeStrokeShadowDrawParameters_FilledMode_LeavesValuesUnchanged()
    {
        Circle sut = new()
        {
            IsFilled = true,
            StrokeWidth = 2f,
            DropshadowBlurX = 8f,
        };

        Color baseColor = new(50, 60, 70, 200);
        (float strokeWidth, Color color) = sut.ComputeStrokeShadowDrawParameters(baseColor);

        strokeWidth.ShouldBe(-6f);
        color.ShouldBe(baseColor);
    }

    [Fact]
    public void ComputeStrokeShadowDrawParameters_StrokeOnly_BlurZero_LeavesValuesUnchanged()
    {
        Circle sut = new()
        {
            IsFilled = false,
            StrokeWidth = 3f,
            DropshadowBlurX = 0f,
        };

        Color baseColor = new(50, 60, 70, 200);
        (float strokeWidth, Color color) = sut.ComputeStrokeShadowDrawParameters(baseColor);

        strokeWidth.ShouldBe(3f);
        color.ShouldBe(baseColor);
    }

    [Fact]
    public void ComputeStrokeShadowDrawParameters_StrokeGreaterThanBlur_LeavesValuesUnchanged()
    {
        Circle sut = new()
        {
            IsFilled = false,
            StrokeWidth = 10f,
            DropshadowBlurX = 4f,
        };

        Color baseColor = new(50, 60, 70, 200);
        (float strokeWidth, Color color) = sut.ComputeStrokeShadowDrawParameters(baseColor);

        strokeWidth.ShouldBe(6f);
        color.ShouldBe(baseColor);
    }

    [Fact]
    public void ComputeStrokeShadowDrawParameters_StrokeEqualsBlur_FullAlpha_PositiveStrokeWidth()
    {
        // At stroke = blur, lineThickness = 0 — Apos would bail. Engage the fix with full alpha
        // (ratio = 1) and a small positive stroke width so Apos still draws the AA-only band.
        Circle sut = new()
        {
            IsFilled = false,
            StrokeWidth = 4f,
            DropshadowBlurX = 4f,
        };

        Color baseColor = new(50, 60, 70, 200);
        (float strokeWidth, Color color) = sut.ComputeStrokeShadowDrawParameters(baseColor);

        strokeWidth.ShouldBeGreaterThan(0f);
        color.A.ShouldBe((byte)200);
    }

    [Fact]
    public void ComputeStrokeShadowDrawParameters_StrokeHalfOfBlur_HalvesStartingAlpha()
    {
        Circle sut = new()
        {
            IsFilled = false,
            StrokeWidth = 2f,
            DropshadowBlurX = 4f,
        };

        Color baseColor = new(50, 60, 70, 200);
        (float strokeWidth, Color color) = sut.ComputeStrokeShadowDrawParameters(baseColor);

        strokeWidth.ShouldBeGreaterThan(0f);
        color.A.ShouldBe((byte)100);
        // RGB preserved — only alpha is scaled.
        color.R.ShouldBe((byte)50);
        color.G.ShouldBe((byte)60);
        color.B.ShouldBe((byte)70);
    }

    // Issue (follow-up to #2950) — Apos.Shapes' aaSize is consumed by the shader in screen-pixel
    // space (fwidth-based AA), so a fixed DropshadowBlurX rendered the same number of screen
    // pixels regardless of camera zoom. As the camera zooms in, the object grew but the shadow
    // halo did not, making the shadow appear to shrink and shift relative to its host. The fix
    // multiplies the value by the current camera zoom before handing it to Apos, so the visible
    // shadow halo holds a constant *world* extent — matching how the rest of Gum's wireframe
    // anchors values to world space.

    // Issue (follow-up to #2950) — Apos.Shapes paints a 1-pixel AA fringe regardless of the
    // strokeWidth argument, so a stroke-only shape with StrokeWidth = 0 still shows a hairline
    // of stroke color (user repro: pink circle with the stroke supposedly "off"). Gate render
    // on HasVisibleOutput so we skip the !IsFilled draw entirely when the user disabled stroke.

    [Fact]
    public void HasVisibleOutput_FilledMode_TrueRegardlessOfStrokeWidth()
    {
        Circle filledZeroStroke = new() { IsFilled = true, StrokeWidth = 0f };
        filledZeroStroke.HasVisibleOutput.ShouldBeTrue();

        Circle filledWithStroke = new() { IsFilled = true, StrokeWidth = 3f };
        filledWithStroke.HasVisibleOutput.ShouldBeTrue();
    }

    [Fact]
    public void HasVisibleOutput_StrokeOnly_PositiveWidth_True()
    {
        Circle sut = new() { IsFilled = false, StrokeWidth = 3f };

        sut.HasVisibleOutput.ShouldBeTrue();
    }

    [Fact]
    public void HasVisibleOutput_StrokeOnly_ZeroWidth_False()
    {
        Circle sut = new() { IsFilled = false, StrokeWidth = 0f };

        sut.HasVisibleOutput.ShouldBeFalse();
    }

    [Fact]
    public void HasVisibleOutput_StrokeOnly_NegativeWidth_False()
    {
        // Defensive — runtime should never push a negative width, but if it does the AA halo
        // would still draw at width 0. Treat as suppressed.
        Circle sut = new() { IsFilled = false, StrokeWidth = -1f };

        sut.HasVisibleOutput.ShouldBeFalse();
    }

    [Fact]
    public void GetShadowAntiAliasSize_AtUnityZoom_ReturnsRoundedBlur()
    {
        Circle sut = new() { DropshadowBlurX = 5f };

        sut.GetShadowAntiAliasSize(cameraZoom: 1f).ShouldBe(5);
    }

    [Fact]
    public void GetShadowAntiAliasSize_ZoomedIn_ScalesBlurUp()
    {
        // At zoom = 2, a 5-world-unit blur should reach 10 screen pixels so the shadow halo
        // remains 5 world units wide on screen.
        Circle sut = new() { DropshadowBlurX = 5f };

        sut.GetShadowAntiAliasSize(cameraZoom: 2f).ShouldBe(10);
    }

    [Fact]
    public void GetShadowAntiAliasSize_ZoomedOut_ScalesBlurDown()
    {
        // At zoom = 0.5, a 4-world-unit blur is 2 screen pixels.
        Circle sut = new() { DropshadowBlurX = 4f };

        sut.GetShadowAntiAliasSize(cameraZoom: 0.5f).ShouldBe(2);
    }

    [Fact]
    public void GetShadowAntiAliasSize_ZeroBlur_ReturnsZero()
    {
        Circle sut = new() { DropshadowBlurX = 0f };

        sut.GetShadowAntiAliasSize(cameraZoom: 4f).ShouldBe(0);
    }

    // Issue #2950 follow-up — strict-anchor shadow geometry. The desired alpha profile is:
    //   alpha = 1                          for r <= R - B/2
    //   alpha smoothsteps from 1 to 0      for r in [R - B/2, R + B/2]
    //   alpha = 0                          for r >= R + B/2
    // So the 50% line sits exactly at the host radius R (matching CSS box-shadow / Figma /
    // Photoshop). When B > 2R the inner ramp edge would be negative; Apos.Shapes clamps such
    // a radius to 0, sliding the entire ramp outward. The helper truncates the inner ramp at
    // rDisk = 0, widens aaSize so the outer edge still sits at R + B/2, and reduces base
    // alpha so the (still-smoothstep) curve passes through (R, 0.5) and (R + B/2, 0).
    //
    // Apos's AA falloff is `3t^2 - 2t^3` (smoothstep), confirmed empirically. Because
    // smoothstep is symmetric (smoothstep(0.5) = 0.5), the standard case math is identical
    // to a linear ramp: 50% sits at the midpoint of the ramp regardless of curve shape.

    [Fact]
    public void ComputeShadowDrawGeometry_ZeroBlur_RendersDiskAtHostRadius()
    {
        Circle sut = new() { DropshadowBlurX = 0f };

        (float effRadius, int effAaSize, float alphaScale) = sut.ComputeShadowDrawGeometry(hostRadius: 10f, cameraZoom: 1f);

        effRadius.ShouldBe(10f);
        effAaSize.ShouldBe(0);
        alphaScale.ShouldBe(1f);
    }

    [Fact]
    public void ComputeShadowDrawGeometry_BlurEqualHalfRadius_RendersSymmetricRamp()
    {
        // Standard case: B = R = 10, so B <= 2R. Ramp spans [R - B/2, R + B/2] = [5, 15],
        // 50% line at R = 10.
        Circle sut = new() { DropshadowBlurX = 10f };

        (float effRadius, int effAaSize, float alphaScale) = sut.ComputeShadowDrawGeometry(hostRadius: 10f, cameraZoom: 1f);

        effRadius.ShouldBe(5f);
        effAaSize.ShouldBe(10);
        alphaScale.ShouldBe(1f);
    }

    [Fact]
    public void ComputeShadowDrawGeometry_BlurEqualTwiceRadius_BoundaryCase()
    {
        // Boundary: B = 2R, inner ramp edge sits exactly at r = 0. Still alphaScale = 1.
        Circle sut = new() { DropshadowBlurX = 20f };

        (float effRadius, int effAaSize, float alphaScale) = sut.ComputeShadowDrawGeometry(hostRadius: 10f, cameraZoom: 1f);

        effRadius.ShouldBe(0f);
        effAaSize.ShouldBe(20);
        alphaScale.ShouldBe(1f);
    }

    [Fact]
    public void ComputeShadowDrawGeometry_BlurGreaterThanTwoRadius_TruncatesAndScalesAlpha()
    {
        // User's repro: R = 36, B = 250. Expected from the math:
        //   aaSize_world = R + B/2 = 161
        //   t = R / aaSize = 36 / 161 = 0.2236
        //   smoothstep = 3t^2 - 2t^3 = 0.1276
        //   alphaScale = 0.5 / (1 - 0.1276) = 0.5731
        Circle sut = new() { DropshadowBlurX = 250f };

        (float effRadius, int effAaSize, float alphaScale) = sut.ComputeShadowDrawGeometry(hostRadius: 36f, cameraZoom: 1f);

        effRadius.ShouldBe(0f);
        effAaSize.ShouldBe(161);
        alphaScale.ShouldBe(0.5731f, tolerance: 0.001f);
    }

    [Fact]
    public void ComputeShadowDrawGeometry_AaSizeScalesByZoom()
    {
        // The aaSize return is in screen pixels (= world aaSize * zoom) so the visible halo
        // holds a constant world extent under zoom (mirrors GetShadowAntiAliasSize).
        Circle sut = new() { DropshadowBlurX = 10f };

        (float effRadius, int effAaSize, float alphaScale) = sut.ComputeShadowDrawGeometry(hostRadius: 10f, cameraZoom: 2f);

        effRadius.ShouldBe(5f);
        effAaSize.ShouldBe(20);
        alphaScale.ShouldBe(1f);
    }

    // Issue #2977 — a stroke-only Circle's dropshadow is a ring, not a filled disk, so the
    // filled-disk anchor model in ComputeShadowDrawGeometry (which pulls the radius inward by
    // blur/2) is wrong for it: once blur exceeds the stroke width, the effective shadow stroke
    // width is clamped to ~0 and the ring centerline collapses to R - blur/2, marching inward as
    // blur grows — the shape visibly "shrinks." ComputeStrokeShadowDrawRadius anchors the shadow
    // ring's centerline (shadowRadius - effectiveShadowStrokeWidth/2) at the body stroke's
    // centerline (R - StrokeWidth/2) regardless of blur; blur only widens the AA halo (aaSize).

    [Fact]
    public void ComputeStrokeShadowDrawRadius_AnchorsCenterlineAtStrokeCenterline_RegardlessOfBlur()
    {
        // Stroke centerline = R - StrokeWidth/2 = 50 - 4/2 = 48. The shadow ring centerline
        // (shadowRadius - effectiveShadowStrokeWidth/2) must equal 48 no matter how the effective
        // shadow stroke width was clamped for blur.
        Circle sut = new() { IsFilled = false, StrokeWidth = 4f };

        // blur < stroke → effective shadow stroke width = StrokeWidth - blur = 4 - 2 = 2.
        float radiusSmallBlur = sut.ComputeStrokeShadowDrawRadius(hostRadius: 50f, effectiveShadowStrokeWidth: 2f);
        (radiusSmallBlur - 2f / 2f).ShouldBe(48f, tolerance: 0.001f);

        // blur >= stroke → effective shadow stroke width clamped to the ~0 epsilon.
        float radiusLargeBlur = sut.ComputeStrokeShadowDrawRadius(hostRadius: 50f, effectiveShadowStrokeWidth: 0.01f);
        (radiusLargeBlur - 0.01f / 2f).ShouldBe(48f, tolerance: 0.001f);
    }

    [Fact]
    public void ComputeStrokeShadowDrawRadius_BlurLessThanStroke_MatchesLegacyDiskRadius()
    {
        // For blur < StrokeWidth the new stroke anchor reduces to the old radius - blur/2, so the
        // common small-blur case is unchanged. R=50, StrokeWidth=10, blur=4 →
        // effectiveShadowStrokeWidth = StrokeWidth - blur = 6, so the radius is
        // 50 - 10/2 + 6/2 = 48 = 50 - blur/2.
        Circle sut = new() { IsFilled = false, StrokeWidth = 10f };

        sut.ComputeStrokeShadowDrawRadius(hostRadius: 50f, effectiveShadowStrokeWidth: 6f).ShouldBe(48f);
    }

    [Fact]
    public void ComputeStrokeShadowDrawParameters_ZeroStrokeWithBlur_ZeroAlpha()
    {
        // stroke = 0, blur > 0 → ratio = 0 → shadow effectively invisible.
        Circle sut = new()
        {
            IsFilled = false,
            StrokeWidth = 0f,
            DropshadowBlurX = 4f,
        };

        Color baseColor = new(50, 60, 70, 200);
        (float strokeWidth, Color color) = sut.ComputeStrokeShadowDrawParameters(baseColor);

        strokeWidth.ShouldBeGreaterThan(0f);
        color.A.ShouldBe((byte)0);
    }

    // Issue #2956 — UseGradient is a *pattern* flag, not a *visibility* flag. A slot whose
    // solid color alpha is 0 (e.g. the default-transparent fill on a stroke-only plain Circle)
    // must NOT paint its gradient — Apos.Shapes' gradient draw bypasses the slot's solid color
    // and would otherwise paint an opaque gradient on a slot the user has explicitly hidden.
    // SkPaint achieves this naturally (paint.Color.alpha modulates the shader output); Apos
    // does not, so we gate the gradient draw explicitly at the call site. Sample cells in the
    // gallery now set FillColor opaque to keep the gradient visible — see migration note.

    // Issue #2958 — FillRadiusInset exists to pull the filled body's outer edge inside the
    // companion stroke band (#2834). The shadow pass shares the same RenderInternal but must
    // NOT inherit that inset — otherwise the shadow draws with a smaller radius than the body's
    // outer edge, leaving a visible ring of missing shadow under the stroke (worse at higher
    // camera zoom because the inset is in world units).

    [Fact]
    public void ComputeFillDrawRadius_BodyPass_NoInset_ReturnsRadius()
    {
        Circle sut = new() { FillRadiusInset = 0f };

        sut.ComputeFillDrawRadius(radius: 50f, isShadowPass: false).ShouldBe(50f);
    }

    [Fact]
    public void ComputeFillDrawRadius_BodyPass_WithInset_SubtractsInset()
    {
        Circle sut = new() { FillRadiusInset = 4f };

        sut.ComputeFillDrawRadius(radius: 50f, isShadowPass: false).ShouldBe(46f);
    }

    [Fact]
    public void ComputeFillDrawRadius_BodyPass_InsetExceedsRadius_ClampsToZero()
    {
        Circle sut = new() { FillRadiusInset = 60f };

        sut.ComputeFillDrawRadius(radius: 50f, isShadowPass: false).ShouldBe(0f);
    }

    [Fact]
    public void ComputeFillDrawRadius_ShadowPass_IgnoresInset()
    {
        Circle sut = new() { FillRadiusInset = 4f };

        sut.ComputeFillDrawRadius(radius: 50f, isShadowPass: true).ShouldBe(50f);
    }

    // Issue #2998 — the gradient-visibility gate keys off the GRADIENT STOP alphas (Alpha1 /
    // Alpha2), NOT the slot's solid Color. The new RectangleRuntime / CircleRuntime default
    // FillColor to transparent (#2938 stroke-only default); a theme that wants a gradient fill
    // sets UseGradient + Color1/Color2 and never touches the solid color, so gating on Color.A
    // suppressed the gradient entirely (Forest Glade buttons / list rows went invisible). The
    // solid color is irrelevant to whether a gradient paints — only the stops are.

    [Fact]
    public void ShouldPaintGradient_BothGradientStopsTransparent_OpaqueSolid_False()
    {
        // Both stops invisible → no gradient, regardless of an opaque solid color.
        Circle sut = new() { UseGradient = true, Color = new Color(255, 255, 255, 255), Alpha1 = 0, Alpha2 = 0 };

        sut.ShouldPaintGradient(forcedColor: null).ShouldBeFalse();
    }

    [Fact]
    public void ShouldPaintGradient_ForcedColor_False()
    {
        // forcedColor means the slot is painting a dropshadow this pass, never the gradient.
        Circle sut = new() { UseGradient = true, Alpha1 = 255, Alpha2 = 255 };

        sut.ShouldPaintGradient(forcedColor: new Color(0, 0, 0, 255)).ShouldBeFalse();
    }

    [Fact]
    public void ShouldPaintGradient_GradientOff_False()
    {
        Circle sut = new() { UseGradient = false, Alpha1 = 255, Alpha2 = 255 };

        sut.ShouldPaintGradient(forcedColor: null).ShouldBeFalse();
    }

    [Fact]
    public void ShouldPaintGradient_OneGradientStopVisible_TransparentSolid_True()
    {
        // A single visible stop is enough; the transparent solid color must not suppress it.
        Circle sut = new() { UseGradient = true, Color = new Color(0, 0, 0, 0), Alpha1 = 255, Alpha2 = 0 };

        sut.ShouldPaintGradient(forcedColor: null).ShouldBeTrue();
    }

    [Fact]
    public void ShouldPaintGradient_VisibleGradientStops_TransparentSolid_True()
    {
        // The core regression repro: gradient stops are opaque but the solid fill is transparent
        // (the new default). The gradient must still paint.
        Circle sut = new() { UseGradient = true, Color = new Color(0, 0, 0, 0), Alpha1 = 255, Alpha2 = 255 };

        sut.ShouldPaintGradient(forcedColor: null).ShouldBeTrue();
    }
}
