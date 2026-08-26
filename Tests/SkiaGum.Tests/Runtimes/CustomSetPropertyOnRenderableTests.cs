using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;
using SkiaGum.Content;
using SkiaGum.GueDeriving;
using SkiaSharp;

namespace SkiaGum.Tests.Runtimes;

// Covers the AdditionalPropertyOnRenderable extension hook on SkiaGum's SetPropertyOnRenderable
// dispatch (issue #3650 file-unification convergence). The unified MonoGame/Raylib copy checks this
// hook before falling back to reflection; SkiaGum's copy had dropped it entirely, so a plugin
// registering AdditionalPropertyOnRenderable (e.g. the Apos.Shapes runtimes) would silently never
// run under SkiaGum.
public class CustomSetPropertyOnRenderableTests
{
    public CustomSetPropertyOnRenderableTests()
    {
        // Wire up the SkiaGum custom property setter so SetProperty routes correctly.
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void SetPropertyOnRenderable_UnhandledProperty_ShouldInvokeAdditionalPropertyOnRenderable()
    {
        ContainerRuntime container = new();
        IRenderableIpso renderable = (IRenderableIpso)container.RenderableComponent;
        bool wasCalled = false;

        CustomSetPropertyOnRenderable.AdditionalPropertyOnRenderable = (ipso, gue, propertyName, value) =>
        {
            wasCalled = true;
            return true;
        };
        try
        {
            CustomSetPropertyOnRenderable.SetPropertyOnRenderable(renderable, container, "ThisPropertyDoesntExistAnywhere", 42);
        }
        finally
        {
            CustomSetPropertyOnRenderable.AdditionalPropertyOnRenderable = null;
        }

        wasCalled.ShouldBeTrue();
    }

    [Fact]
    public void SetPropertyOnRenderable_Typeface_ShouldForwardToRenderable()
    {
        // Typeface (#3708): SkiaGum's SetProperty dispatch had no arm for this at all -- it never
        // existed on Skia before, so the string-based path (codegen/state application) had nothing
        // to route to. Mirrors the MonoGame/Raylib Font/BitmapFont dispatch coverage.
        Gum.GueDeriving.TextRuntime textRuntime = new();
        IRenderableIpso renderable = (IRenderableIpso)textRuntime.RenderableComponent;
        SKTypeface typeface = SKTypeface.FromFamilyName("Arial");

        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(renderable, textRuntime, "Typeface", typeface);

        ((Text)renderable).Typeface.ShouldBe(typeface);
    }

    // Issue #4005 — Text's new dropshadow variables. HasDropshadow/DropshadowOffsetX/Y and the
    // color channels already had dispatch arms (they route onto SkiaGum.Renderables.Text, which
    // has matching properties). But the scalar DropshadowBlur has no arm and no matching property
    // on the renderable (only DropshadowBlurX/DropshadowBlurY exist there, which Text.Render reads
    // directly) -- setting it via SetProperty silently no-op'd even after the reflection fallback.
    // The scalar must seed both per-axis renderable properties, not TextRuntime's own separate
    // (and unread-by-rendering) DropshadowBlur field.
    [Fact]
    public void SetPropertyOnRenderable_TextDropshadowBlur_ShouldForwardToRenderableBothAxes()
    {
        Gum.GueDeriving.TextRuntime textRuntime = new();
        Text renderable = (Text)textRuntime.RenderableComponent;

        textRuntime.SetProperty("DropshadowBlur", 7f);

        renderable.DropshadowBlurX.ShouldBe(7f);
        renderable.DropshadowBlurY.ShouldBe(7f);
    }

    [Fact]
    public void SetProperty_SourceFileOnSvg_ShouldUpdateMaintainFileAspectRatioHeight()
    {
        const string svgMarkup = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 200 100\"><rect width=\"200\" height=\"100\" /></svg>";
        string filePath = Path.Combine(Path.GetTempPath(), $"gum-svg-{Guid.NewGuid():N}.svg");
        File.WriteAllText(filePath, svgMarkup);
        SkiaResourceManager.Initialize(resourceAssembly: null);

        try
        {
            GraphicalUiElement graphicalUiElement = new();
            VectorSprite vectorSprite = new();
            graphicalUiElement.SetContainedObject(vectorSprite);
            graphicalUiElement.Width = 200;
            graphicalUiElement.Height = 100;
            graphicalUiElement.HeightUnits = Gum.DataTypes.DimensionUnitType.MaintainFileAspectRatio;
            graphicalUiElement.UpdateLayout();

            graphicalUiElement.SetProperty("SourceFile", filePath);

            graphicalUiElement.AbsoluteHeight.ShouldBe(100);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}
