using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Issue #4302: TextRuntime.RegenerateOversampledFont rasterizes a font at a multiple of FontSize (for
// crisper text under camera zoom). Gated behind the global TextRuntime.UseFontOversampling toggle
// (off by default, so pixel-art projects see no behavior change) and requires an IInMemoryFontCreator
// (KernSmith) -- a disk-based font cache only holds a fixed set of pre-baked sizes, so arbitrary-ratio
// regeneration only makes sense with dynamic generation.
//
// Issue #4317 follow-up: the original mechanism compensated by overwriting the public Text.FontScale,
// which silently stomped a caller's own FontScale/[FontScale] BBCode usage. It now compensates via the
// internal-only Text.OversampleCompensationScale, which composes multiplicatively with FontScale
// instead -- see the "ComposesWith" tests below. This file also covers the automatic, render-time
// trigger (UpdateAutomaticFontOversampling) that replaces the old manual "press R" call.
public class TextRuntimeFontOversamplingTests : BaseTestClass
{
    // Issue #4330 (manual-test finding): the automatic per-frame trigger never actually engaged in a
    // real running game -- checking UseFontOversampling and zooming did nothing, on MonoGame or
    // Raylib. Root cause: TextRuntime's constructor assigns the backing field directly
    // (_containedText = textRenderable) instead of going through the ContainedText PROPERTY, whose
    // lazy-init getter is the only place OnPreRender ever got wired ("if (_containedText == null)").
    // Since the field is already non-null by the time any property setter (Font, Red, etc.) later
    // reads ContainedText, that branch never ran for a normally-constructed TextRuntime -- only for
    // one that never wired up the auto-trigger. Every existing test called RegenerateOversampledFont/
    // UpdateAutomaticFontOversampling directly, never exercising the real OnPreRender path, which is
    // how this survived unnoticed.
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
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            var stubFont = new BitmapFont((Texture2D)null!, StubFontData);
            stubFont.SetFontPattern(256, 256);
            var creator = new CapturingInMemoryFontCreator(stubFont);

            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.FontSize = 20;

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeTrue();
            creator.CapturedFontSize.ShouldBe(50f);
            var text = (Text)textRuntime.RenderableComponent;
            text.BitmapFont.ShouldBeSameAs(stubFont);
            // Issue #4309: compensation is now derived from ACTUAL measured width (pinned native vs.
            // oversampled), not a naive FontSize/rasterFontSize ratio. CapturingInMemoryFontCreator
            // returns the identical stub font object regardless of requested size, so there is
            // genuinely nothing to compensate for -- 1f is correct here. See
            // RegenerateOversampledFont_PinsOutgoingNativeMeasurement_NoJumpWhenOversamplingActivates
            // for a fake whose oversampled font actually differs, covering the general case.
            text.OversampleCompensationScale.ShouldBe(1f);
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
    public void RegenerateOversampledFont_RequestsExactFractionalRasterSize_WithoutRounding()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            var stubFont = new BitmapFont((Texture2D)null!, StubFontData);
            stubFont.SetFontPattern(256, 256);
            var creator = new CapturingInMemoryFontCreator(stubFont);

            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.FontSize = 20;

            textRuntime.RegenerateOversampledFont(2.37f);

            // 20 * 2.37 = 47.4 -- must reach the rasterizer exactly, not rounded to 47 (issue #4317
            // asks for the exact fractional target now that #4304 made BmfcSave.FontSize a float).
            creator.CapturedFontSize.ShouldBe(47.4f, tolerance: 0.0001f);
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void RegenerateOversampledFont_WhenUserFontScaleIsSet_ComposesWithCompensationInsteadOfOverwritingIt()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            var stubFont = new BitmapFont((Texture2D)null!, StubFontData);
            stubFont.SetFontPattern(256, 256);
            var creator = new CapturingInMemoryFontCreator(stubFont);

            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.FontSize = 20;
            textRuntime.FontScale = 2f; // the caller's own explicit scale -- must survive oversampling.

            textRuntime.RegenerateOversampledFont(2.5f);

