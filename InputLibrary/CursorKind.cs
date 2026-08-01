namespace InputLibrary
{
    /// <summary>
    /// The set of cursor icons the Gum tool's editor surfaces can display over an
    /// <see cref="IInputHostControl"/>. Gum-owned and platform-neutral, so the host contract doesn't
    /// name a UI-framework-specific cursor type and can be implemented by any host.
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
