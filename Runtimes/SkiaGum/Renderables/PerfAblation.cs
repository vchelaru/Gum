namespace SkiaGum.Renderables;

/// <summary>Spike only (perf-per-draw-cost): switches that skip one piece of a draw so its cost can be measured.</summary>
public static class PerfAblation
{
    public static bool TextSkipBlockCheck;
    public static bool TextSkipRotation;
    public static bool TextSkipSnap;
    public static bool TextSkipMatrixChain;
    public static readonly bool TextOldTransformPath = System.Environment.GetEnvironmentVariable("PERDRAW_OLDTEXT") == "1";
    public static bool TextSkipRenderPaint;
    public static bool TextSkipClipper;
    public static bool TextDirectBlob;
    public static bool TextSkipStats;
    public static bool TextBypassPicture;

    public static bool ShapeDirectDrawBound;
    public static bool SkipWhiteTintFilter;
    public static readonly bool KeepWhiteTintFilter = System.Environment.GetEnvironmentVariable("PERDRAW_KEEPWHITE") == "1";

    public static bool NsSkipRenderOffset;
    public static bool NsSkipSnap;
    public static bool NsSkipBorderCompute;
    public static bool NsSkipLatticeChecks;

    public static bool SpriteSkipRenderOffset;
    public static bool SpriteSkipSampledSource;
    public static bool Alt;

    // Frame stage probe: Start() then Mark(slot) adds the ticks since the previous mark to that slot.
    public static readonly bool NestedPreRender = System.Environment.GetEnvironmentVariable("PERDRAW_NESTEDPRERENDER") == "1";
    public static bool StagesOn;
    public static readonly long[] StageTicks = new long[16];
    public static long PreRenderCalls;
    public static long RenderCalls;
    static long _last;
    public static int Frame;
    public static int ResetAtFrame = -1;
    public static long BakedCount;
    public static long CollectVisits;
    public static readonly bool WalkTwice = System.Environment.GetEnvironmentVariable("PERDRAW_TWICE") == "1";
    public static void Start()
    {
        if (!StagesOn) return;
        if (Frame++ == ResetAtFrame) { System.Array.Clear(StageTicks); PreRenderCalls = 0; RenderCalls = 0; BakedCount = 0; CollectVisits = 0; }
        _last = System.Diagnostics.Stopwatch.GetTimestamp();
    }
    public static void Mark(int slot)
    {
        if (!StagesOn) return;
        long now = System.Diagnostics.Stopwatch.GetTimestamp();
        StageTicks[slot] += now - _last;
        _last = now;
    }

    public static void Reset()
    {
        TextSkipBlockCheck = TextSkipRotation = TextSkipSnap = TextSkipMatrixChain = TextSkipRenderPaint = TextSkipClipper = TextDirectBlob = TextSkipStats = TextBypassPicture = false;
        ShapeDirectDrawBound = SkipWhiteTintFilter = false;
        NsSkipRenderOffset = NsSkipSnap = NsSkipBorderCompute = NsSkipLatticeChecks = false;
        SpriteSkipRenderOffset = SpriteSkipSampledSource = Alt = false;
    }
}
