using Gum.Localization;

namespace Gum.Managers;

/// <summary>
/// Loads a localization database from a CSV file into an <see cref="ILocalizationService"/>. The
/// only implementation depends on CsvLibrary, which targets net8.0-windows and can't be
/// referenced from this headless assembly, so this interface exists to keep that dependency out
/// of headless callers (e.g. FileCommands).
/// </summary>
public interface ICsvLocalizationLoader
{
    /// <summary>
    /// Parses <paramref name="fileName"/> as a CSV and adds its entries to <paramref name="service"/>.
    /// </summary>
    void AddDatabaseFromCsv(ILocalizationService service, string fileName, char delimiter);
}
