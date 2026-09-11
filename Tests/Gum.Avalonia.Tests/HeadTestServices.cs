using Gum.Avalonia.Services;
using Gum.Services;
using Gum.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Builds the head's real service graph the way <c>Program.CreateHostBuilder</c> does, but on an
/// in-memory configuration and a temp settings file so tests never touch the user's settings.
/// </summary>
public static class HeadTestServices
{
    /// <summary>Builds the container.</summary>
    public static ServiceProvider Build()
    {
        string settingsPath = Path.Combine(Path.GetTempPath(), "GumAvaloniaTests", Guid.NewGuid().ToString("N"), "appsettings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath, "{}");

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile(settingsPath, optional: false, reloadOnChange: false)
            .Build();

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.ConfigureWritable<ThemeSettings>(configuration, nameof(ThemeSettings), settingsPath);
        services.ConfigureWritable<LayoutSettings>(configuration, nameof(LayoutSettings), settingsPath);
        services.AddGumCore();
        services.AddGumAvalonia();
        ServiceProvider provider = services.BuildServiceProvider();
        // The plugin host and a few not-yet-drained services still reach the container through the locator.
        Locator.Register(provider);
        return provider;
    }
}
