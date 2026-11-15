using System.Drawing;

namespace Gum.Services;

/// <param name="CategoryColorName">The palette name of the edited state's category when it has a color, else null; a head draws the banner in that color's theme variant instead of <paramref name="StateBackground"/>.</param>
public record StateEditingIndicatorInfo(bool HasStateInformation, string? StateInformation, Color StateBackground, string? CategoryColorName = null);

/// <summary>
/// Computes the "Editing state X" / "Displaying custom (animated) state" banner shown above the
/// Variables and Alignment tabs when the selected state isn't the element's default state.
/// </summary>
public interface IStateEditingIndicatorService
{
    StateEditingIndicatorInfo GetInfo();
}
