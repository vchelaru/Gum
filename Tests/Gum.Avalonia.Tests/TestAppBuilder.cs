using Avalonia;
using Avalonia.Headless;
using Gum.Avalonia;
using Gum.Avalonia.Tests;
using Microsoft.Extensions.DependencyInjection;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Gum.Avalonia.Tests;

/// <summary>Boots the head's <see cref="App"/> on Avalonia's headless platform for the xunit tests.</summary>
public static class TestAppBuilder
{
    /// <summary>The container the headless app runs on; shared by every test in the assembly.</summary>
    public static IServiceProvider Services { get; } = HeadTestServices.Build();

    /// <summary>Called by the Avalonia xunit integration.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure(() => new App(Services, new HeadOptions(null, null)))
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });
}
