using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaDataUi.Controls;
using Shouldly;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.DataUi;

/// <summary>
/// The composite Avalonia editors against the shared <see cref="EditorFixture"/>: each writes a
/// value set through its UI to the fixture and shows a fixture change after a refresh.
/// </summary>
public class CompositeEditorTests : IDisposable
{
    private readonly IDataUiFilePicker? _originalPicker;
    private readonly bool _originalBackspaceDeletes;

    public CompositeEditorTests()
    {
        _originalPicker = FilePickingLogic.FilePicker;
        _originalBackspaceDeletes = DataUiKeyboard.BackspaceDeletes;
    }

    public void Dispose()
    {
        FilePickingLogic.FilePicker = _originalPicker;
        DataUiKeyboard.BackspaceDeletes = _originalBackspaceDeletes;
    }

    private sealed class FakePicker : IDataUiFilePicker
    {
        public string? NextFile { get; set; }
        public string? LastFilter { get; private set; }

        public string? PickFile(string filter)
        {
            LastFilter = filter;
            return NextFile;
        }

        public string? PickFolder() => NextFile;

        public string? LastRevealed { get; private set; }

        public void RevealFile(string filePath)
        {
            LastRevealed = filePath;
        }
    }

    [AvaloniaFact]
    public void AngleSelectorDisplay_TypedDegreesWriteRadians_AndTheDialDragWritesDegrees()
    {
        EditorFixture fixture = new EditorFixture { Angle = 0 };
        AngleSelectorDisplay display = new AngleSelectorDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Angle)) };

        display.TextBox.Text = "90*2";
        display.ApplyTypedText();
        fixture.Angle.ShouldBe((float)Math.PI, 0.0001f);

        AngleSelectorDisplay degrees = new AngleSelectorDisplay { TypeToPushToInstance = AngleType.Degrees };
        degrees.InstanceMember = fixture.Member(nameof(EditorFixture.Angle));
        degrees.BeginDialDrag();
        degrees.DragDialTo(0, -10, isShiftDown: false);
        degrees.EndDialDrag();
        fixture.Angle.ShouldBe(90f);

        fixture.Angle = 45;
        degrees.Refresh();
        degrees.TextBox.Text.ShouldBe("45");
    }

    [AvaloniaFact]
    public void ToggleButtonOptionDisplay_PressingAnOptionWritesItsValue_AndKeepsOneButtonPressed()
    {
        EditorFixture fixture = new EditorFixture { Choice = FixtureChoice.First };
        ToggleButtonOption first = new ToggleButtonOption("First", FixtureChoice.First);
        ToggleButtonOption third = new ToggleButtonOption("Third", FixtureChoice.Third);
        ToggleButtonOptionDisplay display = new ToggleButtonOptionDisplay(new[] { first, third })
        {
            InstanceMember = fixture.Member(nameof(EditorFixture.Choice)),
        };
        display.Buttons[0].IsChecked.ShouldBe(true);

        display.Press(third);

        fixture.Choice.ShouldBe(FixtureChoice.Third);
        display.Buttons.Count(button => button.IsChecked == true).ShouldBe(1);

        fixture.Choice = FixtureChoice.First;
        display.Refresh();
        display.Buttons[0].IsChecked.ShouldBe(true);
    }

    [AvaloniaFact]
    public void StringListTextBoxDisplay_CommitsLinesAsAList_AndReportsTheCaretsLine()
    {
        EditorFixture fixture = new EditorFixture();
        StringListTextBoxDisplay display = new StringListTextBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Lines)) };
        display.EditorTextBox.Text.ShouldBe("alpha");

        display.EditorTextBox.Text = "one\ntwo";
        display.TrySetValueOnInstance();

        fixture.Lines.ShouldBe(new List<string> { "one", "two" });
        display.EditorTextBox.CaretIndex = 5;
        display.GetCurrentLineText().ShouldBe("two");
    }

    [AvaloniaFact]
    public void StringListTextBoxDisplay_ApplyButton_ShowsOnlyWhileEditsAreUnapplied_AndNeverTakesFocus()
    {
        EditorFixture fixture = new EditorFixture();
        StringListTextBoxDisplay display = new StringListTextBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Lines)) };
        display.ApplyButton.IsVisible.ShouldBeFalse();
        display.ApplyButton.Focusable.ShouldBeFalse();
        display.ApplyButton.IsTabStop.ShouldBeFalse();

        display.EditorTextBox.Text = "one\ntwo";
        display.ApplyButton.IsVisible.ShouldBeTrue();
        fixture.Lines.ShouldBe(new List<string> { "alpha" });

        display.ApplyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        fixture.Lines.ShouldBe(new List<string> { "one", "two" });
        display.ApplyButton.IsVisible.ShouldBeFalse();

        display.EditorTextBox.Text = "three";
        display.EditorTextBox.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));

        fixture.Lines.ShouldBe(new List<string> { "three" });
        display.ApplyButton.IsVisible.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void StringListTextBoxDisplay_CtrlEnterApplies_EnterDoesNot_AndEscapeReverts()
    {
        EditorFixture fixture = new EditorFixture();
        StringListTextBoxDisplay display = new StringListTextBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Lines)) };

        display.EditorTextBox.Text = "one";
        display.EditorTextBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
        fixture.Lines.ShouldBe(new List<string> { "alpha" });

        KeyEventArgs ctrlEnter = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter, KeyModifiers = KeyModifiers.Control };
        display.EditorTextBox.RaiseEvent(ctrlEnter);
        ctrlEnter.Handled.ShouldBeTrue();
        fixture.Lines.ShouldBe(new List<string> { "one" });
        display.ApplyButton.IsVisible.ShouldBeFalse();

        display.EditorTextBox.Text = "discarded";
        KeyEventArgs escape = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape };
        display.EditorTextBox.RaiseEvent(escape);

        escape.Handled.ShouldBeTrue();
        display.EditorTextBox.Text.ShouldBe("one");
        display.ApplyButton.IsVisible.ShouldBeFalse();
        fixture.Lines.ShouldBe(new List<string> { "one" });

        // With nothing to revert, Escape is left for whatever else listens for it.
        KeyEventArgs secondEscape = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape };
        display.EditorTextBox.RaiseEvent(secondEscape);
        secondEscape.Handled.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void ListBoxDisplay_AddsEditsAndRemovesEntries_WithoutTouchingTheMembersListUntilCommitted()
    {
        EditorFixture fixture = new EditorFixture { Numbers = new List<int> { 1, 2 } };
        List<int> original = fixture.Numbers;
        ListBoxDisplay display = new ListBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Numbers)) };

        display.AddOrReplace("3");
        fixture.Numbers.ShouldBe(new List<int> { 1, 2, 3 });
        original.ShouldBe(new List<int> { 1, 2 });

        display.ListBox.SelectedIndex = 0;
        display.BeginEditSelected();
        display.IndexEditing.ShouldBe(0);
        display.AddOrReplace("7");
        fixture.Numbers.ShouldBe(new List<int> { 7, 2, 3 });

        display.ListBox.SelectedIndex = 1;
        display.RemoveSelected();
        fixture.Numbers.ShouldBe(new List<int> { 7, 3 });
    }

    [AvaloniaTheory]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    public void ListEditors_Backspace_RemovesTheSelectedEntry_OnlyWhereBackspaceDeletes(bool backspaceDeletes, int entriesLeft)
    {
        // On macOS the key labeled "delete" sends Backspace (#5552).
        DataUiKeyboard.BackspaceDeletes = backspaceDeletes;
        EditorFixture fixture = new EditorFixture { Numbers = new List<int> { 1, 2 }, Files = new List<string> { "a.csv", "b.csv" } };
        ListBoxDisplay list = new ListBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Numbers)) };
        MultiFileDisplay files = new MultiFileDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Files)) };
        list.ListBox.SelectedIndex = 0;
        files.ListBox.SelectedIndex = 0;

        list.ListBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Back });
        files.ListBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Back });

        fixture.Numbers.Count.ShouldBe(entriesLeft);
        fixture.Files.Count.ShouldBe(entriesLeft);
    }

    [AvaloniaFact]
    public void ListBoxDisplay_ReboundToAnotherMember_StopsEditing()
    {
        EditorFixture fixture = new EditorFixture();
        ListBoxDisplay display = new ListBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Numbers)) };
        display.ListBox.SelectedIndex = 1;
        display.BeginEditSelected();

        display.InstanceMember = fixture.Member(nameof(EditorFixture.Lines));

        display.IndexEditing.ShouldBeNull();
        display.AddOrReplace("beta");
        fixture.Lines.ShouldBe(new List<string> { "alpha", "beta" });
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_PickedFileIsWrittenWithTheFilter()
    {
        FakePicker picker = new FakePicker { NextFile = "/fonts/a.ttf" };
        FilePickingLogic.FilePicker = picker;
        EditorFixture fixture = new EditorFixture();
        FileSelectionDisplay display = new FileSelectionDisplay { Filter = "TrueType Font|*.ttf" };
        display.InstanceMember = fixture.Member(nameof(EditorFixture.File));

        display.PickFile();

        fixture.File.ShouldBe("/fonts/a.ttf");
        picker.LastFilter.ShouldBe("TrueType Font|*.ttf");
        display.TextBox.Text.ShouldBe("/fonts/a.ttf");
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithChoices_ListsThemWhenOpened_AndPickingOneWritesIt()
    {
        EditorFixture fixture = new EditorFixture { File = "old.png" };
        string? shownCurrentValue = null;
        FileSelectionDisplay display = new FileSelectionDisplay
        {
            FileChoices = current =>
            {
                shownCurrentValue = current;
                return new[] { "b.png", "old.png", "z.png" };
            }
        };
        display.InstanceMember = fixture.Member(nameof(EditorFixture.File));
        display.Refresh();

        display.ChoiceBox.IsVisible.ShouldBeTrue();
        display.ChoiceBox.IsEditable.ShouldBeTrue();
        display.ChoiceBox.SelectedItem.ShouldBe("old.png");

        display.ChoiceBox.IsDropDownOpen = true;

        shownCurrentValue.ShouldBe("old.png");
        display.ChoiceBox.Items.Cast<object?>().ShouldBe(new object[] { "b.png", "old.png", "z.png" });
        display.ChoiceBox.SelectedItem.ShouldBe("old.png");
        fixture.File.ShouldBe("old.png", "listing the choices does not write anything");

        display.ChoiceBox.SelectedItem = "z.png";

        fixture.File.ShouldBe("z.png", "a pick from the open list commits at once");
        display.TextBox.Text.ShouldBe("z.png");
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithBuiltInChoices_ListsThemUnderTheirOwnHeader_AndPickingOneCopiesItAndWritesTheCopy()
    {
        EditorFixture fixture = new EditorFixture { File = "old.slang" };
        IReadOnlyList<string>? heldByTheProject = null;
        string? imported = null;
        FileSelectionDisplay display = new FileSelectionDisplay
        {
            FileChoices = _ => new[] { "Shaders/Mine.slang", "old.slang" },
            BuiltInChoices = held =>
            {
                heldByTheProject = held;
                return new[] { "Blur.slang", "Sepia.slang" };
            },
            ImportBuiltIn = name =>
            {
                imported = name;
                return "Shaders/" + name;
            },
        };
        display.InstanceMember = fixture.Member(nameof(EditorFixture.File));
        display.Refresh();

        display.ChoiceBox.IsDropDownOpen = true;

        heldByTheProject.ShouldBe(new[] { "Shaders/Mine.slang", "old.slang" });
        display.ChoiceBox.Items.Cast<object?>().Select(item => item?.ToString()).ShouldBe(
            new[] { "Project", "Shaders/Mine.slang", "old.slang", "Built-in", "Blur.slang", "Sepia.slang" });
        display.ChoiceBox.SelectedItem.ShouldBe("old.slang");
        fixture.File.ShouldBe("old.slang", "listing the choices does not copy or write anything");
        imported.ShouldBeNull();

        display.ChoiceBox.SelectedItem = display.ChoiceBox.Items.Cast<object?>().Single(item => item?.ToString() == "Blur.slang");
        Dispatcher.UIThread.RunJobs();

        imported.ShouldBe("Blur.slang");
        fixture.File.ShouldBe("Shaders/Blur.slang");
        display.TextBox.Text.ShouldBe("Shaders/Blur.slang");
        display.ChoiceBox.SelectedItem.ShouldBe("Shaders/Blur.slang");
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithBuiltInChoices_PickingOneImportsAfterTheDropDownHasClosed()
    {
        // The import can open a prompt, which must not open under the still-open drop-down popup.
        EditorFixture fixture = new EditorFixture { File = "old.slang" };
        bool? dropDownOpenWhenImporting = null;
        FileSelectionDisplay? display = null;
        display = new FileSelectionDisplay
        {
            FileChoices = _ => new[] { "old.slang" },
            BuiltInChoices = _ => new[] { "Blur.slang" },
            ImportBuiltIn = name =>
            {
                dropDownOpenWhenImporting = display!.ChoiceBox.IsDropDownOpen;
                return "Shaders/" + name;
            },
        };
        display.InstanceMember = fixture.Member(nameof(EditorFixture.File));
        display.Refresh();
        display.ChoiceBox.IsDropDownOpen = true;

        display.ChoiceBox.SelectedItem = display.ChoiceBox.Items.Cast<object?>().Single(item => item?.ToString() == "Blur.slang");

        dropDownOpenWhenImporting.ShouldBeNull("the pick is still routing");
        display.ChoiceBox.IsDropDownOpen = false;
        Dispatcher.UIThread.RunJobs();
        dropDownOpenWhenImporting.ShouldBe(false);
        fixture.File.ShouldBe("Shaders/Blur.slang");
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithBuiltInChoices_ACopyThatCannotBeMade_LeavesTheValueAlone()
    {
        EditorFixture fixture = new EditorFixture { File = "old.slang" };
        FileSelectionDisplay display = new FileSelectionDisplay
        {
            FileChoices = _ => new[] { "old.slang" },
            BuiltInChoices = _ => new[] { "Blur.slang" },
            ImportBuiltIn = _ => null,
        };
        display.InstanceMember = fixture.Member(nameof(EditorFixture.File));
        display.Refresh();
        display.ChoiceBox.IsDropDownOpen = true;

        display.ChoiceBox.SelectedItem = display.ChoiceBox.Items.Cast<object?>().Single(item => item?.ToString() == "Blur.slang");
        Dispatcher.UIThread.RunJobs();

        fixture.File.ShouldBe("old.slang");
        display.ChoiceBox.SelectedItem.ShouldBe("old.slang");
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithBuiltInChoices_NothingToOffer_ShowsNoHeaders_AndAnEmptyProjectListHasNoProjectHeader()
    {
        EditorFixture fixture = new EditorFixture { File = "" };
        List<string> builtIn = new List<string>();
        FileSelectionDisplay display = new FileSelectionDisplay
        {
            FileChoices = _ => new[] { "old.slang" },
            BuiltInChoices = _ => builtIn,
            ImportBuiltIn = name => name,
        };
        display.InstanceMember = fixture.Member(nameof(EditorFixture.File));
        display.Refresh();

        display.ChoiceBox.IsDropDownOpen = true;
        display.ChoiceBox.Items.Cast<object?>().ShouldBe(new object[] { "old.slang" });

        display.FileChoices = _ => Array.Empty<string>();
        builtIn.Add("Blur.slang");
        display.ChoiceBox.IsDropDownOpen = false;
        display.ChoiceBox.IsDropDownOpen = true;

        display.ChoiceBox.Items.Cast<object?>().Select(item => item?.ToString()).ShouldBe(new[] { "Built-in", "Blur.slang" });
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithChoices_AValueNobodyReferencesStillShows_AndAnEmptyListHasNoSelection()
    {
        EditorFixture fixture = new EditorFixture { File = "lonely.png" };
        FileSelectionDisplay display = new FileSelectionDisplay { FileChoices = current => current == null ? new string[0] : new[] { current } };
        display.InstanceMember = fixture.Member(nameof(EditorFixture.File));
        display.Refresh();

        display.ChoiceBox.SelectedItem.ShouldBe("lonely.png");

        fixture.File = "";
        display.Refresh();
        display.ChoiceBox.IsDropDownOpen = true;

        display.ChoiceBox.SelectedItem.ShouldBeNull();
        display.ChoiceBox.Items.Count.ShouldBe(0);
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithChoices_TypedTextCommitsOnEnterOrFocusLoss_AndEmptyClears()
    {
        EditorFixture fixture = new EditorFixture { File = "old.png" };
        FileSelectionDisplay display = new FileSelectionDisplay { FileChoices = _ => new[] { "old.png" } };
        display.InstanceMember = fixture.Member(nameof(EditorFixture.File));
        display.Refresh();

        display.ChoiceBox.Text = "https://example.com/typed.png";
        fixture.File.ShouldBe("old.png", "typing alone commits nothing");
        display.ChoiceBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
        fixture.File.ShouldBe("https://example.com/typed.png");

        display.ChoiceBox.Text = "other/typed.png";
        display.ChoiceBox.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));
        fixture.File.ShouldBe("other/typed.png");

        display.ChoiceBox.Text = "";
        display.ChoiceBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
        fixture.File.ShouldBe("");
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithChoices_EscapeRevertsTypedText()
    {
        EditorFixture fixture = new EditorFixture { File = "old.png" };
        FileSelectionDisplay display = new FileSelectionDisplay { FileChoices = _ => new[] { "old.png" } };
        display.InstanceMember = fixture.Member(nameof(EditorFixture.File));
        display.Refresh();

        display.ChoiceBox.Text = "half-typed";
        display.ChoiceBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
        display.ChoiceBox.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));

        display.ChoiceBox.Text.ShouldBe("old.png");
        fixture.File.ShouldBe("old.png");
    }

    // Counts writes, which a plain fixture cannot: a write of the value already held is a spurious commit.
    private sealed class CountingFileFixture
    {
        private string _file = "";
        public int Writes { get; private set; }
        public string File
        {
            get => _file;
            set
            {
                _file = value;
                Writes++;
            }
        }
    }

    private static Window HostInWindow(Control content)
    {
        Window window = new Window { Content = content, Width = 400, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static FileSelectionDisplay CountingDisplay(CountingFileFixture fixture, Func<string?, IReadOnlyList<string>> choices)
    {
        FileSelectionDisplay display = new FileSelectionDisplay { FileChoices = choices };
        display.InstanceMember = new InstanceMember(nameof(CountingFileFixture.File), fixture);
        display.Refresh();
        return display;
    }

    private static void PressKey(FileSelectionDisplay display, Key key) =>
        display.ChoiceBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key });

    private static void LoseFocus(FileSelectionDisplay display) =>
        display.ChoiceBox.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));

    [AvaloniaFact]
    public void FileSelectionDisplay_WithChoices_RefreshWhileTheListIsOpen_LeavesTheListAndWritesNothing()
    {
        CountingFileFixture fixture = new CountingFileFixture();
        FileSelectionDisplay display = CountingDisplay(fixture, _ => new[] { "a.png", "b.png" });
        display.ChoiceBox.IsDropDownOpen = true;

        display.Refresh(forceRefreshEvenIfFocused: true);

        display.ChoiceBox.Items.Cast<object?>().ShouldBe(new object[] { "a.png", "b.png" });
        fixture.Writes.ShouldBe(0);
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithChoices_RefreshWhileFocused_KeepsTheTypedText()
    {
        CountingFileFixture fixture = new CountingFileFixture { File = "old.png" };
        FileSelectionDisplay display = CountingDisplay(fixture, _ => new[] { "old.png" });
        Window window = HostInWindow(display);
        try
        {
            display.ChoiceBox.Focus();
            Dispatcher.UIThread.RunJobs();
            display.ChoiceBox.Text = "half-typed";

            display.Refresh();

            display.ChoiceBox.Text.ShouldBe("half-typed");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithChoices_ForcedRefreshWhileFocused_ShowsTheValue_AndLosingFocusAfterwardsWritesNothing()
    {
        CountingFileFixture fixture = new CountingFileFixture { File = "old.png" };
        FileSelectionDisplay display = CountingDisplay(fixture, _ => new[] { "old.png" });
        int writesAfterSetup = fixture.Writes;
        Window window = HostInWindow(display);
        try
        {
            display.ChoiceBox.Focus();
            Dispatcher.UIThread.RunJobs();
            display.ChoiceBox.Text = "half-typed";

            display.Refresh(forceRefreshEvenIfFocused: true);
            Dispatcher.UIThread.RunJobs();
            LoseFocus(display);

            display.ChoiceBox.Text.ShouldBe("old.png");
            fixture.File.ShouldBe("old.png");
            fixture.Writes.ShouldBe(writesAfterSetup);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithChoices_RefreshAfterAnOutsideChange_FollowsTheValueAndClearsWhenEmptied()
    {
        CountingFileFixture fixture = new CountingFileFixture { File = "old.png" };
        FileSelectionDisplay display = CountingDisplay(fixture, current => current == null ? new string[0] : new[] { current });

        fixture.File = "new.png";
        display.Refresh();
        display.ChoiceBox.Text.ShouldBe("new.png");
        display.ChoiceBox.SelectedItem.ShouldBe("new.png");

        fixture.File = "";
        display.Refresh();
        display.ChoiceBox.Text.ShouldBeNullOrEmpty();
        display.ChoiceBox.SelectedItem.ShouldBeNull();
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithChoices_EnterOrLostFocusOnTheShownText_WritesNothing()
    {
        CountingFileFixture fixture = new CountingFileFixture { File = "old.png" };
        FileSelectionDisplay display = CountingDisplay(fixture, _ => new[] { "old.png" });
        int writesAfterSetup = fixture.Writes;

        PressKey(display, Key.Enter);
        LoseFocus(display);

        fixture.Writes.ShouldBe(writesAfterSetup, "an unedited field is not a change");
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithChoices_OpeningTheListRepeatedly_LeavingAndPickingTheCurrentFile_WriteNothing()
    {
        CountingFileFixture fixture = new CountingFileFixture { File = "b.png" };
        FileSelectionDisplay display = CountingDisplay(fixture, _ => new[] { "a.png", "b.png" });
        int writesAfterSetup = fixture.Writes;

        for (int i = 0; i < 3; i++)
        {
            display.ChoiceBox.IsDropDownOpen = true;
            display.ChoiceBox.IsDropDownOpen = false;
            LoseFocus(display);
        }
        display.ChoiceBox.IsDropDownOpen = true;
        display.ChoiceBox.SelectedItem = "b.png";

        fixture.Writes.ShouldBe(writesAfterSetup);
        display.ChoiceBox.Text.ShouldBe("b.png");
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithChoices_EscapeThenEnter_WritesNothing()
    {
        CountingFileFixture fixture = new CountingFileFixture { File = "old.png" };
        FileSelectionDisplay display = CountingDisplay(fixture, _ => new[] { "old.png" });
        int writesAfterSetup = fixture.Writes;

        display.ChoiceBox.Text = "half-typed";
        PressKey(display, Key.Escape);
        PressKey(display, Key.Enter);

        fixture.Writes.ShouldBe(writesAfterSetup);
        display.ChoiceBox.Text.ShouldBe("old.png");
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithChoices_ClearingTheText_WritesTheEmptyPathOnce_EvenWithALostFocusAfterTheEnter()
    {
        CountingFileFixture fixture = new CountingFileFixture { File = "old.png" };
        FileSelectionDisplay display = CountingDisplay(fixture, _ => new[] { "old.png" });
        int writesAfterSetup = fixture.Writes;

        display.ChoiceBox.Text = "";
        PressKey(display, Key.Enter);
        LoseFocus(display);

        fixture.File.ShouldBe("");
        fixture.Writes.ShouldBe(writesAfterSetup + 1, "the lost focus after the Enter finds the shown text unchanged");
        display.ChoiceBox.SelectedItem.ShouldBeNull();
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_RevealButton_FollowsWhetherThePathIsEmpty_AfterRefreshAndAfterACommit()
    {
        CountingFileFixture fixture = new CountingFileFixture();
        FileSelectionDisplay display = CountingDisplay(fixture, _ => new[] { "a.png" });
        Button reveal = ((Grid)display.Content!).Children.OfType<Button>().Single(button => button.Content?.ToString() == "↗");
        reveal.IsEnabled.ShouldBeFalse();

        display.ChoiceBox.Text = "a.png";
        PressKey(display, Key.Enter);
        reveal.IsEnabled.ShouldBeTrue();

        fixture.File = "";
        display.Refresh();
        reveal.IsEnabled.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_SettingFileChoicesRepeatedlyOrToNull_KeepsExactlyOneFieldInTheRow()
    {
        CountingFileFixture fixture = new CountingFileFixture { File = "a.png" };
        FileSelectionDisplay display = new FileSelectionDisplay();
        display.InstanceMember = new InstanceMember(nameof(CountingFileFixture.File), fixture);
        Grid grid = (Grid)display.Content!;

        display.FileChoices = _ => new[] { "a.png" };
        display.FileChoices = _ => new[] { "a.png", "b.png" };
        grid.Children.Count(child => child == display.ChoiceBox).ShouldBe(1);
        grid.Children.Contains(display.TextBox).ShouldBeFalse();

        display.FileChoices = null;
        grid.Children.Count(child => child == display.TextBox).ShouldBe(1);
        grid.Children.Contains(display.ChoiceBox).ShouldBeFalse();
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_RefreshWithNoMember_DoesNothing()
    {
        FileSelectionDisplay display = new FileSelectionDisplay { FileChoices = _ => new[] { "a.png" } };

        Should.NotThrow(() => display.Refresh(forceRefreshEvenIfFocused: true));
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithoutChoices_ButtonsAreAsTallAsTheTextBox()
    {
        EditorFixture fixture = new EditorFixture { File = "a.png" };
        FileSelectionDisplay display = new FileSelectionDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.File)) };
        display.Refresh();
        Window window = new Window { Content = display, Width = 400, Height = 100 };
        window.Show();
        try
        {
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            foreach (Button button in display.GetVisualDescendants().OfType<Button>())
            {
                button.Bounds.Height.ShouldBe(display.TextBox.Bounds.Height, 0.5, $"the {button.Content} button");
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_WithoutChoices_StaysATextBox()
    {
        EditorFixture fixture = new EditorFixture { File = "a.png" };
        FileSelectionDisplay display = new FileSelectionDisplay();
        display.InstanceMember = fixture.Member(nameof(EditorFixture.File));
        display.Refresh();

        display.TextBox.Parent.ShouldNotBeNull();
        display.ChoiceBox.Parent.ShouldBeNull("the drop-down is not part of a plain file row");
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_RevealsRelativeToItsOwnBaseFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "GumCompositeEditorTests", Guid.NewGuid().ToString("N")) + "/";
        Directory.CreateDirectory(folder + "Code/Generated");
        FakePicker picker = new FakePicker();
        FilePickingLogic.FilePicker = picker;
        EditorFixture fixture = new EditorFixture { File = "Generated" };
        FileSelectionDisplay display = new FileSelectionDisplay { RevealRelativeTo = () => folder + "Code/" };
        display.InstanceMember = fixture.Member(nameof(EditorFixture.File));
        display.Refresh();

        display.Reveal();

        picker.LastRevealed.ShouldNotBeNull().ShouldEndWith("/Code/Generated");
    }

    [AvaloniaFact]
    public void MultiFileDisplay_AddsPickedFiles_AndMovesKeepingTheSelection()
    {
        FakePicker picker = new FakePicker();
        FilePickingLogic.FilePicker = picker;
        EditorFixture fixture = new EditorFixture();
        MultiFileDisplay display = new MultiFileDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Files)) };

        picker.NextFile = "a.csv";
        display.AddFile();
        picker.NextFile = "b.csv";
        display.AddFile();
        fixture.Files.ShouldBe(new List<string> { "a.csv", "b.csv" });

        display.ListBox.SelectedIndex = 1;
        display.MoveSelected(-1);

        fixture.Files.ShouldBe(new List<string> { "b.csv", "a.csv" });
        display.ListBox.SelectedIndex.ShouldBe(0);
    }
}
