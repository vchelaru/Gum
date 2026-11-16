using SkiaSharp;
using System;
using System.Collections.Generic;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Collects consecutive plain texts (one fill color, no outline, shadow, blend, rotation or own clip) drawn under the
/// same matrix and draws them as one multi-run <see cref="SKTextBlob"/> (#8156). Ganesh charges about the same for
/// every text-blob op whatever it draws, so fewer ops is what saves time.
/// </summary>
/// <remarks>
/// <para>Only a GPU canvas batches. Anything that draws flushes the batch first (the Renderer does it at every other
/// renderable, clip change, render target, bake and the end of each draw entry point, and a text that does not join
/// flushes it itself), so a pending text never moves past another draw. A NineSlice batch flush draws its deferred
/// texts through this batch and flushes it before returning.</para>
/// <para>The merged blob is shaped-text caching, not render caching: each flush of a frame has a slot (the first flush
/// the first slot, and so on) that keeps its merged blob with the per-text blobs and positions, relative to the first
/// text, it was built from. A flush whose texts match draws the kept blob, so a static or scrolling run allocates
/// nothing; a changed text rebuilds only the blob of the run it is in. A replaced blob is disposed.</para>
/// </remarks>
internal sealed class TextBlobBatch
{
    private const int MaxSlots = 64;

    /// <summary>One shaped run of a text: its own blob (the identity the merged blob is checked against), the glyphs
    /// and positions it was built from, and where it draws.</summary>
    public struct Item
    {
        public SKTextBlob Blob;
        public ushort[] Glyphs;
        public SKPoint[] Positions;
        public SKTypeface Typeface;
        public float FontSize;
        public float X;
        public float Y;
    }

    // One flush's kept merged blob and the items, relative to the first item, it was built from.
    private sealed class Slot
    {
        public Item[] Items = Array.Empty<Item>();
        public int Count;
        public SKTextBlob? Merged;
    }

    private readonly RenderStateChangeStatistics _statistics;
    private readonly SKPaint _paint = new();
    private readonly SKTextBlobBuilder _builder = new();
    private readonly SKFont _font = new();
    private readonly List<Slot> _slots = new();
    private int _nextSlot;

    private Item[] _items = new Item[16];
    private int _count;
    private SKCanvas? _canvas;
    private SKMatrix _matrix;
    private SKColor _color;
    private SKFontHinting _hinting;
    private SKFontEdging _edging;
    private bool _subpixel;
    private SKPoint[] _positions = new SKPoint[64];

    private SKCanvas? _lastCheckedCanvas;
    private bool _isLastCheckedCanvasGpu;

    public TextBlobBatch(RenderStateChangeStatistics statistics)
    {
        _statistics = statistics;
    }

    /// <summary>False draws every text on its own. Test hook, through Renderer.BatchesTexts.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The most runs one merged blob holds (16). A longer run of texts splits into several draws in order, so Skia can
    /// still reject a part that is outside the clip. Settable for tests.
    /// </summary>
    internal int MaxBatchLength { get; set; } = 16;

    /// <summary>Whether texts are waiting for <see cref="Flush"/>.</summary>
    public bool HasPending => _count > 0;

    /// <summary>The merged blobs built since this batch was made. Test hook.</summary>
    internal int MergedBuildCount { get; private set; }

    /// <summary>Whether texts drawn on <paramref name="canvas"/> batch: only on a GPU canvas.</summary>
    public bool IsGpuCanvas(SKCanvas canvas)
    {
        if (!ReferenceEquals(canvas, _lastCheckedCanvas))
        {
            _lastCheckedCanvas = canvas;
            _isLastCheckedCanvasGpu = canvas.Context != null;
        }
        return _isLastCheckedCanvasGpu;
    }

    /// <summary>
    /// Starts one text of <paramref name="runCount"/> runs. Flushes first when the pending runs have another canvas,
    /// matrix, color or font settings, or the text would not fit, so the text's <see cref="Add"/> calls go into one
    /// draw with the runs before it.
    /// </summary>
    public void Begin(SKCanvas canvas, SKMatrix matrix, SKColor color, SKFontHinting hinting, SKFontEdging edging,
        bool subpixel, int runCount)
    {
        if (_count > 0 && (!ReferenceEquals(canvas, _canvas) || matrix != _matrix || color != _color
            || hinting != _hinting || edging != _edging || subpixel != _subpixel || _count + runCount > MaxBatchLength))
        {
            Flush();
        }
        if (_count == 0)
        {
            _canvas = canvas;
            _matrix = matrix;
            _color = color;
            _hinting = hinting;
            _edging = edging;
            _subpixel = subpixel;
        }
    }

