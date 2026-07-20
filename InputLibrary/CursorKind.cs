namespace InputLibrary
{
    /// <summary>
    /// The set of cursor icons the Gum tool's editor surfaces can display over an
    /// <see cref="IInputHostControl"/>. Gum-owned and platform-neutral, so the host contract
    /// doesn't require a WinForms-specific <see cref="System.Windows.Forms.Cursor"/> and can be
    /// implemented by a non-WinForms host (e.g. a future WPF-native rendering surface).
    /// </summary>
    public enum CursorKind
    {
        Arrow,
        Cross,
        Hand,
        SizeAll,
        SizeNS,
        SizeWE,
        SizeNESW,
        SizeNWSE
    }
}
