using Gum.Commands;
using Gum.DataTypes;
using Gum.Plugins.BaseClasses;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace Gum.Plugins.InternalPlugins.SvgExportPlugin;

// As of ADR-0005 Phase 3, the exportability/guard decisions live in SvgExportMenuLogic
// (Gum.Presentation) so they can be unit tested headlessly. This plugin builds the WPF menu item
// and forwards clicks; the actual gumcli invocation stays in SvgExportCommand.
[Export(typeof(PluginBase))]
internal class MainSvgExportPlugin : PriorityPlugin
{
    private MenuItem _exportSvgMenuItem;
    private readonly ISvgExportCommand _svgExportCommand;
    private readonly SvgExportMenuLogic _svgExportMenuLogic;

    [ImportingConstructor]
    public MainSvgExportPlugin(
        IDialogService dialogService,
        IGuiCommands guiCommands,
        ISelectedState selectedState,
        IProjectState projectState)
    {
        // SVG export is plugin-specific, so the command is instantiated here rather than
        // registered app-wide. These services are injected (they are also property-injected
        // into PluginBase for use in StartUp()/handlers).
        _svgExportCommand = new SvgExportCommand(dialogService, guiCommands);
        _svgExportMenuLogic = new SvgExportMenuLogic(selectedState, projectState, guiCommands);
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
        bool isExportable = _svgExportMenuLogic.GetIsExportable(out var element);

        _exportSvgMenuItem.Header = isExportable ? $"Export {element!.Name} to SVG" : "Export to SVG";
        _exportSvgMenuItem.IsEnabled = isExportable;
    }

    private void HandleExportSvgClicked(object? sender, System.Windows.RoutedEventArgs e)
    {
        if (!_svgExportMenuLogic.TryPrepareExport(out var element, out var projectSave))
        {
            return;
        }

        _svgExportCommand.ExportElementToSvg(element!, projectSave!);
    }
}
