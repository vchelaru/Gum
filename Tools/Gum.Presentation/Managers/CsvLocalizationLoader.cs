using Gum.Localization;

namespace Gum.Managers;

/// <summary>
/// Thin wrapper around <see cref="LocalizationServiceExtensions.AddDatabaseFromCsv"/> (which
/// depends on CsvLibrary) so headless callers can reach it via <see cref="ICsvLocalizationLoader"/>.
/// </summary>
public class CsvLocalizationLoader : ICsvLocalizationLoader
{
    /// <inheritdoc/>
    public void AddDatabaseFromCsv(ILocalizationService service, string fileName, char delimiter) =>
        service.AddDatabaseFromCsv(fileName, delimiter);
}
