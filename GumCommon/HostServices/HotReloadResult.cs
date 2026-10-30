using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum;

#if !IOS && !ANDROID

/// <summary>
/// What a hot reload could not do exactly, or did differently from how a restart would.
/// </summary>
public enum HotReloadIssueKind
{
    /// <summary>The project lists a file that is not on disk. The reload went on without it.</summary>
    MissingFile,

    /// <summary>A changed font file could not be copied. It is tried again on the next reload.</summary>
    FontFailure,

    /// <summary>
    /// The project could not be read, or applying it threw, or a changed localization file could not be
    /// read, or a changed file cannot be applied while the game runs (a replaced image). The UI is
    /// unchanged or partly updated.
    /// </summary>
    ReloadFailed,

    /// <summary>The screen or component the game shows was renamed, moved or deleted. It stays on screen as it was.</summary>
    ShownElementGone,

    /// <summary>An instance of a deleted component was removed from the running UI, as a restart would.</summary>
    InstanceRemoved,

    /// <summary>
    /// An instance's component was changed or renamed, so its visual was rebuilt. The tree is right,
    /// but references and Forms handlers the game holds on the old visual are stale.
    /// </summary>
    InstanceRebuilt,

    /// <summary>
    /// A screen or component changed its base type. The visuals already on screen keep what they
    /// inherited from the old base until a restart.
    /// </summary>
    ComponentBaseTypeChanged,

    /// <summary>
    /// A component changed which variables it exposes. Instances already on screen keep the exposure
    /// they were built with until a restart.
    /// </summary>
    ExposedVariableChanged,

    /// <summary>
    /// The default state of a standard element changed. Instances already on screen keep their old
    /// values until a restart.
    /// </summary>
    StandardElementChanged,

    /// <summary>
    /// A screen or component derives from an element that is not in the project. Visuals already on
    /// screen keep what they inherited from it until a restart.
    /// </summary>
    BaseElementMissing,

    /// <summary>
    /// A font source file (.ttf or .otf) changed. Fonts already generated from it, and the Text using
    /// them, keep the old glyphs until a restart.
    /// </summary>
    FontSourceChanged
}

/// <summary>One thing a reload reported, with the same text that is written to the console.</summary>
public sealed class HotReloadIssue
{
    public HotReloadIssue(HotReloadIssueKind kind, string message, string? elementName = null, string? instanceName = null)
    {
        Kind = kind;
        Message = message;
        ElementName = elementName;
        InstanceName = instanceName;
    }

    /// <summary>What happened.</summary>
    public HotReloadIssueKind Kind { get; }

    /// <summary>The screen or component involved, or null when the issue is about the whole reload.</summary>
    public string? ElementName { get; }

    /// <summary>The instance involved, or null when the issue is not about one instance.</summary>
    public string? InstanceName { get; }

    /// <summary>The human-readable text for this issue.</summary>
    public string Message { get; }

    /// <summary>
    /// True when the running tree is the same as a restart of the project would show, even with this issue.
    /// </summary>
    public bool MatchesRestart => Kind is HotReloadIssueKind.InstanceRemoved or HotReloadIssueKind.InstanceRebuilt;

    // Whether the problem lasts after this reload, so every later reload lists it again.
    internal bool Persists => Kind is HotReloadIssueKind.ShownElementGone
        or HotReloadIssueKind.ComponentBaseTypeChanged
        or HotReloadIssueKind.ExposedVariableChanged
        or HotReloadIssueKind.StandardElementChanged
        or HotReloadIssueKind.BaseElementMissing;
}

/// <summary>The outcome of one reload attempt, raised through <see cref="IGumHotReloadManager.ReloadFinished"/>.</summary>
public sealed class HotReloadResult
{
    public HotReloadResult(IEnumerable<HotReloadIssue> issues)
    {
        Issues = issues.ToList();
    }

    /// <summary>Every issue present in this reload, including ones an earlier reload had too.</summary>
    public IReadOnlyList<HotReloadIssue> Issues { get; }

    /// <summary>True when the reload had no issues.</summary>
    public bool IsClean => Issues.Count == 0;

    /// <summary>
    /// True when the running tree equals what a fresh load of the same project would show: no issue
    /// says otherwise. A clean reload matches.
    /// </summary>
    public bool MatchesRestart => Issues.All(issue => issue.MatchesRestart);
}

#endif // !IOS && !ANDROID
