using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;
using System.Text;

namespace SkiaGum.GpuTests;

/// <summary>
/// #8052: a scrollbar hands a ListBox fractional scroll positions, and stacked Forms NineSlices sit at fractional Y
/// (a line height such as 30.698242). Adjacent items must never show a seam: no background through a touching
/// edge, no pixel off the texture's palette (a blended hairline), no outline row doubled or missing, and no
/// flicker between two scroll positions 0.05 px apart. Real Forms theme tiles (UISpriteSheet) and synthetic
/// textures with borders of 1 to 6 texels, on a GPU surface at canvas scales 1 and 2.
/// </summary>
[Collection(GpuCollection.Name)]
public class NineSliceStackSeamTests
{
    readonly WglGpuContext _gpu;

    public NineSliceStackSeamTests(WglGpuContext gpu)
    {
        _gpu = gpu;
    }

    static readonly SKColor Background = new(255, 0, 255);
    static readonly SKColor Outline = new(0xAA, 0xAA, 0xAA);
    static readonly SKColor Fill = SKColors.White;
    static readonly SKColor SyntheticBorder = new(200, 40, 40);
    static readonly SKColor SyntheticFill = new(40, 40, 200);

    /// <summary>One texture to stack: where it comes from, the texel width of the line along its edge, and its two colors.</summary>
    public record TextureSpec(string Name, int Border, SKColor EdgeColor, SKColor InnerColor, int[]? Source)
    {
        public override string ToString() => Name;
    }

    // Border 1 is the Forms "Bordered" tile: a one-texel gray line, with the nine-slice cut at 8 texels.
    static readonly TextureSpec FormsBordered = new("FormsBordered", 1, Outline, Fill, new[] { 24, 48, 24, 24 });
    static TextureSpec Synthetic(int border) => new("Synthetic" + border, border, SyntheticBorder, SyntheticFill, null);

