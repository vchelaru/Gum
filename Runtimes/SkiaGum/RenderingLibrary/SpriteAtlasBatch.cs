using SkiaSharp;
using System;
using System.Collections.Generic;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Collects consecutive sprites that draw from the same image and draws them with one
/// <see cref="SKCanvas.DrawAtlas(SKImage, SKRect[], SKRotationScaleMatrix[], SKColor[], SKBlendMode, SKSamplingOptions, SKPaint)"/>
/// call, the same cost model as MonoGame's SpriteBatch (#7826). Anything that changes what a later draw sees
/// (another image, canvas, sampling or alpha, a clip change, any other renderable) flushes the pending sprites
/// first, so draw order and output stay as if each sprite drew alone.
/// </summary>
/// <remarks>
/// Each sprite is placed with a device-space RSXform, so sprites under different canvas matrices (rotation,
/// camera) share a batch; the flush draws with an identity matrix. A sprite whose device transform is not a
/// rotation plus uniform scale (a flip, a non-uniform scale) cannot be an RSXform and is drawn on its own by
/// its caller. SkiaSharp's DrawAtlas takes whole arrays only, so the flush copies into a pooled array of the
/// exact batch length; once each length has been seen, a frame allocates nothing.
/// </remarks>
internal sealed class SpriteAtlasBatch
{
    sealed class ExactArrays
    {
        public readonly SKRect[] Sources;
        public readonly SKRotationScaleMatrix[] Transforms;
        public readonly SKColor[] Colors;

        public ExactArrays(int length)
        {
            Sources = new SKRect[length];
            Transforms = new SKRotationScaleMatrix[length];
            Colors = new SKColor[length];
        }
    }

    readonly RenderStateChangeStatistics _statistics;
    readonly SKPaint _paint = new SKPaint { IsAntialias = false };
    readonly Dictionary<int, ExactArrays> _exactArrays = new Dictionary<int, ExactArrays>();

    SKRect[] _sources = new SKRect[16];
    SKRotationScaleMatrix[] _transforms = new SKRotationScaleMatrix[16];
    SKColor[] _colors = new SKColor[16];
    int _count;
    bool _hasTint;

    SKCanvas? _canvas;
    object? _key;
    SKImage? _image;
    SKSamplingOptions _sampling;
    byte _alpha;

    public SpriteAtlasBatch(RenderStateChangeStatistics statistics)
    {
        _statistics = statistics;
    }

    /// <summary>The number of sprites waiting for <see cref="Flush"/>.</summary>
    public int PendingCount => _count;

    /// <summary>
    /// Adds a sprite drawing <paramref name="source"/> of <paramref name="image"/> into <paramref name="destination"/>
    /// under the canvas's current matrix, tinted by <paramref name="tint"/> (Modulate, opaque) and faded by
    /// <paramref name="alpha"/>. Returns false, adding nothing, when the sprite cannot be an RSXform; the caller
    /// then flushes and draws it itself. <paramref name="key"/> decides whether two sprites draw the same pixels:
    /// the sprites' shared bitmap, so sprites that each wrap the same bitmap in their own image still batch.
    /// </summary>
    public bool TryAdd(SKCanvas canvas, object key, SKImage image, SKRect source, SKRect destination,
        SKColor tint, byte alpha, SKSamplingOptions sampling)
    {
        if (source.Width <= 0 || source.Height <= 0)
        {
            return false;
        }

        float scaleX = destination.Width / source.Width;
        float scaleY = destination.Height / source.Height;
        if (scaleX != scaleY)
        {
            return false;
        }

        SKMatrix total = canvas.TotalMatrix;
        // The source rectangle's top left lands on the destination's top left, scaled uniformly.
        SKMatrix device = total.PreConcat(new SKMatrix(scaleX, 0, destination.Left, 0, scaleX, destination.Top, 0, 0, 1));
        if (device.Persp0 != 0 || device.Persp1 != 0 || device.Persp2 != 1 ||
            device.ScaleX != device.ScaleY || device.SkewX != -device.SkewY)
        {
            return false;
        }

        if (_count > 0 && (!ReferenceEquals(canvas, _canvas) || !ReferenceEquals(key, _key) ||
            !sampling.Equals(_sampling) || alpha != _alpha))
        {
            Flush();
        }

        if (_count == 0)
        {
            _canvas = canvas;
            _key = key;
            _image = image;
            _sampling = sampling;
            _alpha = alpha;
            _hasTint = false;
        }

        if (_count == _sources.Length)
        {
            Array.Resize(ref _sources, _count * 2);
            Array.Resize(ref _transforms, _count * 2);
            Array.Resize(ref _colors, _count * 2);
        }

        _sources[_count] = source;
        _transforms[_count] = new SKRotationScaleMatrix(device.ScaleX, device.SkewY, device.TransX, device.TransY);
        _colors[_count] = tint;
        _hasTint |= tint != SKColors.White;
        _count++;
        return true;
    }

    /// <summary>Draws the pending sprites with one DrawAtlas call, counted as one draw call.</summary>
    public void Flush()
    {
        if (_count == 0)
        {
            return;
        }

        SKCanvas canvas = _canvas!;
        int count = _count;
        _count = 0;

        if (!_exactArrays.TryGetValue(count, out ExactArrays? exact))
        {
            exact = new ExactArrays(count);
            _exactArrays[count] = exact;
        }
        Array.Copy(_sources, exact.Sources, count);
        Array.Copy(_transforms, exact.Transforms, count);

        _paint.Color = new SKColor(255, 255, 255, _alpha);

        canvas.Save();
        canvas.ResetMatrix();
        if (_hasTint)
        {
            Array.Copy(_colors, exact.Colors, count);
            canvas.DrawAtlas(_image, exact.Sources, exact.Transforms, exact.Colors, SKBlendMode.Modulate, _sampling, _paint);
        }
        else
        {
            canvas.DrawAtlas(_image, exact.Sources, exact.Transforms, _sampling, _paint);
        }
        canvas.Restore();

        _statistics.RecordDrawCall();
        _canvas = null;
        _key = null;
        _image = null;
    }
}