            var text = (Text)textRuntime.RenderableComponent;
            text.FontScale.ShouldBe(2f, "because the caller's own FontScale must not be stomped by oversampling");
            // Compensation is 1f here (see the "leaves FontScale untouched" test above for why) --
            // the identical stub font is returned for any requested size, so there's nothing to
            // compensate for; the effective scale is just the caller's FontScale.
            ((IText)text).FontScale.ShouldBe(2f * 1f,
                "because the effective on-screen scale is the caller's FontScale composed with the " +
                "internal compensation, not just the compensation alone");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void FontSize_WhenChangedAfterOversampling_ResetsCompensationBackToNeutral()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            // ProportionalFontCreator (not the identical-object stub used elsewhere in this file):
            // its glyph metrics genuinely scale with the requested size, so the pre-reset compensation
            // below is meaningfully different from the post-reset neutral value.
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new ProportionalFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.FontSize = 20;
            textRuntime.RegenerateOversampledFont(2.5f);
            var text = (Text)textRuntime.RenderableComponent;
            text.OversampleCompensationScale.ShouldBe(20f / 50f);

            // A plain FontSize change (not going through RegenerateOversampledFont) re-resolves the
            // font at its normal, un-oversampled size -- the OLD compensation ratio must not linger
            // and shrink/grow the new font incorrectly (issue #4317).
            textRuntime.FontSize = 30;

            text.OversampleCompensationScale.ShouldBe(1f);
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Issue #4330 (manual-test finding): UpdateAutomaticFontOversampling early-returned when
    // UseFontOversampling was false, but never undid an ALREADY-oversampled font -- so toggling the
    // flag off mid-session (the checkbox on the Zoom demo screen) had no visible effect; the text
    // stayed at whatever raster size it was last regenerated to, forever. The flag is meant to be a
    // live on/off toggle, not a one-way ratchet.
    [Fact]
    public void UpdateAutomaticFontOversampling_WhenDisabledAfterOversamplingWasActive_RevertsToNativeFont()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new ProportionalFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.FontSize = 20;
            textRuntime.UpdateAutomaticFontOversampling(2.5f).ShouldBeTrue(); // raster = 50px
            var text = (Text)textRuntime.RenderableComponent;
            text.OversampleCompensationScale.ShouldNotBe(1f);

            // Flag turned off, but the effective zoom (from the still-zoomed camera) is unchanged --
            // this is exactly what happens in a real game the frame after unchecking the box.
            TextRuntime.UseFontOversampling = false;
            textRuntime.UpdateAutomaticFontOversampling(2.5f);

            text.OversampleCompensationScale.ShouldBe(1f,
                "because disabling oversampling must revert to the native font, not leave the last-oversampled raster stuck in place");
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
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            var creator = new CapturingInMemoryFontCreator(new BitmapFont((Texture2D)null!, StubFontData));

            TextRuntime.UseFontOversampling = false;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.FontSize = 20;
            var text = (Text)textRuntime.RenderableComponent;
            var originalFont = text.BitmapFont;
            var originalFontScale = text.FontScale;
            // Assigning FontSize above already resolves this Text's normal (non-oversampled) font
            // through the same global InMemoryFontCreator, so reset the call tracking here to isolate
            // whether RegenerateOversampledFont itself invokes the creator.
            creator.ResetCallTracking();

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeFalse();
            creator.WasCalled.ShouldBeFalse();
            text.BitmapFont.ShouldBeSameAs(originalFont);
            text.FontScale.ShouldBe(originalFontScale);
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
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = null;

