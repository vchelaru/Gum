namespace SkiaGum.Renderables;

/// <summary>Spike only (perf-per-draw-cost): switches that skip one piece of a draw so its cost can be measured.</summary>
public static class PerfAblation
{
    public static bool TextSkipBlockCheck;
    public static bool TextSkipRotation;
    public static bool TextSkipSnap;
    public static bool TextSkipMatrixChain;
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

    public static void Reset()
    {
        TextSkipBlockCheck = TextSkipRotation = TextSkipSnap = TextSkipMatrixChain = TextSkipRenderPaint = TextSkipClipper = TextDirectBlob = TextSkipStats = TextBypassPicture = false;
        ShapeDirectDrawBound = SkipWhiteTintFilter = false;
        NsSkipRenderOffset = NsSkipSnap = NsSkipBorderCompute = NsSkipLatticeChecks = false;
        SpriteSkipRenderOffset = SpriteSkipSampledSource = Alt = false;
    }
}
