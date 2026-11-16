using SkiaSharp;
using System;
using System.Collections.Generic;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Collects NineSlices that would each draw one lattice from the same image and draws them with one
/// <see cref="SKCanvas.DrawVertices(SKVertices, SKBlendMode, SKPaint)"/> call (#8153). Ganesh charges about the same
/// for every image op whatever it draws, so fewer ops is what saves time.
/// </summary>
/// <remarks>
/// <para>Each NineSlice adds its nine cells as a 4x4 grid of device-space vertices with texel coordinates, so the
/// flush draws with an identity matrix. Only a GPU canvas batches: on a CPU raster canvas DrawVertices blends
/// semi-transparent texels one step differently from the lattice, so raster, SVG and picture canvases keep the
/// lattice.</para>
/// <para>Text drawn while a batch is pending is deferred: it draws right after the batch, in order, instead of
/// flushing it. That moves the text above the NineSlices that join the batch after it, so a NineSlice joins only
/// when it overlaps no deferred text's drawn bounds; otherwise the batch flushes first. A text that cannot give
/// conservative drawn bounds is not deferred and flushes the batch. Anything else that draws flushes the batch
/// first (the Renderer does it at every other renderable, clip change, render target and the end of each draw
/// entry point), so the clip and the canvas a deferred text sees are the ones it was deferred under.</para>
/// <para>SkiaSharp's SKVertices takes whole arrays only, so the flush copies into a pooled array set of the
/// exact batch length, as <see cref="SpriteAtlasBatch"/> does; the SKVertices wrapper itself is one small
/// allocation per flush.</para>
/// </remarks>
internal sealed class NineSliceVertexBatch
{
    private const int VerticesPerNineSlice = 16;
    private const int IndicesPerNineSlice = 54;

    private sealed class ExactArrays
    {
        public readonly SKPoint[] Positions;
        public readonly SKPoint[] TexCoords;
        public readonly ushort[] Indices;

        public ExactArrays(int nineSliceCount)
        {
            Positions = new SKPoint[nineSliceCount * VerticesPerNineSlice];
            TexCoords = new SKPoint[nineSliceCount * VerticesPerNineSlice];
            Indices = new ushort[nineSliceCount * IndicesPerNineSlice];
            int index = 0;
            for (int n = 0; n < nineSliceCount; n++)
            {
                int first = n * VerticesPerNineSlice;
                for (int row = 0; row < 3; row++)
                {
                    for (int column = 0; column < 3; column++)
                    {
                        ushort topLeft = (ushort)(first + (row * 4) + column);
                        Indices[index++] = topLeft;
                        Indices[index++] = (ushort)(topLeft + 1);
                        Indices[index++] = (ushort)(topLeft + 4);
                        Indices[index++] = (ushort)(topLeft + 1);
                        Indices[index++] = (ushort)(topLeft + 5);
                        Indices[index++] = (ushort)(topLeft + 4);
                    }
                }
            }
        }
    }

    private struct DeferredText
    {
        public global::SkiaGum.Text Text;
        public float X;
        public float Y;
        public SKMatrix Matrix;
        public SKRect DeviceBounds;
    }

    private readonly RenderStateChangeStatistics _statistics;
    private readonly SKPaint _paint;
    private readonly Dictionary<int, ExactArrays> _exactArrays;

    private SKPoint[] _positions;
    private SKPoint[] _texCoords;
    private int _count;

    private DeferredText[] _deferred;
    private int _deferredCount;

    private SKCanvas? _canvas;
    private object? _key;
    private SKShader? _shader;

    private SKCanvas? _lastCheckedCanvas;
    private bool _isLastCheckedCanvasGpu;

    public NineSliceVertexBatch(RenderStateChangeStatistics statistics)
    {
        _statistics = statistics;
        _paint = new SKPaint { IsAntialias = false, Color = SKColors.White };
        _exactArrays = new Dictionary<int, ExactArrays>();
        _positions = new SKPoint[16 * VerticesPerNineSlice];
        _texCoords = new SKPoint[16 * VerticesPerNineSlice];
        _deferred = new DeferredText[16];
    }

    /// <summary>Whether NineSlices (and maybe deferred texts) are waiting for <see cref="Flush"/>.</summary>
    public bool HasPending => _count > 0;

    /// <summary>The NineSlices waiting for <see cref="Flush"/>.</summary>
    public int PendingCount => _count;

    /// <summary>
    /// The most NineSlices one DrawVertices call draws (256). A longer run splits into several calls in order, so the
    /// exact-length pool holds at most this many lengths. Settable for tests.
    /// </summary>
    internal int MaxBatchLength { get; set; } = 256;