            TextRuntime textRuntime = new();
            textRuntime.FontSize = 20;
            var text = (Text)textRuntime.RenderableComponent;
            var originalFont = text.BitmapFont;
            var originalFontScale = text.FontScale;

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeFalse();
            text.BitmapFont.ShouldBeSameAs(originalFont);
            text.FontScale.ShouldBe(originalFontScale);
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Issue #4317: UpdateAutomaticFontOversampling(float) is the testable decision core behind the
    // Text.OnPreRender-wired automatic trigger -- given an effective zoom, decide whether to
    // re-rasterize. The parameterless overload (camera/layer lookup) isn't unit-tested here since it
    // needs a real SystemManagers/Renderer/Camera graph; that's covered by the FontPlayground manual
    // test instead.
    [Fact]
    public void UpdateAutomaticFontOversampling_WhenZoomedIn_RegeneratesAtEffectiveZoom()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            var stubFont = new BitmapFont((Texture2D)null!, StubFontData);
            stubFont.SetFontPattern(256, 256);
            var creator = new CapturingInMemoryFontCreator(stubFont);

            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.FontSize = 20;
            creator.ResetCallTracking();

            bool result = textRuntime.UpdateAutomaticFontOversampling(2.5f);

            result.ShouldBeTrue();
            creator.CapturedFontSize.ShouldBe(50f);
            var text = (Text)textRuntime.RenderableComponent;
            // 1f: the identical stub font is returned for any requested size, so there's nothing to
            // compensate for (see RegenerateOversampledFont_WhenEnabledWithCreator_... 's comment).
            text.OversampleCompensationScale.ShouldBe(1f);
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
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            var stubFont = new BitmapFont((Texture2D)null!, StubFontData);
            stubFont.SetFontPattern(256, 256);
            var creator = new CapturingInMemoryFontCreator(stubFont);

            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.FontSize = 20;
            textRuntime.UpdateAutomaticFontOversampling(2.5f).ShouldBeTrue(); // raster = 50px, establishes a baseline to zoom back out from
            creator.ResetCallTracking();

            // Zoomed OUT (0.5x) -- oversampling only ever increases raster resolution, it must not
            // also rasterize below native size when zoomed out.
            bool result = textRuntime.UpdateAutomaticFontOversampling(0.5f);

            result.ShouldBeTrue();
            creator.CapturedFontSize.ShouldBe(20f, "because a zoom below 1 must clamp to native resolution (raster = FontSize), not undersample below it");
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
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            var stubFont = new BitmapFont((Texture2D)null!, StubFontData);
            stubFont.SetFontPattern(256, 256);
            var creator = new CapturingInMemoryFontCreator(stubFont);

            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.FontSize = 20;
            textRuntime.UpdateAutomaticFontOversampling(2.5f).ShouldBeTrue(); // raster = 50px
            creator.ResetCallTracking();

            // 20 * 2.52 = 50.4 -- less than 1px away from the 50px already rasterized.
            bool result = textRuntime.UpdateAutomaticFontOversampling(2.52f);

            result.ShouldBeFalse();
            creator.WasCalled.ShouldBeFalse("because continuous zooming must not re-rasterize every frame for imperceptible deltas");
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
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            var stubFont = new BitmapFont((Texture2D)null!, StubFontData);
            stubFont.SetFontPattern(256, 256);
            var creator = new CapturingInMemoryFontCreator(stubFont);

            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.FontSize = 20;
            textRuntime.UpdateAutomaticFontOversampling(2.5f).ShouldBeTrue(); // raster = 50px
            creator.ResetCallTracking();

            // 20 * 3.0 = 60 -- 10px past the 50px already rasterized.
            bool result = textRuntime.UpdateAutomaticFontOversampling(3.0f);

            result.ShouldBeTrue();
            creator.WasCalled.ShouldBeTrue();
            creator.CapturedFontSize.ShouldBe(60f);
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void GetEffectiveZoom_WhenLayerIsNull_ReturnsCameraZoom()
    {
        var camera = new Camera { Zoom = 3f };

        TextRuntime.GetEffectiveZoom(null, camera).ShouldBe(3f);
    }

    [Fact]
    public void GetEffectiveZoom_WhenLayerHasNoZoomOverride_FallsBackToCameraZoom()
    {
        var camera = new Camera { Zoom = 3f };
        var layer = new Layer { LayerCameraSettings = new LayerCameraSettings() };

        TextRuntime.GetEffectiveZoom(layer, camera).ShouldBe(3f);
    }

