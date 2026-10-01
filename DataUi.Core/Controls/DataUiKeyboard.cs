namespace WpfDataUi.Controls;

/// <summary>
/// Platform keyboard conventions for the grid's editors, set once by the host at startup because the
/// grid creates its editors itself, outside the service container.
/// </summary>
public static class DataUiKeyboard
{
    /// <summary>
    /// Whether Backspace removes the selected entry in a list editor, as Delete does. True on macOS,
    /// where the key labeled "delete" sends Backspace. A text field keeps Backspace either way.
    /// </summary>
    public static bool BackspaceDeletes { get; set; }
}
