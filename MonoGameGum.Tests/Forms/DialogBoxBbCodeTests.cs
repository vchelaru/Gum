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

        public IFormsText Core => (IFormsText)TextInstance.RenderableComponent;
    }

    private static (DialogBox DialogBox, TaggedDialogBoxVisual Visual) Create(bool paginating = false)
    {
        TaggedDialogBoxVisual visual = new(paginating);
        return ((DialogBox)visual.FormsControlAsObject, visual);
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
    // Red until #8330 (Skia)
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

    // Red until #8330 (Skia)
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
    // Red until #8331 (both FontScale rows, MonoGame and Skia)
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

    // RawText holds the stripped text on MonoGame and the markup on Skia, so pages are compared stripped.
    private static string Strip(string markup) =>
        BbCodeParser.RemoveTags(markup, BbCodeParser.Parse(markup, BbCodeParser.KnownTags));

    private static string Normalize(string value) =>
        string.Join(" ", value.Split(new[] { ' ', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries));
}
