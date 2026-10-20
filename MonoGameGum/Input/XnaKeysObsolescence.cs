namespace Gum.Input;

/// <summary>
/// The shared message for every platform-specific XNA <c>Keys</c> overload obsoleted in #5028.
/// Phase 4 (error: true) and phase 5 (removal) find them by searching for this constant.
/// </summary>
internal static class XnaKeysObsolescence
{
    public const string Message =
        "Use the Gum.Forms.Input.Keys overload. Will become an error no earlier than March 2027 and be removed no earlier than July 2027.";
}
