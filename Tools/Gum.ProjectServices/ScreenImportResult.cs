using Gum.DataTypes;

namespace Gum.ProjectServices;

/// <summary>Outcome of <see cref="IScreenImportService.ImportScreen"/>.</summary>
public class ScreenImportResult
{
    public bool Success { get; private init; }

    /// <summary>Set when <see cref="Success"/> is false: the name that already exists in the project.</summary>
    public string? ConflictingScreenName { get; private init; }

    /// <summary>Set when <see cref="Success"/> is true: the screen that was added.</summary>
    public ScreenSave? ImportedScreen { get; private init; }

    public static ScreenImportResult Conflict(string screenName) =>
        new() { Success = false, ConflictingScreenName = screenName };

    public static ScreenImportResult Ok(ScreenSave screenSave) =>
        new() { Success = true, ImportedScreen = screenSave };
}
