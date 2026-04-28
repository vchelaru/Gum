using Gum.DataTypes;
using Gum.Forms.Controls.Games;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class DialogBoxTests : BaseTestClass
{
    sealed class TestDialogBoxVisual : InteractiveGue
    {
        public TextRuntime TextInstance { get; }
        public ContainerRuntime ContinueIndicatorInstance { get; }

        public TestDialogBoxVisual(bool paginating = false) : base(new InvisibleRenderable())
        {
            HasEvents = true;
            Width = 400;
            Height = 100;

            TextInstance = new TextRuntime { Name = "TextInstance", Text = string.Empty };
            if (paginating)
            {
                // Fixed pixel size + TruncateLine triggers DialogBox.ConvertToPages's
                // pagination path. Width 200 / Height ~42 fits roughly 2 lines at the
                // default font's 21px line height.
                TextInstance.Width = 200;
                TextInstance.Height = 42;
                TextInstance.WidthUnits = DimensionUnitType.Absolute;
                TextInstance.HeightUnits = DimensionUnitType.Absolute;
                TextInstance.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
            }
            AddChild(TextInstance);

            ContinueIndicatorInstance = new ContainerRuntime { Name = "ContinueIndicatorInstance", Visible = false };
            AddChild(ContinueIndicatorInstance);

            FormsControlAsObject = new DialogBox(this);
        }
    }

    static (DialogBox dialogBox, TestDialogBoxVisual visual) CreateDialogBox(bool paginating = false)
    {
        var visual = new TestDialogBoxVisual(paginating);
        return ((DialogBox)visual.FormsControlAsObject, visual);
    }

    static void Tick(DialogBox dialogBox, double secondDifference) =>
        ((IUpdateEveryFrame)dialogBox).Activity(secondDifference);

    [Fact]
    public void Constructor_ShouldWireUpDialogBoxToVisual()
    {
        var (dialogBox, visual) = CreateDialogBox();

        dialogBox.ShouldNotBeNull();
        dialogBox.Visual.ShouldBe(visual);
        visual.TextInstance.ShouldNotBeNull();
        visual.ContinueIndicatorInstance.ShouldNotBeNull();
        visual.ContinueIndicatorInstance.Visible.ShouldBeFalse();
    }

    [Fact]
    public void Show_WithLettersPerSecond_ShouldStartAtZeroLetters()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 10;

        dialogBox.Show("hello");

        visual.TextInstance.MaxLettersToShow.ShouldBe(0);
        visual.ContinueIndicatorInstance.Visible.ShouldBeFalse();
    }

    [Fact]
    public void Show_WithZeroLettersPerSecond_ShouldShowAllLettersImmediately()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 0;

        dialogBox.Show("hello");

        visual.TextInstance.MaxLettersToShow.ShouldBe(5);
    }

    [Fact]
    public void Activity_ShouldAdvanceMaxLettersToShow_ProportionalToElapsedTime()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 10;
        dialogBox.Show("hello world");

        Tick(dialogBox, 0.5);

        visual.TextInstance.MaxLettersToShow.ShouldBe(5);
    }

    [Fact]
    public void Activity_ShouldFireFinishedTypingPage_WhenAllLettersRevealed()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 10;
        int finishedFireCount = 0;
        dialogBox.FinishedTypingPage += (_, _) => finishedFireCount++;

        dialogBox.Show("hello");
        Tick(dialogBox, 1.0);

        visual.TextInstance.MaxLettersToShow.ShouldBe(5);
        finishedFireCount.ShouldBe(1);
        visual.ContinueIndicatorInstance.Visible.ShouldBeTrue();
    }

    [Fact]
    public void Activity_ShouldNotFireFinishedTypingPageMultipleTimes()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 10;
        int finishedFireCount = 0;
        dialogBox.FinishedTypingPage += (_, _) => finishedFireCount++;

        dialogBox.Show("hello");
        Tick(dialogBox, 1.0);
        Tick(dialogBox, 1.0);
        Tick(dialogBox, 1.0);

        finishedFireCount.ShouldBe(1);
    }

    [Fact]
    public void Activity_BeforeShow_ShouldBeNoOp()
    {
        var (dialogBox, visual) = CreateDialogBox();
        int finishedFireCount = 0;
        dialogBox.FinishedTypingPage += (_, _) => finishedFireCount++;

        Tick(dialogBox, 1.0);

        finishedFireCount.ShouldBe(0);
    }

    [Fact]
    public void Activity_ShouldClampMaxLettersToShow_AtTargetCount()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 1000;
        dialogBox.Show("hi");

        Tick(dialogBox, 10.0);

        visual.TextInstance.MaxLettersToShow.ShouldBe(2);
    }

    [Fact]
    public void Activity_OnTwoDialogBoxes_ShouldAdvanceIndependently()
    {
        var (dialogBoxA, visualA) = CreateDialogBox();
        var (dialogBoxB, visualB) = CreateDialogBox();
        dialogBoxA.LettersPerSecond = 10;
        dialogBoxB.LettersPerSecond = 10;

        dialogBoxA.Show("hello");
        dialogBoxB.Show("hello");
        Tick(dialogBoxA, 0.3);

        visualA.TextInstance.MaxLettersToShow.ShouldBe(3);
        visualB.TextInstance.MaxLettersToShow.ShouldBe(0);
    }

    [Fact]
    public void AnimateSelf_OnVisual_ShouldDispatchToActivity()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 10;
        dialogBox.Show("hello");

        visual.AnimateSelf(0.5);

        visual.TextInstance.MaxLettersToShow.ShouldBe(5);
    }

    const string LongText =
        "This is sentence one of a fairly long passage. " +
        "This is sentence two which keeps going on and on. " +
        "Sentence three adds even more text to push past the height. " +
        "Sentence four ensures we definitely overflow the visible area. " +
        "And sentence five is here to be very sure of pagination.";

    [Fact]
    public void Show_String_ShouldSplitLongTextAcrossMultiplePages()
    {
        var (dialogBox, _) = CreateDialogBox(paginating: true);
        dialogBox.LettersPerSecond = 0;

        dialogBox.Show(LongText);

        // First page is shown (popped); remaining pages should still be queued.
        dialogBox.PagesRemaining.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Show_Enumerable_ShouldSplitEachLongEntryAcrossMultiplePages()
    {
        var (dialogBoxA, _) = CreateDialogBox(paginating: true);
        var (dialogBoxB, _) = CreateDialogBox(paginating: true);
        dialogBoxA.LettersPerSecond = 0;
        dialogBoxB.LettersPerSecond = 0;

        // Two long entries should produce strictly more pages than one long entry —
        // confirming each entry runs through ConvertToPages, not just appended verbatim.
        dialogBoxA.Show(new[] { LongText });
        dialogBoxB.Show(new[] { LongText, LongText });

        dialogBoxB.PagesRemaining.ShouldBeGreaterThan(dialogBoxA.PagesRemaining);
    }

    [Fact]
    public void Show_String_AndShow_SingleEntryArray_ShouldProduceSamePageCount()
    {
        var (dialogBoxA, _) = CreateDialogBox(paginating: true);
        var (dialogBoxB, _) = CreateDialogBox(paginating: true);
        dialogBoxA.LettersPerSecond = 0;
        dialogBoxB.LettersPerSecond = 0;

        dialogBoxA.Show(LongText);
        dialogBoxB.Show(new[] { LongText });

        dialogBoxA.PagesRemaining.ShouldBe(dialogBoxB.PagesRemaining);
    }

    [Fact]
    public void Show_Enumerable_TwoIdenticalLongEntries_ShouldProduceTwiceTheSplitPagesOfOne()
    {
        var (dialogBoxA, _) = CreateDialogBox(paginating: true);
        var (dialogBoxB, _) = CreateDialogBox(paginating: true);
        dialogBoxA.LettersPerSecond = 0;
        dialogBoxB.LettersPerSecond = 0;

        dialogBoxA.Show(new[] { LongText });
        dialogBoxB.Show(new[] { LongText, LongText });

        // PagesRemaining = pages queued AFTER ShowNextPage popped one,
        // so the two-entry case should have (2 * (A.PagesRemaining + 1)) - 1
        // = 2 * A.PagesRemaining + 1 pages remaining.
        dialogBoxB.PagesRemaining.ShouldBe(2 * dialogBoxA.PagesRemaining + 1);
    }

    [Fact]
    public void Show_Enumerable_ShouldKeepShortEntriesAsLiteralPages()
    {
        var (dialogBox, _) = CreateDialogBox(paginating: true);
        dialogBox.LettersPerSecond = 0;

        dialogBox.Show(new[] { "one", "two", "three" });

        // Three short entries -> three pages, first popped -> two remaining.
        dialogBox.PagesRemaining.ShouldBe(2);
    }
}
