using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Forms.Controls.Games;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// DialogBox with BBCode text: tags are not letters, so the typewriter reveal and the page split count only the
/// displayed text. Linked into SkiaGum.Tests so both runtimes run the same cases.
/// </summary>
public class DialogBoxBbCodeTests : BaseTestClass
{
    private sealed class TaggedDialogBoxVisual : InteractiveGue
    {
        public TextRuntime TextInstance { get; }

        public TaggedDialogBoxVisual(bool paginating) : base(new InvisibleRenderable())
        {
            HasEvents = true;
            Width = 400;
            Height = 100;
            TextInstance = new TextRuntime { Name = "TextInstance", Text = string.Empty };
            if (paginating)
            {
                TextInstance.Width = 200;
                TextInstance.Height = 42;
                TextInstance.WidthUnits = DimensionUnitType.Absolute;
                TextInstance.HeightUnits = DimensionUnitType.Absolute;
                TextInstance.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
            }
            AddChild(TextInstance);
            AddChild(new ContainerRuntime { Name = "ContinueIndicatorInstance", Visible = false });
            FormsControlAsObject = new DialogBox(this);
        }

        public IFormsText Core => (IFormsText)TextInstance.RenderableComponent!;
    }

    private static (DialogBox DialogBox, TaggedDialogBoxVisual Visual) Create(bool paginating = false)
    {
        TaggedDialogBoxVisual visual = new(paginating);
        return ((DialogBox)visual.FormsControlAsObject!, visual);
    }

    private static void Tick(DialogBox dialogBox, double seconds) =>
        ((IUpdateEveryFrame)dialogBox).Activity(seconds);

    [Theory]
    [InlineData("[Color=Red]hello[/Color] world", "hello world")]
    [InlineData("a[FontScale=2]bc[/FontScale]d", "abcd")]
    [InlineData("press [Glyph=0x42] now", "press B now")]
    [InlineData("[color=red]x[/COLOR]y", "xy")]
    public void Show_TaggedText_ShowsEveryDisplayedLetterAtOnce(string markup, string displayed)
    {
        var (dialogBox, visual) = Create();
        dialogBox.LettersPerSecond = 0;

        dialogBox.Show(markup);

        string.Concat(visual.Core.WrappedText).ShouldBe(displayed);
        (visual.Core.MaxLettersToShow ?? 0).ShouldBeGreaterThanOrEqualTo(displayed.Length);
    }

    // At 10 letters a second the five displayed letters are all out after 0.5 seconds, so typing is finished
    // then; the tags must not add letters to wait for.
    [Fact]
    public void Show_TaggedTextTyping_FinishesWhenTheDisplayedLettersAreOut()
    {
        var (dialogBox, visual) = Create();
        dialogBox.LettersPerSecond = 10;
        int finished = 0;
        dialogBox.FinishedTypingPage += (_, _) => finished++;

        dialogBox.Show("[Color=Red]ab[/Color]c[Glyph=0x42]d");

        visual.Core.MaxLettersToShow.ShouldBe(0);
        Tick(dialogBox, 0.25);
        visual.Core.MaxLettersToShow.ShouldBe(2, "a tag opening after the second letter is not a letter");
        finished.ShouldBe(0);
        Tick(dialogBox, 0.3);
        finished.ShouldBe(1, "all five displayed letters are out");
    }

    [Fact]
    public void Click_WhileTypingTaggedText_RevealsTheWholePage()
    {
        var (dialogBox, visual) = Create();
        dialogBox.LettersPerSecond = 10;

        dialogBox.Show("[Color=Red]abcdef[/Color]gh");
        Tick(dialogBox, 0.3);
        visual.CallClick();

        (visual.Core.MaxLettersToShow ?? 0).ShouldBeGreaterThanOrEqualTo(8);
        dialogBox.IsVisible.ShouldBeTrue("the first click finishes typing, it does not dismiss");
    }

