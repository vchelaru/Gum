using Microsoft.Extensions.Configuration;

namespace Gum.Settings;

/// <summary>Registers the per-user <c>appsettings.json</c> with the configuration builder.</summary>
public static class AppSettingsConfigurationExtensions
{
    /// <summary>
    /// Adds <paramref name="fileName"/> (relative to the builder's base path) as a required JSON file.
    /// It does not watch the file: <see cref="WritableOptions{T}.Update"/> is the only writer and
    /// reloads after saving, while a watcher's own reload runs on a thread-pool thread and throws
    /// when it meets the save, or an antivirus or sync client, holding the file.
    /// </summary>
    public static IConfigurationBuilder AddAppSettingsJsonFile(this IConfigurationBuilder builder, string fileName)
    {
        return builder.AddJsonFile(fileName, optional: false, reloadOnChange: false);
    }
}
