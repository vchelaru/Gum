namespace Gum.Services;

public record GridSnapWarningInfo(bool HasWarning, string? WarningText);

/// <summary>
/// Computes the "won't fully snap" warning shown above the main editor canvas when Snap to Grid
/// is enabled but the current selection includes an object using non-pixel units - grid snap only
/// applies to pixel-based positioning/sizing (issue #4137).
/// </summary>
public interface IGridSnapWarningService
{
    GridSnapWarningInfo GetInfo();
}
