using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Tests.Performance;
using SkiaSharp;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A Text laid out inside a stack draws, wraps, measures and hit-tests exactly like a Text built fresh in the
/// same state, however the stack got there: a sibling above it moved it (only Y changed), or the stack's width
/// changed (#7865). Pins that keeping the cached block across those layouts leaves the output unchanged.
/// </summary>
public class TextStackLayoutOutputTests : BaseTestClass
{
    private readonly SkiaSceneHost _host = new SkiaSceneHost();

    public override void Dispose()
    {
        Gum.GumService.Default.Root.Children.Clear();
        base.Dispose();
    }

    private const string Wrappable = "The quick brown fox jumps over the lazy dog again and again";
    private const string Bb = "[Color=Red]Hello[/Color] [FontSize=30]big[/FontSize] world that keeps going on and on";
    private const string Emoji = "Hi \U0001F600 there \U0001F44D friends from far away";
    private const string Uneven = "Short\nA much longer second line\nMid line";

    public enum Sizing
    {
        /// <summary>Width RelativeToChildren: measured unbounded, then the width is set to what was measured.</summary>
        AutoSized,
        /// <summary>Width RelativeToParent: wraps at the stack's width.</summary>
        FillsParent,
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (HorizontalAlignment alignment in new[] { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right })
        {
            yield return new object[] { Uneven, alignment, Sizing.AutoSized };
            yield return new object[] { Wrappable, alignment, Sizing.AutoSized };
            yield return new object[] { Bb, alignment, Sizing.AutoSized };
            yield return new object[] { Emoji, alignment, Sizing.AutoSized };
            yield return new object[] { Wrappable, alignment, Sizing.FillsParent };
            yield return new object[] { Bb, alignment, Sizing.FillsParent };
            yield return new object[] { Emoji, alignment, Sizing.FillsParent };
        }
    }

    private sealed class Scene
    {
        public ContainerRuntime Stack = null!;
        public ContainerRuntime Spacer = null!;
        public TextRuntime Text = null!;
        public TextRuntime Tail = null!;
    }

    private Scene Build(string text, HorizontalAlignment alignment, Sizing sizing, float stackWidth, float spacerHeight)
    {
        Scene scene = new Scene();
        scene.Stack = new ContainerRuntime();
        scene.Stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        scene.Stack.X = 10;
        scene.Stack.Y = 10;
        scene.Stack.Width = stackWidth;
        scene.Stack.WidthUnits = DimensionUnitType.Absolute;
        scene.Stack.Height = 0;
        scene.Stack.HeightUnits = DimensionUnitType.RelativeToChildren;
        scene.Stack.AddToRoot();

        scene.Spacer = new ContainerRuntime();
        scene.Spacer.Width = 10;
        scene.Spacer.Height = spacerHeight;
        scene.Stack.AddChild(scene.Spacer);

        scene.Text = new TextRuntime();
        scene.Text.Text = text;
        scene.Text.HorizontalAlignment = alignment;
        if (sizing == Sizing.FillsParent)
        {
            scene.Text.Width = 0;
            scene.Text.WidthUnits = DimensionUnitType.RelativeToParent;
        }
        scene.Stack.AddChild(scene.Text);

        scene.Tail = new TextRuntime();
        scene.Tail.Text = "Tail text";
        scene.Stack.AddChild(scene.Tail);

        scene.Stack.UpdateLayout();
        return scene;
    }

