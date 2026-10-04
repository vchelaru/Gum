using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using AvaloniaDataUi.Controls;
using Shouldly;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.DataUi;

/// <summary>
/// Each simple Avalonia editor against the shared <see cref="EditorFixture"/>: a value set through
/// the editor reaches the fixture, and a fixture change shows in the editor after a refresh.
/// </summary>
public class SimpleEditorTests
{
    [AvaloniaFact]
    public void TextBoxDisplay_ParsesTypedTextIntoTheMember_AndShowsRefreshedValues()
    {
        EditorFixture fixture = new EditorFixture();
        TextBoxDisplay display = new TextBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Number)) };
        display.TextBox.Text.ShouldBe("1");

        display.TextBox.Text = "12*2";
        display.TrySetValueOnInstance();

        fixture.Number.ShouldBe(24f);

        fixture.Number = 7;
        display.Refresh();
        display.TextBox.Text.ShouldBe("7");
    }

    [AvaloniaFact]
    public void TextBoxDisplay_LabelScrub_WritesIntermediateThenFullValues_ClampedToMin()
    {
        EditorFixture fixture = new EditorFixture { Count = 3 };
        TextBoxDisplay display = new TextBoxDisplay { MinValue = 0, InstanceMember = fixture.Member(nameof(EditorFixture.Count)) };

        display.BeginScrub();
        display.ApplyScrub(2);
        fixture.Count.ShouldBe(5);

        display.ApplyScrub(-20);
        display.EndScrub();
        fixture.Count.ShouldBe(0);
    }

    [AvaloniaFact]
    public void TextBoxDisplay_LabelScrub_StartsAnywhereInTheLabelColumn()
    {
        EditorFixture fixture = new EditorFixture { Count = 3 };
        TextBoxDisplay display = new TextBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Count)) };
        Window window = new Window { Content = display, Width = 400, Height = 200 };
        window.Show();
        window.UpdateLayout();
        // Right of the "Count" glyphs and above the vertically centered text: empty label-column space.
        Point press = display.TranslatePoint(new Point(95, 1), window)!.Value;

        window.MouseDown(press, MouseButton.Left);
        window.MouseMove(press + new Point(4, 0));
        window.MouseUp(press + new Point(4, 0), MouseButton.Left);

        fixture.Count.ShouldBe(7);
        window.InputHitTest(press).ShouldBeAssignableTo<InputElement>()!.Cursor.ShouldNotBeNull();
        window.Close();
    }

    [AvaloniaFact]
    public void TextBoxDisplay_LabelScrub_FromNull_CommitsFullOnlyOnRelease()
    {
        EditorFixture fixture = new EditorFixture { MaybeNumber = null };
        InstanceMember member = fixture.Member(nameof(EditorFixture.MaybeNumber));
        List<(object? Value, SetPropertyCommitType CommitType)> commits = new();
        member.CustomSetPropertyEvent += (_, args) =>
        {
            fixture.MaybeNumber = (float?)args.Value;
            commits.Add((args.Value, args.CommitType));
        };
        TextBoxDisplay display = new TextBoxDisplay { InstanceMember = member };
        Window window = new Window { Content = display, Width = 400, Height = 200 };
        window.Show();
        window.UpdateLayout();
        Point press = display.TranslatePoint(new Point(95, 1), window)!.Value;

        window.MouseDown(press, MouseButton.Left);
        window.MouseMove(press + new Point(4, 0));
        window.MouseUp(press + new Point(4, 0), MouseButton.Left);

        fixture.MaybeNumber.ShouldBe(4f);
        display.TextBox.IsEnabled.ShouldBeTrue();
        commits.Count(c => c.CommitType == SetPropertyCommitType.Full).ShouldBe(1);
        commits.Last().ShouldBe((4f, SetPropertyCommitType.Full));
        window.Close();
    }

    [AvaloniaFact]
    public void TextBoxDisplay_LosingFocus_AfterAScrub_DoesNotWriteTheScrubbedValueAgain()
    {
        // The scrub's release already committed. Leaving the field afterwards must not write the
        // shown text a second time: when the value changed meanwhile (an undo), that write reverts it.
        EditorFixture fixture = new EditorFixture { Count = 3 };
        TextBoxDisplay display = new TextBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Count)) };
        TextBox elsewhere = new TextBox();
        StackPanel panel = new StackPanel { Children = { display, elsewhere } };
        Window window = new Window { Content = panel, Width = 400, Height = 200 };
        window.Show();
        window.UpdateLayout();
        display.TextBox.Focus();
        Point press = display.TranslatePoint(new Point(95, 1), window)!.Value;
        window.MouseDown(press, MouseButton.Left);
        window.MouseMove(press + new Point(4, 0));
        window.MouseUp(press + new Point(4, 0), MouseButton.Left);
        fixture.Count.ShouldBe(7);

        fixture.Count = 3;
        elsewhere.Focus();

        fixture.Count.ShouldBe(3);
        window.Close();
    }

    [AvaloniaFact]
    public void TextBoxDisplay_NullableType_IsNullCheckBoxWritesNull()
    {
        EditorFixture fixture = new EditorFixture { MaybeNumber = 2 };
        TextBoxDisplay display = new TextBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.MaybeNumber)) };

        display.TryGetValueOnUi(out object? shown).ShouldBe(ApplyValueResult.Success);
        shown.ShouldBe(2f);

        display.TrySetValueOnUi(null);
        display.TrySetValueOnInstance(null);

        fixture.MaybeNumber.ShouldBeNull();
    }

    [AvaloniaFact]
    public void MultiLineTextBoxDisplay_EnterAddsALine_AndTheApplyButtonOrCommandEnterCommitsWithLineBreaks()
    {
        EditorFixture fixture = new EditorFixture { Text = "start" };
        MultiLineTextBoxDisplay display = new MultiLineTextBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Text)) };
        display.EditorTextBox.AcceptsReturn.ShouldBeTrue();
        display.ApplyButton.IsVisible.ShouldBeFalse();

        display.EditorTextBox.Text = "one\ntwo";
        display.EditorTextBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
        fixture.Text.ShouldBe("start");
        display.ApplyButton.IsVisible.ShouldBeTrue();

        display.ApplyButton.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        fixture.Text.ShouldBe("one\ntwo");
        display.ApplyButton.IsVisible.ShouldBeFalse();

        display.EditorTextBox.Text = "three";
        display.EditorTextBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter, KeyModifiers = AvaloniaDataUi.PlatformKeyModifiers.Command });
        fixture.Text.ShouldBe("three");
    }

    // Apply is on the platform command key: Cmd+Enter on macOS, Ctrl+Enter elsewhere (#5540).
    [Theory]
    [InlineData(KeyModifiers.Meta, KeyModifiers.Meta, true, "Apply (⌘Enter)")]
    [InlineData(KeyModifiers.Meta, KeyModifiers.Control, false, "Apply (⌘Enter)")]
    [InlineData(KeyModifiers.Control, KeyModifiers.Control, true, "Apply (Ctrl+Enter)")]
    [InlineData(KeyModifiers.Control, KeyModifiers.None, false, "Apply (Ctrl+Enter)")]
    public void MultiLineTextBoxDisplay_AppliesOnTheCommandKeyPlusEnter_AndSaysSoInTheTooltip(
        KeyModifiers commandModifiers, KeyModifiers pressed, bool expectedApply, string expectedTooltip)
    {
        MultiLineTextBoxDisplay.IsApplyGesture(Key.Enter, pressed, commandModifiers).ShouldBe(expectedApply);
        MultiLineTextBoxDisplay.IsApplyGesture(Key.A, pressed, commandModifiers).ShouldBeFalse();
        MultiLineTextBoxDisplay.ApplyToolTip(commandModifiers).ShouldBe(expectedTooltip);
    }

    [AvaloniaFact]
    public void CheckBoxDisplay_CheckingWritesTheMember_AndRefreshShowsIt()
    {
        EditorFixture fixture = new EditorFixture { Flag = false };
        CheckBoxDisplay display = new CheckBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Flag)) };

        display.CheckBox.IsChecked = true;

        fixture.Flag.ShouldBeTrue();

        fixture.Flag = false;
        display.Refresh();
        display.CheckBox.IsChecked.ShouldBe(false);
    }

    [AvaloniaFact]
    public void NullableBoolDisplay_SetsTrueFalseAndNone()
    {
        EditorFixture fixture = new EditorFixture { Maybe = true };
        NullableBoolDisplay display = new NullableBoolDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Maybe)) };

        display.TrySetValueOnUi(false);
        display.TrySetValueOnInstance();
        fixture.Maybe.ShouldBe(false);

        display.TrySetValueOnUi(null);
        display.TrySetValueOnInstance();
        fixture.Maybe.ShouldBeNull();
    }

    [AvaloniaFact]
    public void ComboBoxDisplay_ListsEnumValues_AndSelectingOneWritesIt()
    {
        EditorFixture fixture = new EditorFixture { Choice = FixtureChoice.First };
        ComboBoxDisplay display = new ComboBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Choice)) };

        display.ComboBox.Items.Cast<object>().ShouldBe(new object[] { FixtureChoice.First, FixtureChoice.Second, FixtureChoice.Third });

        display.ComboBox.SelectedItem = FixtureChoice.Third;

        fixture.Choice.ShouldBe(FixtureChoice.Third);
    }

    [AvaloniaFact]
    public void ComboBoxDisplay_ShowsAValueThatIsNotAnOption_WithoutWritingIt()
    {
        EditorFixture fixture = new EditorFixture { Text = "missing" };
        InstanceMember member = fixture.Member(nameof(EditorFixture.Text));
        member.CustomOptions = new List<object> { "start", "other" };
        int writes = 0;
        member.AfterSetByUi += (_, _) => writes++;

        ComboBoxDisplay display = new ComboBoxDisplay { InstanceMember = member };

        display.ComboBox.SelectedItem.ShouldBe("missing");
        writes.ShouldBe(0);
        fixture.Text.ShouldBe("missing");

        display.ComboBox.SelectedItem = "other";
        Dispatcher.UIThread.RunJobs();

        fixture.Text.ShouldBe("other");
        display.ComboBox.Items.Cast<object>().ShouldBe(new object[] { "start", "other" });
    }

    [AvaloniaFact]
    public void ComboBoxDisplay_Editable_CommitsTypedText()
    {
        EditorFixture fixture = new EditorFixture { Text = "start" };
        InstanceMember member = fixture.Member(nameof(EditorFixture.Text));
        member.CustomOptions = new List<object> { "start", "other" };
        ComboBoxDisplay display = new ComboBoxDisplay { IsEditable = true, InstanceMember = member };

        display.ComboBox.Text = "typed";
        display.TrySetValueOnInstance();

        fixture.Text.ShouldBe("typed");
    }

    [AvaloniaFact]
    public void SliderDisplay_CommitsTheSliderPosition_ScaledByTheMultiplier()
    {
        EditorFixture fixture = new EditorFixture { Number = 0.25f };
        SliderDisplay display = new SliderDisplay { MinValue = 0, MaxValue = 1, DisplayedValueMultiplier = 100 };
        display.InstanceMember = fixture.Member(nameof(EditorFixture.Number));
        display.Slider.Value.ShouldBe(25);

        display.Slider.Value = 60;
        // Without a pointer drag in progress, moving the slider applies nothing until the commit.
        fixture.Number.ShouldBe(0.25f);
        display.HandleSliderCommitted();

        fixture.Number.ShouldBe(0.6f, 0.0001f);
    }

    [AvaloniaFact]
    public void SliderDisplay_MinMaxLabels_SitCloseToTheTrack()
    {
        EditorFixture fixture = new EditorFixture { Number = 0.5f };
        SliderDisplay display = new SliderDisplay { MinValue = 0, MaxValue = 255, InstanceMember = fixture.Member(nameof(EditorFixture.Number)) };
        Window window = new Window { Content = display, Width = 400, Height = 200 };
        window.Show();
        window.UpdateLayout();

        // At its default ~50px the slider leaves the labels floating far below the track.
        display.Slider.DesiredSize.Height.ShouldBeLessThanOrEqualTo(30);
    }

    [AvaloniaFact]
    public void SliderDisplay_Drag_WritesIntermediateValuesThenOneFullOnRelease()
    {
        EditorFixture fixture = new EditorFixture { Number = 0 };
        InstanceMember member = fixture.Member(nameof(EditorFixture.Number));
        List<(object? Value, SetPropertyCommitType CommitType)> commits = new();
        member.CustomSetPropertyEvent += (_, args) =>
        {
            fixture.Number = Convert.ToSingle(args.Value);
            commits.Add((args.Value, args.CommitType));
        };
        SliderDisplay display = new SliderDisplay { MinValue = 0, MaxValue = 100, InstanceMember = member };
        Window window = new Window { Content = display, Width = 400, Height = 200 };
        window.Show();
        window.UpdateLayout();
        Rect sliderBounds = display.Slider.Bounds;
        Point start = display.Slider.TranslatePoint(new Point(sliderBounds.Width * 0.25, sliderBounds.Height / 2), window)!.Value;
        Point end = display.Slider.TranslatePoint(new Point(sliderBounds.Width * 0.75, sliderBounds.Height / 2), window)!.Value;

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start + new Point(10, 0));
        window.MouseMove(end);

        commits.ShouldNotBeEmpty();
        commits.ShouldAllBe(c => c.CommitType == SetPropertyCommitType.Intermediate);
        fixture.Number.ShouldBe((float)display.Slider.Value, 1f);

        window.MouseUp(end, MouseButton.Left);

        commits.Count(c => c.CommitType == SetPropertyCommitType.Full).ShouldBe(1);
        commits.Last().CommitType.ShouldBe(SetPropertyCommitType.Full);
        window.Close();
    }

    [AvaloniaFact]
    public void ReadOnlyMember_DisablesTheEditor()
    {
        EditorFixture fixture = new EditorFixture();
        InstanceMember member = fixture.Member(nameof(EditorFixture.Flag));
        member.IsReadOnly = true;

        CheckBoxDisplay display = new CheckBoxDisplay { InstanceMember = member };

        ((global::Avalonia.Controls.Control)display.Content!).IsEnabled.ShouldBeFalse();
    }
}
