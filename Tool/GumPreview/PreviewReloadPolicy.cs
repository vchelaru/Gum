using Gum;

namespace GumPreview;

/// <summary>
/// Decides when Preview rebuilds what it shows after a hot reload. A reload whose tree matches what a
/// restart would show (<see cref="HotReloadResult.MatchesRestart"/>) is left as it is; one that does
/// not asks for a rebuild, so Preview ends up as a fresh launch of the saved project would. The
/// rebuild happens later, at a safe point of the game loop (<see cref="TryTakePendingReload"/>),
/// never inside the reload's own callback.
/// </summary>
/// <remarks>
/// No loop guard is needed: a rebuild only reads the project and touches no watched file, so it
/// cannot start a reload. Some issues are listed again in every later result until an edit reverts
/// them, and each of those saves still needs its own rebuild, so a repeated result is never skipped.
/// A rebuild is asked for at most once per finished reload.
/// </remarks>
internal sealed class PreviewReloadPolicy
{
    private bool _isReloadPending;

    /// <summary>True when a rebuild has been asked for and not yet taken.</summary>
    public bool IsReloadPending => _isReloadPending;

    /// <summary>Records what a finished hot reload means for Preview. Does not rebuild anything.</summary>
    /// <param name="result">What the reload reported.</param>
    /// <param name="isShownElementMissing">
    /// True while Preview shows nothing because the selected element did not exist when it was last
    /// shown, or its last rebuild failed. The reload may have added the element (the tool selects a new
    /// name before the save lands), and a reload with an empty tree reports nothing wrong, so any
    /// finished reload asks for a rebuild.
    /// </param>
    public void OnReloadFinished(HotReloadResult result, bool isShownElementMissing = false)
    {
        // The newest result describes the tree, so it replaces what an older one asked for.
        _isReloadPending = !result.MatchesRestart || isShownElementMissing;
    }

    /// <summary>
    /// True once for each asked-for rebuild; the caller then rebuilds. Call it from the game thread at
    /// the start of a frame.
    /// </summary>
    public bool TryTakePendingReload()
    {
        bool wasPending = _isReloadPending;
        _isReloadPending = false;
        return wasPending;
    }
}
