using Gum.DataTypes;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.ToolStates;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;

namespace Gum.Plugins.InternalPlugins.SvgExportPlugin;

[Export(typeof(PluginBase))]
internal class MainSvgExportPlugin : InternalPlugin
{
    private MenuItem _exportSvgMenuItem;
    private readonly ISelectedState _selectedState;
    private readonly IProjectState _projectState;

    public MainSvgExportPlugin()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _projectState = Locator.GetRequiredService<IProjectState>();
    }

    public override void StartUp()
    {
        _exportSvgMenuItem = AddMenuItem("File", "Export", "Export to SVG");
        _exportSvgMenuItem.IsEnabled = false;
        _exportSvgMenuItem.Click += HandleExportSvgClicked;

        this.ElementSelected += HandleElementSelected;

        RefreshMenuItem();
    }

    private void HandleElementSelected(ElementSave? element)
    {
        RefreshMenuItem();
    }

    private void RefreshMenuItem()
    {
        var element = _selectedState.SelectedElement;

        bool isExportable = element is ScreenSave or ComponentSave;

        if (isExportable)
        {
            _exportSvgMenuItem.Header = $"Export {element!.Name} to SVG";
            _exportSvgMenuItem.IsEnabled = true;
        }
        else
        {
            _exportSvgMenuItem.Header = "Export to SVG";
            _exportSvgMenuItem.IsEnabled = false;
        }
    }

    private void HandleExportSvgClicked(object? sender, System.Windows.RoutedEventArgs e)
    {
        var element = _selectedState.SelectedElement;
        if (element is not (ScreenSave or ComponentSave))
        {
            return;
        }

        var projectSave = _projectState.GumProjectSave;
        if (projectSave == null)
        {
            _guiCommands.PrintOutput("No project is loaded.");
            return;
        }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            DefaultExt = ".svg",
            Filter = "SVG Files (*.svg)|*.svg",
            FileName = element.Name + ".svg",
        };

        if (dlg.ShowDialog() != true)
        {
            return;
        }

        string gumCliPath = FindGumCliPath();

        if (gumCliPath == null)
        {
            _guiCommands.PrintOutput("Could not find gumcli. Expected in GumCli subfolder next to Gum.exe.");
            return;
        }

        string projectPath = projectSave.FullFileName;
        string elementName = element.Name;
        string outputPath = dlg.FileName;

        RunGumCliSvgExport(gumCliPath, projectPath, elementName, outputPath);
    }

    private void RunGumCliSvgExport(string gumCliPath, string projectPath, string elementName, string outputPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = gumCliPath,
                Arguments = $"svg \"{projectPath}\" \"{elementName}\" --output \"{outputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _guiCommands.PrintOutput("Failed to start gumcli process.");
                return;
            }

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                _guiCommands.PrintOutput(stdout.TrimEnd());
            }
            else
            {
                _guiCommands.PrintOutput($"SVG export failed (exit code {process.ExitCode}): {stderr.TrimEnd()}");
            }
        }
        catch (Exception ex)
        {
            _guiCommands.PrintOutput($"SVG export error: {ex.Message}");
        }
    }

    private static string? FindGumCliPath()
    {
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        string cliPath = Path.Combine(exeDir, "GumCli", "gumcli.exe");

        if (File.Exists(cliPath))
        {
            return cliPath;
        }

        return null;
    }
}
