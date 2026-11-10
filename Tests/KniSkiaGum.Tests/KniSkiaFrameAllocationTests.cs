using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using MonoGameGum.TestsCommon;
using Shouldly;
using SkiaSharp;
using Xunit.Abstractions;
using XnaGameTime = Microsoft.Xna.Framework.GameTime;
using XnaTouchCollection = Microsoft.Xna.Framework.Input.Touch.TouchCollection;
using XnaTouchLocation = Microsoft.Xna.Framework.Input.Touch.TouchLocation;

namespace KniSkiaGum.Tests;

/// <summary>
/// Per-frame managed allocations of the KNI Skia host's own frame path (issue #7819): <c>GumService.Update</c>
/// and <c>GumService.Draw()</c> with the service owning its render target. The target is a CPU fake through
/// <c>TargetFactoryOverride</c>, because a real SkiaGameRendering target needs a KNI platform package and a GPU
/// device that this test project does not have; SkiaGameRendering's own Begin/End are therefore not measured.
/// The renderables' draw cost is guarded in SkiaGum.Tests' DrawAllocationTests.
/// </summary>
public class KniSkiaFrameAllocationTests : BaseTestClass
{
    private sealed class RasterTarget : IKniSkiaTarget
    {
        private readonly SKBitmap _bitmap;

        public RasterTarget(int width, int height)
        {
            _bitmap = new SKBitmap(width, height);
            Canvas = new SKCanvas(_bitmap);
        }

        public SKCanvas Canvas { get; }
        public int PresentCount { get; private set; }

        public SKColor PixelAt(int x, int y) => _bitmap.GetPixel(x, y);

        public void Begin() => Canvas.Clear(SKColors.Transparent);

        public void End() => PresentCount++;

        public void EndWithoutDrawing()
        {
        }

        public void Dispose()
        {
            Canvas.Dispose();
            _bitmap.Dispose();
        }
    }

    private static readonly XnaTouchCollection NoFingers = new XnaTouchCollection(Array.Empty<XnaTouchLocation>());

    private readonly ITestOutputHelper _output;
    private readonly List<RasterTarget> _created = new();

    public KniSkiaFrameAllocationTests(ITestOutputHelper output)
    {
        _output = output;
        GumService.Default.Uninitialize();
        GumService.Default.InitializeWithoutWindow(640, 480);
        GumService.Default.Cursor.ReadMouse = () => default;
        GumService.Default.Keyboard.ReadKeyboard = () => default;
        // A platform touch panel with no fingers on it. Without a platform, KNI's TouchPanel read throws
        // every frame, which is a test artifact rather than host cost.
        GumService.Default.Cursor.ReadTouches = () => NoFingers;
        GumService.Default.WindowSizeOverride = () => (640, 480);
        GumService.Default.TargetFactoryOverride = (w, h) =>
        {
            RasterTarget target = new RasterTarget(w, h);
            _created.Add(target);
            return target;
        };
        GumService.Default.IsGameActiveOverride = () => true;
    }

    public override void Dispose()
    {
        GumService.Default.Root.Children.Clear();
        GumService.Default.IsGameActiveOverride = null;
        GumService.Default.WindowSizeOverride = null;
        GumService.Default.TargetFactoryOverride = null;
        GumService.Default.Uninitialize();

        SKSurface surface = SKSurface.Create(new SKImageInfo(
            SkiaHostTests.SkiaHostTestFramework.CanvasWidth, SkiaHostTests.SkiaHostTestFramework.CanvasHeight));
        GumService.Default.InitializeWithoutWindow(
            surface.Canvas, SkiaHostTests.SkiaHostTestFramework.CanvasWidth, SkiaHostTests.SkiaHostTestFramework.CanvasHeight);
        GumService.Default.Cursor.ReadMouse = () => default;
        GumService.Default.Keyboard.ReadKeyboard = () => default;
        SkiaHostTests.SkiaHostTestBase.CaptureRenderableBaseline();
        Gum.Forms.Controls.FrameworkElement.KeyboardsForUiControl.Clear();

        base.Dispose();
    }

    private static void AddScene()
    {
        new RectangleRuntime { Width = 50, Height = 50, IsFilled = true, FillColor = SKColors.Red }.AddToRoot();
        new TextRuntime { Text = "Hello world", Y = 60 }.AddToRoot();

        SKBitmap bitmap = new SKBitmap(16, 16);
        bitmap.Erase(SKColors.Green);
        new SpriteRuntime
        {
            Texture = bitmap,
            X = 100,
            Width = 32,
            Height = 32,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute
        }.AddToRoot();
    }

    // Ratchets: zero is the target. What remains, sampled over 40,000 frames with GCAllocationTick, is
    // the FULL_DIAGNOSTICS duplicate-gamepad HashSet in FormsUtilities.UpdateGamepads, the boxed key
    // enumerator of PolledKeyboard.SupportedKeys (an IEnumerable), and one closure not yet traced.
    // The seconds overload also builds a GameTime each frame (40 bytes): KNI keeps its setters internal.
    [Fact]
    public void UpdateWithGameTimeAndDraw_UnchangedScreen_AllocationBaseline()
    {
        AddScene();
        // A KNI Game advances one GameTime in place, but its setters are internal, so the frames' times
        // are built before measuring: 3 attempts of 50 warmup plus 300 measured frames.
        TimeSpan frame = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
        XnaGameTime[] gameTimes = new XnaGameTime[3 * (50 + 300)];
        for (int i = 0; i < gameTimes.Length; i++)
        {
            gameTimes[i] = new XnaGameTime(frame * (i + 1), frame);
        }
        int frameIndex = 0;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                GumService.Default.Update(gameTimes[frameIndex++]);
                GumService.Default.Draw();
            },
            warmupIterations: 50,
            measuredIterations: 300);

        _output.WriteLine($"Update(GameTime) + Draw: {result.BytesPerIteration:N0} bytes/frame");

        // Liveness: the host's own target was created, presented, and holds the red square.
        _created.Count.ShouldBe(1);
        _created[0].PresentCount.ShouldBeGreaterThan(300);
        _created[0].PixelAt(25, 25).Red.ShouldBe((byte)255);
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(128);
    }

    [Fact]
    public void UpdateWithSecondsAndDraw_UnchangedScreen_AllocationBaseline()
    {
        AddScene();
        double totalSeconds = 0;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                totalSeconds += 1.0 / 60.0;
                GumService.Default.Update(totalSeconds);
                GumService.Default.Draw();
            },
            warmupIterations: 50,
            measuredIterations: 300);

        _output.WriteLine($"Update(double) + Draw: {result.BytesPerIteration:N0} bytes/frame");

        _created.Count.ShouldBe(1);
        _created[0].PixelAt(25, 25).Red.ShouldBe((byte)255);
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(168);
    }
}
