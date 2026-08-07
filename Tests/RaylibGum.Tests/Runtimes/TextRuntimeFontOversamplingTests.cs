using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Renderables;
using KernSmith.Gum;
using RaylibGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

// Issue #4330: Raylib parity for #4317/#4309 (MonoGame font oversampling). TextRuntime.RegenerateOversampledFont
// re-rasterizes a Raylib_cs.Font at a multiple of FontSize for crisper text under camera zoom, gated behind the
// global TextRuntime.UseFontOversampling toggle and requiring an IRaylibFontCreator (KernSmith). Uses the real
// KernSmithRaylibFontCreator (not a synthetic fake) so a pass here is strong evidence the visible demo works too --
// hand-crafting a fake Raylib_cs.Font (native Recs/Glyphs pointer arrays) is impractical from managed test code.
public class TextRuntimeFontOversamplingTests : BaseTestClass
{
    // Issue #4330 (manual-test finding, MonoGame side -- same bug mirrored here since Raylib's
    // TextRuntime constructor follows the identical pattern): the automatic per-frame trigger never
    // engaged in a real running game. Root cause: the constructor assigns the backing field directly
    // (_containedText = textRenderable) instead of going through the ContainedText PROPERTY, whose
    // lazy-init getter was the only place OnPreRender ever got wired. Every existing test called
    // RegenerateOversampledFont/UpdateAutomaticFontOversampling directly, never exercising the real
    // OnPreRender path, which is how this survived unnoticed on both platforms.
    [Fact]
    public void Constructor_WiresOnPreRenderForAutomaticOversampling_WithoutAnyExplicitOversamplingCall()
    {
        TextRuntime textRuntime = new();

        var text = (Text)textRuntime.RenderableComponent;

        text.OnPreRender.ShouldNotBeNull(
            "because the automatic per-frame oversampling trigger must be wired by construction, " +
            "not only as a side effect of manually calling RegenerateOversampledFont/UpdateAutomaticFontOversampling");
    }

    [Fact]
    public void RegenerateOversampledFont_WhenEnabledWithCreator_RegeneratesAtOversampledSizeAndLeavesFontScaleUntouched()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 20;

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeTrue();
            var text = (Text)textRuntime.RenderableComponent;
            text.Font.BaseSize.ShouldBe(50);
            text.FontScale.ShouldBe(1f, "because oversampling must compensate through the internal-only " +
                "OversampleCompensationScale, not by overwriting the public FontScale (issue #4317)");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void RegenerateOversampledFont_WhenOversamplingDisabled_DoesNotRegenerateFont()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = false;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 20;
            var text = (Text)textRuntime.RenderableComponent;
            int originalBaseSize = text.Font.BaseSize;

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeFalse();
            text.Font.BaseSize.ShouldBe(originalBaseSize);
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void RegenerateOversampledFont_WhenNoInMemoryFontCreatorRegistered_DoesNotRegenerateFont()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = null;

            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 20;

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeFalse();
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void UpdateAutomaticFontOversampling_WhenZoomBelowOne_ClampsToNativeResolution_NeverUndersamples()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 20;
            textRuntime.UpdateAutomaticFontOversampling(2.5f).ShouldBeTrue(); // raster = 50px, establishes a baseline to zoom back out from

            // Zoomed OUT (0.5x) -- oversampling only ever increases raster resolution, it must not
            // also rasterize below native size when zoomed out.
            bool result = textRuntime.UpdateAutomaticFontOversampling(0.5f);

            result.ShouldBeTrue();
            var text = (Text)textRuntime.RenderableComponent;
            text.Font.BaseSize.ShouldBe(20, "because a zoom below 1 must clamp to native resolution (raster = FontSize), not undersample below it");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Issue #4330 (manual-test finding): UpdateAutomaticFontOversampling early-returned when
    // UseFontOversampling was false, but never undid an ALREADY-oversampled font -- the checkbox on
    // the Zoom demo screen appeared to do nothing when unchecked. The flag is meant to be a live
    // on/off toggle, not a one-way ratchet.
    [Fact]
    public void UpdateAutomaticFontOversampling_WhenDisabledAfterOversamplingWasActive_RevertsToNativeFont()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 20;
            textRuntime.UpdateAutomaticFontOversampling(2.5f).ShouldBeTrue(); // raster = 50px
            var text = (Text)textRuntime.RenderableComponent;
            text.Font.BaseSize.ShouldBe(50);

