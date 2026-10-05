using System.Linq;
using Gum.Converters;
using Gum.DataTypes;
using Gum.ToolStates;
using Gum.Wireframe;

namespace Gum.Services;

public class GridSnapWarningService : IGridSnapWarningService
{
    private readonly ISelectionManager _selectionManager;
    private readonly ISelectedState _selectedState;

    public GridSnapWarningService(ISelectionManager selectionManager, ISelectedState selectedState)
    {
        _selectionManager = selectionManager;
        _selectedState = selectedState;
    }

    public void SelectOffender(InstanceSave instance)
    {
        _selectedState.SelectedInstance = instance;
    }

    public GridSnapWarningInfo GetInfo()
    {
        if (!_selectionManager.SnapToGrid || !_selectionManager.HasSelection)
        {
            return new GridSnapWarningInfo(false, null, []);
        }

        var selectedGues = _selectionManager.SelectedGues;

        var nonPixelGues = selectedGues.Where(gue =>
            !gue.XUnits.GetIsPixelBased() ||
            !gue.YUnits.GetIsPixelBased() ||
            !gue.WidthUnits.GetIsPixelBased() ||
            !gue.HeightUnits.GetIsPixelBased()).ToList();

        if (nonPixelGues.Count == 0)
        {
            return new GridSnapWarningInfo(false, null, []);
        }

        string warningText = selectedGues.Count == 1
            ? $"Snap to Grid: {selectedGues[0].Name} uses non-pixel units and won't fully snap"
            : "Snap to Grid: one or more selected objects use non-pixel units and won't fully snap";

        var offenders = nonPixelGues.Select(gue => gue.Tag).OfType<InstanceSave>().ToList();

        return new GridSnapWarningInfo(true, warningText, offenders);
    }
}
