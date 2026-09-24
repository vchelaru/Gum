using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Headless.XUnit;
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
    public void TextBoxDisplay_NullableType_IsNullCheckBoxWritesNull()
    {
        EditorFixture fixture = new EditorFixture { MaybeNumber = 2 };
        TextBoxDisplay display = new TextBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.MaybeNumber)) };

        display.TryGetValueOnUi(out object? shown).ShouldBe(ApplyValueResult.Success);
        shown.ShouldBe(2f);

        display.TrySetValueOnUi(null!);
        display.TrySetValueOnInstance(null!);

        fixture.MaybeNumber.ShouldBeNull();
    }

    [AvaloniaFact]
    public void MultiLineTextBoxDisplay_KeepsLineBreaks()
    {
        EditorFixture fixture = new EditorFixture();
        MultiLineTextBoxDisplay display = new MultiLineTextBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Text)) };

        display.TextBox.AcceptsReturn.ShouldBeTrue();
        display.TextBox.Text = "one\ntwo";
        display.TrySetValueOnInstance();

        fixture.Text.ShouldBe("one\ntwo");
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

        display.TrySetValueOnUi(null!);
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
    public void ComboBoxDisplay_Editable_CommitsTypedText()
    {
        EditorFixture fixture = new EditorFixture { Text = "start" };
        InstanceMember member = fixture.Member(nameof(EditorFixture.Text));
        member.CustomOptions = new List<object> { "start", "other" };
        EditableComboBoxDisplay display = new EditableComboBoxDisplay { InstanceMember = member };

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
        display.HandleSliderCommitted();

        fixture.Number.ShouldBe(0.6f, 0.0001f);
    }

    [AvaloniaFact]
    public void PlusMinusTextBox_StepsByOneOrFiveWithCtrl()
    {
        EditorFixture fixture = new EditorFixture { Count = 3 };
        PlusMinusTextBox display = new PlusMinusTextBox { InstanceMember = fixture.Member(nameof(EditorFixture.Count)) };

        display.Step(1, isCtrlDown: false);
        fixture.Count.ShouldBe(4);

        display.Step(-1, isCtrlDown: true);
        fixture.Count.ShouldBe(-1);
    }

    [AvaloniaFact]
    public void ReadOnlyMember_DisablesTheEditor()
    {
        EditorFixture fixture = new EditorFixture();
        InstanceMember member = fixture.Member(nameof(EditorFixture.Flag));
        member.IsReadOnly = true;

        CheckBoxDisplay display = new CheckBoxDisplay { InstanceMember = member };

        display.IsEnabled.ShouldBeFalse();
    }
}
