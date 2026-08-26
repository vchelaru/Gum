using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using Xunit;
using Color = System.Drawing.Color;

namespace GumToolUnitTests.SvgPlugin;

// Issue #4512: Maintain File Aspect Ratio Height does not work for tool-rendered Svg instances.
// The tool's Svg standard type is created as SkiaTexturedRenderable(new RenderableSvg())
// (MainSkiaPlugin.HandleCreateRenderbleFor), and RenderableSvg does implement IAspectRatio.
// But GraphicalUiElement's MaintainFileAspectRatio math casts its *contained object* --
// the SkiaTexturedRenderable wrapper, not the RenderableSvg it wraps -- to IAspectRatio.
// SkiaTexturedRenderable never forwarded that interface, so the cast always failed and
// height never followed width for any Skia-plugin-rendered shape in the tool.
public class SkiaTexturedRenderableAspectRatioTests
{
    [Fact]
    public void ChangingWidth_ShouldUpdateMaintainFileAspectRatioHeight_FromWrappedDrawableAspectRatio()
    {
        var drawable = new FakeAspectRatioDrawable(aspectRatio: 2f);
        var renderable = new SkiaTexturedRenderable(drawable);

        GraphicalUiElement graphicalUiElement = new();
        graphicalUiElement.SetContainedObject(renderable);
        graphicalUiElement.Width = 200;
        graphicalUiElement.Height = 100;
        graphicalUiElement.HeightUnits = DimensionUnitType.MaintainFileAspectRatio;
        graphicalUiElement.UpdateLayout();

        graphicalUiElement.Width = 400;

        graphicalUiElement.AbsoluteHeight.ShouldBe(200);
    }

    private class FakeAspectRatioDrawable : ISkiaSurfaceDrawable, IAspectRatio
    {
        public FakeAspectRatioDrawable(float aspectRatio)
        {
            AspectRatio = aspectRatio;
        }

        public float Width { get; set; }
        public float Height { get; set; }
        public bool NeedsUpdate { get; set; }
        public Color Color => Color.White;
        public bool ShouldApplyColorOnSpriteRender => false;
        public float XSizeSpillover => 0;
        public float YSizeSpillover => 0;
        public ColorOperation ColorOperation { get; set; }
        public bool CanRenderAt0Dimension => true;
        public float AspectRatio { get; }

        public void DrawToSurface(SKSurface surface) { }
        public void PreRender() { }
    }
}