    [Fact]
    public void GetEffectiveZoom_WhenLayerOverridesZoom_UsesLayerZoomInsteadOfCamera()
    {
        var camera = new Camera { Zoom = 3f };
        var layer = new Layer { LayerCameraSettings = new LayerCameraSettings { Zoom = 5f } };

        TextRuntime.GetEffectiveZoom(layer, camera).ShouldBe(5f);
    }

    [Fact]
    public void GetEffectiveZoom_WhenLayerIsScreenSpaceWithNoZoomOverride_IgnoresCameraAndReturnsOne()
    {
        var camera = new Camera { Zoom = 3f };
        var layer = new Layer { LayerCameraSettings = new LayerCameraSettings { IsInScreenSpace = true } };

        TextRuntime.GetEffectiveZoom(layer, camera).ShouldBe(1f,
            "because a screen-space layer with no explicit Zoom override must stay 1:1 regardless of camera zoom");
    }

    // Reproduction attempt for the wrap-earlier report on #4302: goes through the real TextRuntime
    // property cascade (not a bare Text) with a fixed Width, using a fake creator whose glyph metrics
    // scale perfectly with the requested FontSize -- i.e. the oversampled font is an exact multiple of
    // the base font, same as the "AB AB" bare-Text regression test in TextTests.cs. If this passes,
    // the TextRuntime/GraphicalUiElement plumbing isn't adding its own discrepancy on top of the core
    // BitmapFont/FontScale swap (already proven sound by that other test) -- meaning a real-world
    // wrap shift comes from the actual rasterizer's glyph metrics not scaling perfectly linearly,
    // not from a logic bug in Gum's wrap-width math.
    [Fact]
    public void RegenerateOversampledFont_WithProportionalFont_DoesNotChangeWrappedLineCount()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new ProportionalFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.WidthUnits = DimensionUnitType.Absolute;
            textRuntime.FontSize = 20;
            textRuntime.Width = 5 * 20; // exactly fits "AB AB" (5 chars * FontSize-as-xadvance)
            textRuntime.Text = "AB AB";

            var text = (Text)textRuntime.RenderableComponent;
            text.WrappedText.Count.ShouldBe(1, "because AB AB exactly fits the base font/width before any oversampling");

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeTrue();
            text.WrappedText.Count.ShouldBe(1,
                "because the fake creator's glyph metrics scale exactly with FontSize, so FontScale " +
                "compensates perfectly -- the on-screen size, and therefore the wrap, must not change");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // The real bug (not the rounding-noise theory above): RelativeToChildren sizes the box to fit its
    // OWN content, so it must never wrap, full stop -- regardless of which font is loaded or how
    // imperfectly that font's glyph metrics scale. RegenerateOversampledFont swaps BitmapFont/FontScale
    // directly on the renderable without calling back into GraphicalUiElement.UpdateLayout(), so a
    // RelativeToChildren box's Width is never re-derived from the new font -- it stays frozen at the
    // natural width measured against the OLD font. That staleness happens to cancel out mathematically
    // when the new font scales perfectly linearly (see the Proportional test above), but any real
    // rasterizer's hinting/rounding makes the oversampled font's glyphs not an exact multiple of the
    // base font's, which the frozen Width has no way to absorb -- unlike a fresh RelativeToChildren
    // measurement, which would just re-size to fit. NearlyProportionalFontCreator simulates that
    // realistic 1px-per-glyph rounding noise at the oversampled size.
    [Fact]
    public void RegenerateOversampledFont_WithRelativeToChildrenWidth_ShouldNeverWrap()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new NearlyProportionalFontCreator(baseFontSize: 20);

            TextRuntime textRuntime = new();
            textRuntime.WidthUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.FontSize = 20;
            textRuntime.Text = "AB AB";

