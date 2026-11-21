using System;
using System.Collections.Generic;

namespace Gum.Diagnostics;

/// <summary>The machine and tool facts that are always in a diagnostics file.</summary>
public sealed record DiagnosticsSystemInfo(
    string GumVersion,
    string OperatingSystem,
    string DotNetRuntime,
    string CpuArchitecture,
    string DisplayScale,
    string GraphicsCard);

/// <summary>Everything the tool has to offer a diagnostics file, collected on the UI thread before the zip is built.</summary>
public sealed record DiagnosticsContent(
    IReadOnlyList<string> LogFiles,
    string ErrorsText,
    string PluginsText,
    IReadOnlyList<string> SettingsFiles,
    string? GumProjectFile,
    string? CSharpProjectFolder);

/// <summary>What the user chose in the dialog.</summary>
public sealed record DiagnosticsRequest(
    string Description,
    bool IncludeGumProject,
    bool IncludeCSharpProject);

/// <summary>Machine facts and folders the dialog needs; a seam so tests never touch the real Documents folder.</summary>
public interface IDiagnosticsEnvironment
{
    /// <summary>Gum version, OS, runtime, architecture, display scale and graphics card.</summary>
    DiagnosticsSystemInfo GetSystemInfo();

    /// <summary>The folder the zip is saved in (the user's Documents folder).</summary>
    string SaveFolder { get; }

    /// <summary>The account name to scrub from text files.</summary>
    string UserName { get; }

    /// <summary>The home folder to scrub from text files.</summary>
    string HomeFolder { get; }

    /// <summary>Today's date, for the file name.</summary>
    DateTime Today { get; }
}

/// <summary>Gathers the tool's own logs, errors, plugins, settings and project locations.</summary>
public interface IDiagnosticsContentSource
{
    /// <summary>Collects the content; call on the UI thread.</summary>
    DiagnosticsContent Collect();

    /// <summary>The loaded .gumx, or null when no project is loaded.</summary>
    string? GumProjectFile { get; }

    /// <summary>The folder of the game's C# project, or null when it cannot be located.</summary>
    string? CSharpProjectFolder { get; }
}
