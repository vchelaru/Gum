namespace Gum.RenderTarget.Benchmarks;

/// <summary>The measurements for one scenario and config.</summary>
public sealed class ResultRow
{
    public string Scenario { get; init; } = "";
    public BenchConfig Config { get; init; }
    public int MeasuredFrames { get; init; }

    /// <summary>Wall time of the scenario's Build (excluded from the frame numbers). For B and B0 it includes
    /// creating the containers and, for B, compiling the shader; for C0 it includes baking every target.</summary>
    public double SetupMs { get; init; }

    /// <summary>CPU time inside the Gum draw call, per frame.</summary>
    public FrameStats DrawCpuMs { get; init; }

    /// <summary>Start of one Draw to the start of the next, per frame.</summary>
    public FrameStats FrameMs { get; init; }

    /// <summary>Whole window, ending after a one-pixel readback that drains the GPU, divided by the frames.</summary>
    public double DrainedWallMsPerFrame { get; init; }

    // GraphicsDevice.Metrics for one frame.
    public long GlDrawCalls { get; init; }
    public long GlClears { get; init; }
    public long GlTargetSwitches { get; init; }

    // Gum's own counters for the same frame.
    public int GumDrawCallCount { get; init; }
    public int GumSpriteBatchBegins { get; init; }
    public int GumShapeBatchBegins { get; init; }

    public double AllocBytesPerFrame { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen2Collections { get; init; }
}