            var text = (Text)textRuntime.RenderableComponent;
            text.WrappedText.Count.ShouldBe(1, "because RelativeToChildren sizes the box to exactly fit this text before any oversampling");

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeTrue();
            text.WrappedText.Count.ShouldBe(1,
                "because RelativeToChildren must re-size to fit whatever font is currently loaded -- it can never " +
                "wrap its own content, even when the oversampled font's real-world glyph metrics aren't a perfect multiple of the base font's");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Issue #4309: the ShouldNeverWrap test above only proves the text stays on ONE line -- it doesn't
    // prove the measured WIDTH is stable. A RelativeToChildren box has no reason to shrink while zoom
    // moves in one direction, but that's exactly what re-measuring against each new oversampled raster
    // in turn can produce, since hinting isn't linear across raster sizes. The fix pins measurement to
    // whatever the native (pre-oversample) BitmapFont measured -- no KernSmith design-metrics API
    // needed, just NearlyProportionalFontCreator's plain TryCreateFont.
    [Fact]
    public void RegenerateOversampledFont_WithRelativeToChildrenWidth_MeasuredWidthDoesNotJitterAcrossRasterSizes()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new NearlyProportionalFontCreator(baseFontSize: 20);

            TextRuntime textRuntime = new();
            textRuntime.WidthUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.FontSize = 20;
            textRuntime.Text = "AB AB";

            var text = (Text)textRuntime.RenderableComponent;
            float widthBeforeOversampling = text.WrappedTextWidth;

            textRuntime.RegenerateOversampledFont(2.5f);
            float widthAt2_5x = text.WrappedTextWidth;

            textRuntime.RegenerateOversampledFont(3.7f);
            float widthAt3_7x = text.WrappedTextWidth;

            widthAt2_5x.ShouldBe(widthBeforeOversampling, tolerance: 0.01f,
                "because the pinned native measurement must not jitter even though the rasterized " +
                "DISPLAY font's glyph advances have +1px-per-glyph noise at this raster size");
            widthAt3_7x.ShouldBe(widthBeforeOversampling, tolerance: 0.01f,
                "because the measured width must stay pinned to the native size across ANY raster size, not just one lucky match");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Issue #4309 (render-side gap found via manual test, FontScale 2 at ~2.9x zoom): the box is now
    // correctly pinned/stable, but a naive FontSize/rasterFontSize compensation ratio assumes the
    // oversampled font's glyphs are an exact linear scale of the native font's -- they're not, since
    // hinting isn't linear across raster sizes. That leaves a visible gap between the drawn text and
    // its own box. The fix derives the compensation from the ACTUAL measured width ratio between the
    // pinned and oversampled fonts (for this Text's own content), so the drawn text exactly fills the box.
    [Fact]
    public void RegenerateOversampledFont_CompensationDerivedFromActualMeasurement_DrawnTextExactlyFillsPinnedBox()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new NearlyProportionalFontCreator(baseFontSize: 20);

            TextRuntime textRuntime = new();
            textRuntime.WidthUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.FontSize = 20;
            textRuntime.Text = "AB AB";

            var text = (Text)textRuntime.RenderableComponent;
            float pinnedWidth = text.WrappedTextWidth; // the box size, already proven stable above

            textRuntime.RegenerateOversampledFont(2.5f);

            // The actual DRAWN width: the live (oversampled) BitmapFont's own raw measurement, scaled
            // by the composed effective scale -- what Sprite.Render actually puts on screen, as
            // distinct from the separately-pinned box measurement.
            float drawnWidth = text.BitmapFont.MeasureString("AB AB") * ((IText)text).FontScale;

            drawnWidth.ShouldBe(pinnedWidth, tolerance: 0.01f,
                "because the drawn text must exactly fill its own box, not just approximately -- a naive " +
                "FontSize/rasterFontSize ratio would leave a visible gap since the oversampled font's " +
                "glyph advances aren't an exact linear scale of the native font's");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Issue #4309 (discontinuity found via manual test): an earlier version of this fix measured against
    // KernSmith design-unit metrics -- a DIFFERENT measurement system than the native rasterized font,
    // not guaranteed to agree with it (hinted vs. unhinted). That mismatched every text whose display
    // font was never actually oversampled (a "control" instance), and jumped visibly the moment
    // oversampling activated for the rest. The fix instead pins the OUTGOING native BitmapFont itself as
    // the measurement source -- never a different, independently-computed number -- so there is nothing
    // to disagree with: measurement equals exactly what was being rendered the frame before.
    [Fact]
    public void RegenerateOversampledFont_PinsOutgoingNativeMeasurement_NoJumpWhenOversamplingActivates()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new HintingMismatchFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.WidthUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.FontSize = 20;
            textRuntime.Text = "AB";

