namespace WpfDataUi.DataTypes;

/// <summary>
/// Framework-neutral identities of the standard editors. Assign one of these types to
/// <see cref="InstanceMember.PreferredDisplayer"/> from code that must not reference a UI
/// framework; each grid's <see cref="DisplayerRegistry"/> maps it to that head's control. A
/// concrete control type still works as a preferred displayer in the head that owns it.
/// </summary>
public static class StandardDisplayers
{
    /// <summary>Single-line text; the fallback for any type without a better editor.</summary>
    public sealed class TextBox { private TextBox() { } }

    /// <summary>Multi-line text.</summary>
    public sealed class MultiLineTextBox { private MultiLineTextBox() { } }

    /// <summary>A check box for <see cref="bool"/>.</summary>
    public sealed class CheckBox { private CheckBox() { } }

    /// <summary>True / False / None radio buttons for a nullable <see cref="bool"/>.</summary>
    public sealed class NullableBool { private NullableBool() { } }

    /// <summary>A drop-down of the member's custom options or enum values.</summary>
    public sealed class ComboBox { private ComboBox() { } }

    /// <summary>A drop-down that also accepts typed text.</summary>
    public sealed class EditableComboBox { private EditableComboBox() { } }

    /// <summary>An editable list for list-typed members.</summary>
    public sealed class ListBox { private ListBox() { } }

    /// <summary>A slider plus a text field, for bounded numbers.</summary>
    public sealed class Slider { private Slider() { } }

    /// <summary>A text field with minus and plus buttons.</summary>
    public sealed class PlusMinus { private PlusMinus() { } }

    /// <summary>A dial plus a text field for an angle.</summary>
    public sealed class AngleSelector { private AngleSelector() { } }

    /// <summary>A text field with a file picker.</summary>
    public sealed class FileSelection { private FileSelection() { } }

    /// <summary>An ordered list of files with add, remove, and reorder.</summary>
    public sealed class MultiFile { private MultiFile() { } }

    /// <summary>Multi-line text edited as a list of strings, one per line.</summary>
    public sealed class StringList { private StringList() { } }

    /// <summary>One labeled numeric field per channel of a composite member.</summary>
    public sealed class InlineChannels { private InlineChannels() { } }
}
