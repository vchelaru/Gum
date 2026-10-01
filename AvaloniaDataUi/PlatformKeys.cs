using Avalonia.Input;
using WpfDataUi.Controls;

namespace AvaloniaDataUi;

/// <summary>Key checks that follow the platform's conventions (see <see cref="DataUiKeyboard"/>).</summary>
public static class PlatformKeys
{
    /// <summary>Whether <paramref name="key"/> removes the selection: Delete, plus Backspace where <see cref="DataUiKeyboard.BackspaceDeletes"/>.</summary>
    public static bool IsDelete(this Key key) =>
        key == Key.Delete || (key == Key.Back && DataUiKeyboard.BackspaceDeletes);
}
