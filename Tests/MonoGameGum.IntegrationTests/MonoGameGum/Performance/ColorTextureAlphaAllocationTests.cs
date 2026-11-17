using System;
using System.Linq;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Performance;

/// <summary>
/// ColorTextureAlpha's fog pass swaps the sprite's blend for a cached copy (#7837). Drawing a
/// ColorTextureAlpha sprite and then a Modulate sprite every frame switches between the copy and the
/// original on each frame, and must allocate nothing once warm.
/// </summary>
[Collection(SharedGame.CollectionName)]
[Trait("Quarantine", "MesaAllocation")]
public class ColorTextureAlphaAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public ColorTextureAlphaAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ColorTextureAlphaThenModulateSprite_AllocatesNothingPerFrame()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        using Texture2D texture = new(gd, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.White);
        texture.SetData(pixels);

        ContainerRuntime root = new();
        root.Width = 64;
        root.Height = 32;
        root.AddChild(CreateSprite(texture, x: 0, ColorOperation.ColorTextureAlpha));
        root.AddChild(CreateSprite(texture, x: 32, ColorOperation.Modulate));
        root.AddToManagers(managers, null);
        root.UpdateLayout();

        using RenderTarget2D scene = new(gd, 64, 32, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);
        try
        {
            // Bytes per call and per step, so a Mesa-only reading says which frame and which call allocated.
            // Allocated before the window starts; reading the counter allocates nothing.
            const int Warmup = 30;
            const int Measured = 200;
            long[] targetBytes = new long[Warmup + Measured];
            long[] drawBytes = new long[Warmup + Measured];
            long[] restoreBytes = new long[Warmup + Measured];
            long[] callTimestamps = new long[Warmup + Measured];
            DateTime[] callStarts = new DateTime[Warmup + Measured];
            DateTime[] callEnds = new DateTime[Warmup + Measured];
            // #8190: the runtime's own events (JIT, type loads, exceptions), to say what ran inside an allocating call.
            using RuntimeEventRecorder recorder = new();
            long testThreadId = RuntimeEventRecorder.CurrentOsThreadId();
            long firstTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            int call = 0;
            // Diagnostic: an exception thrown and caught inside a draw allocates its object and trace.
            List<string> exceptions = new List<string>();
            int testThread = Environment.CurrentManagedThreadId;
            EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> onException = (_, args) =>
            {
                if (Environment.CurrentManagedThreadId == testThread)
                {
                    exceptions.Add($"call {call}: {args.Exception.GetType().FullName}: {args.Exception.Message} at {args.Exception.StackTrace}");
                }
            };
            AppDomain.CurrentDomain.FirstChanceException += onException;
            int gcsBefore = GC.CollectionCount(0);
            AllocationResult result = AllocationMeasurer.Measure(
                () =>
                {
                    DateTime callStart = DateTime.UtcNow;
                    long start = GC.GetAllocatedBytesForCurrentThread();
                    gd.SetRenderTarget(scene);
                    gd.Clear(Color.Black);
                    long drawStart = GC.GetAllocatedBytesForCurrentThread();
                    managers.Renderer.Draw(managers);
                    long restoreStart = GC.GetAllocatedBytesForCurrentThread();
                    gd.SetRenderTarget(null);
                    long end = GC.GetAllocatedBytesForCurrentThread();
                    if (call < targetBytes.Length)
                    {
                        targetBytes[call] = drawStart - start;
                        drawBytes[call] = restoreStart - drawStart;
                        restoreBytes[call] = end - restoreStart;
                        callTimestamps[call] = System.Diagnostics.Stopwatch.GetTimestamp();
                        callStarts[call] = callStart;
                        callEnds[call] = DateTime.UtcNow;
                    }
                    call++;
                },
                warmupIterations: Warmup,
                measuredIterations: Measured);
            int gcs = GC.CollectionCount(0) - gcsBefore;
            AppDomain.CurrentDomain.FirstChanceException -= onException;
            string detail = $"first-chance exceptions: [{string.Join(" | ", exceptions)}]; gen0 GCs: {gcs};calls that allocated (0..{Warmup - 1} are warm-up; {(System.Diagnostics.Stopwatch.GetTimestamp() - firstTimestamp) * 1000 / System.Diagnostics.Stopwatch.Frequency} ms in all): " + string.Join(", ",
                Enumerable.Range(0, Warmup + Measured).Where(i => targetBytes[i] != 0 || drawBytes[i] != 0 || restoreBytes[i] != 0)
                    .Select(i => $"#{i} at {(callTimestamps[i] - firstTimestamp) * 1000 / System.Diagnostics.Stopwatch.Frequency} ms target={targetBytes[i]} draw={drawBytes[i]} restore={restoreBytes[i]}"));

            int[] allocatingCalls = Enumerable.Range(Warmup, Measured)
                .Where(i => targetBytes[i] != 0 || drawBytes[i] != 0 || restoreBytes[i] != 0).ToArray();
            if (allocatingCalls.Length > 0)
            {
                detail += $"; runtime events from call {allocatingCalls[0]} to {allocatingCalls[^1]}: " + recorder.Describe(
                    callStarts[allocatingCalls[0]].AddMilliseconds(-2), callEnds[allocatingCalls[^1]].AddMilliseconds(2), testThreadId);
            }

            _output.WriteLine($"ColorTextureAlpha + Modulate sprite: {result.BytesPerIteration:N0} bytes/frame");
            Console.WriteLine($"[#8181 diagnostic] ColorTextureAlpha: {result.BytesPerIteration:N0} bytes/frame; {detail}");

            // Liveness: the fog pass ran with the swapped copy (color source One), and the Modulate sprite
            // with the original blend (color source SourceAlpha), so both Begins were measured.
            BeginParameters[] states = managers.Renderer.SpriteRenderer.LastFrameDrawStates.ToArray();
            states.Any(state => state.BlendState?.ColorSourceBlend == global::Gum.Blend.One).ShouldBeTrue();
            states.Any(state => state.BlendState?.ColorSourceBlend == global::Gum.Blend.SourceAlpha).ShouldBeTrue();

            result.BytesPerIteration.ShouldBeLessThanOrEqualTo(0, $"measured {result.BytesPerIteration:N0} bytes/frame; {detail}");
        }
        finally
        {
            root.RemoveFromManagers();
        }
    }

    private static SpriteRuntime CreateSprite(Texture2D texture, float x, ColorOperation colorOperation)
    {
        SpriteRuntime sprite = new();
        sprite.Texture = texture;
        sprite.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.Width = 16;
        sprite.Height = 16;
        sprite.X = x;
        sprite.Color = new Color(255, 128, 0);
        sprite.Alpha = 128;
        sprite.ColorOperation = colorOperation;
        return sprite;
    }

    private sealed class MinimalGame : SharedGameLease
    {
        public global::Gum.GumService GumService { get; } = new global::Gum.GumService();

        protected override void OnInitialize() => GumService.Initialize(Game, global::Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
        }
    }
}
