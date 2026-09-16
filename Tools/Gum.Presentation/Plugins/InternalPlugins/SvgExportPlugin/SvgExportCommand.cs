using System;
using System.IO;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Services;
using Gum.Services.Dialogs;

namespace Gum.Plugins.InternalPlugins.SvgExportPlugin;

/// <summary>
/// Orchestrates exporting an element to SVG: prompts for an output path and runs gumcli's SVG
/// export logic in-process. Called by <see cref="MainSvgExportPlugin"/>.
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
    /// Locates the bundled gumcli's managed assembly, expected at <c>GumCli/gumcli.dll</c> next to
    /// the tool (published framework-dependent - see build-and-release.yml). Returns null if it
    /// does not exist. Virtual so tests can supply a deterministic result.
    /// </summary>
    protected virtual string? FindGumCliPath()
    {
        string cliPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GumCli", "gumcli.dll");
        return File.Exists(cliPath) ? cliPath : null;
    }

    /// <summary>
    /// Runs gumcli's SVG export in-process and prints the result. Loads <paramref name="gumCliPath"/>
    /// into an isolated <see cref="IsolatedPluginHost"/> rather than spawning gumcli as a subprocess
    /// (issue #4723) - this avoids shipping a second self-contained .NET runtime purely to run SVG
    /// export, while keeping gumcli's static state (ObjectFinder.Self, RenderingLibrary.SystemManagers)
    /// isolated from the tool's own. Virtual so tests can observe the invocation without loading a
    /// real gumcli.dll.
    /// </summary>
    protected virtual void RunGumCliSvgExport(
        string gumCliPath, string projectPath, string elementName, string outputPath)
    {
        try
        {
            using IsolatedPluginHost host = new(gumCliPath);
            object? error = host.InvokeStaticMethod(
                gumCliPath,
                "Gum.Cli.Commands.SvgCommand",
                "ExportSvgInProcess",
                new object?[] { projectPath, elementName, outputPath });

            if (error is string errorMessage)
            {
                _guiCommands.PrintOutput($"SVG export failed: {errorMessage}");
            }
            else
            {
                _guiCommands.PrintOutput($"SVG written to: {outputPath}");
            }
        }
        catch (Exception e)
        {
            _guiCommands.PrintOutput($"SVG export error: {e.Message}");
        }
    }
}
