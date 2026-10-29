using System;
using System.Linq;
using Gum;

namespace GumPreview;

/// <summary>
/// Decides when Preview rebuilds what it shows after a hot reload. A reload whose tree matches what a
/// restart would show (<see cref="HotReloadResult.MatchesRestart"/>) is left as it is; one that does
/// not asks for a full rebuild, so Preview ends up as a fresh launch would. The rebuild happens later,
/// at a safe point of the game loop (<see cref="TryTakePendingReload"/>), never inside the reload's own
/// callback.
/// </summary>
/// <remarks>
/// Loop guard: a full rebuild does not touch the watched files, so it cannot start another reload by
/// itself. What can repeat is a problem that outlasts the rebuild (a missing texture file stays missing
/// on every save). The issues of the last rebuild are remembered; a result whose non-matching issues
/// are identical asks for nothing, since a rebuild just made the tree fresh and the live reload that
/// followed hit the same problem. The memory clears when a result matches a restart, so the problem
/// coming back rebuilds again, and when a rebuild fails, so the next result retries.
/// </remarks>
internal sealed class PreviewReloadPolicy
{
    private bool _isReloadPending;
    private string? _pendingSignature;
    private string? _lastReloadedSignature;

    /// <summary>True when a rebuild has been asked for and not yet taken.</summary>
    public bool IsReloadPending => _isReloadPending;

    /// <summary>Records what a finished hot reload means for Preview. Does not rebuild anything.</summary>
    public void OnReloadFinished(HotReloadResult result)
    {
        if (result.MatchesRestart)
        {
            // The newest result describes the tree, so a rebuild asked for by an older one is moot.
            _isReloadPending = false;
            _pendingSignature = null;
            _lastReloadedSignature = null;
            return;
        }

        string signature = SignatureOf(result);
        if (signature == _lastReloadedSignature)
        {
            _isReloadPending = false;
            _pendingSignature = null;
            return;
        }

        _isReloadPending = true;
        _pendingSignature = signature;
    }

    /// <summary>
    /// True once for each asked-for rebuild; the caller then rebuilds. Call it from the game thread at
    /// the start of a frame.
    /// </summary>
    public bool TryTakePendingReload()
    {
        if (!_isReloadPending)
        {
            return false;
        }
        _isReloadPending = false;
        _lastReloadedSignature = _pendingSignature;
        _pendingSignature = null;
        return true;
    }

    /// <summary>The rebuild just taken did not complete; the next non-matching result tries again.</summary>
    public void NotifyReloadFailed() => _lastReloadedSignature = null;

    // Only the issues that make the tree differ from a restart; the ones that don't can come and go.
    private static string SignatureOf(HotReloadResult result) =>
        string.Join("\n", result.Issues
            .Where(issue => !issue.MatchesRestart)
            .Select(issue => $"{issue.Kind}|{issue.ElementName}|{issue.InstanceName}|{issue.Message}")
            .OrderBy(line => line, StringComparer.Ordinal));
}