    [Fact]
    public void Click_AfterTaggedTextIsTypedOut_DismissesTheDialog()
    {
        var (dialogBox, visual) = Create();
        dialogBox.LettersPerSecond = 10;

        dialogBox.Show("[Color=Red]abcdef[/Color]gh");
        Tick(dialogBox, 0.9);
        visual.CallClick();

        dialogBox.IsVisible.ShouldBeFalse("all eight displayed letters were typed out, so the click dismisses");
    }

    // Every page keeps all of its own lines: none is cut off by the box, so no letter is lost between pages.
    [Theory]
    [InlineData("one two three four five six seven [FontScale=2]eight nine ten eleven[/FontScale] twelve thirteen fourteen fifteen sixteen")]
    [InlineData("[FontScale=1.5]one two three four five six seven eight nine ten eleven twelve thirteen[/FontScale]")]
    [InlineData("[Color=Red]one two three four five six seven eight nine ten[/Color] eleven twelve thirteen fourteen fifteen sixteen seventeen")]
    public void Show_PaginatedTaggedText_EveryPageShowsAllOfItsLetters(string markup)
    {
        var (dialogBox, visual) = Create(paginating: true);
        dialogBox.LettersPerSecond = 0;

        dialogBox.Show(markup);

        List<string> shown = new();
        int guard = 0;
        while (true)
        {
            string raw = Strip(visual.Core.RawText ?? string.Empty);
            string drawn = string.Concat(visual.Core.WrappedText);
            Normalize(drawn).ShouldBe(Normalize(raw), $"page {shown.Count} must not be truncated by the box");
            shown.Add(raw);
            if (dialogBox.PagesRemaining == 0 || ++guard > 20)
            {
                break;
            }
            visual.CallClick();
        }

        Normalize(string.Concat(shown)).ShouldBe(Normalize(Strip(markup)));
    }

    // A word wider than the box breaks across lines as a Text does, and every one of its letters is drawn on some page.
    [Theory]
    [InlineData("one two Supercalifragilisticexpialidociousandthensomemoreletters three four five six seven eight")]
    [InlineData("one two [Color=Red]Supercalifragilisticexpialidociousandthensomemoreletters[/Color] three four five six seven eight")]
    [InlineData("Supercalifragilisticexpialidociousandthensomemorelettersthatkeepgoingpastthreelinesofthebox")]
    public void Show_PaginatedWordWiderThanTheBox_DrawsEveryLetterOnSomePage(string markup)
    {
        var (dialogBox, visual) = Create(paginating: true);
        dialogBox.LettersPerSecond = 0;

        dialogBox.Show(markup);

        List<string> drawn = new();
        int guard = 0;
        while (true)
        {
            drawn.Add(string.Concat(visual.Core.WrappedText));
            if (dialogBox.PagesRemaining == 0 || ++guard > 20)
            {
                break;
            }
            visual.CallClick();
        }

        RemoveWhitespace(string.Concat(drawn)).ShouldBe(RemoveWhitespace(Strip(markup)));
    }

    private static string RemoveWhitespace(string value) =>
        string.Concat(value.Where(character => !char.IsWhiteSpace(character)));

    [Fact]
    public void Show_EmptyTextInAPaginatingBox_ShowsOneEmptyPage()
    {
        var (dialogBox, visual) = Create(paginating: true);
        dialogBox.LettersPerSecond = 0;

        dialogBox.Show(string.Empty);

        dialogBox.IsVisible.ShouldBeTrue();
        dialogBox.PagesRemaining.ShouldBe(0);
        string.Concat(visual.Core.WrappedText).ShouldBe(string.Empty, "the measuring text must not stay in the Text");
    }

