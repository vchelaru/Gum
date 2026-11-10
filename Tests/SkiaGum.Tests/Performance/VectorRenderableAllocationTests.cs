using System.Text;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using SkiaSharp.Skottie;
using Svg.Skia;
using Xunit.Abstractions;

namespace SkiaGum.Tests.Performance;

/// <summary>
/// Per-frame managed allocation of one idle Update + Draw of the Skia vector renderables (issue #1934):
/// Lottie, SVG and the line shapes that <see cref="DrawAllocationTests"/> does not cover. Zero is the
/// target; a nonzero limit is a ratchet that comes down as the source is removed.
/// </summary>
public class VectorRenderableAllocationTests : BaseTestClass
{
    private const string SvgText =
        "<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'>" +
        "<rect width='16' height='16' fill='#ff0000'/></svg>";

    // A 100x100 red solid layer that lasts one second.
    private const string LottieText =
        "{\"v\":\"5.5.2\",\"fr\":30,\"ip\":0,\"op\":30,\"w\":100,\"h\":100,\"nm\":\"t\",\"ddd\":0,\"assets\":[]," +
        "\"layers\":[{\"ddd\":0,\"ind\":1,\"ty\":1,\"nm\":\"s\",\"sc\":\"#ff0000\",\"sw\":100,\"sh\":100," +
        "\"ip\":0,\"op\":30,\"st\":0,\"ks\":{}}]}";

    private readonly ITestOutputHelper _output;
    private readonly SkiaSceneHost _host = new SkiaSceneHost();
    private readonly List<IDisposable> _disposables = new List<IDisposable>();

    public VectorRenderableAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public override void Dispose()
    {
        _host.Dispose();
        foreach (IDisposable disposable in _disposables)
        {
            disposable.Dispose();
        }
        base.Dispose();
    }

    private SKSvg LoadSvg()
    {
        SKSvg svg = new SKSvg();
        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(SvgText));
        svg.Load(stream);
        _disposables.Add(svg);
        return svg;
    }

    private GraphicalUiElement Create(string kind)
    {
        switch (kind)
        {
            case "Svg":
                return new SvgRuntime { Texture = LoadSvg(), Width = 100, Height = 100, WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute };
            case "SvgTinted":
                return new SvgRuntime { Texture = LoadSvg(), Color = SKColors.Gray, Width = 100, Height = 100, WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute };
            case "VectorSprite":
                VectorSprite sprite = new VectorSprite { Texture = LoadSvg() };
                return new GraphicalUiElement(sprite) { Width = 100, Height = 100, WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute };
            case "Lottie":
                Animation animation = Animation.Parse(LottieText)
                    ?? throw new InvalidOperationException("The test Lottie JSON did not parse.");
                _disposables.Add(animation);
                return new LottieAnimationRuntime { Animation = animation, Loops = true, Width = 100, Height = 100, WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute };
            case "LineGrid":
                return new LineGridRuntime { Width = 100, Height = 100, CellWidth = 10, CellHeight = 10, Color = SKColors.Red };
            case "LineRectangle":
                LineRectangle lineRectangle = new LineRectangle { Color = SKColors.Red, IsFilled = false, StrokeWidth = 2 };
                return new GraphicalUiElement(lineRectangle) { Width = 100, Height = 100, WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute };
            default:
                throw new ArgumentException("Unknown kind " + kind);
        }
    }

    [Theory]
    [InlineData("Svg", 0)]
    [InlineData("SvgTinted", 0)]
    [InlineData("VectorSprite", 0)]
    [InlineData("Lottie", 0)]
    [InlineData("LineGrid", 0)]
    [InlineData("LineRectangle", 0)]
    public void UpdateAndDraw_UnchangedVectorRenderable_AllocationBaseline(string kind, int limitBytesPerFrame)
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
}
