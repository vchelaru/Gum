using System;

namespace SkiaSharp;

/// <summary>
/// Stand-in for SkiaSharp 4's <c>SKPathBuilder</c> covering the members that Runtimes/SkiaGum's
/// Arc and RoundedRectangle use. This plugin is hosted by the Avalonia head, which resolves SkiaSharp 3,
/// where the type doesn't exist. Delete this file once the tool moves to SkiaSharp 4.
/// </summary>
internal sealed class SKPathBuilder : IDisposable
{
    private SKPath _path = new SKPath();

    public void AddArc(SKRect oval, float startAngle, float sweepAngle) => _path.AddArc(oval, startAngle, sweepAngle);

    public void AddRoundRect(SKRect rect, float rx, float ry) => _path.AddRoundRect(rect, rx, ry);

    public void ArcTo(SKRect oval, float startAngle, float sweepAngle, bool forceMoveTo) =>
        _path.ArcTo(oval, startAngle, sweepAngle, forceMoveTo);

    public void Close() => _path.Close();

    /// <summary>Returns the built path and starts a new empty one. The caller owns the returned path.</summary>
    public SKPath Detach()
    {
        SKPath result = _path;
        _path = new SKPath();
        return result;
    }

    public void LineTo(float x, float y) => _path.LineTo(x, y);

    public void MoveTo(float x, float y) => _path.MoveTo(x, y);

    public void Dispose() => _path.Dispose();
}
