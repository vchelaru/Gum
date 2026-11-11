using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Forms.Controls.Games;
using RenderingLibrary;
using Gum.Wireframe;
using Gum.GueDeriving;
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
    public void OnFocusUpdate_DisabledFocused_GamepadBDoesNotRevealText()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.Show("hello");
        dialogBox.IsEnabled = false;
        dialogBox.AllowFocusWhenDisabled = true;
        dialogBox.IsFocused = true;
        var gamepad = new Gum.Input.GamePad();
        gamepad.SetButtonState(Gum.Input.GamepadButton.B, true);
        gamepad.Activity(1);
        Gum.Forms.Controls.FrameworkElement.GamePadsForUiControl.Add(gamepad);

        ((Gum.Wireframe.IInputReceiver)dialogBox).OnFocusUpdate();

        visual.TextInstance.MaxLettersToShow.ShouldNotBe(5);
    }

    [Fact]
    public void OnFocusUpdate_DisabledFocused_AdvancePredicateIsNotConsulted()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.Show("hello");
        dialogBox.IsEnabled = false;
        dialogBox.AllowFocusWhenDisabled = true;
        dialogBox.IsFocused = true;
        bool consulted = false;
        dialogBox.AdvancePageInputPredicate = () => { consulted = true; return true; };

        ((Gum.Wireframe.IInputReceiver)dialogBox).OnFocusUpdate();

        consulted.ShouldBeFalse();
    }

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
    public void Show_ShouldThrowDescriptiveException_WhenVisualHasNoTextInstance()
    {
        InteractiveGue visual = new(new InvisibleRenderable());
        DialogBox dialogBox = new(visual);

        System.InvalidOperationException exception =
            Should.Throw<System.InvalidOperationException>(() => dialogBox.Show("Hello"));

        exception.Message.ShouldContain("TextInstance");
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
    public void Show_WithGlyphTag_ShouldCountGlyphAsOneLetter()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 0;

        dialogBox.Show("A[Glyph=0xE000]B");

        visual.TextInstance.MaxLettersToShow.ShouldBe(3);
    }

    [Fact]
    public void ShowAsync_WithGlyphTagInDialogPageTask_ShouldCountGlyphAsOneLetter()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 0;

        // The first page is shown before the first await, so the task is left running.
        _ = dialogBox.ShowAsync(new DialogPageTask[] { "A[Glyph=0x1F600]B" });

        visual.TextInstance.MaxLettersToShow.ShouldBe(4);
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

    [Fact]
    public void Dismiss_ShouldClearPagesAndDropFocus_BeforeFinishedShowingFires()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 0;
        dialogBox.IsFocused = true;

        dialogBox.Show(new[] { "one", "two", "three" });
        dialogBox.PagesRemaining.ShouldBeGreaterThan(0); // sanity: pages queued

        bool sawFinishedShowing = false;
        dialogBox.FinishedShowing += (_, _) =>
        {
            sawFinishedShowing = true;
            dialogBox.PagesRemaining.ShouldBe(0,
                "Dismiss must clear pages before raising FinishedShowing");
            dialogBox.IsFocused.ShouldBeFalse(
                "Dismiss must drop focus before raising FinishedShowing");
        };

        dialogBox.Dismiss();

        sawFinishedShowing.ShouldBeTrue();
    }

    [Fact]
    public void FinishedShowingHandler_ReShow_ShouldSurviveDismissCleanup()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 0;

        dialogBox.Show("first");
        dialogBox.FinishedShowing += (_, _) =>
        {
            // Typical chained-dialog usage: show the next dialog from the handler.
            dialogBox.Show(new[] { "second A", "second B" });
            dialogBox.IsFocused = true;
        };

        dialogBox.Dismiss();

        // The re-shown dialog must not be wiped by Dismiss()'s post-event cleanup.
        dialogBox.IsVisible.ShouldBeTrue();
        dialogBox.IsFocused.ShouldBeTrue();
        dialogBox.PagesRemaining.ShouldBe(1); // two pages, first popped, rest must survive
    }

    [Fact]
    public void OnFocusUpdate_ShouldRaiseFocusUpdate()
    {
        (DialogBox dialogBox, TestDialogBoxVisual _) = CreateDialogBox();
        bool wasRaised = false;
        dialogBox.FocusUpdate += _ => wasRaised = true;

        dialogBox.OnFocusUpdate();

        wasRaised.ShouldBeTrue();
    }

    [Fact]
    public void Visual_Reassigned_ShouldIgnoreOldVisualClick()
    {
        var (dialogBox, oldVisual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 10;
        InteractiveGue newVisual = new(new InvisibleRenderable());
        TextRuntime newText = new() { Name = "TextInstance", Text = string.Empty };
        newVisual.AddChild(newText);
        dialogBox.Visual = newVisual;
        dialogBox.Show("hello world");

        oldVisual.CallClick();

        newText.MaxLettersToShow.ShouldBe(0, "because only the current visual should advance the dialog");
    }

    private static List<IRenderableIpso> RecordLayerRegistrations()
    {
        List<IRenderableIpso> registered = new();
        GraphicalUiElement.AddRenderableToManagers = (renderable, managers, layer) => registered.Add(renderable);
        return registered;
    }

    [Fact]
    public void Show_WhenVisualIsInsideManagedContainer_ShouldNotRegisterRenderableAsTopLevel()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();
        ContainerRuntime container = new();
        Gum.GumService.Default.Root.AddChild(container);
        container.AddChild(visual);
        container.EffectiveManagers.ShouldNotBeNull("the container is under the managed Root");

        dialogBox.Show("hello");

        registered.ShouldNotContain((IRenderableIpso)visual.RenderableComponent,
            "because the dialog is already drawn through its container and a second top-level item draws it twice");
        visual.Parent.ShouldBe(container);
    }

    [Fact]
    public void ShowAsync_WhenVisualIsInsideManagedContainer_ShouldNotRegisterRenderableAsTopLevel()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();
        ContainerRuntime container = new();
        Gum.GumService.Default.Root.AddChild(container);
        container.AddChild(visual);

        _ = dialogBox.ShowAsync("hello");

        registered.ShouldNotContain((IRenderableIpso)visual.RenderableComponent);
    }

    [Fact]
    public void Show_WhenVisualHasNoParent_ShouldAddToRootWithoutRegisteringTopLevelRenderable()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();

        dialogBox.Show("hello");

        visual.Parent.ShouldBe(Gum.GumService.Default.Root);
        registered.ShouldNotContain((IRenderableIpso)visual.RenderableComponent);
    }

    [Fact]
    public void Show_WhenNoAncestorIsManaged_ShouldStillRegisterRenderableSoItDraws()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();
        ContainerRuntime detachedContainer = new();
        detachedContainer.AddChild(visual);

        dialogBox.Show("hello");

        registered.ShouldContain((IRenderableIpso)visual.RenderableComponent);
    }

    // What the renderer would draw this frame: Root plus every renderable Show put on a layer.
    private static int CountDrawsOfDialogText(List<IRenderableIpso> registered, TestDialogBoxVisual visual)
    {
        Layer layer = new();
        layer.Add((IRenderableIpso)Gum.GumService.Default.Root.RenderableComponent);
        foreach (IRenderableIpso renderable in registered)
        {
            layer.Add(renderable);
        }
        List<DrawCommand> commands = new();
        HierarchicalOrderer.Instance.BuildDrawList(layer, commands);
        return commands.Count(command =>
            command.Kind == DrawCommandKind.DrawRenderable && ReferenceEquals(command.Target, visual.TextInstance));
    }

    [Fact]
    public void Show_WithNoParent_ShouldDrawDialogTextOnce()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();

        dialogBox.Show("hello");

        CountDrawsOfDialogText(registered, visual).ShouldBe(1);
    }

    [Fact]
    public void Show_InsideManagedContainer_ShouldDrawDialogTextOnce()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();
        ContainerRuntime container = new();
        Gum.GumService.Default.Root.AddChild(container);
        container.AddChild(visual);

        dialogBox.Show("hello");

        CountDrawsOfDialogText(registered, visual).ShouldBe(1);
    }

    [Fact]
    public void Show_DeeplyNestedInManagedContainers_ShouldDrawDialogTextOnce()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();
        ContainerRuntime outer = new();
        ContainerRuntime middle = new();
        ContainerRuntime inner = new();
        Gum.GumService.Default.Root.AddChild(outer);
        outer.AddChild(middle);
        middle.AddChild(inner);
        inner.AddChild(visual);

        dialogBox.Show("hello");

        CountDrawsOfDialogText(registered, visual).ShouldBe(1);
    }

    [Fact]
    public void ShowAsync_WithPageTasks_InsideManagedContainer_ShouldNotRegisterRenderableAsTopLevel()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();
        ContainerRuntime container = new();
        Gum.GumService.Default.Root.AddChild(container);
        container.AddChild(visual);

        _ = dialogBox.ShowAsync(new DialogPageTask[] { "hello" });

        registered.ShouldNotContain((IRenderableIpso)visual.RenderableComponent);
    }

    [Fact]
    public void ShowAsync_WithStringPages_InsideManagedContainer_ShouldNotRegisterRenderableAsTopLevel()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();
        ContainerRuntime container = new();
        Gum.GumService.Default.Root.AddChild(container);
        container.AddChild(visual);

        _ = dialogBox.ShowAsync(new[] { "hello", "world" });

        registered.ShouldNotContain((IRenderableIpso)visual.RenderableComponent);
    }

    [Fact]
    public void ShowAsync_WithNoParent_ShouldAddToRootWithoutRegisteringTopLevelRenderable()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();

        _ = dialogBox.ShowAsync("hello");

        visual.Parent.ShouldBe(Gum.GumService.Default.Root);
        registered.ShouldNotContain((IRenderableIpso)visual.RenderableComponent);
    }

    [Fact]
    public void Show_InsideContainerAddedToManagersOnCustomLayer_ShouldNotRegisterOnDefaultLayer()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();
        ContainerRuntime container = new();
        container.AddToManagers(SystemManagers.Default, new Layer());
        container.AddChild(visual);

        dialogBox.Show("hello");

        registered.ShouldNotContain((IRenderableIpso)visual.RenderableComponent,
            "because the container, on the user's own layer, already draws the dialog");
        visual.Parent.ShouldBe(container);
    }

    [Theory]
    [InlineData("PopupRoot")]
    [InlineData("ModalRoot")]
    public void Show_InsidePopupOrModalRoot_ShouldNotRegisterRenderableAsTopLevel(string rootName)
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();
        InteractiveGue root = rootName == "PopupRoot" ? Gum.GumService.Default.PopupRoot : Gum.GumService.Default.ModalRoot;
        root.AddChild(visual);

        dialogBox.Show("hello");

        registered.ShouldNotContain((IRenderableIpso)visual.RenderableComponent);
        visual.Parent.ShouldBe(root);
    }

    [Fact]
    public void Show_WithLayerArgument_WhenAncestorIsManaged_ShouldNotRegisterOnThatLayer()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();
        ContainerRuntime container = new();
        Gum.GumService.Default.Root.AddChild(container);
        container.AddChild(visual);

        dialogBox.Show("hello", new Layer());

        registered.ShouldNotContain((IRenderableIpso)visual.RenderableComponent);
    }

    [Fact]
    public void Show_CalledTwice_ShouldAddVisualToRootOnce()
    {
        var (dialogBox, visual) = CreateDialogBox();

        dialogBox.Show("first");
        dialogBox.Show("second");

        Gum.GumService.Default.Root.Children.Count(child => child == visual).ShouldBe(1);
    }

    [Fact]
    public void Show_CalledTwiceWithNoManagedAncestor_ShouldRegisterRenderableOnce()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();
        ContainerRuntime detachedContainer = new();
        detachedContainer.AddChild(visual);

        dialogBox.Show("first");
        dialogBox.Show("second");

        registered.Count(renderable => ReferenceEquals(renderable, visual.RenderableComponent)).ShouldBe(1);
    }

    [Fact]
    public void Show_AfterVisualRemovedFromRoot_ShouldAddItBackToRootOnce()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.Show("first");
        visual.Parent = null;

        dialogBox.Show("second");

        Gum.GumService.Default.Root.Children.Count(child => child == visual).ShouldBe(1);
    }

    [Fact]
    public void Show_ThenVisualMovedToManagedContainer_ShouldDrawDialogTextOnce()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.Show("hello");
        ContainerRuntime container = new();
        Gum.GumService.Default.Root.AddChild(container);

        container.AddChild(visual);

        Gum.GumService.Default.Root.Children.ShouldNotContain(visual);
        CountDrawsOfDialogText(registered, visual).ShouldBe(1);
    }

    [Fact]
    public void Show_AfterDismiss_ShouldBeVisibleAgain()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.Show("first");
        dialogBox.Dismiss();
        dialogBox.IsVisible.ShouldBeFalse();

        dialogBox.Show("second");

        dialogBox.IsVisible.ShouldBeTrue();
        Gum.GumService.Default.Root.Children.Count(child => child == visual).ShouldBe(1);
    }

    [Fact]
    public void Show_AfterClose_ShouldStillDrawDialogTextOnce()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogBox, visual) = CreateDialogBox();
        ContainerRuntime container = new();
        Gum.GumService.Default.Root.AddChild(container);
        container.AddChild(visual);
        dialogBox.Show("first");
        dialogBox.Close();

        dialogBox.Show("second");

        CountDrawsOfDialogText(registered, visual).ShouldBe(1);
    }

    [Fact]
    public void Show_TwoDialogsInSameManagedContainer_ShouldRegisterNeitherAsTopLevel()
    {
        List<IRenderableIpso> registered = RecordLayerRegistrations();
        var (dialogA, visualA) = CreateDialogBox();
        var (dialogB, visualB) = CreateDialogBox();
        ContainerRuntime container = new();
        Gum.GumService.Default.Root.AddChild(container);
        container.AddChild(visualA);
        container.AddChild(visualB);

        dialogA.Show("a");
        dialogB.Show("b");

        registered.ShouldNotContain((IRenderableIpso)visualA.RenderableComponent);
        registered.ShouldNotContain((IRenderableIpso)visualB.RenderableComponent);
    }

    private static Gum.Localization.LocalizationService CreateLocalization(string key, string translation)
    {
        Gum.Localization.LocalizationService localizationService = new();
        localizationService.AddDatabase(
            new Dictionary<string, string[]> { { key, new[] { translation } } },
            new List<string> { "English" });
        localizationService.CurrentLanguage = 0;
        return localizationService;
    }

    [Fact]
    public void Show_WhenTranslationIsLongerThanKey_ShouldTypeTheWholeTranslation()
    {
        const string translation = "Hello there, traveler! Welcome to the village.";
        CustomSetPropertyOnRenderable.LocalizationService = CreateLocalization("Greeting", translation);
        try
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show("Greeting");

            Tick(dialogBox, 1000);

            ((Text)visual.TextInstance.RenderableComponent).RawText.ShouldBe(translation);
            (visual.TextInstance.MaxLettersToShow ?? 0).ShouldBeGreaterThanOrEqualTo(translation.Length,
                "because every letter of the translated text should be revealed, not just as many as the key has");
        }
        finally
        {
            CustomSetPropertyOnRenderable.LocalizationService = null;
        }
    }

    [Fact]
    public void Show_WithImmediatePrintAndLongerTranslation_ShouldRevealTheWholeTranslation()
    {
        const string translation = "Hello there, traveler! Welcome to the village.";
        CustomSetPropertyOnRenderable.LocalizationService = CreateLocalization("Greeting", translation);
        try
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 0;

            dialogBox.Show("Greeting");

            (visual.TextInstance.MaxLettersToShow ?? 0).ShouldBeGreaterThanOrEqualTo(translation.Length);
        }
        finally
        {
            CustomSetPropertyOnRenderable.LocalizationService = null;
        }
    }

    [Fact]
    public void Click_WhileTypingLongerTranslation_ShouldRevealTheWholeTranslation()
    {
        const string translation = "Hello there, traveler! Welcome to the village.";
        CustomSetPropertyOnRenderable.LocalizationService = CreateLocalization("Greeting", translation);
        try
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show("Greeting");

            visual.CallClick();

            (visual.TextInstance.MaxLettersToShow ?? 0).ShouldBeGreaterThanOrEqualTo(translation.Length,
                "because a click while typing skips to the end of the translated page");
        }
        finally
        {
            CustomSetPropertyOnRenderable.LocalizationService = null;
        }
    }

    [Fact]
    public void Click_WhenTranslationIsLongerThanKeyAndTypingIsDone_ShouldAdvanceToNextPage()
    {
        CustomSetPropertyOnRenderable.LocalizationService =
            CreateLocalization("Greeting", "Hello there, traveler! Welcome to the village.");
        try
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show(new[] { "Greeting", "Second page" });
            dialogBox.PagesRemaining.ShouldBe(1);
            Tick(dialogBox, 1000);

            visual.CallClick();

            dialogBox.PagesRemaining.ShouldBe(0, "because a click on a fully typed page shows the next page");
        }
        finally
        {
            CustomSetPropertyOnRenderable.LocalizationService = null;
        }
    }

    [Fact]
    public void Click_WhenTranslationIsShorterThanKeyAndTypingIsDone_ShouldAdvanceToNextPage()
    {
        CustomSetPropertyOnRenderable.LocalizationService =
            CreateLocalization("Greeting_Long_Key_Name", "Hi");
        try
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show(new[] { "Greeting_Long_Key_Name", "Second page" });
            Tick(dialogBox, 1000);

            visual.CallClick();

            dialogBox.PagesRemaining.ShouldBe(0);
        }
        finally
        {
            CustomSetPropertyOnRenderable.LocalizationService = null;
        }
    }

    private const string LongTranslation = "Hello there, traveler! Welcome to the village.";

    private static Gum.Localization.LocalizationService CreateLocalization(
        Dictionary<string, string[]> entries, params string[] languages)
    {
        Gum.Localization.LocalizationService localizationService = new();
        localizationService.AddDatabase(entries, new List<string>(languages));
        localizationService.CurrentLanguage = 0;
        return localizationService;
    }

    private static void WithLocalization(Gum.Localization.LocalizationService service, System.Action action)
    {
        CustomSetPropertyOnRenderable.LocalizationService = service;
        try
        {
            action();
        }
        finally
        {
            CustomSetPropertyOnRenderable.LocalizationService = null;
        }
    }

    private static string RawTextOf(TestDialogBoxVisual visual) =>
        ((Text)visual.TextInstance.RenderableComponent).RawText ?? string.Empty;

    [Fact]
    public void LanguageChanged_WhileTyping_ShouldRevealTheWholeNewTranslation()
    {
        WithLocalization(CreateLocalization(
            new Dictionary<string, string[]> { { "Greeting", new[] { "Hi", LongTranslation } } },
            "English", "Spanish"), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show("Greeting");
            Tick(dialogBox, 0.1);

            CustomSetPropertyOnRenderable.LocalizationService!.CurrentLanguage = 1;
            RawTextOf(visual).ShouldBe(LongTranslation, "the live dialog text re-translates on a language change");
            Tick(dialogBox, 1000);

            (visual.TextInstance.MaxLettersToShow ?? 0).ShouldBeGreaterThanOrEqualTo(LongTranslation.Length);
        });
    }

    [Fact]
    public void LanguageChanged_WhileTyping_ShouldFireFinishedTypingPageOnceAndShowContinueIndicatorAtTheNewEnd()
    {
        WithLocalization(CreateLocalization(
            new Dictionary<string, string[]> { { "Greeting", new[] { "Hi", LongTranslation } } },
            "English", "Spanish"), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            int finishedCount = 0;
            dialogBox.FinishedTypingPage += (_, _) => finishedCount++;
            dialogBox.Show("Greeting");
            Tick(dialogBox, 0.1);
            CustomSetPropertyOnRenderable.LocalizationService!.CurrentLanguage = 1;

            Tick(dialogBox, 1.0);
            finishedCount.ShouldBe(0, "only 10 of the 46 new letters are revealed");
            visual.ContinueIndicatorInstance.Visible.ShouldBeFalse();
            Tick(dialogBox, 1000);
            Tick(dialogBox, 1000);

            finishedCount.ShouldBe(1);
            visual.ContinueIndicatorInstance.Visible.ShouldBeTrue();
        });
    }

    [Fact]
    public void LanguageChanged_ToShorterTextWhileTyping_ShouldFinishAtTheNewLength()
    {
        WithLocalization(CreateLocalization(
            new Dictionary<string, string[]> { { "Greeting", new[] { LongTranslation, "Hi" } } },
            "English", "Spanish"), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            int finishedCount = 0;
            dialogBox.FinishedTypingPage += (_, _) => finishedCount++;
            dialogBox.Show(new[] { "Greeting", "Second" });
            Tick(dialogBox, 0.5);
            CustomSetPropertyOnRenderable.LocalizationService!.CurrentLanguage = 1;

            Tick(dialogBox, 0.1);

            finishedCount.ShouldBe(1, "the 2 letter text is fully revealed, not waiting for the old 46 letters");
            visual.TextInstance.MaxLettersToShow.ShouldBe(2);
            visual.ContinueIndicatorInstance.Visible.ShouldBeTrue();
            Tick(dialogBox, 1000);
            finishedCount.ShouldBe(1);
            visual.CallClick();
            dialogBox.PagesRemaining.ShouldBe(0, "the first click advances since nothing is left to reveal");
        });
    }

    [Fact]
    public void LanguageChanged_AfterTypingFinished_ShouldNotRestartTypingAndShouldNeedOneClickToRevealThenOneToAdvance()
    {
        WithLocalization(CreateLocalization(
            new Dictionary<string, string[]> { { "Greeting", new[] { "Hi", LongTranslation } } },
            "English", "Spanish"), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            int finishedCount = 0;
            dialogBox.FinishedTypingPage += (_, _) => finishedCount++;
            dialogBox.Show(new[] { "Greeting", "Second" });
            Tick(dialogBox, 1000);
            finishedCount.ShouldBe(1);
            CustomSetPropertyOnRenderable.LocalizationService!.CurrentLanguage = 1;
            Tick(dialogBox, 1000);

            finishedCount.ShouldBe(1, "a finished page does not start typing again");
            visual.CallClick();
            (visual.TextInstance.MaxLettersToShow ?? 0).ShouldBeGreaterThanOrEqualTo(LongTranslation.Length);
            dialogBox.PagesRemaining.ShouldBe(1, "the first click only reveals the rest of the new text");
            visual.CallClick();

            dialogBox.PagesRemaining.ShouldBe(0);
        });
    }

    [Fact]
    public void LanguageChanged_OnLastPageWhileTyping_ShouldRevealTheNewTextThenDismissOnClick()
    {
        WithLocalization(CreateLocalization(
            new Dictionary<string, string[]> { { "Greeting", new[] { "Hi", LongTranslation } } },
            "English", "Spanish"), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            int finishedShowingCount = 0;
            dialogBox.FinishedShowing += (_, _) => finishedShowingCount++;
            dialogBox.Show("Greeting");
            dialogBox.PagesRemaining.ShouldBe(0);
            Tick(dialogBox, 0.1);
            CustomSetPropertyOnRenderable.LocalizationService!.CurrentLanguage = 1;

            Tick(dialogBox, 1000);
            (visual.TextInstance.MaxLettersToShow ?? 0).ShouldBeGreaterThanOrEqualTo(LongTranslation.Length);
            finishedShowingCount.ShouldBe(0, "the page is revealed but not yet dismissed");
            visual.CallClick();

            finishedShowingCount.ShouldBe(1);
            dialogBox.IsVisible.ShouldBeFalse();
        });
    }

    #region Pagination of translated text (#7896)

    // Tagged so BBCode has to survive the split: the Color run straddles a page break.
    private const string TaggedLongText =
        "This is sentence one of a fairly long passage. " +
        "[Color=Red]This is sentence two which keeps going on and on. " +
        "Sentence three adds even more text to push past the height.[/Color] " +
        "Sentence four ensures we definitely overflow the visible area. " +
        "And sentence five is here to be very sure of pagination[Glyph=0xE000]";

    // Every page the dialog shows, in order, as the text it holds (tags stripped).
    private static List<string> CollectShownPages(DialogBox dialogBox, TestDialogBoxVisual visual)
    {
        List<string> shown = new List<string> { RawTextOf(visual) };
        while (dialogBox.PagesRemaining > 0)
        {
            visual.CallClick();
            shown.Add(RawTextOf(visual));
        }
        return shown;
    }

    // What the dialog shows for this text when no localization service is involved.
    private static List<string> ShownPagesWithoutLocalization(string text)
    {
        var (dialogBox, visual) = CreateDialogBox(paginating: true);
        dialogBox.LettersPerSecond = 0;
        dialogBox.Show(text);
        return CollectShownPages(dialogBox, visual);
    }

    [Fact]
    public void Show_WithTranslationThatOverflowsThePage_ShouldSplitIntoMorePages()
    {
        WithLocalization(CreateLocalization("Greeting", LongText), () =>
        {
            var (dialogBox, _) = CreateDialogBox(paginating: true);
            dialogBox.LettersPerSecond = 0;

            dialogBox.Show("Greeting");

            dialogBox.PagesRemaining.ShouldBeGreaterThan(0,
                "because the translated text, not the short key, is what has to fit on a page");
        });
    }

    [Fact]
    public void Show_WithTranslationThatOverflowsThePage_ShouldShowEachPieceExactlyAndLeaveOutLocSuffix()
    {
        List<string> expected = ShownPagesWithoutLocalization(LongText);
        expected.Count.ShouldBeGreaterThan(1, "sanity: the text must overflow");

        WithLocalization(CreateLocalization("Greeting", LongText), () =>
        {
            var (dialogBox, visual) = CreateDialogBox(paginating: true);
            dialogBox.LettersPerSecond = 0;

            dialogBox.Show("Greeting");
            List<string> shown = CollectShownPages(dialogBox, visual);

            shown.ShouldBe(expected);
            shown.ShouldAllBe(piece => !piece.Contains("(loc)"));
        });
    }

    [Fact]
    public void Show_WithTranslationThatOverflowsThePage_ShouldCountPagesRemainingExactly()
    {
        int expectedRemaining = ShownPagesWithoutLocalization(LongText).Count - 1;

        WithLocalization(CreateLocalization("Greeting", LongText), () =>
        {
            var (dialogBox, _) = CreateDialogBox(paginating: true);
            dialogBox.LettersPerSecond = 0;

            dialogBox.Show("Greeting");

            dialogBox.PagesRemaining.ShouldBe(expectedRemaining);
        });
    }

    [Fact]
    public void Show_WithTranslationThatFitsThePage_ShouldStayOnePage()
    {
        WithLocalization(CreateLocalization("Greeting", "Hello there."), () =>
        {
            var (dialogBox, visual) = CreateDialogBox(paginating: true);
            dialogBox.LettersPerSecond = 0;

            dialogBox.Show("Greeting");

            dialogBox.PagesRemaining.ShouldBe(0);
            RawTextOf(visual).ShouldBe("Hello there.");
        });
    }

    [Fact]
    public void Show_WithLocalizeTextFalse_ShouldKeepTheLiteralKeyOnOnePage()
    {
        WithLocalization(CreateLocalization("Greeting", LongText), () =>
        {
            var (dialogBox, visual) = CreateDialogBox(paginating: true);
            visual.TextInstance.LocalizeText = false;
            dialogBox.LettersPerSecond = 0;

            dialogBox.Show("Greeting");

            dialogBox.PagesRemaining.ShouldBe(0, "text that does not localize is never translated, so it never overflows");
            RawTextOf(visual).ShouldBe("Greeting");
        });
    }

    [Fact]
    public void Show_WithTaggedTranslationThatOverflows_ShouldSplitAndCountLikeTheSameTextWithoutLocalization()
    {
        List<string> expected = ShownPagesWithoutLocalization(TaggedLongText);
        expected.Count.ShouldBeGreaterThan(1, "sanity: the text must overflow");

        WithLocalization(CreateLocalization("Greeting", TaggedLongText), () =>
        {
            var (dialogBox, visual) = CreateDialogBox(paginating: true);
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show("Greeting");

            int firstPageLength = RawTextOf(visual).Length;
            Tick(dialogBox, 1000);
            visual.TextInstance.MaxLettersToShow.ShouldBe(firstPageLength,
                "tags are not counted as letters, so the whole first page is revealed");

            dialogBox.LettersPerSecond = 0;
            List<string> shown = CollectShownPages(dialogBox, visual);
            shown.ShouldBe(expected);
            shown.ShouldAllBe(piece => !piece.Contains("[") && !piece.Contains("(loc)"));
        });
    }

    [Fact]
    public void Show_WithMultiplePagesEachOverflowing_ShouldTranslateAndSplitEachOne()
    {
        var (referenceBox, referenceVisual) = CreateDialogBox(paginating: true);
        referenceBox.LettersPerSecond = 0;
        referenceBox.Show(new[] { LongText, TaggedLongText });
        List<string> expected = CollectShownPages(referenceBox, referenceVisual);

        WithLocalization(CreateLocalization(
            new Dictionary<string, string[]>
            {
                { "A", new[] { "A", LongText } },
                { "B", new[] { "B", TaggedLongText } },
            }, "English", "Spanish"), () =>
        {
            CustomSetPropertyOnRenderable.LocalizationService!.CurrentLanguage = 1;
            var (dialogBox, visual) = CreateDialogBox(paginating: true);
            dialogBox.LettersPerSecond = 0;

            dialogBox.Show(new[] { "A", "B" });

            CollectShownPages(dialogBox, visual).ShouldBe(expected);
        });
    }

    [Fact]
    public void Show_WithSplitTranslation_ThenLanguageChange_ShouldKeepThePiecesAlreadyBuilt()
    {
        List<string> expected = ShownPagesWithoutLocalization(LongText);

        WithLocalization(CreateLocalization(
            new Dictionary<string, string[]> { { "Greeting", new[] { "Greeting", LongText, "Otro texto" } } },
            "English", "Spanish", "Other"), () =>
        {
            CustomSetPropertyOnRenderable.LocalizationService!.CurrentLanguage = 1;
            var (dialogBox, visual) = CreateDialogBox(paginating: true);
            dialogBox.LettersPerSecond = 0;
            dialogBox.Show("Greeting");

            CustomSetPropertyOnRenderable.LocalizationService.CurrentLanguage = 2;

            CollectShownPages(dialogBox, visual).ShouldBe(expected,
                "pages split from the translation at Show time are not re-translated and never get a (loc) suffix");
        });
    }

    [Fact]
    public void ShowAsync_StringWithTranslationThatOverflows_ShouldSplitLikeShow()
    {
        int expectedRemaining = ShownPagesWithoutLocalization(LongText).Count - 1;

        WithLocalization(CreateLocalization("Greeting", LongText), () =>
        {
            var (dialogBox, visual) = CreateDialogBox(paginating: true);
            dialogBox.LettersPerSecond = 0;

            // The first page is shown before the first await, so the loop is left running.
            _ = dialogBox.ShowAsync("Greeting");

            dialogBox.PagesRemaining.ShouldBe(expectedRemaining);
            RawTextOf(visual).ShouldNotContain("(loc)");
        });
    }

    [Fact]
    public void ShowAsync_DialogPageTaskWithTranslatedPage_ShouldShowTheTranslationWithoutLocSuffix()
    {
        WithLocalization(CreateLocalization("Greeting", "Hello there."), () =>
        {
            var (dialogBox, visual) = CreateDialogBox(paginating: true);
            dialogBox.LettersPerSecond = 0;

            _ = dialogBox.ShowAsync(new DialogPageTask[] { "Greeting" });

            RawTextOf(visual).ShouldBe("Hello there.");
        });
    }

    #endregion

    #region Stale typing state (#7902)

    [Fact]
    public void Dismiss_WhileTyping_ShouldNotRaiseFinishedTypingPageOnLaterTick()
    {
        var (dialogBox, _) = CreateDialogBox();
        dialogBox.LettersPerSecond = 10;
        int finishedCount = 0;
        dialogBox.FinishedTypingPage += (_, _) => finishedCount++;
        dialogBox.Show("This text takes a while to type out.");
        Tick(dialogBox, 0.5);

        dialogBox.Dismiss();
        Tick(dialogBox, 1000);

        finishedCount.ShouldBe(0);
    }

    [Fact]
    public void Dismiss_WhileTyping_ShouldNotRevealMoreLettersOnLaterTick()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 10;
        dialogBox.Show("This text takes a while to type out.");
        Tick(dialogBox, 0.5);
        int revealedAtDismiss = visual.TextInstance.MaxLettersToShow ?? 0;

        dialogBox.Dismiss();
        Tick(dialogBox, 1000);

        visual.TextInstance.MaxLettersToShow.ShouldBe(revealedAtDismiss);
    }

    [Fact]
    public void Close_WhileTyping_ShouldNotRaiseFinishedTypingPageOnLaterTick()
    {
        var (dialogBox, _) = CreateDialogBox();
        dialogBox.LettersPerSecond = 10;
        int finishedCount = 0;
        dialogBox.FinishedTypingPage += (_, _) => finishedCount++;
        dialogBox.Show("This text takes a while to type out.");
        Tick(dialogBox, 0.5);

        dialogBox.Close();
        Tick(dialogBox, 1000);

        finishedCount.ShouldBe(0);
    }

    [Fact]
    public void Show_WhileTypingAnotherDialog_ShouldRestartTypingFromZero()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 10;
        dialogBox.Show("This text takes a while to type out.");
        Tick(dialogBox, 2);

        dialogBox.Show("Second text that is also long enough.");

        visual.TextInstance.MaxLettersToShow.ShouldBe(0);
    }

    [Fact]
    public void ShowAsync_AdvancingFromEmptyPageIntoTaskPage_ShouldNotRaiseFinishedTypingPageDuringTheTask()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 10;
        int finishedCount = 0;
        dialogBox.FinishedTypingPage += (_, _) => finishedCount++;
        System.Threading.Tasks.TaskCompletionSource<bool> taskGate = new();
        // An empty page has nothing left to reveal, so the first click advances before any tick.
        _ = dialogBox.ShowAsync(new DialogPageTask[]
        {
            string.Empty,
            new System.Func<System.Threading.Tasks.Task>(() => taskGate.Task),
        });

        visual.CallClick();
        Tick(dialogBox, 1000);

        finishedCount.ShouldBe(0, "the dialog is hidden while the task runs");
    }

    #endregion

    #region Localization behavior pinned by PR 7898

    [Fact]
    public void Show_WithMissingKeyInLoadedDatabase_ShouldRevealTheWholeSuffixedText()
    {
        // A loaded database turns an unknown key into "<key>(loc)", which is longer than the key.
        WithLocalization(CreateLocalization("Known", "Hi"), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show(new[] { "Unknown", "Second" });
            Tick(dialogBox, 1000);

            RawTextOf(visual).ShouldBe("Unknown(loc)");
            (visual.TextInstance.MaxLettersToShow ?? 0).ShouldBeGreaterThanOrEqualTo(RawTextOf(visual).Length);
            visual.CallClick();
            dialogBox.PagesRemaining.ShouldBe(0);
        });
    }

    [Fact]
    public void Click_WithEachPageTranslated_ShouldShowTheSecondTranslationAfterTheFirst()
    {
        WithLocalization(CreateLocalization(
            new Dictionary<string, string[]>
            {
                { "One", new[] { "First page text that is fairly long." } },
                { "Two", new[] { "Second page text." } },
            }, "English"), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show(new[] { "One", "Two" });
            RawTextOf(visual).ShouldBe("First page text that is fairly long.");
            Tick(dialogBox, 1000);

            visual.CallClick();

            RawTextOf(visual).ShouldBe("Second page text.");
        });
    }

    [Fact]
    public void Show_WithBbCodeInLongerTranslation_ShouldRevealAllVisibleLetters()
    {
        WithLocalization(CreateLocalization("Greeting", "Hello [Color=Red]there, traveler[/Color] and welcome!"), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show(new[] { "Greeting", "Second" });
            Tick(dialogBox, 1000);

            RawTextOf(visual).ShouldBe("Hello there, traveler and welcome!");
            (visual.TextInstance.MaxLettersToShow ?? 0).ShouldBeGreaterThanOrEqualTo(RawTextOf(visual).Length);
            visual.CallClick();
            dialogBox.PagesRemaining.ShouldBe(0);
        });
    }

    [Fact]
    public void Show_WithGlyphTagInTranslation_ShouldCountGlyphAsOneLetter()
    {
        WithLocalization(CreateLocalization("Greeting", "A[Glyph=0xE000]B"), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 0;

            dialogBox.Show("Greeting");

            visual.TextInstance.MaxLettersToShow.ShouldBe(3);
            RawTextOf(visual).ShouldNotContain("[Glyph");
        });
    }

    [Fact]
    public void LanguageChanged_AfterTypingFinished_ShouldNeedOneClickToRevealThenOneToAdvance()
    {
        WithLocalization(CreateLocalization(
            new Dictionary<string, string[]> { { "Greeting", new[] { "Hi", LongTranslation } } },
            "English", "Spanish"), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show(new[] { "Greeting", "Second" });
            Tick(dialogBox, 1000);
            CustomSetPropertyOnRenderable.LocalizationService!.CurrentLanguage = 1;

            visual.CallClick();
            (visual.TextInstance.MaxLettersToShow ?? 0).ShouldBeGreaterThanOrEqualTo(LongTranslation.Length);
            dialogBox.PagesRemaining.ShouldBe(1, "the first click only reveals the rest of the new text");
            visual.CallClick();

            dialogBox.PagesRemaining.ShouldBe(0);
        });
    }

    [Fact]
    public void LanguageChanged_ToShorterTextWhileTyping_ShouldLetAClickAdvance()
    {
        WithLocalization(CreateLocalization(
            new Dictionary<string, string[]> { { "Greeting", new[] { LongTranslation, "Hi" } } },
            "English", "Spanish"), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show(new[] { "Greeting", "Second" });
            Tick(dialogBox, 0.5);
            CustomSetPropertyOnRenderable.LocalizationService!.CurrentLanguage = 1;
            Tick(dialogBox, 1000);

            visual.CallClick();

            dialogBox.PagesRemaining.ShouldBe(0);
        });
    }

    [Fact]
    public void Show_WithEmptyTranslation_ShouldLetAClickAdvance()
    {
        WithLocalization(CreateLocalization("Empty", string.Empty), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show(new[] { "Empty", "Second" });
            Tick(dialogBox, 1000);

            visual.CallClick();

            dialogBox.PagesRemaining.ShouldBe(0);
        });
    }

    [Fact]
    public void Activity_WithLongerTranslation_ShouldFireFinishedTypingPageExactlyOnce()
    {
        WithLocalization(CreateLocalization("Greeting", LongTranslation), () =>
        {
            var (dialogBox, _) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            int finishedCount = 0;
            dialogBox.FinishedTypingPage += (_, _) => finishedCount++;
            dialogBox.Show("Greeting");

            Tick(dialogBox, 1000);
            Tick(dialogBox, 1000);

            finishedCount.ShouldBe(1);
        });
    }

    [Fact]
    public void Activity_WithLongerTranslation_ShouldShowContinueIndicatorOnlyWhenAllLettersAreRevealed()
    {
        WithLocalization(CreateLocalization("Greeting", LongTranslation), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show("Greeting");

            Tick(dialogBox, 1.0);
            visual.ContinueIndicatorInstance.Visible.ShouldBeFalse("only 10 of the 46 letters are revealed");
            Tick(dialogBox, 1000);

            visual.ContinueIndicatorInstance.Visible.ShouldBeTrue();
        });
    }

    [Fact]
    public void Activity_WithLongerTranslation_ShouldRevealLettersPastTheKeyLengthOverTime()
    {
        WithLocalization(CreateLocalization("Greeting", LongTranslation), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show("Greeting");

            Tick(dialogBox, 0.5);
            Tick(dialogBox, 0.5);

            visual.TextInstance.MaxLettersToShow.ShouldBe(10, "the key has 8 letters but the translation has 46");
        });
    }

    [Fact]
    public void ShowNextPage_WithImmediatePrint_ShouldRevealTheWholeTranslation()
    {
        WithLocalization(CreateLocalization("Greeting", LongTranslation), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show(new[] { "Greeting", "Greeting" });

            dialogBox.ShowNextPage(forceImmediatePrint: true);

            (visual.TextInstance.MaxLettersToShow ?? 0).ShouldBeGreaterThanOrEqualTo(LongTranslation.Length);
        });
    }

    [Fact]
    public void ShowAsync_WithLongerTranslation_ShouldRevealTheWholeTranslation()
    {
        WithLocalization(CreateLocalization("Greeting", LongTranslation), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            _ = dialogBox.ShowAsync("Greeting");

            Tick(dialogBox, 1000);

            (visual.TextInstance.MaxLettersToShow ?? 0).ShouldBeGreaterThanOrEqualTo(LongTranslation.Length);
        });
    }

    [Fact]
    public void Show_WithoutLocalizationService_ShouldRevealAllLettersAndAdvance()
    {
        var (dialogBox, visual) = CreateDialogBox();
        dialogBox.LettersPerSecond = 10;
        dialogBox.Show(new[] { "Greeting", "Second" });
        Tick(dialogBox, 1000);

        visual.CallClick();

        dialogBox.PagesRemaining.ShouldBe(0);
    }

    [Fact]
    public void Show_AfterDismissWithDifferentTranslation_ShouldUseTheNewLength()
    {
        WithLocalization(CreateLocalization(
            new Dictionary<string, string[]>
            {
                { "Long", new[] { LongTranslation } },
                { "Short", new[] { "Hi" } },
            }, "English"), () =>
        {
            var (dialogBox, visual) = CreateDialogBox();
            dialogBox.LettersPerSecond = 10;
            dialogBox.Show("Long");
            Tick(dialogBox, 1000);
            dialogBox.Dismiss();

            dialogBox.Show(new[] { "Short", "Second" });
            Tick(dialogBox, 1000);
            visual.CallClick();

            dialogBox.PagesRemaining.ShouldBe(0);
        });
    }

    #endregion
}