            // Flag turned off, but the effective zoom (from the still-zoomed camera) is unchanged --
            // exactly what happens in a real game the frame after unchecking the box.
            TextRuntime.UseFontOversampling = false;
            textRuntime.UpdateAutomaticFontOversampling(2.5f);

            text.Font.BaseSize.ShouldBe(20,
                "because disabling oversampling must revert to the native font, not leave the last-oversampled raster stuck in place");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void UpdateAutomaticFontOversampling_WhenRasterPixelDeltaBelowOne_DoesNotRegenerateAgain()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 20;
            textRuntime.UpdateAutomaticFontOversampling(2.5f).ShouldBeTrue(); // raster = 50px

            // 20 * 2.52 = 50.4 -- less than 1px away from the 50px already rasterized.
            bool result = textRuntime.UpdateAutomaticFontOversampling(2.52f);

            result.ShouldBeFalse("because continuous zooming must not re-rasterize every frame for imperceptible deltas");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void UpdateAutomaticFontOversampling_WhenRasterPixelDeltaAtLeastOne_RegeneratesAgain()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 20;
            textRuntime.UpdateAutomaticFontOversampling(2.5f).ShouldBeTrue(); // raster = 50px

            // 20 * 3.0 = 60 -- 10px past the 50px already rasterized.
            bool result = textRuntime.UpdateAutomaticFontOversampling(3.0f);

            result.ShouldBeTrue();
            var text = (Text)textRuntime.RenderableComponent;
            text.Font.BaseSize.ShouldBe(60);
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void FontSize_WhenChangedAfterOversampling_ResetsCompensationSoDrawnWidthMatchesNewNativeSize()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 20;
            textRuntime.Text = "AB";
            textRuntime.RegenerateOversampledFont(2.5f);

            // A plain FontSize change (not going through RegenerateOversampledFont) re-resolves the
            // font at its normal, un-oversampled size -- the OLD compensation ratio must not linger
            // and shrink/grow the new font incorrectly (issue #4317/#4330).
            textRuntime.FontSize = 30;

            var text = (Text)textRuntime.RenderableComponent;
            text.Font.BaseSize.ShouldBe(30, "because the plain FontSize setter re-resolves at native size, not the stale oversampled raster");
            text.WrappedTextWidth.ShouldBeGreaterThan(0);
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void GetEffectiveZoom_WhenLayerOverridesZoom_UsesLayerZoomInsteadOfCamera()
    {
        var camera = new Camera { Zoom = 3f };
        var layer = new RenderingLibrary.Graphics.Layer { LayerCameraSettings = new RenderingLibrary.Graphics.LayerCameraSettings { Zoom = 5f } };

        TextRuntime.GetEffectiveZoom(layer, camera).ShouldBe(5f);
    }

    // Issue #4309's real fix, ported: reproduces a continuous pinch-zoom sequence against the REAL
    // KernSmith rasterizer (not a synthetic fake with hand-picked linear metrics) and asserts the
    // RelativeToChildren box's measured width never drifts from its native (zoom=1) measurement --
    // proving Text.MeasurementFont pinning actually holds up against real, non-linearly-hinted glyphs.
    [Fact]
    public void RegenerateOversampledFont_RealFont_ContinuousZoomSequence_MeasuredWidthDoesNotDriftFromNative()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.WidthUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.HeightUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 12;
            textRuntime.Text = "Stack Ag 12pt";

            var text = (Text)textRuntime.RenderableComponent;
            float nativeWidth = text.WrappedTextWidth;
            float nativeHeight = text.WrappedTextHeight;

