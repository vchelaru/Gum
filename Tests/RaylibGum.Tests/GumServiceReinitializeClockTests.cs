using Gum.Forms;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests;

/// <summary>
/// The clock <c>Update</c> keeps does not survive <c>Uninitialize</c> (#6325): the first frame after
/// initializing the same instance again is a fresh start, and <see cref="IGumService.GameTime"/> is null
/// until then. Like the other lifecycle tests it tears down the assembly-wide service and puts it back.
/// </summary>
public class GumServiceReinitializeClockTests
{
    [Fact]
    public void Update_AfterUninitializeAndInitialize_TreatsTheFirstFrameAsAFreshStart()
    {
        Gum.GumService service = Gum.GumService.Default;
        service.Uninitialize();

        try
        {
            service.Initialize(DefaultVisualsVersion.V3);
            ((IGumService)service).Update(100.0);
            service.Uninitialize();
            service.Initialize(DefaultVisualsVersion.V3);
            ElapsedRecorder recorder = new ElapsedRecorder();
            service.Root.Children.Add(new InteractiveGue { FormsControlAsObject = recorder });

            ((IGumService)service).Update(1.0);

            // Raylib has no previous frame on a fresh start, so the first Update advances by the whole clock
            // (1 second here), not by minus the 100 seconds of the previous run.
            recorder.Seconds.ShouldBe(new[] { 1.0 });
        }
        finally
        {
            service.Uninitialize();
            TestAssemblyInitialize.ApplyDefaultTestState();
        }
    }

    [Fact]
    public void GameTime_AfterUninitialize_IsNull()
    {
        Gum.GumService service = Gum.GumService.Default;
        service.Uninitialize();

        try
        {
            service.Initialize(DefaultVisualsVersion.V3);
            ((IGumService)service).Update(5.0);
            ((IGumService)service).GameTime.ShouldBe(5f);

            service.Uninitialize();

            ((IGumService)service).GameTime.ShouldBeNull();
        }
        finally
        {
            service.Uninitialize();
            TestAssemblyInitialize.ApplyDefaultTestState();
        }
    }

    private class ElapsedRecorder : IUpdateEveryFrame
    {
        public List<double> Seconds { get; } = new List<double>();

        public void Activity(double secondDifference) => Seconds.Add(secondDifference);
    }
}
