using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// #7311 / #7312 - the gradient stop channels (Red1..Alpha1, Red2..Alpha2) and the gradient *Units
/// variables set on a <see cref="CircleRuntime"/> must reach its gradient, so a loaded gradient renders
/// as authored (not a white fill) and a percent-authored gradient matches the pixel-authored one.
/// </summary>
public class CircleGradientTests : BaseTestClass
{
    private static CircleRuntime Create()
    {
        CircleRuntime shape = new();
        shape.Width = 300;
        shape.Height = 200;
        shape.SetProperty("IsFilled", true);
        shape.SetProperty("UseGradient", true);
        return shape;
    }

    [Fact]
    public void SetProperty_Red1Green1Blue1Alpha1_SetsGradientStartOnRenderable()
    {
        CircleRuntime shape = Create();
        shape.SetProperty("Red1", 91);
        shape.SetProperty("Green1", 161);
        shape.SetProperty("Blue1", 207);
        shape.SetProperty("Alpha1", 128);

        LineCircle renderable = (LineCircle)shape.RenderableComponent!;
        renderable.Color1.R.ShouldBe((byte)91);
        renderable.Color1.G.ShouldBe((byte)161);
        renderable.Color1.B.ShouldBe((byte)207);
        renderable.Color1.A.ShouldBe((byte)128);
    }

    [Fact]
    public void SetProperty_Red2Green2Blue2Alpha2_SetsGradientEndOnRenderable()
    {
        CircleRuntime shape = Create();
        shape.SetProperty("Red2", 10);
        shape.SetProperty("Green2", 20);
        shape.SetProperty("Blue2", 30);
        shape.SetProperty("Alpha2", 40);

        LineCircle renderable = (LineCircle)shape.RenderableComponent!;
        renderable.Color2.R.ShouldBe((byte)10);
        renderable.Color2.G.ShouldBe((byte)20);
        renderable.Color2.B.ShouldBe((byte)30);
        renderable.Color2.A.ShouldBe((byte)40);
    }

    [Fact]
    public void SetProperty_PercentEndpointUnits_ResolvesEndpointsToPixels()
    {
        CircleRuntime shape = Create();
        shape.SetProperty("GradientX1", 10f);
        shape.SetProperty("GradientY1", 20f);
        shape.SetProperty("GradientX2", 100f);
        shape.SetProperty("GradientY2", 50f);
        shape.SetProperty("GradientX1Units", PositionUnitType.PercentageWidth);
        shape.SetProperty("GradientY1Units", PositionUnitType.PercentageHeight);
        shape.SetProperty("GradientX2Units", PositionUnitType.PercentageWidth);
        shape.SetProperty("GradientY2Units", PositionUnitType.PercentageHeight);

        LineCircle renderable = (LineCircle)shape.RenderableComponent!;
        renderable.EffectiveGradientX1.ShouldBe(30f);
        renderable.EffectiveGradientY1.ShouldBe(40f);
        renderable.EffectiveGradientX2.ShouldBe(300f);
        renderable.EffectiveGradientY2.ShouldBe(100f);
    }

    [Fact]
    public void SetProperty_PercentRadiusUnits_ResolvesRadiiToPixels()
    {
        CircleRuntime shape = Create();
        shape.SetProperty("GradientInnerRadius", 10f);
        shape.SetProperty("GradientOuterRadius", 50f);
        shape.SetProperty("GradientInnerRadiusUnits", DimensionUnitType.PercentageOfParent);
        shape.SetProperty("GradientOuterRadiusUnits", DimensionUnitType.PercentageOfParent);

        LineCircle renderable = (LineCircle)shape.RenderableComponent!;
        renderable.EffectiveGradientInnerRadius.ShouldBe(30f);
        renderable.EffectiveGradientOuterRadius.ShouldBe(150f);
    }

    [Fact]
    public void Draw_LinearGradientWithPercentUnits_PaintsStartAndEndColorsNotWhite()
    {
        const int size = 64;
        ContainerRuntime container = new();
        container.Width = size;
        container.Height = size;
        container.IsRenderTarget = true;

        CircleRuntime shape = new();
        shape.Width = size;
        shape.Height = size;
        shape.SetProperty("IsFilled", true);
        shape.SetProperty("UseGradient", true);
        shape.SetProperty("GradientType", GradientType.Linear);
        shape.SetProperty("Red1", 255);
        shape.SetProperty("Green1", 0);
        shape.SetProperty("Blue1", 0);
        shape.SetProperty("Red2", 0);
        shape.SetProperty("Green2", 0);
        shape.SetProperty("Blue2", 255);
        shape.SetProperty("GradientX1", 0f);
        shape.SetProperty("GradientY1", 0f);
        shape.SetProperty("GradientX2", 100f);
        shape.SetProperty("GradientY2", 0f);
        shape.SetProperty("GradientX2Units", PositionUnitType.PercentageWidth);
        container.Children.Add(shape);

        GumService.Default.Root.Children.Add(container);
        GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();

        Image image = LoadImageFromTexture(Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value.Texture);
        Color left = GetImageColor(image, size / 2 - 22, size / 2);
        Color right = GetImageColor(image, size / 2 + 22, size / 2);
        UnloadImage(image);
        GumService.Default.Root.Children.Clear();

        left.R.ShouldBeGreaterThan((byte)180);
        left.B.ShouldBeLessThan((byte)80);
        right.B.ShouldBeGreaterThan((byte)180);
        right.R.ShouldBeLessThan((byte)80);
    }
}