    static SKBitmap SyntheticBitmap(int border)
    {
        int size = border * 3;
        var bitmap = new SKBitmap(size, size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool edge = x < border || y < border || x >= size - border || y >= size - border;
                bitmap.SetPixel(x, y, edge ? SyntheticBorder : SyntheticFill);
            }
        }
        return bitmap;
    }

    /// <summary>The geometry of a stack, in world units. Items touch when spacing is 0.</summary>
    record Layout(float Left, float BaseTop, float Width, float ItemHeight, float Spacing, int Count, float Scroll, float Zoom)
    {
        public float ItemTop(int i) => BaseTop - Scroll + i * (ItemHeight + Spacing);
    }

    SKBitmap Render(TextureSpec spec, Layout layout, SKFilterMode filter)
    {
        GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        SKFilterMode previousFilter = RenderingLibrary.Graphics.Renderer.TextureFilter;
        int pixelWidth = (int)MathF.Ceiling((layout.Left + layout.Width + 4) * layout.Zoom);
        int pixelHeight = (int)MathF.Ceiling((layout.BaseTop + layout.Count * (layout.ItemHeight + layout.Spacing) + 4) * layout.Zoom);
        try
        {
            return _gpu.Render(pixelWidth, pixelHeight, 0, canvas =>
            {
                var managers = new SystemManagers();
                managers.Initialize();
                SystemManagers.Default = managers;
                managers.Canvas = canvas;
                managers.Renderer.ClearsCanvas = false;
                managers.Renderer.Camera.Zoom = layout.Zoom;
                RenderingLibrary.Graphics.Renderer.TextureFilter = filter;
                SKBitmap? synthetic = null;
                SKBitmap texture;
                if (spec.Source == null)
                {
                    synthetic = SyntheticBitmap(spec.Border);
                    texture = synthetic;
                }
                else
                {
                    texture = managers.GetOrLoadEmbeddedTexture2d("UISpriteSheet.png");
                }
                canvas.Clear(Background);

                // A ListBox's shape: a viewport that clips, holding a stack moved by the scroll offset.
                var viewport = new ContainerRuntime
                {
                    X = layout.Left - 3, // wider than the items, so the clip never crops an item edge
                    Y = 0,
                    WidthUnits = DimensionUnitType.Absolute,
                    HeightUnits = DimensionUnitType.Absolute,
                    Width = layout.Width + 6,
                    Height = pixelHeight / layout.Zoom,
                    ClipsChildren = true,
                };
                viewport.AddToManagers(managers, null);
                var stack = new ContainerRuntime
                {
                    X = 3,
                    Y = layout.BaseTop - layout.Scroll,
                    WidthUnits = DimensionUnitType.Absolute,
                    HeightUnits = DimensionUnitType.RelativeToChildren,
                    Width = layout.Width,
                    Height = 0,
                    ChildrenLayout = ChildrenLayout.TopToBottomStack,
                    StackSpacing = layout.Spacing,
                };
                viewport.Children.Add(stack);
                for (int i = 0; i < layout.Count; i++)
                {
                    var item = new NineSliceRuntime
                    {
                        Texture = texture,
                        WidthUnits = DimensionUnitType.Absolute,
                        HeightUnits = DimensionUnitType.Absolute,
                        Width = layout.Width,
                        Height = layout.ItemHeight,
                    };
                    if (spec.Source == null)
                    {
                        item.TextureAddress = TextureAddress.EntireTexture;
                    }
                    else
                    {
                        item.TextureAddress = TextureAddress.Custom;
                        item.TextureLeft = spec.Source[0];
                        item.TextureTop = spec.Source[1];
                        item.TextureWidth = spec.Source[2];
                        item.TextureHeight = spec.Source[3];
                    }
                    stack.Children.Add(item);
                }
                managers.Draw();
                synthetic?.Dispose();
            });
        }
        finally
        {
            RenderingLibrary.Graphics.Renderer.TextureFilter = previousFilter;
        }
    }

    // The pixels whose centers are inside [low, high] in device pixels.
    // A pixel whose center is within 0.02 of an edge is left out: float noise decides which side it falls on.
    static (int First, int Last) PixelsInside(float low, float high) =>
        ((int)MathF.Ceiling(low - 0.5f + Epsilon), (int)MathF.Floor(high - 0.5f - Epsilon));

    // Collection tests run one at a time, so a test may tighten this and restore it.
    const float Epsilon = 0.02f;

    // True when the edge sits so close to a pixel center that its rounding is a coin toss.
    static bool Ambiguous(float edge) => MathF.Abs((edge - 0.5f) - MathF.Round(edge - 0.5f)) < Epsilon;

    static bool Same(SKColor a, SKColor b) => a.Red == b.Red && a.Green == b.Green && a.Blue == b.Blue && a.Alpha == b.Alpha;

    /// <summary>Everything wrong with one frame, empty when it is clean.</summary>
    static List<string> Analyze(SKBitmap pixels, TextureSpec spec, Layout layout)
    {
        var problems = new List<string>();
        float zoom = layout.Zoom;
        int expectedBand = (int)MathF.Round(spec.Border * zoom);
        (int firstX, int lastX) = PixelsInside(layout.Left * zoom, (layout.Left + layout.Width) * zoom);
        int centerX = (firstX + lastX) / 2;
        int corner = (int)MathF.Ceiling(zoom) + 1;

        for (int i = 0; i < layout.Count; i++)
        {
            float top = layout.ItemTop(i) * zoom;
            float bottom = (layout.ItemTop(i) + layout.ItemHeight) * zoom;
            (int firstY, int lastY) = PixelsInside(top, bottom);
            string item = $"item {i} (rows {firstY}..{lastY})";

            for (int y = firstY; y <= lastY; y++)
            {
                for (int x = firstX; x <= lastX; x++)
                {
                    bool inCorner = (x < firstX + corner || x > lastX - corner) && (y < firstY + corner || y > lastY - corner);
                    SKColor p = pixels.GetPixel(x, y);
                    if (Same(p, Background))
                    {
                        if (!inCorner)
                        {
                            problems.Add($"{item}: background shows through at ({x}, {y})");
                        }
                    }
                    else if (!Same(p, spec.EdgeColor) && !Same(p, spec.InnerColor))
                    {
                        problems.Add($"{item}: off-palette pixel {p} at ({x}, {y})");
                    }
                }
            }

            // Edge bands, counted from the exact first and last pixel whose center is inside. An edge so close to a
            // pixel center that its rounding is a coin toss is skipped.
            int centerY = (firstY + lastY) / 2;
            float leftEdge = layout.Left * zoom;
            float rightEdge = (layout.Left + layout.Width) * zoom;
            int rawFirstY = (int)MathF.Ceiling(top - 0.5f);
            int rawLastY = (int)MathF.Ceiling(bottom - 0.5f) - 1;
            int rawFirstX = (int)MathF.Ceiling(leftEdge - 0.5f);
            int rawLastX = (int)MathF.Ceiling(rightEdge - 0.5f) - 1;

            if (!Ambiguous(top))
            {
                int run = 0;
                for (int y = rawFirstY; y <= rawLastY && Same(pixels.GetPixel(centerX, y), spec.EdgeColor); y++)
                {
                    run++;
                }
                if (run != expectedBand)
                {
                    problems.Add($"{item}: top edge is {run} px, expected {expectedBand}");
                }
            }
            if (!Ambiguous(bottom))
            {
                int run = 0;
                for (int y = rawLastY; y >= rawFirstY && Same(pixels.GetPixel(centerX, y), spec.EdgeColor); y--)
                {
                    run++;
                }
                if (run != expectedBand)
                {
                    problems.Add($"{item}: bottom edge is {run} px, expected {expectedBand}");
                }
            }
            if (!Ambiguous(leftEdge))
            {
                int run = 0;
                for (int x = rawFirstX; x <= rawLastX && Same(pixels.GetPixel(x, centerY), spec.EdgeColor); x++)
                {
                    run++;
                }
                if (run != expectedBand)
                {
                    problems.Add($"{item}: left edge is {run} px, expected {expectedBand}");
                }
            }
            if (!Ambiguous(rightEdge))
            {
                int run = 0;
                for (int x = rawLastX; x >= rawFirstX && Same(pixels.GetPixel(x, centerY), spec.EdgeColor); x--)
                {
                    run++;
                }
                if (run != expectedBand)
                {
                    problems.Add($"{item}: right edge is {run} px, expected {expectedBand}");
                }
            }
        }
        return problems;
    }

    /// <summary>The rows where the center column changes color, top to bottom.</summary>
    static List<int> Transitions(SKBitmap pixels, int x)
    {
        var rows = new List<int>();
        SKColor previous = pixels.GetPixel(x, 0);
        for (int y = 1; y < pixels.Height; y++)
        {
            SKColor current = pixels.GetPixel(x, y);
            if (!Same(current, previous))
            {
                rows.Add(y);
            }
            previous = current;
        }
        return rows;
    }

    static void DumpIfRequested(SKBitmap pixels, string name)
    {
        string? folder = Environment.GetEnvironmentVariable("GUM_SEAM_DUMP");
        if (string.IsNullOrEmpty(folder))
        {
            return;
        }
        Directory.CreateDirectory(folder);
        const int magnify = 6;
        using var big = new SKBitmap(pixels.Width * magnify, pixels.Height * magnify);
        using (var canvas = new SKCanvas(big))
        {
            using var source = SKImage.FromBitmap(pixels);
            canvas.DrawImage(source, new SKRect(0, 0, big.Width, big.Height), new SKSamplingOptions(SKFilterMode.Nearest));
        }
        using var image = SKImage.FromBitmap(big);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.Combine(folder, name + ".png"), data.ToArray());
    }

    static string Join(List<string> problems) =>
        problems.Count == 0 ? "" : string.Join("\n", problems.Take(8)) + (problems.Count > 8 ? $"\n... {problems.Count} in total" : "");

    public static IEnumerable<object[]> StackCases()
    {
        var specs = new[] { FormsBordered, Synthetic(1), Synthetic(2), Synthetic(3), Synthetic(4), Synthetic(6) };
        foreach (TextureSpec spec in specs)
        {
            foreach (float zoom in new[] { 1f, 2f })
            {
                // 30.698242 is a Forms button line height; 24 is whole; 20.5 sits on half pixels.
                foreach (float height in new[] { 30.698242f, 24f, 20.5f })
                {
                    foreach (float spacing in new[] { 0f, 6f })
                    {
                        foreach (float left in new[] { 8f, 8.35f })
                        {
                            yield return new object[] { spec, zoom, height, spacing, left };
                        }
                    }
                }
            }
        }
    }

    /// <summary>Scroll positions 0 to 2 px in steps of 0.05.</summary>
    static IEnumerable<float> Scrolls()
    {
        for (int i = 0; i <= 40; i++)
        {
            yield return i * 0.05f;
        }
    }

    [Theory]
    [MemberData(nameof(StackCases))]
    public void Draw_StackedNineSlicesWhileScrolling_ShouldHaveNoSeam(
        TextureSpec spec, float zoom, float height, float spacing, float left)
    {
        // Above the first item's top so no scroll position clips it; a fractional base like a scrollbar's content offset.
        const float baseTop = 8.698242f;
        const float width = 48.5f;
        var failures = new StringBuilder();
        foreach (float scroll in Scrolls())
        {
            var layout = new Layout(left, baseTop, width, height, spacing, 5, scroll, zoom);
            using SKBitmap pixels = Render(spec, layout, SKFilterMode.Nearest);
            List<string> problems = Analyze(pixels, spec, layout);
            if (problems.Count > 0)
            {
                DumpIfRequested(pixels, $"{spec.Name}-z{zoom}-h{height}-s{spacing}-x{left}-scroll{scroll:F2}");
                failures.AppendLine($"scroll {scroll:F2}:\n{Join(problems)}");
                break;
            }
        }
        failures.Length.ShouldBe(0, $"{spec.Name}, zoom {zoom}, height {height}, spacing {spacing}, left {left}\n{failures}");
    }

    [Theory]
    [MemberData(nameof(StackCases))]
    public void Draw_StackedNineSlicesWhileScrolling_ShouldNotFlickerBetweenAdjacentOffsets(
        TextureSpec spec, float zoom, float height, float spacing, float left)
    {
        const float baseTop = 8.698242f;
        const float width = 48.5f;
        List<int>? previous = null;
        SKBitmap? previousPixels = null;
        float previousScroll = 0;
        try
        {
            foreach (float scroll in Scrolls())
            {
                var layout = new Layout(left, baseTop, width, height, spacing, 5, scroll, zoom);
                SKBitmap pixels = Render(spec, layout, SKFilterMode.Nearest);
                (int firstX, int lastX) = PixelsInside(left * zoom, (left + width) * zoom);
                List<int> rows = Transitions(pixels, (firstX + lastX) / 2);
                if (previous != null)
                {
                    string context = $"{spec.Name}, zoom {zoom}, height {height}, spacing {spacing}, left {left}, scroll {previousScroll:F2} to {scroll:F2}\n"
                        + $"edges before: {string.Join(",", previous)}\nedges after:  {string.Join(",", rows)}";
                    bool flickers = rows.Count != previous.Count;
                    for (int i = 0; !flickers && i < rows.Count; i++)
                    {
                        // 0.05 px of scroll moves an edge by at most one device pixel.
                        flickers = Math.Abs(rows[i] - previous[i]) > 1;
                    }
                    if (flickers)
                    {
                        string name = $"flicker-{spec.Name}-z{zoom}-h{height}-s{spacing}-x{left}";
                        DumpIfRequested(previousPixels!, name + "-before");
                        DumpIfRequested(pixels, name + "-after");
                        flickers.ShouldBeFalse($"an edge appeared, vanished or jumped more than one pixel: {context}");
                    }
                }
                previousPixels?.Dispose();
                previousPixels = pixels;
                previous = rows;
                previousScroll = scroll;
            }
        }
        finally
        {
            previousPixels?.Dispose();
        }
    }
    /// <summary>
    /// The scroll positions around a pixel-center tie, where an item's top edge sits within a few thousandths of a
    /// pixel of a pixel center and a one-texel outline used to vanish for a frame. Whichever row an edge rounds to,
    /// the outline of the first and last item and the line between touching items must keep their full thickness.
    /// </summary>
    [Theory]
    [InlineData(1f, 0.198242f)]
    [InlineData(2f, 0.448242f)]
    public void Draw_StackedNineSlicesAtAPixelCenterTie_ShouldKeepEveryOutline(float zoom, float tieScroll)
    {
        const int count = 4;
        var failures = new StringBuilder();
        for (int step = -40; step <= 40; step++)
        {
            float scroll = tieScroll + step * 0.0005f;
            var layout = new Layout(8f, 8.698242f, 48.5f, 24f, 0f, count, scroll, zoom);
            using SKBitmap pixels = Render(FormsBordered, layout, SKFilterMode.Nearest);
            (int firstX, int lastX) = PixelsInside(layout.Left * zoom, (layout.Left + layout.Width) * zoom);
            int x = (firstX + lastX) / 2;
            int band = (int)MathF.Round(FormsBordered.Border * zoom);

            int top = 0;
            while (Same(pixels.GetPixel(x, top), Background))
            {
                top++;
            }
            int bottom = pixels.Height - 1;
            while (Same(pixels.GetPixel(x, bottom), Background))
            {
                bottom--;
            }
            var found = new List<string>();
            int run = GrayRun(pixels, x, top);
            if (run != band)
            {
                found.Add($"first item's top outline is {run} px at row {top}, expected {band}");
            }
            run = GrayRun(pixels, x, bottom);
            if (run != band)
            {
                found.Add($"last item's bottom outline is {run} px at row {bottom}, expected {band}");
            }
            for (int i = 1; i < count; i++)
            {
                int boundary = (int)MathF.Round(layout.ItemTop(i) * zoom);
                run = GrayRun(pixels, x, boundary);
                if (run != 2 * band)
                {
                    found.Add($"line between items {i - 1} and {i} is {run} px at row {boundary}, expected {2 * band}");
                }
            }
            if (found.Count > 0)
            {
                DumpIfRequested(pixels, $"tie-z{zoom}-scroll{scroll:F4}");
                failures.AppendLine($"scroll {scroll:F4}: {string.Join("; ", found)}");
            }
        }
        failures.Length.ShouldBe(0, failures.ToString());
    }

    // The number of gray rows in the column through row y, counting up and down from it; 0 when y is not gray.
    static int GrayRun(SKBitmap pixels, int x, int y)
    {
        if (!Same(pixels.GetPixel(x, y), Outline))
        {
            // The boundary row rounds either way; look one row each side.
            if (Same(pixels.GetPixel(x, y - 1), Outline))
            {
                y--;
            }
            else if (Same(pixels.GetPixel(x, y + 1), Outline))
            {
                y++;
            }
            else
            {
                return 0;
            }
        }
        int first = y;
        while (first > 0 && Same(pixels.GetPixel(x, first - 1), Outline))
        {
            first--;
        }
        int last = y;
        while (last < pixels.Height - 1 && Same(pixels.GetPixel(x, last + 1), Outline))
        {
            last++;
        }
        return last - first + 1;
    }

    // ---- Sweeps over awkward offsets (#8069) ----
    // These do not assume which row an edge rounds to. They read the center column and center row as runs of colors and
    // count what a seam looks like: a gap (background inside the stack), a hairline (a color the texture does not have),
    // outline loss (an edge line thinner than the texture's) and overlap (an edge line thicker, or a fill band shorter
    // than the item, i.e. two items covering the same rows).

    sealed class SeamCounts
    {
        public int Frames, Gap, Hairline, OutlineLoss, Overlap;
        public string? FirstExample;
        public int Total => Gap + Hairline + OutlineLoss + Overlap;
        public override string ToString() => $"{Frames} frames: gap {Gap}, hairline {Hairline}, outline loss {OutlineLoss}, overlap {Overlap}" + (FirstExample == null ? "" : $"; first: {FirstExample}");
    }

    // Runs of equal color along the line, from the first pixel that is not background to the last.
    static List<(SKColor Color, int Length)> RunsAlong(SKBitmap pixels, int fixedCoordinate, bool vertical)
    {
        int length = vertical ? pixels.Height : pixels.Width;
        SKColor Get(int i) => vertical ? pixels.GetPixel(fixedCoordinate, i) : pixels.GetPixel(i, fixedCoordinate);
        int start = 0;
        while (start < length && Same(Get(start), Background))
        {
            start++;
        }
        int end = length - 1;
        while (end > start && Same(Get(end), Background))
        {
            end--;
        }
        var runs = new List<(SKColor, int)>();
        for (int i = start; i <= end; i++)
        {
            SKColor c = Get(i);
            if (runs.Count > 0 && Same(runs[^1].Item1, c))
            {
                runs[^1] = (c, runs[^1].Item2 + 1);
            }
            else
            {
                runs.Add((c, 1));
            }
        }
        return runs;
    }

    static void CountSeams(SKBitmap pixels, TextureSpec spec, Layout layout, SeamCounts counts, string label)
    {
        counts.Frames++;
        float zoom = layout.Zoom;
        int band = (int)MathF.Round(spec.Border * zoom);
        int spacing = (int)MathF.Round(layout.Spacing * zoom);
        (int firstX, int lastX) = PixelsInside(layout.Left * zoom, (layout.Left + layout.Width) * zoom);
        int centerX = (firstX + lastX) / 2;
        float topEdge = layout.ItemTop(0) * zoom;
        int centerY = (int)MathF.Round(topEdge + layout.ItemHeight * zoom / 2);

        void Add(ref int counter, string what)
        {
            counter++;
            counts.FirstExample ??= $"{label}: {what}";
        }

        // Vertical: edge line, fill, edge line (touching items: one line of twice the width), and so on, with the
        // spacing as background between spaced items.
        var runs = RunsAlong(pixels, centerX, vertical: true);
        int items = 0;
        for (int r = 0; r < runs.Count; r++)
        {
            (SKColor color, int length) = runs[r];
            bool isEdge = Same(color, spec.EdgeColor);
            bool isFill = Same(color, spec.InnerColor);
            bool isBackground = Same(color, Background);
            if (!isEdge && !isFill && !isBackground)
            {
                Add(ref counts.Hairline, $"color {color} for {length} rows");
            }
            else if (isBackground)
            {
                // Only legal between spaced items, and only as wide as the spacing (give or take a rounding pixel).
                if (spacing == 0 || Math.Abs(length - spacing) > 1)
                {
                    Add(ref counts.Gap, $"{length} background rows inside the stack (spacing {spacing})");
                }
            }
            else if (isEdge)
            {
                bool first = r == 0;
                bool last = r == runs.Count - 1;
                bool touchingJoin = spacing == 0 && !first && !last;
                int expected = touchingJoin ? 2 * band : band;
                if (length < expected)
                {
                    Add(ref counts.OutlineLoss, $"edge line {length} rows, expected {expected}");
                }
                else if (length > expected)
                {
                    Add(ref counts.Overlap, $"edge line {length} rows, expected {expected}");
                }
            }
            else
            {
                items++;
                int expectedFill = (int)MathF.Round(layout.ItemHeight * zoom) - 2 * band;
                if (length < expectedFill - 1)
                {
                    Add(ref counts.Overlap, $"fill {length} rows, expected {expectedFill}");
                }
                else if (length > expectedFill + 1)
                {
                    Add(ref counts.Gap, $"fill {length} rows, expected {expectedFill}");
                }
            }
        }
        if (items != layout.Count)
        {
            Add(ref counts.Gap, $"{items} items found, expected {layout.Count}");
        }

        // Horizontal: edge line, fill, edge line.
        var across = RunsAlong(pixels, centerY, vertical: false);
        foreach ((SKColor color, int length) in across)
        {
            if (!Same(color, spec.EdgeColor) && !Same(color, spec.InnerColor))
            {
                Add(ref counts.Hairline, $"color {color} for {length} columns across");
            }
        }
        if (across.Count == 3)
        {
            if (across[0].Length < band || across[2].Length < band)
            {
                Add(ref counts.OutlineLoss, $"side line {across[0].Length}/{across[2].Length} columns, expected {band}");
            }
            else if (across[0].Length > band || across[2].Length > band)
            {
                Add(ref counts.Overlap, $"side line {across[0].Length}/{across[2].Length} columns, expected {band}");
            }
        }
        else
        {
            Add(ref counts.OutlineLoss, $"{across.Count} runs across the item, expected 3");
        }
    }

    static IEnumerable<TextureSpec> SweepSpecs() => new[] { FormsBordered, Synthetic(1), Synthetic(3), Synthetic(6) };

    static void WriteSummary(string name, string text)
    {
        string? folder = Environment.GetEnvironmentVariable("GUM_SEAM_DUMP");
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "sweep-summary.txt"), name + ": " + text + Environment.NewLine);
        }
    }

    /// <summary>Scroll positions k * 0.0126123 (about 160 of them across 2 px), so edges land on every fraction.</summary>
    [Theory]
    [InlineData(1f, 0f)]
    [InlineData(1f, 6f)]
    [InlineData(2f, 0f)]
    [InlineData(2f, 6f)]
    public void Draw_StackedNineSlicesAtAwkwardScrollSteps_ShouldHaveNoSeam(float zoom, float spacing)
    {
        var counts = new SeamCounts();
        foreach (TextureSpec spec in SweepSpecs())
        {
            foreach (float height in new[] { 30.698242f, 20.5f })
            {
                for (int k = 0; k <= 158; k++)
                {
                    var layout = new Layout(8.35f, 8.698242f, 48.5f, height, spacing, 4, k * 0.0126123f, zoom);
                    using SKBitmap pixels = Render(spec, layout, SKFilterMode.Nearest);
                    CountSeams(pixels, spec, layout, counts, $"{spec.Name} height {height} scroll {layout.Scroll:F4}");
                }
            }
        }
        WriteSummary($"step 0.0126123, zoom {zoom}, spacing {spacing}", counts.ToString());
        counts.Total.ShouldBe(0, counts.ToString());
    }

    /// <summary>A few hundred frames per case at random base positions, scrolls, offsets and sizes from a fixed seed.</summary>
    [Theory]
    [InlineData(1f, 0f)]
    [InlineData(1f, 6f)]
    [InlineData(2f, 0f)]
    [InlineData(2f, 6f)]
    public void Draw_StackedNineSlicesAtRandomOffsets_ShouldHaveNoSeam(float zoom, float spacing)
    {
        var random = new Random(8069 + (int)zoom * 10 + (int)spacing);
        var counts = new SeamCounts();
        TextureSpec[] specs = SweepSpecs().ToArray();
        for (int i = 0; i < 800; i++)
        {
            TextureSpec spec = specs[i % specs.Length];
            float baseTop = 8.698242f + random.Next(0, 160) * 0.0126123f;
            float scroll = (float)(random.NextDouble() * 2);
            float left = 4f + (float)random.NextDouble() * 6f;
            float height = i % 3 == 0 ? 30.698242f : 18f + (float)random.NextDouble() * 16f;
            var layout = new Layout(left, baseTop, 40f + (float)random.NextDouble() * 20f, height, spacing, 4, scroll, zoom);
            using SKBitmap pixels = Render(spec, layout, SKFilterMode.Nearest);
            CountSeams(pixels, spec, layout, counts, $"{spec.Name} left {left:F4} base {baseTop:F4} scroll {scroll:F4} height {height:F4}");
        }
        WriteSummary($"random, zoom {zoom}, spacing {spacing}", counts.ToString());
        counts.Total.ShouldBe(0, counts.ToString());
    }
}