    private string Capture(Scene scene)
    {
        _host.Clear();
        _host.Update();
        _host.Draw();
        byte[] pixels = _host.CaptureBytes();
        int pixelHash = 17;
        foreach (byte b in pixels)
        {
            pixelHash = unchecked(pixelHash * 31 + b);
        }

        System.Text.StringBuilder builder = new();
        foreach (TextRuntime text in new[] { scene.Text, scene.Tail })
        {
            builder.Append($"lines=[{string.Join("|", text.WrappedText)}] ");
            builder.Append($"x={text.AbsoluteLeft} y={text.AbsoluteTop} w={text.GetAbsoluteWidth()} h={text.GetAbsoluteHeight()} ");
            float left = text.AbsoluteLeft;
            float top = text.AbsoluteTop;
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 5; column++)
                {
                    float x = left + column * text.GetAbsoluteWidth() / 4f;
                    float y = top + (row + 0.5f) * text.GetAbsoluteHeight() / 4f;
                    builder.Append(text.GetCharacterIndexAtPosition(x, y)).Append(',');
                }
            }
            builder.Append(' ');
        }
        builder.Append($"stackH={scene.Stack.GetAbsoluteHeight()} pixels={pixelHash}");
        return builder.ToString();
    }

    private void ShouldMatchFresh(Scene changed, string text, HorizontalAlignment alignment, Sizing sizing,
        float stackWidth, float spacerHeight, string because)
    {
        string actual = Capture(changed);

        // Hidden, so only the fresh scene is in the pixels. Hiding does not touch the Text's layout.
        changed.Stack.Visible = false;
        Scene fresh = Build(text, alignment, sizing, stackWidth, spacerHeight);
        string expected = Capture(fresh);
        fresh.Stack.RemoveFromRoot();
        changed.Stack.Visible = true;

        actual.ShouldBe(expected, because);
        CachedBlockShouldMatchBlockBuiltNow(changed.Text, because);
        CachedBlockShouldMatchBlockBuiltNow(changed.Tail, because);
    }

    private static string DescribeBlock(TextBlock block)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(700, 300));
        surface.Canvas.Clear(SKColors.Transparent);
        block.Paint(surface.Canvas, new SKPoint(10, 10));
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        int hash = 17;
        foreach (byte b in bitmap.Bytes)
        {
            hash = unchecked(hash * 31 + b);
        }

        string lines = string.Join(";", block.Lines.Select(line => $"{line.Start}+{line.Length}"));
        return $"{lines} w={block.MeasuredWidth} h={block.MeasuredHeight} pixels={hash}";
    }

    // Independent of the cache: the block the Text holds must be what a block laid out right now at the Text's width is.
    private static void CachedBlockShouldMatchBlockBuiltNow(TextRuntime runtime, string because)
    {
        Text text = (Text)runtime.RenderableComponent;
        DescribeBlock(text.GetCachedTextBlock()).ShouldBe(DescribeBlock(text.GetTextBlock(text.Width)), because);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void SpacerHeightChange_OutputMatchesFresh(string text, HorizontalAlignment alignment, Sizing sizing)
    {
        Scene changed = Build(text, alignment, sizing, 300, 10);
        _host.Update();
        _host.Draw();

        foreach (float spacerHeight in new float[] { 11, 10, 13, 10 })
        {
            changed.Spacer.Height = spacerHeight;
            ShouldMatchFresh(changed, text, alignment, sizing, 300, spacerHeight, $"spacer {spacerHeight}");
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void StackWidthChange_OutputMatchesFresh(string text, HorizontalAlignment alignment, Sizing sizing)
    {
        Scene changed = Build(text, alignment, sizing, 300, 10);
        _host.Update();
        _host.Draw();

        // 301 changes the width without changing any wrap; 120 and 90 change the wrapping of a text that fills its parent.
        foreach (float stackWidth in new float[] { 301, 300, 120, 300, 90, 301 })
        {
            changed.Stack.Width = stackWidth;
            ShouldMatchFresh(changed, text, alignment, sizing, stackWidth, 10, $"stack width {stackWidth}");
        }
    }

    // Layout sets an auto-sized Text's Width to what it measured with no width limit. A block laid out with no limit
    // must be the block laid out at exactly that width, for every alignment, or keeping it would change the output.
    [Theory]
    [MemberData(nameof(Cases))]
    public void UnboundedBlock_AtItsMeasuredWidth_IsTheBlockBuiltAtThatWidth(string content, HorizontalAlignment alignment, Sizing sizing)
    {
        Text text = new Text();
        text.RawText = content;
        text.HorizontalAlignment = alignment;
        text.Width = null;

        TextBlock unbounded = text.GetTextBlock(null);
        TextBlock bounded = text.GetTextBlock(unbounded.MeasuredWidth);

        DescribeBlock(unbounded).ShouldBe(DescribeBlock(bounded));
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void GetCachedTextBlock_UnboundedThenItsMeasuredWidthThenUnbounded_KeepsTheBlock(HorizontalAlignment alignment)
    {
        Text text = new Text { RawText = Uneven, HorizontalAlignment = alignment, Width = null };
        TextBlock unbounded = text.GetCachedTextBlock();

        for (int layout = 0; layout < 3; layout++)
        {
            text.Width = unbounded.MeasuredWidth;
            ReferenceEquals(text.GetCachedTextBlock(), unbounded).ShouldBeTrue("measured width");
            text.Width = null;
            ReferenceEquals(text.GetCachedTextBlock(), unbounded).ShouldBeTrue("no limit");
        }
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left, true)]
    [InlineData(HorizontalAlignment.Center, false)]
    [InlineData(HorizontalAlignment.Right, false)]
    public void GetCachedTextBlock_UnboundedThenWiderWidth_KeepsTheBlockOnlyWhenLeftAligned(HorizontalAlignment alignment, bool keeps)
    {
        Text text = new Text { RawText = Uneven, HorizontalAlignment = alignment, Width = null };
        TextBlock unbounded = text.GetCachedTextBlock();

        text.Width = unbounded.MeasuredWidth + 40;
        TextBlock atWiderWidth = text.GetCachedTextBlock();

        ReferenceEquals(atWiderWidth, unbounded).ShouldBe(keeps);
        DescribeBlock(atWiderWidth).ShouldBe(DescribeBlock(text.GetTextBlock(text.Width)));
    }

    [Fact]
    public void GetCachedTextBlock_UnboundedThenNarrowerWidth_Wraps()
    {
        Text text = new Text { RawText = Wrappable, Width = null };
        TextBlock unbounded = text.GetCachedTextBlock();
        int unboundedLines = unbounded.Lines.Count;

        text.Width = unbounded.MeasuredWidth / 2;
        TextBlock narrow = text.GetCachedTextBlock();

        narrow.Lines.Count.ShouldBeGreaterThan(unboundedLines);
        DescribeBlock(narrow).ShouldBe(DescribeBlock(text.GetTextBlock(text.Width)));
        text.Width = null;
        text.GetCachedTextBlock().Lines.Count.ShouldBe(unboundedLines);
    }

    // The comparison above is only worth something if the capture sees alignment, position and wrapping.
    [Fact]
    public void Capture_SeesAlignmentPositionAndWrapping()
    {
        string left = Capture(Build(Uneven, HorizontalAlignment.Left, Sizing.AutoSized, 300, 10));
        Gum.GumService.Default.Root.Children.Clear();
        string right = Capture(Build(Uneven, HorizontalAlignment.Right, Sizing.AutoSized, 300, 10));
        Gum.GumService.Default.Root.Children.Clear();
        string moved = Capture(Build(Uneven, HorizontalAlignment.Left, Sizing.AutoSized, 300, 12));
        Gum.GumService.Default.Root.Children.Clear();
        string narrow = Capture(Build(Wrappable, HorizontalAlignment.Left, Sizing.FillsParent, 120, 10));
        Gum.GumService.Default.Root.Children.Clear();
        string wide = Capture(Build(Wrappable, HorizontalAlignment.Left, Sizing.FillsParent, 400, 10));

        new[] { left, right, moved, narrow, wide }.Distinct().Count().ShouldBe(5);
    }

    [Fact]
    public void StackWidthChange_FillingTextRewraps()
    {
        Scene scene = Build(Wrappable, HorizontalAlignment.Left, Sizing.FillsParent, 400, 10);
        int wide = scene.Text.WrappedText.Count;

        scene.Stack.Width = 120;
        int narrow = scene.Text.WrappedText.Count;

        wide.ShouldBeLessThan(narrow);
        scene.Stack.Width = 400;
        scene.Text.WrappedText.Count.ShouldBe(wide);
    }

    [Fact]
    public void TextChange_AfterLayoutsInStack_MatchesFresh()
    {
        Scene changed = Build(Uneven, HorizontalAlignment.Center, Sizing.AutoSized, 300, 10);
        _host.Update();
        _host.Draw();
        changed.Spacer.Height = 11;
        changed.Stack.Width = 301;
        changed.Text.Text = Wrappable;
        changed.Spacer.Height = 10;

        ShouldMatchFresh(changed, Wrappable, HorizontalAlignment.Center, Sizing.AutoSized, 301, 10, "text changed after layouts");
    }

    [Fact]
    public void AlignmentChange_AfterLayoutsInStack_MatchesFresh()
    {
        Scene changed = Build(Uneven, HorizontalAlignment.Left, Sizing.AutoSized, 300, 10);
        _host.Update();
        _host.Draw();
        changed.Spacer.Height = 11;
        changed.Text.HorizontalAlignment = HorizontalAlignment.Right;
        changed.Spacer.Height = 10;

        ShouldMatchFresh(changed, Uneven, HorizontalAlignment.Right, Sizing.AutoSized, 300, 10, "alignment changed after layouts");
    }
}
