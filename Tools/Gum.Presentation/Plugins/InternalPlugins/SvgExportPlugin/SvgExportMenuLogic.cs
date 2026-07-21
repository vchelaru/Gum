using Gum.Commands;
using Gum.DataTypes;
using Gum.ToolStates;

namespace Gum.Plugins.InternalPlugins.SvgExportPlugin;

/// <summary>
/// Decision logic behind the "Export to SVG" menu item, relocated out of the WPF-hosted
/// <c>MainSvgExportPlugin</c> (ADR-0005 Phase 3) so it can be unit tested headlessly. The actual
/// export (shelling out to gumcli) stays in <c>SvgExportCommand</c>, which this class does not
/// invoke - the plugin calls it once this class confirms the export can proceed.
/// </summary>
public class SvgExportMenuLogic
{
    private readonly ISelectedState _selectedState;
    private readonly IProjectState _projectState;
    private readonly IGuiCommands _guiCommands;

    public SvgExportMenuLogic(ISelectedState selectedState, IProjectState projectState, IGuiCommands guiCommands)
    {
        _selectedState = selectedState;
        _projectState = projectState;
        _guiCommands = guiCommands;
    }

    /// <summary>
    /// Whether the currently-selected element can be exported to SVG (Screens and Components
    /// only), along with the element itself (null when nothing exportable is selected).
    /// </summary>
    public bool GetIsExportable(out ElementSave? element)
    {
        element = _selectedState.SelectedElement;
        return element is ScreenSave or ComponentSave;
    }

    /// <summary>
    /// Validates that an export can proceed: the selection is exportable and a project is loaded.
    /// Prints an explanatory message via <see cref="IGuiCommands"/> and returns false if not.
    /// </summary>
    public bool TryPrepareExport(out ElementSave? element, out GumProjectSave? projectSave)
    {
        element = _selectedState.SelectedElement;
        if (element is not (ScreenSave or ComponentSave))
        {
            projectSave = null;
            return false;
        }

        projectSave = _projectState.GumProjectSave;
        if (projectSave == null)
        {
            _guiCommands.PrintOutput("No project is loaded.");
            return false;
        }

        return true;
    }
}
