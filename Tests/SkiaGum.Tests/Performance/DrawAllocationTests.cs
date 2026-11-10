using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;
using SkiaSharp;
using Xunit.Abstractions;

namespace SkiaGum.Tests.Performance;

/// <summary>
/// Per-frame allocation baselines for drawing on the Skia runtime (issue #1934). Unlike layout, which
/// is shared code, Skia draws through its own renderables, so these are ratchets: each limit sits just
/// above what the renderable allocates today and comes down as allocation sources are removed. Zero is
/// the target. The counter sees managed bytes only, not the native memory behind an <c>SKPaint</c>.
/// </summary>
public class DrawAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;
    private readonly SkiaSceneHost _host = new SkiaSceneHost();

    public DrawAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public override void Dispose()
    {
        _host.Dispose();
        base.Dispose();
    }

    private static GraphicalUiElement Create(string kind)
    {
        switch (kind)
        {
            case "SolidRectangle":
                return new SolidRectangleRuntime { Width = 100, Height = 100, Color = SKColors.Red };
            case "RoundedRectangle":
                return new RoundedRectangleRuntime { Width = 100, Height = 100, Color = SKColors.Red };
            case "Circle":
                return new ColoredCircleRuntime { Width = 100, Height = 100, Color = SKColors.Red };
            case "Arc":
                return new ArcRuntime { Width = 100, Height = 100, Color = SKColors.Red };
            case "Line":
                return new LineRuntime { Width = 100, Height = 100, Color = SKColors.Red };
            case "Text":
                return new TextRuntime { Text = "Hello world" };
            case "TextWrapped":
                return new TextRuntime
                {
                    Text = "The quick brown fox jumps over the lazy dog and keeps going",
                    Width = 120,
                    WidthUnits = DimensionUnitType.Absolute
                };
            case "TextDropshadow":
                return new TextRuntime { Text = "Hello world", HasDropshadow = true };
            case "TextBbCode":
                return new TextRuntime { Text = "Hello [Color=Red]world[/Color]" };
            case "RoundedRectangleStroke":
                return new RoundedRectangleRuntime { Width = 100, Height = 100, FillColor = SKColors.Red, StrokeColor = SKColors.Blue, StrokeWidth = 4 };
            case "RoundedRectangleGradient":
                return new RoundedRectangleRuntime { Width = 100, Height = 100, UseGradient = true };
            case "RoundedRectangleDropshadow":
                return new RoundedRectangleRuntime
                {
                    Width = 100,
                    Height = 100,
                    Color = SKColors.Red,
                    HasDropshadow = true,
                    DropshadowOffsetX = 4,
                    DropshadowOffsetY = 4,
                    DropshadowBlurX = 4,
                    DropshadowBlurY = 4
                };
            case "CircleStroke":
                return new ColoredCircleRuntime { Width = 100, Height = 100, FillColor = SKColors.Red, StrokeColor = SKColors.Blue, StrokeWidth = 4 };
            case "Sprite":
                return new SpriteRuntime
                {
                    Texture = CreateBitmap(16),
                    Width = 100,
                    Height = 100,
                    WidthUnits = DimensionUnitType.Absolute,
                    HeightUnits = DimensionUnitType.Absolute
                };
            case "NineSlice":
                return new NineSliceRuntime
                {
                    Texture = CreateBitmap(24),
                    Width = 100,
                    Height = 100,
                    WidthUnits = DimensionUnitType.Absolute,
                    HeightUnits = DimensionUnitType.Absolute
                };
            case "Polygon":
                PolygonRuntime polygon = new PolygonRuntime { Color = SKColors.Red };
                polygon.SetPoints(new[]
                {
                    new System.Numerics.Vector2(0, 0),
                    new System.Numerics.Vector2(100, 0),
                    new System.Numerics.Vector2(100, 100),
                    new System.Numerics.Vector2(0, 100)
                });
                return polygon;
            case "ClippedContainer":
                ContainerRuntime clipped = new ContainerRuntime
                {
                    Width = 100,
                    Height = 100,
                    WidthUnits = DimensionUnitType.Absolute,
                    HeightUnits = DimensionUnitType.Absolute,
                    ClipsChildren = true
                };
                clipped.AddChild(new SolidRectangleRuntime { Width = 150, Height = 150, Color = SKColors.Red });
                return clipped;
            case "RenderTargetContainer":
                ContainerRuntime renderTarget = new ContainerRuntime
                {
                    Width = 100,
                    Height = 100,
                    WidthUnits = DimensionUnitType.Absolute,
                    HeightUnits = DimensionUnitType.Absolute,
                    IsRenderTarget = true
                };
                renderTarget.AddChild(new SolidRectangleRuntime { Width = 80, Height = 80, Color = SKColors.Red });
                return renderTarget;
            default:
                throw new ArgumentException("Unknown kind " + kind);
        }
    }

    private static SKBitmap CreateBitmap(int size)
    {
        SKBitmap bitmap = new SKBitmap(size, size);
        bitmap.Erase(SKColors.Green);
        return bitmap;
    }

    // The limit is the per-frame managed bytes of one idle Update + Draw of a single renderable.
    [Theory]
    [InlineData("SolidRectangle", 0)]
    [InlineData("RoundedRectangle", 0)]
    [InlineData("Circle", 0)]
    [InlineData("Line", 0)]
    [InlineData("Arc", 0)]
    [InlineData("Text", 0)]
    [InlineData("TextBbCode", 0)]
    [InlineData("TextDropshadow", 0)]
    [InlineData("TextWrapped", 0)]
    [InlineData("RoundedRectangleStroke", 0)]
    [InlineData("RoundedRectangleGradient", 0)]
    [InlineData("RoundedRectangleDropshadow", 0)]
    [InlineData("CircleStroke", 0)]
    [InlineData("Sprite", 0)]
    [InlineData("NineSlice", 0)]
    [InlineData("Polygon", 0)]
    [InlineData("ClippedContainer", 0)]
    [InlineData("RenderTargetContainer", 0)]
    public void UpdateAndDraw_UnchangedRenderable_AllocationBaseline(string kind, int limitBytesPerFrame)
    {
        Create(kind).AddToRoot();

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                _host.Update();
                _host.Draw();
            },
            warmupIterations: 50,
            measuredIterations: 300);

        _output.WriteLine($"{kind}: {result.BytesPerIteration:N0} bytes/frame");

        // Liveness: the draw must put pixels on the surface, or a silent no-op would pass.
        _host.CountPaintedSamples().ShouldBeGreaterThan(0);
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(limitBytesPerFrame);
    }

    // Builds the element for an animated case and returns what changes it on a given frame. The limit
    // is the per-frame managed bytes of Update + Draw while that property changes every frame.
    private static Action<int> CreateAnimated(string kind, out GraphicalUiElement element)
    {
        switch (kind)
        {
            case "Color":
                RoundedRectangleRuntime colored = new RoundedRectangleRuntime { Width = 100, Height = 100, Color = SKColors.Red };
                element = colored;
                return frame => colored.Color = frame % 2 == 0 ? SKColors.Red : SKColors.Blue;
            case "StrokeWidth":
                ColoredCircleRuntime stroked = new ColoredCircleRuntime
                {
                    Width = 100,
                    Height = 100,
                    FillColor = SKColors.Red,
                    StrokeColor = SKColors.Blue,
                    StrokeWidth = 4
                };
                element = stroked;
                return frame => stroked.StrokeWidth = 4 + frame % 2;
            case "Size":
                RoundedRectangleRuntime sized = new RoundedRectangleRuntime
                {
                    Width = 100,
                    Height = 100,
                    Color = SKColors.Red,
                    WidthUnits = DimensionUnitType.Absolute
                };
                element = sized;
                return frame => sized.Width = 100 + frame % 2;
            case "DropshadowOffset":
                RoundedRectangleRuntime shadowed = (RoundedRectangleRuntime)Create("RoundedRectangleDropshadow");
                element = shadowed;
                return frame => shadowed.DropshadowOffsetX = 4 + frame % 2;
            case "GradientColor":
                RoundedRectangleRuntime gradient = (RoundedRectangleRuntime)Create("RoundedRectangleGradient");
                element = gradient;
                return frame => gradient.Red1 = frame % 2 == 0 ? 0 : 255;
            case "SpriteColor":
                SpriteRuntime sprite = (SpriteRuntime)Create("Sprite");
                element = sprite;
                return frame => sprite.Color = frame % 2 == 0 ? SKColors.White : SKColors.Gray;
            case "TextDropshadowOffset":
                TextRuntime text = (TextRuntime)Create("TextDropshadow");
                element = text;
                return frame => text.DropshadowOffsetX = 4 + frame % 2;
            default:
                throw new ArgumentException("Unknown kind " + kind);
        }
    }

    [Theory]
    [InlineData("Color", 0)]
    [InlineData("StrokeWidth", 0)]
    [InlineData("Size", 0)]
    [InlineData("DropshadowOffset", 120)]
    [InlineData("GradientColor", 220)]
    [InlineData("SpriteColor", 120)]
    [InlineData("TextDropshadowOffset", 120)]
    public void UpdateAndDraw_AnimatedProperty_AllocationBaseline(string kind, int limitBytesPerFrame)
    {
        Action<int> animate = CreateAnimated(kind, out GraphicalUiElement element);
        element.AddToRoot();
        int frame = 0;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                frame++;
                animate(frame);
                _host.Update();
                _host.Draw();
            },
            warmupIterations: 50,
            measuredIterations: 300);

        _output.WriteLine($"{kind}: {result.BytesPerIteration:N0} bytes/frame");

        _host.CountPaintedSamples().ShouldBeGreaterThan(0);
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(limitBytesPerFrame);
    }

    [Fact]
    public void UpdateAndDraw_RealisticFormsTree_AllocationBaseline()
    {
        SkiaSceneHost.BuildRealisticFormsScene();

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                _host.Update();
                _host.Draw();
            },
            warmupIterations: 50,
            measuredIterations: 300);

        _output.WriteLine($"Idle Update+Draw [{SkiaSceneHost.RealisticFormsSceneDescription}]: " +
            $"{result.BytesPerIteration:N0} bytes/frame");

        _host.CountPaintedSamples().ShouldBeGreaterThan(0);
        result.BytesPerIteration.ShouldBe(0);
    }
}
