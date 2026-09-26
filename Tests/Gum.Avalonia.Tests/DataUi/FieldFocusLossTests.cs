using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Plugins.VariableGrid;
using Shouldly;
using WpfDataUi.DataTypes;
using AvaloniaColor = Avalonia.Media.Color;
using DrawingColor = System.Drawing.Color;

namespace Gum.Avalonia.Tests.DataUi;

/// <summary>
/// Each editor field that commits on focus loss: after the user commits with Enter, the member
/// changes elsewhere (an undo, another editor) and the editor refreshes; leaving the field then must
/// not write anything, because the user typed nothing since (#5143).
/// </summary>
public class FieldFocusLossTests
{
    [AvaloniaFact]
    public void TextBoxDisplay_LeavingAfterAnOutsideChange_WritesNothing()
    {
        EditorFixture fixture = new EditorFixture { Number = 1 };
        InstanceMember member = fixture.Member(nameof(EditorFixture.Number));
        TextBoxDisplay display = new TextBoxDisplay { InstanceMember = member };

        int writes = TypeEnterChangeElsewhereAndLeave(display, display.TextBox, member, "5", () => fixture.Number = 9);

        writes.ShouldBe(0);
        fixture.Number.ShouldBe(9f);
    }

    [AvaloniaFact]
    public void SliderDisplay_LeavingAfterAnOutsideChange_WritesNothing()
    {
        EditorFixture fixture = new EditorFixture { Number = 1 };
        InstanceMember member = fixture.Member(nameof(EditorFixture.Number));
        SliderDisplay display = new SliderDisplay { InstanceMember = member };

        int writes = TypeEnterChangeElsewhereAndLeave(display, display.TextBox, member, "5", () => fixture.Number = 9);

        writes.ShouldBe(0);
        fixture.Number.ShouldBe(9f);
    }

    [AvaloniaFact]
    public void PlusMinusTextBox_LeavingAfterAnOutsideChange_WritesNothing()
    {
        EditorFixture fixture = new EditorFixture { Count = 1 };
        InstanceMember member = fixture.Member(nameof(EditorFixture.Count));
        PlusMinusTextBox display = new PlusMinusTextBox { InstanceMember = member };

        int writes = TypeEnterChangeElsewhereAndLeave(display, display.TextBox, member, "5", () => fixture.Count = 9);

        writes.ShouldBe(0);
        fixture.Count.ShouldBe(9);
    }

    [AvaloniaFact]
    public void AngleSelectorDisplay_LeavingAfterAnOutsideChange_WritesNothing()
    {
        EditorFixture fixture = new EditorFixture { Angle = 1 };
        InstanceMember member = fixture.Member(nameof(EditorFixture.Angle));
        AngleSelectorDisplay display = new AngleSelectorDisplay { TypeToPushToInstance = WpfDataUi.Controls.AngleType.Degrees, InstanceMember = member };

        int writes = TypeEnterChangeElsewhereAndLeave(display, display.TextBox, member, "5", () => fixture.Angle = 9);

        writes.ShouldBe(0);
        fixture.Angle.ShouldBe(9f);
    }

    [AvaloniaFact]
    public void FileSelectionDisplay_LeavingAfterAnOutsideChange_WritesNothing()
    {
        EditorFixture fixture = new EditorFixture { File = "a.png" };
        InstanceMember member = fixture.Member(nameof(EditorFixture.File));
        FileSelectionDisplay display = new FileSelectionDisplay { InstanceMember = member };

        int writes = TypeEnterChangeElsewhereAndLeave(display, display.TextBox, member, "b.png", () => fixture.File = "c.png");

        writes.ShouldBe(0);
        fixture.File.ShouldBe("c.png");
    }

    [AvaloniaFact]
    public void InlineChannelsDisplay_LeavingAfterAnOutsideChange_WritesNothing()
    {
        EditorFixture fixture = new EditorFixture { Red = 1, Green = 2 };
        InstanceMember red = fixture.Member(nameof(EditorFixture.Red));
        InstanceMember green = fixture.Member(nameof(EditorFixture.Green));
        CompositeInstanceMember composite = new CompositeInstanceMember(
            "Channels", new[] { red, green }, typeof(string),
            channels => string.Join(",", channels), value => new object?[] { 0f, 0f });
        InlineChannelsDisplay display = new InlineChannelsDisplay { InstanceMember = composite };

        int writes = TypeEnterChangeElsewhereAndLeave(display, display.FieldTextBoxes[0], red, "5", () => fixture.Red = 9);

        writes.ShouldBe(0);
        fixture.Red.ShouldBe(9f);
    }

    [AvaloniaFact]
    public void ColorDisplay_Hex_LeavingAfterAnOutsideChange_WritesNothing()
    {
        ColorFixture fixture = new ColorFixture { Color = DrawingColor.FromArgb(255, 1, 2, 3) };
        InstanceMember member = new InstanceMember(nameof(ColorFixture.Color), fixture);
        ColorDisplay display = new ColorDisplay { InstanceMember = member };
        DrawingColor changedElsewhere = DrawingColor.FromArgb(255, 9, 9, 9);

        int writes = TypeEnterChangeElsewhereAndLeave(display, display.HexTextBox, member, "102030", () => fixture.Color = changedElsewhere);

        writes.ShouldBe(0);
        fixture.Color.ShouldBe(changedElsewhere);
    }

    [AvaloniaFact]
    public void CompactColorPicker_Channel_LeavingAfterAnOutsideChange_RaisesNoChange()
    {
        CompactColorPicker picker = new CompactColorPicker { Color = AvaloniaColor.FromRgb(1, 2, 3) };
        int changes = 0;
        picker.ColorChanged += (_, _) => changes++;
        TextBox field = picker.ChannelTextBoxes[0];
        TextBox elsewhere = new TextBox();
        Window window = Show(picker, elsewhere);

        TypeAndEnter(window, field, "50");
        changes = 0;
        picker.Color = AvaloniaColor.FromRgb(9, 9, 9);
        elsewhere.Focus();

        changes.ShouldBe(0);
        picker.Color.ShouldBe(AvaloniaColor.FromRgb(9, 9, 9));
        window.Close();
    }

    /// <summary>
    /// Types <paramref name="typed"/> into <paramref name="field"/> and presses Enter, then applies
    /// <paramref name="changeElsewhere"/> and refreshes as the grid does, then moves focus away.
    /// Returns how many writes <paramref name="member"/> took after the Enter commit.
    /// </summary>
    private static int TypeEnterChangeElsewhereAndLeave(DataUiDisplayBase display, TextBox field, InstanceMember member,
        string typed, Action changeElsewhere)
    {
        int writes = 0;
        member.AfterSetByUi += (_, _) => writes++;
        TextBox elsewhere = new TextBox();
        Window window = Show(display, elsewhere);

        TypeAndEnter(window, field, typed);
        writes.ShouldBeGreaterThan(0, "the Enter commit");
        writes = 0;

        changeElsewhere();
        display.Refresh();
        elsewhere.Focus();

        window.Close();
        return writes;
    }

    private static Window Show(Control content, TextBox elsewhere)
    {
        Window window = new Window
        {
            Content = new StackPanel { Children = { content, elsewhere } },
            Width = 500,
            Height = 400,
        };
        window.Show();
        window.UpdateLayout();
        return window;
    }

    private static void TypeAndEnter(Window window, TextBox field, string text)
    {
        field.Focus();
        field.SelectAll();
        window.KeyTextInput(text);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        field.IsFocused.ShouldBeTrue();
    }

    private sealed class ColorFixture
    {
        public DrawingColor Color { get; set; }
    }
}
