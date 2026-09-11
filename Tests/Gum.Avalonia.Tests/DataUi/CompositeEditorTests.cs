using Avalonia.Headless.XUnit;
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

    public CompositeEditorTests()
    {
        _originalPicker = FilePickingLogic.FilePicker;
    }

    public void Dispose()
    {
        FilePickingLogic.FilePicker = _originalPicker;
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

        public void RevealFile(string filePath)
        {
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

    [AvaloniaFact]
    public void InlineChannelsDisplay_EachFieldCommitsToItsOwnChannel()
    {
        EditorFixture fixture = new EditorFixture { Red = 1, Green = 2 };
        InstanceMember red = fixture.Member(nameof(EditorFixture.Red));
        InstanceMember green = fixture.Member(nameof(EditorFixture.Green));
        CompositeInstanceMember composite = new CompositeInstanceMember(
            "Channels", new[] { red, green }, typeof(string),
            channels => string.Join(",", channels), value => new object?[] { 0f, 0f });
        InlineChannelsDisplay display = new InlineChannelsDisplay { InstanceMember = composite };
        display.FieldTextBoxes.Select(textBox => textBox.Text).ShouldBe(new[] { "1", "2" });

        display.FieldTextBoxes[1].Text = "5.5";
        display.CommitField(1);

        fixture.Green.ShouldBe(5.5f);
        fixture.Red.ShouldBe(1f);
    }
}
