using System.Drawing;

namespace Gum.Services;

public record StateEditingIndicatorInfo(bool HasStateInformation, string? StateInformation, Color StateBackground);

/// <summary>
/// Computes the "Editing state X" / "Displaying custom (animated) state" banner shown above the
/// Variables and Alignment tabs when the selected state isn't the element's default state.
/// </summary>
public interface IStateEditingIndicatorService
{
    StateEditingIndicatorInfo GetInfo();
}
