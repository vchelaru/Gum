using System.Collections.Generic;
using Gum.DataTypes;

namespace Gum.Services;

/// <param name="Offenders">The selected instances using non-pixel units, so the UI can offer to select each one.</param>
public record GridSnapWarningInfo(bool HasWarning, string? WarningText, IReadOnlyList<InstanceSave> Offenders);

/// <summary>
/// Computes the "won't fully snap" warning shown above the main editor canvas when Snap to Grid
/// is enabled but the current selection includes an object using non-pixel units - grid snap only
/// applies to pixel-based positioning/sizing (issue #4137).
/// </summary>
public interface IGridSnapWarningService
{
    GridSnapWarningInfo GetInfo();

    /// <summary>Narrows the selection to <paramref name="instance"/> (issue #5703).</summary>
    void SelectOffender(InstanceSave instance);
}