    /// <summary>Adds one run of the text <see cref="Begin"/> started.</summary>
    public void Add(in Item item)
    {
        if (_count == _items.Length)
        {
            Array.Resize(ref _items, _count * 2);
        }
        _items[_count++] = item;
    }

    /// <summary>Draws the pending runs with one text-blob draw, counted as one draw call.</summary>
    public void Flush()
    {
        if (_count == 0)
        {
            return;
        }

        SKCanvas canvas = _canvas!;
        int count = _count;
        _count = 0;
        _canvas = null;

        // A NineSlice batch flush hands over its deferred texts under the matrix they were deferred under.
        bool isOtherMatrix = canvas.TotalMatrix != _matrix;
        if (isOtherMatrix)
        {
            canvas.Save();
            canvas.SetMatrix(_matrix);
        }
        _paint.Color = _color;
        if (count == 1)
        {
            ref Item only = ref _items[0];
            canvas.DrawText(only.Blob, only.X, only.Y, _paint);
        }
        else
        {
            SKTextBlob? merged = GetSlotBlob(count);
            if (merged != null)
            {
                canvas.DrawText(merged, _items[0].X, _items[0].Y, _paint);
            }
        }
        if (isOtherMatrix)
        {
            canvas.Restore();
        }
        _statistics.RecordDrawCall();

        // The pending items reference blobs the texts own; drop them so a removed text is not kept alive.
        Array.Clear(_items, 0, count);
    }

    /// <summary>
    /// Starts a host frame: the next flush uses the first slot again. Called once per host frame by the Renderer.
    /// </summary>
    public void BeginFrame() => _nextSlot = 0;

    // The pending runs as this flush's slot's merged blob, rebuilt only when they differ from what the slot drew last.
    private SKTextBlob? GetSlotBlob(int count)
    {
        // A host that never starts a frame (no Update) reuses the slots in a ring instead of growing them.
        if (_nextSlot == MaxSlots)
        {
            _nextSlot = 0;
        }
        if (_nextSlot == _slots.Count)
        {
            _slots.Add(new Slot());
        }
        Slot slot = _slots[_nextSlot++];

        float x0 = _items[0].X;
        float y0 = _items[0].Y;
        bool isHit = slot.Merged != null && slot.Count == count;
        for (int i = 0; isHit && i < count; i++)
        {
            ref Item item = ref _items[i];
            ref Item kept = ref slot.Items[i];
            isHit = ReferenceEquals(item.Blob, kept.Blob) && item.X - x0 == kept.X && item.Y - y0 == kept.Y;
        }
        if (isHit)
        {
            return slot.Merged;
        }

        if (slot.Items.Length < count)
        {
            slot.Items = new Item[System.Math.Max(count, MaxBatchLength)];
        }
        _font.Hinting = _hinting;
        _font.Edging = _edging;
        _font.Subpixel = _subpixel;
        for (int i = 0; i < count; i++)
        {
            ref Item item = ref _items[i];
            float relativeX = item.X - x0;
            float relativeY = item.Y - y0;
            slot.Items[i] = item;
            slot.Items[i].X = relativeX;
            slot.Items[i].Y = relativeY;

            _font.Typeface = item.Typeface;
            _font.Size = item.FontSize;
            int glyphCount = item.Glyphs.Length;
            if (_positions.Length < glyphCount)
            {
                _positions = new SKPoint[glyphCount * 2];
            }
            for (int g = 0; g < glyphCount; g++)
            {
                SKPoint position = item.Positions[g];
                _positions[g] = new SKPoint(position.X + relativeX, position.Y + relativeY);
            }
            SKPositionedRunBuffer buffer = _builder.AllocatePositionedRun(_font, glyphCount);
            buffer.SetGlyphs(item.Glyphs.AsSpan());
            buffer.SetPositions(_positions.AsSpan(0, glyphCount));
        }
        // Items past the count would keep blobs of texts that left the run alive.
        Array.Clear(slot.Items, count, slot.Items.Length - count);
        slot.Count = count;
        slot.Merged?.Dispose();
        slot.Merged = _builder.Build();
        MergedBuildCount++;
        return slot.Merged;
    }
}