            var text = (Text)textRuntime.RenderableComponent;

            // Native/hinted xadvance is 24/char = 48 total -- this IS what's actually rendered, so it
            // must be exactly what's measured too, both before and after oversampling activates.
            float widthAtNativeZoom = text.WrappedTextWidth;

            textRuntime.RegenerateOversampledFont(2.5f);
            float widthAfterOversamplingActivates = text.WrappedTextWidth;

            widthAtNativeZoom.ShouldBe(48f, tolerance: 0.01f);
            widthAfterOversamplingActivates.ShouldBe(widthAtNativeZoom, tolerance: 0.01f,
                "because there must be no discontinuity the moment oversampling first kicks in");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Issue #4309 (landmine found via manual test): an earlier version of this fix fetched its
    // measurement source only from the font-property-resolution choke point, gated on
    // UseFontOversampling's value AT THAT MOMENT -- a project-wide static toggle that can be flipped on
    // at any point in a project's lifecycle, independent of when a given Text's font was last resolved.
    // Pinning is now driven entirely by RegenerateOversampledFont itself (always captures whatever the
    // CURRENT native font is, whenever it's first called), so the flag's value/timing when the font was
    // originally resolved is irrelevant by construction -- nothing to self-heal, because nothing can go
    // stale in the first place.
    [Fact]
    public void RegenerateOversampledFont_UseFontOversamplingTimingDoesNotMatter_NoOrderingLandmine()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = false;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new HintingMismatchFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.WidthUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.FontSize = 20;
            textRuntime.Text = "AB";

            var text = (Text)textRuntime.RenderableComponent;
            float widthBeforeFlagEnabled = text.WrappedTextWidth;

