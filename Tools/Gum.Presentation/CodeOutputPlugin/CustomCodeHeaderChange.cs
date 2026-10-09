using Gum.ProjectServices.CodeGeneration;
using System.Collections.Generic;

namespace CodeOutputPlugin;

/// <summary>
/// Decides whether a code settings edit changes the namespace or class header that custom code
/// files must declare, and describes the change for the update prompt (#5855). These settings
/// leave files where they are, so <see cref="CodeFileLocationChange"/> never sees them.
/// </summary>
public class CustomCodeHeaderChange
{
    /// <summary>
    /// A description such as "Root Namespace from Game to MyGame", one clause per changed setting,
    /// or null when no setting that changes custom code headers changed.
    /// </summary>
    public string? Describe(CodeOutputProjectSettings before, CodeOutputProjectSettings after)
    {
        List<string> changes = new List<string>();

        AddIfChanged(changes, "Root Namespace", before.RootNamespace ?? string.Empty, after.RootNamespace ?? string.Empty);
        AddIfChanged(changes, "Append Folder to Namespace", before.AppendFolderToNamespace.ToString(), after.AppendFolderToNamespace.ToString());
        AddIfChanged(changes, "Inheritance Location", before.InheritanceLocation.ToString(), after.InheritanceLocation.ToString());

        return changes.Count == 0 ? null : string.Join(", ", changes);
    }

    private static void AddIfChanged(List<string> changes, string setting, string before, string after)
    {
        if (before != after)
        {
            changes.Add($"{setting} from {Shown(before)} to {Shown(after)}");
        }
    }

    private static string Shown(string value) => value.Length == 0 ? "(none)" : value;
}
