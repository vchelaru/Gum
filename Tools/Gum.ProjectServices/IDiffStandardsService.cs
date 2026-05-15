using System.Collections.Generic;

namespace Gum.ProjectServices;

/// <summary>
/// Compares a project's Standards (Circle, Text, NineSlice, etc.) against the bundled
/// Default template's Standards and reports any drift. The "Standards must match Default"
/// invariant exists because Standards are the universal base type — a theme that modifies
/// them pollutes every consumer that creates a Standard runtime (e.g. a <c>TextRuntime</c>)
/// outside the theme's Forms controls.
/// </summary>
public interface IDiffStandardsService
{
    /// <summary>
    /// Diffs the Standards on disk under <c>&lt;project&gt;/Standards/*.gutx</c> against
    /// the bundled Default Standards. Project-only Standards (e.g. <c>RoundedRectangle</c>)
    /// are listed but not diffed. Components and Behaviors are not compared. Files are
    /// deserialized raw — the runtime project-initialize pass is deliberately skipped so
    /// the diff reflects what is checked in, not what is in memory.
    /// </summary>
    DiffStandardsResult Diff(string projectFilePath);
}

/// <summary>
/// Result of a <see cref="IDiffStandardsService.Diff"/> run.
/// </summary>
public class DiffStandardsResult
{
    /// <summary>
    /// One entry per variable that differs between the project and Default. Variables
    /// present on only one side are also reported here with
    /// <see cref="StandardVariableDiff.Kind"/> set accordingly.
    /// </summary>
    public List<StandardVariableDiff> Differences { get; } = new List<StandardVariableDiff>();

    /// <summary>
    /// Standards present in the project but not in the Default template
    /// (e.g. <c>RoundedRectangle</c>, <c>ColoredCircle</c>). These are not diffed.
    /// </summary>
    public List<string> ProjectOnlyStandards { get; } = new List<string>();

    /// <summary>
    /// Standards present in the Default template but not in the project. These are not
    /// diffed; the absence is reported as a single entry so a downstream tool can decide
    /// whether to treat it as drift.
    /// </summary>
    public List<string> MissingFromProject { get; } = new List<string>();

    /// <summary>True when no differences were found and no Default Standards are missing.</summary>
    public bool HasDrift => Differences.Count > 0 || MissingFromProject.Count > 0;
}

/// <summary>
/// A single drift entry: one variable on one Standard in one state.
/// </summary>
public class StandardVariableDiff
{
    public string StandardName { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public string VariableName { get; set; } = string.Empty;
    public StandardVariableDiffKind Kind { get; set; }

    /// <summary>Display string of the value in the project (or <c>(unset)</c>).</summary>
    public string ProjectValue { get; set; } = string.Empty;

    /// <summary>Display string of the value in the Default template (or <c>(unset)</c>).</summary>
    public string DefaultValue { get; set; } = string.Empty;
}

/// <summary>
/// Categorizes how a variable differs from the Default Standard.
/// </summary>
public enum StandardVariableDiffKind
{
    /// <summary>Variable exists on both sides but the value differs.</summary>
    Changed,

    /// <summary>Variable exists in the project but not in the Default template.</summary>
    AddedInProject,

    /// <summary>Variable exists in the Default template but not in the project.</summary>
    RemovedFromProject
}