            TextRuntime.UseFontOversampling = true;
            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeTrue();
            text.WrappedTextWidth.ShouldBe(widthBeforeFlagEnabled, tolerance: 0.01f,
                "because the flag being off when the font was first resolved must not matter -- " +
                "RegenerateOversampledFont pins whatever the native font is at the moment it's actually called");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Native/hinted advance (24px/char) is deliberately NOT a round multiple of the base FontSize (20) --
    // simulates a real rasterizer's hinting producing a different result than a naive linear scale would.
    private sealed class HintingMismatchFontCreator : IInMemoryFontCreator
    {
        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            const int xadvance = 24;
            string fontData =
$@"info face=""Arial"" size=-{bmfcSave.FontSize} bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight={bmfcSave.FontSize} base={bmfcSave.FontSize} scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=2
char id=65 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
char id=66 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
";
            BitmapFont font = new BitmapFont((Texture2D)null!, fontData);
            font.SetFontPattern(256, 256);
            return font;
        }
    }

    // Every glyph's xadvance is 1px wider than a perfect (FontSize/baseFontSize)x scale of the base font
    // whenever the requested size isn't baseFontSize itself -- simulates the rounding/hinting noise a
    // real TrueType rasterizer introduces between two different point sizes of the same font.
    private sealed class NearlyProportionalFontCreator : IInMemoryFontCreator
    {
        private readonly int _baseFontSize;

        public NearlyProportionalFontCreator(int baseFontSize)
        {
            _baseFontSize = baseFontSize;
        }

        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            int xadvance = (int)bmfcSave.FontSize;
            if (bmfcSave.FontSize != _baseFontSize)
            {
                xadvance += 1;
            }

            string fontData =
$@"info face=""Arial"" size=-{bmfcSave.FontSize} bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight={bmfcSave.FontSize} base={bmfcSave.FontSize} scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=3
char id=32 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
char id=65 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
char id=66 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
";
            BitmapFont font = new BitmapFont((Texture2D)null!, fontData);
            font.SetFontPattern(256, 256);
            return font;
        }
    }

    // Every glyph's xadvance equals the requested FontSize exactly, so a font generated at size N is
    // a perfect (N/baseSize)x scale of one generated at baseSize -- isolates whether Gum's own
    // wrap-width math introduces error, independent of any real rasterizer's rounding/hinting noise.
    private sealed class ProportionalFontCreator : IInMemoryFontCreator
    {
        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            int xadvance = (int)bmfcSave.FontSize;
            string fontData =
$@"info face=""Arial"" size=-{bmfcSave.FontSize} bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight={bmfcSave.FontSize} base={bmfcSave.FontSize} scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=3
char id=32 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
char id=65 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
char id=66 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
";
            BitmapFont font = new BitmapFont((Texture2D)null!, fontData);
            font.SetFontPattern(256, 256);
            return font;
        }
    }

    // Issue #4370: the RelativeToChildren tests above prove the pinned MEASUREMENT is stable, but an
    // Absolute-width box never re-measures its own content -- it converts the fixed Width constraint
    // into wrap-measurement units by dividing by FontScale (IWrappedTextExtensions.UpdateLines). That
    // division used the render-facing IText.FontScale, which composes in OversampleCompensationScale
    // (the pinned-vs-oversampled measured-width ratio, recomputed fresh every RegenerateOversampledFont
    // call). So the wrap width itself shifted by 1/compensation every time oversampling turned on,
    // even though the pinned MeasurementFont (and therefore the correct wrap width) never changed --
    // reproducing the Zoom demo's "checkbox toggle re-wraps text at a fixed zoom" report. Text.cs
    // already has a MeasurementFontScale (uncompensated) for exactly this purpose (issue #4309); this
    // is the one remaining consumer that still reads the compensated scale instead.
    [Fact]
    public void RegenerateOversampledFont_WithAbsoluteWidth_DoesNotChangeWrappedLineCount()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new NearlyProportionalFontCreator(baseFontSize: 20);

            TextRuntime textRuntime = new();
            textRuntime.WidthUnits = DimensionUnitType.Absolute;
            textRuntime.FontSize = 20;
            // 8 glyphs (A,B,space,A,B,space,A,B) * 20px native xadvance = 160px -- exactly 2 lines at
            // Width=100 ("AB AB " then "AB", assuming a trailing space stays with the first line).
            textRuntime.Width = 100;
            textRuntime.Text = "AB AB AB";

            var text = (Text)textRuntime.RenderableComponent;
            int lineCountBeforeOversampling = text.WrappedText.Count;
            lineCountBeforeOversampling.ShouldBeGreaterThan(1,
                "because the test must start from text that already wraps, or a wrap-width regression has nothing to widen");

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeTrue();
            text.WrappedText.Count.ShouldBe(lineCountBeforeOversampling,
                "because the Absolute Width constraint must always convert to wrap-measurement units via " +
                "the uncompensated measurement scale -- oversampling's display-only compensation must not " +
                "change how many lines a fixed-width box wraps to");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    private const string StubFontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=18 base=18 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=1
char id=32 x=0 y=0 width=9 height=13 xoffset=0 yoffset=4 xadvance=9 page=0 chnl=15
";

    // Captures the FontSize the caller asked for and always returns the stub font supplied at
    // construction, so tests can assert both "what raster size was requested" and "was it wired up".
    private sealed class CapturingInMemoryFontCreator : IInMemoryFontCreator
    {
        private readonly BitmapFont _fontToReturn;

        public CapturingInMemoryFontCreator(BitmapFont fontToReturn)
        {
            _fontToReturn = fontToReturn;
        }

        public bool WasCalled { get; private set; }
        public float CapturedFontSize { get; private set; }

        public void ResetCallTracking() => WasCalled = false;

        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            WasCalled = true;
            CapturedFontSize = bmfcSave.FontSize;
            return _fontToReturn;
        }
    }
}
