using System.Collections.Generic;
using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// What a <see cref="CodeFileMigrationStep"/> does to its <see cref="CodeFileMigrationStep.Source"/>.
/// The Skip actions leave the file untouched and say why.
/// </summary>
public enum CodeFileMigrationAction
{
    /// <summary>A <c>.Generated.cs</c> at an old path. Derived data, so removing it is lossless.</summary>
    RemoveGenerated,

    /// <summary>A custom code file holding only the untouched stub. Codegen writes a fresh one at the current path.</summary>
    RemoveUntouchedStub,

    /// <summary>
    /// A custom code file with real code, moved to <see cref="CodeFileMigrationStep.Destination"/>
    /// (replacing an untouched stub there, if any) with its namespace and class name rewritten.
    /// </summary>
    MoveCustomCode,

    /// <summary>
    /// The custom code file at <see cref="CodeFileMigrationStep.Destination"/> also has real code, so
    /// neither is touched and the user merges them by hand.
    /// </summary>
    SkipConflict,

    /// <summary>
    /// The project has no element by this name, so this is a deleted element's file rather than a
    /// moved one. Left to the orphan rows' Delete File action.
    /// </summary>
    SkipNoElement,
}

/// <summary>One file in a <see cref="CodeFileMigrationPlan"/>.</summary>
public class CodeFileMigrationStep
{
    /// <summary>The element the file belongs to, read from the generated file's <c>//Code for</c> header.</summary>
    public string ElementName { get; }

    /// <inheritdoc cref="CodeFileMigrationAction"/>
    public CodeFileMigrationAction Action { get; }

    /// <summary>The file at its old path.</summary>
    public FilePath Source { get; }

    /// <summary>The element's current custom code path, for <see cref="CodeFileMigrationAction.MoveCustomCode"/> and <see cref="CodeFileMigrationAction.SkipConflict"/>.</summary>
    public FilePath? Destination { get; }

    public CodeFileMigrationStep(string elementName, CodeFileMigrationAction action, FilePath source, FilePath? destination = null)
    {
        ElementName = elementName;
        Action = action;
        Source = source;
        Destination = destination;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Action}: {Source}" + (Destination == null ? "" : $" -> {Destination}");
}

/// <summary>
/// What migrating code files left at old paths by a code settings change would do. Building one
/// reads the disk but never writes to it.
/// </summary>
public class CodeFileMigrationPlan
{
    /// <summary>The steps, in the order the orphans were given.</summary>
    public IReadOnlyList<CodeFileMigrationStep> Steps { get; }

    public CodeFileMigrationPlan(IReadOnlyList<CodeFileMigrationStep> steps)
    {
        Steps = steps;
    }
}
