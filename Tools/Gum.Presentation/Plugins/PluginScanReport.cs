using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.Plugins;

/// <summary>
/// What happened to one file during the plugin-folder scan.
/// </summary>
public enum PluginFileOutcome
{
    /// <summary>Loaded as a managed assembly and added to the MEF catalog.</summary>
    Loaded,

    /// <summary>Not a managed assembly — the native DLLs a plugin ships alongside itself.</summary>
    NotManagedAssembly,

    /// <summary>Found, but could not be loaded. The only outcome a user can act on.</summary>
    LoadFailed,

    /// <summary>Loaded, but this head cannot run it (a WPF plugin on the cross-platform head).</summary>
    NotHostable,
}

/// <param name="FileName">File name only; the folder is on the report.</param>
/// <param name="CouldContainPlugins">
/// Whether the assembly references the one declaring <c>PluginBase</c>. False for a plugin's
/// dependencies, which sit in the same folder and get scanned too.
/// </param>
/// <param name="Detail">Failure reason, when <paramref name="Outcome"/> is a failure.</param>
public record PluginFileScan(
    string FileName,
    PluginFileOutcome Outcome,
    bool CouldContainPlugins,
    string? Detail);

/// <summary>
/// Result of scanning the plugin folder, surfaced in the "Manage Plugins" dialog. A plugin that
/// never loads is otherwise indistinguishable from one that was never installed — this says which,
/// and names the folder that was actually searched.
/// </summary>
/// <param name="ExecutablePath">
/// What <paramref name="FolderPath"/> was derived from. When the scan finds nothing, the first
/// question is whether it looked in the right place, and that is answered here rather than guessed.
/// </param>
/// <param name="FolderEntries">
/// Every file and folder actually present under <paramref name="FolderPath"/>, gathered only when
/// no plugin assembly was found. "The folder is empty" and "the folder has files the scan did not
/// match" are different bugs that produce the same count of zero.
/// </param>
public record PluginScanReport(
    string FolderPath,
    bool FolderExists,
    IReadOnlyList<PluginFileScan> Files,
    string ExecutablePath = "",
    IReadOnlyList<string>? FolderEntries = null)
{
    /// <summary>
    /// Assemblies that reference the one declaring <c>PluginBase</c>, so could hold a plugin. When
    /// this is empty, every plugin shipping as its own DLL is missing.
    /// </summary>
    public IEnumerable<PluginFileScan> PluginAssemblies => Files
        .Where(x => x.Outcome == PluginFileOutcome.Loaded && x.CouldContainPlugins);

    /// <summary>
    /// The scan as readable, copyable text, for the "Manage Plugins" dialog and the Output tab.
    /// </summary>
    public string Describe()
    {
        StringBuilder text = new();
        text.AppendLine($"Plugin folder: {FolderPath}");
        if (!string.IsNullOrEmpty(ExecutablePath))
        {
            text.AppendLine($"Derived from:  {ExecutablePath}");
        }

        if (!FolderExists)
        {
            text.AppendLine("This folder does not exist, so no plugin was loaded from it.");
            text.Append(NoPluginAssembliesAdvice);
            return text.ToString();
        }

        List<PluginFileScan> pluginAssemblies = PluginAssemblies
            .OrderBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        List<PluginFileScan> failures = Files
            .Where(x => x.Outcome == PluginFileOutcome.LoadFailed)
            .OrderBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        int dependencyCount = Files.Count(x => x.Outcome == PluginFileOutcome.Loaded && !x.CouldContainPlugins);
        int nativeCount = Files.Count(x => x.Outcome == PluginFileOutcome.NotManagedAssembly);

        text.AppendLine($".dll files found: {Files.Count} ({pluginAssemblies.Count} holding plugins, " +
            $"{dependencyCount} dependencies, {nativeCount} native, {failures.Count} failed to load)");

        text.AppendLine();
        if (pluginAssemblies.Count == 0)
        {
            AppendFolderContents(text);
            text.Append(NoPluginAssembliesAdvice);
        }
        else
        {
            text.AppendLine("Assemblies holding plugins:");
            foreach (PluginFileScan scan in pluginAssemblies)
            {
                text.AppendLine("    " + scan.FileName);
            }
        }

        if (failures.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Failed to load:");
            foreach (PluginFileScan scan in failures)
            {
                text.AppendLine($"    {scan.FileName} - {scan.Detail}");
            }
        }

        return text.ToString();
    }

    /// <summary>
    /// Lists what is actually in the folder. Reporting only a count of zero leaves "empty folder"
    /// and "the scan looked in the wrong place" indistinguishable, and they need different fixes.
    /// </summary>
    private void AppendFolderContents(StringBuilder text)
    {
        if (FolderEntries is null)
        {
            return;
        }

        if (FolderEntries.Count == 0)
        {
            text.AppendLine("The folder is completely empty - not one file or subfolder.");
        }
        else
        {
            text.AppendLine($"The folder does contain {FolderEntries.Count} other entries:");
            foreach (string entry in FolderEntries)
            {
                text.AppendLine("    " + entry);
            }
        }

        text.AppendLine();
    }

    // Plugins that ship as their own DLL are absent from the list whenever this folder is empty,
    // and the two ways that happens are worth naming rather than leaving to guesswork.
    private const string NoPluginAssembliesAdvice =
        """
        No plugin assemblies were found, so every plugin that ships as its own DLL is missing - the
        code output, Skia, Gum Forms and editor tab plugins among them.

        If you are running a downloaded release, extract the whole .zip before running Gum.exe; the
        Plugins folder has to sit next to it. If you built from source, build GumFull.sln - building
        Gum.csproj or Gum.sln on its own skips the step that copies plugins into this folder.
        """;
}
