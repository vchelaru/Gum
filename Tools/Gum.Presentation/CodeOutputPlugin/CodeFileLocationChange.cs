using Gum.ProjectServices.CodeGeneration;
using System.Collections.Generic;

namespace CodeOutputPlugin;

/// <summary>
/// Decides whether a code settings edit moves where code files belong, and describes the change
/// for the migration prompt. Only the output library (it renames classes, and so files), the code
/// project root, the generated code folder and its prefix move files; namespace and inheritance settings change
/// what is inside the files, not where they are (<see cref="CustomCodeHeaderChange"/> covers those).
/// </summary>
public class CodeFileLocationChange
{
    /// <summary>
    /// A description such as "Output Library from MonoGame (deprecated) to Gum Forms (recommended)",
    /// one clause per changed setting, or null when no setting that moves files changed.
    /// </summary>
    public string? Describe(CodeOutputProjectSettings before, CodeOutputProjectSettings after)
    {
        List<string> changes = new List<string>();

        if (before.OutputLibrary != after.OutputLibrary)
        {
            changes.Add($"Output Library from {LibraryName(before.OutputLibrary)} to {LibraryName(after.OutputLibrary)}");
        }
        AddIfChanged(changes, "Code Project Root", before.CodeProjectRoot, after.CodeProjectRoot);
        AddIfChanged(changes, "Generated Code Folder", before.GeneratedCodeFolder, after.GeneratedCodeFolder);
        AddIfChanged(changes, "Generated Code Folder Prefix", before.GeneratedCodeFolderPrefix, after.GeneratedCodeFolderPrefix);

        return changes.Count == 0 ? null : string.Join(", ", changes);
    }

    private static void AddIfChanged(List<string> changes, string setting, string? before, string? after)
    {
        before ??= string.Empty;
        after ??= string.Empty;
        if (before != after)
        {
            changes.Add($"{setting} from {Shown(before)} to {Shown(after)}");
        }
    }

    private static string Shown(string value) => value.Length == 0 ? "(none)" : value;

    private static string LibraryName(OutputLibrary library) =>
        CodeOutputSettingsMembers.LibraryNames.TryGetValue(library, out string? name) ? name : library.ToString();
}
