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

    // Every page keeps all of its own lines: none is cut off by the box, so no letter is lost between pages.
    [Theory]
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
