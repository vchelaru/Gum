using Gum.DataTypes;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.ToolStates;
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace Gum.Plugins.InternalPlugins.SvgExportPlugin;

[Export(typeof(PluginBase))]
internal class MainSvgExportPlugin : PriorityPlugin
{
    private MenuItem _exportSvgMenuItem;
    private readonly ISelectedState _selectedState;
    private readonly IProjectState _projectState;
    private readonly ISvgExportCommand _svgExportCommand;

    public MainSvgExportPlugin()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _projectState = Locator.GetRequiredService<IProjectState>();
        // SVG export is plugin-specific, so the command is instantiated here rather than
        // registered app-wide. _dialogService/_guiCommands come from PluginBase's ctor.
        _svgExportCommand = new SvgExportCommand(_dialogService, _guiCommands);
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

        _svgExportCommand.ExportElementToSvg(element, projectSave);
    }
}