    // A line taller than the box still gets its own page, and the page shows it.
    [Theory]
    [InlineData("[FontScale=5]hi[/FontScale]")]
    [InlineData("one two three [FontScale=5]hi[/FontScale] four five six seven eight nine ten eleven twelve")]
    public void Show_PaginatedLineTallerThanTheBox_ShowsTheLine(string markup)
    {
        var (dialogBox, visual) = Create(paginating: true);
        dialogBox.LettersPerSecond = 0;

        dialogBox.Show(markup);

        List<string> shown = new();
        int guard = 0;
        while (true)
        {
            shown.Add(string.Concat(visual.Core.WrappedText));
            if (dialogBox.PagesRemaining == 0 || ++guard > 20)
            {
                break;
            }
            visual.CallClick();
        }

        Normalize(string.Concat(shown.Select(page => page + " "))).ShouldBe(Normalize(Strip(markup)));
    }

    // Pages split at blank lines show the same lines, page for page, as the source text.
    [Theory]
    [InlineData("one two\n\nthree four\n\nfive six", new[] { "one two", "three four", "five six" })]
    [InlineData("[Color=Red]one two[/Color]\n\nthree [Color=Blue]four[/Color]\n\nfive six", new[] { "one two", "three four", "five six" })]
    [InlineData("one\n\n\n\nfive", new[] { "one", "", "five" })]
    public void Show_PaginatedTextWithBlankLines_ShowsTheSamePages(string markup, string[] expectedPages)
    {
        var (dialogBox, visual) = Create(paginating: true);
        dialogBox.LettersPerSecond = 0;

        dialogBox.Show(markup);

        ShowEveryPage(dialogBox, visual).ShouldBe(expectedPages);
    }

    // A page that ends in a blank line cuts nothing the reader can see, so it never draws the ellipsis (#8347).
    [Theory]
    [InlineData("one two\n\nthree four\n\nfive six", new[] { "one two", "three four", "five six" })]
    [InlineData("[Color=Red]one two[/Color]\n\nthree [Color=Blue]four[/Color]\n\nfive six", new[] { "one two", "three four", "five six" })]
    [InlineData("one\n\n\n\nfive", new[] { "one", "", "five" })]
    public void Show_PaginatedTextWithBlankLinesAndEllipsis_ShowsNoEllipsis(string markup, string[] expectedPages)
    {
        var (dialogBox, visual) = Create(paginating: true);
        visual.TextInstance.IsTruncatingWithEllipsisOnLastLine = true;
        dialogBox.LettersPerSecond = 0;

        dialogBox.Show(markup);

        ShowEveryPage(dialogBox, visual).ShouldBe(expectedPages);
    }

    // Each page's drawn lines, newline-free and joined by '\n', without the blank lines it ends in.
    private static List<string> ShowEveryPage(DialogBox dialogBox, TaggedDialogBoxVisual visual)
    {
        List<string> pages = new();
        int guard = 0;
        while (true)
        {
            string page = string.Join("\n", visual.Core.WrappedText.Select(line => line.TrimEnd('\n', '\r'))).TrimEnd('\n');
#if SKIA_LINKED_TESTS
            // Skia's WrappedText holds the uncut lines; the ellipsis is a run of the laid-out block.
            var block = ((SkiaGum.Text)visual.TextInstance.RenderableComponent!).GetTextBlock();
            if (block.Lines.SelectMany(line => line.Runs).Any(run => run.RunKind == Topten.RichTextKit.FontRunKind.Ellipsis))
            {
                page += "...";
            }
#endif
            pages.Add(page);
            if (dialogBox.PagesRemaining == 0 || ++guard > 20)
            {
                break;
            }
            visual.CallClick();
        }
        return pages;
    }

    // RawText holds the stripped text on MonoGame and the markup on Skia, so pages are compared stripped.
    private static string Strip(string markup) =>
        BbCodeParser.RemoveTags(markup, BbCodeParser.Parse(markup, BbCodeParser.KnownTags));

    private static string Normalize(string value) =>
        string.Join(" ", value.Split(new[] { ' ', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries));
}