            List<string> failures = new();
            foreach (float zoom in BuildContinuousZoomSequence())
            {
                // Mirrors the real per-frame render hook (Text.OnPreRender / PreRender), not a
                // hand-picked direct call to RegenerateOversampledFont.
                textRuntime.UpdateAutomaticFontOversampling(zoom);

                float width = text.WrappedTextWidth;
                float height = text.WrappedTextHeight;
                if (System.MathF.Abs(width - nativeWidth) > 0.01f || System.MathF.Abs(height - nativeHeight) > 0.01f)
                {
                    failures.Add($"zoom={zoom:0.000}: W={width} (expected {nativeWidth}), H={height} (expected {nativeHeight})");
                }
            }

            failures.ShouldBeEmpty($"measured size drifted from the native {nativeWidth}x{nativeHeight}:\n" +
                string.Join("\n", failures));
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Issue #4365: ResolveRunFont (Runtimes/RaylibGum/Renderables/Text.cs) resolves an inline BBCode
    // run's draw size from the LIVE Font.BaseSize, which IS the oversampled raster once
    // RegenerateOversampledFont has swapped it in -- so a run with no [FontScale]/[FontSize] tag (any
    // OTHER tag, e.g. [Color], still routes through ResolveRunFont's default branch) must compose
    // OversampleCompensationScale the same way EffectiveFontScale/DrawPlainLine already do for plain
    // text, or its reported/drawn size balloons with the oversample raster ratio instead of staying
    // pinned to the native size.
    [Fact]
    public void RegenerateOversampledFont_BBCodeColorOnlyRun_WidthDoesNotDriftFromNative()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.WidthUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.HeightUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 12;
            textRuntime.Text = "[Color=Red]Stack[/Color] Ag 12pt";

            var text = (Text)textRuntime.RenderableComponent;
            float nativeWidth = text.WrappedTextWidth;
            nativeWidth.ShouldBeGreaterThan(0);

            textRuntime.RegenerateOversampledFont(2.5f).ShouldBeTrue();

            text.WrappedTextWidth.ShouldBe(nativeWidth, 0.5f,
                "because a Color-only inline run has no explicit scale and must report the same width " +
                "regardless of the raster the live Font happens to be oversampled to");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Companion to the Color-only case above: an explicit [FontScale=2] tag is an ABSOLUTE per-run
    // scale (ResolveRunFont's "case nameof(FontScale)" branch), not a Text-level FontScale, so it must
    // independently compose OversampleCompensationScale too.
    [Fact]
    public void RegenerateOversampledFont_BBCodeFontScaleTagRun_WidthDoesNotDriftFromNative()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.WidthUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.HeightUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 12;
            textRuntime.Text = "small [FontScale=2]BIG[/FontScale]";

            var text = (Text)textRuntime.RenderableComponent;
            float nativeWidth = text.WrappedTextWidth;
            nativeWidth.ShouldBeGreaterThan(0);

            textRuntime.RegenerateOversampledFont(2.5f).ShouldBeTrue();

            // Tolerance wider than the Color-only case: OversampleCompensationScale is derived from the
            // BASE (unscaled) text's measured width ratio (TextRuntime.RegenerateOversampledFont), which
            // is only an approximation of the [FontScale=2] run's own -- differently, non-linearly
            // hinted -- raster at 2x the base size (the same "not exact multiples" gap the
            // Text.MeasurementFont doc comment calls out for the plain-text path).
            text.WrappedTextWidth.ShouldBe(nativeWidth, 3f,
                "because an inline [FontScale=2] run's size is relative to the native FontSize and must " +
                "not also balloon with the oversample raster ratio");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    private static IEnumerable<float> BuildContinuousZoomSequence()
    {
        List<float> zooms = new();
        for (float z = 1f; z <= 4f; z += 0.3f)
        {
            zooms.Add(z);
        }
        for (float z = 4f; z >= 1f; z -= 0.5f)
        {
            zooms.Add(z);
        }
        return zooms;
    }
}
