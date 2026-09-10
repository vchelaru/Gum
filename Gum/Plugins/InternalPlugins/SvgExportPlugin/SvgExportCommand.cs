using System;
using System.Diagnostics;
using System.IO;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Services.Dialogs;

namespace Gum.Plugins.InternalPlugins.SvgExportPlugin;

/// <summary>
/// Orchestrates exporting an element to SVG: prompts for an output path and shells out to
/// the gumcli tool. Called by <see cref="MainSvgExportPlugin"/>.
/// </summary>
internal interface ISvgExportCommand
{
    /// <summary>
    /// Prompts the user for an output path and exports the given element to SVG via gumcli.
    /// Does nothing if the user cancels the save dialog.
    /// </summary>
    void ExportElementToSvg(ElementSave element, GumProjectSave projectSave);
}

/// <inheritdoc/>
internal class SvgExportCommand : ISvgExportCommand
{
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;

    public SvgExportCommand(IDialogService dialogService, IGuiCommands guiCommands)
    {
        _dialogService = dialogService;
        _guiCommands = guiCommands;
    }

    /// <inheritdoc/>
    public void ExportElementToSvg(ElementSave element, GumProjectSave projectSave)
    {
        string? outputPath = _dialogService.SaveFile(new SaveFileDialogOptions
        {
            Title = "Export to SVG",
            Filter = "SVG Files (*.svg)|*.svg",
            FileName = element.Name + ".svg",
        });

        if (string.IsNullOrEmpty(outputPath))
        {
            return;
        }

        string? gumCliPath = FindGumCliPath();
        if (gumCliPath == null)
        {
            _guiCommands.PrintOutput("Could not find gumcli. Expected in GumCli subfolder next to Gum.exe.");
            return;
        }

        RunGumCliSvgExport(gumCliPath, projectSave.FullFileName, element.Name, outputPath);
    }

    /// <summary>
    /// Builds the gumcli argument string for an SVG export, quoting each value so paths
    /// and element names containing spaces are passed as single arguments.
    /// </summary>
    internal string BuildSvgExportArguments(string projectPath, string elementName, string outputPath)
    {
        return $"svg \"{projectPath}\" \"{elementName}\" --output \"{outputPath}\"";
    }

    /// <summary>
    /// Locates the bundled gumcli, expected in a GumCli subfolder next to the tool: the native
    /// executable (<c>gumcli.exe</c> on Windows, <c>gumcli</c> elsewhere), else <c>gumcli.dll</c>
    /// to run through <c>dotnet</c>. Returns null if none exists. Virtual so tests can supply a
    /// deterministic result.
    /// </summary>
    protected virtual string? FindGumCliPath()
    {
        string cliFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GumCli");
        string executableName = OperatingSystem.IsWindows() ? "gumcli.exe" : "gumcli";

        foreach (string candidate in new[] { executableName, "gumcli.dll" })
        {
            string cliPath = Path.Combine(cliFolder, candidate);
            if (File.Exists(cliPath))
            {
                return cliPath;
            }
        }

        return null;
    }

    /// <summary>
    /// The process to start for <paramref name="gumCliPath"/>: the file itself when it is an
    /// executable, or <c>dotnet</c> with the assembly prepended when it is a <c>.dll</c>.
    /// </summary>
    internal static (string fileName, string arguments) BuildProcessInvocation(string gumCliPath, string arguments)
    {
        if (gumCliPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return ("dotnet", $"\"{gumCliPath}\" {arguments}");
        }

        return (gumCliPath, arguments);
    }

    /// <summary>
    /// Runs gumcli to perform the SVG export and prints its output. Virtual so tests can
    /// observe the invocation without spawning a real process.
    /// </summary>
    protected virtual void RunGumCliSvgExport(
        string gumCliPath, string projectPath, string elementName, string outputPath)
    {
        try
        {
            (string fileName, string arguments) = BuildProcessInvocation(
                gumCliPath, BuildSvgExportArguments(projectPath, elementName, outputPath));
            ProcessStartInfo startInfo = new()
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using Process? process = Process.Start(startInfo);
            if (process == null)
            {
                _guiCommands.PrintOutput("Failed to start gumcli process.");
                return;
            }

            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                _guiCommands.PrintOutput(standardOutput.TrimEnd());
            }
            else
            {
                _guiCommands.PrintOutput(
                    $"SVG export failed (exit code {process.ExitCode}): {standardError.TrimEnd()}");
            }
        }
        catch (Exception e)
        {
            _guiCommands.PrintOutput($"SVG export error: {e.Message}");
        }
    }
}