    /// <summary>
    /// Adds a NineSlice whose four device-space column edges are <paramref name="xs"/> and row edges
    /// <paramref name="ys"/>, sampling the texel edges <paramref name="us"/> and <paramref name="vs"/> of the image
    /// <paramref name="shader"/> draws. Flushes first when the pending batch has another canvas or key, is full, or
    /// has a deferred text the NineSlice overlaps. Call it only for a canvas <see cref="IsGpuCanvas"/> accepts.
    /// <paramref name="key"/> decides whether two NineSlices draw the same pixels (their shared bitmap).
    /// </summary>
    public void Add(SKCanvas canvas, object key, SKShader shader,
        ReadOnlySpan<float> xs, ReadOnlySpan<float> ys, ReadOnlySpan<float> us, ReadOnlySpan<float> vs)
    {
        if (_count > 0)
        {
            SKRect bounds = new SKRect(
                MathF.Min(xs[0], xs[3]), MathF.Min(ys[0], ys[3]), MathF.Max(xs[0], xs[3]), MathF.Max(ys[0], ys[3]));
            if (!ReferenceEquals(canvas, _canvas) || !ReferenceEquals(key, _key) || _count >= MaxBatchLength
                || OverlapsDeferred(bounds))
            {
                Flush();
            }
        }

        if (_count == 0)
        {
            _canvas = canvas;
            _key = key;
            _shader = shader;
        }

        int first = _count * VerticesPerNineSlice;
        if (first + VerticesPerNineSlice > _positions.Length)
        {
            Array.Resize(ref _positions, _positions.Length * 2);
            Array.Resize(ref _texCoords, _texCoords.Length * 2);
        }
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                int vertex = first + (row * 4) + column;
                _positions[vertex] = new SKPoint(xs[column], ys[row]);
                _texCoords[vertex] = new SKPoint(us[column], vs[row]);
            }
        }
        _count++;
    }

    /// <summary>
    /// Defers <paramref name="text"/>'s cached blobs, drawn at (<paramref name="x"/>, <paramref name="y"/>) under
    /// <paramref name="matrix"/>, until right after the pending batch. <paramref name="deviceBounds"/> must hold every
    /// pixel the text can touch. Returns false, deferring nothing, when no batch is pending on
    /// <paramref name="canvas"/>.
    /// </summary>
    public bool TryDefer(SKCanvas canvas, global::SkiaGum.Text text, float x, float y, SKMatrix matrix, SKRect deviceBounds)
    {
        if (_count == 0 || !ReferenceEquals(canvas, _canvas))
        {
            return false;
        }
        if (_deferredCount == _deferred.Length)
        {
            Array.Resize(ref _deferred, _deferredCount * 2);
        }
        _deferred[_deferredCount++] = new DeferredText
        {
            Text = text,
            X = x,
            Y = y,
            Matrix = matrix,
            DeviceBounds = deviceBounds,
        };
        return true;
    }

    /// <summary>
    /// Draws the pending NineSlices with one DrawVertices call, counted as one draw call, then the deferred texts in
    /// the order they were deferred.
    /// </summary>
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
        int vertexCount = count * VerticesPerNineSlice;
        Array.Copy(_positions, exact.Positions, vertexCount);
        Array.Copy(_texCoords, exact.TexCoords, vertexCount);

        _paint.Shader = _shader;
        canvas.Save();
        canvas.ResetMatrix();
        using (SKVertices vertices = SKVertices.CreateCopy(SKVertexMode.Triangles, exact.Positions, exact.TexCoords, null, exact.Indices))
        {
            canvas.DrawVertices(vertices, SKBlendMode.Modulate, _paint);
        }
        canvas.Restore();
        _paint.Shader = null;
        _statistics.RecordDrawCall();

        int deferredCount = _deferredCount;
        _deferredCount = 0;
        if (deferredCount > 0)
        {
            SKMatrix current = canvas.TotalMatrix;
            for (int i = 0; i < deferredCount; i++)
            {
                ref DeferredText deferred = ref _deferred[i];
                // A NineSlice that cannot batch flushes under its own flip or rotation, so the text's own matrix is
                // put back for it.
                bool isOtherMatrix = deferred.Matrix != current;
                if (isOtherMatrix)
                {
                    canvas.Save();
                    canvas.SetMatrix(deferred.Matrix);
                }
                deferred.Text.DrawDeferredBlobs(canvas, deferred.X, deferred.Y);
                if (isOtherMatrix)
                {
                    canvas.Restore();
                }
                deferred.Text = null!;
            }
        }

        _canvas = null;
        _key = null;
        _shader = null;
    }

    private bool OverlapsDeferred(SKRect bounds)
    {
        for (int i = 0; i < _deferredCount; i++)
        {
            SKRect deferred = _deferred[i].DeviceBounds;
            if (deferred.Left < bounds.Right && bounds.Left < deferred.Right
                && deferred.Top < bounds.Bottom && bounds.Top < deferred.Bottom)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Whether NineSlices drawn on <paramref name="canvas"/> batch: only on a GPU canvas, where DrawVertices and the
    /// lattice draw the same bytes.
    /// </summary>
    public bool IsGpuCanvas(SKCanvas canvas)
    {
        if (!ReferenceEquals(canvas, _lastCheckedCanvas))
        {
            _lastCheckedCanvas = canvas;
            _isLastCheckedCanvasGpu = canvas.Context != null;
        }
        return _isLastCheckedCanvasGpu;
    }
}
