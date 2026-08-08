using System.Linq;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Wireframe;

namespace Gum.Services;

public class GridSnapWarningService : IGridSnapWarningService
{
    private readonly ISelectionManager _selectionManager;

    public GridSnapWarningService(ISelectionManager selectionManager)
    {
        _selectionManager = selectionManager;
    }

    public GridSnapWarningInfo GetInfo()
    {
        if (!_selectionManager.SnapToGrid || !_selectionManager.HasSelection)
        {
            return new GridSnapWarningInfo(false, null);
        }

        var selectedGues = _selectionManager.SelectedGues;

        bool hasNonPixelUnit = selectedGues.Any(gue =>
            !gue.XUnits.GetIsPixelBased() ||
            !gue.YUnits.GetIsPixelBased() ||
            !gue.WidthUnits.GetIsPixelBased() ||
            !gue.HeightUnits.GetIsPixelBased());

        if (!hasNonPixelUnit)
        {
            return new GridSnapWarningInfo(false, null);
        }

        string warningText = selectedGues.Count == 1
            ? $"Snap to Grid: {selectedGues[0].Name} uses non-pixel units and won't fully snap"
            : "Snap to Grid: one or more selected objects use non-pixel units and won't fully snap";

        return new GridSnapWarningInfo(true, warningText);
    }
}
