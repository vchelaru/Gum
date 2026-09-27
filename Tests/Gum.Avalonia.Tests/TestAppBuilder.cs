using Avalonia;
using Avalonia.Headless;
using Gum.Avalonia;
using Gum.Avalonia.Tests;
using Microsoft.Extensions.DependencyInjection;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]
// One Application and one UI dispatcher for the whole assembly. Per-test isolation nulls
// Dispatcher.UIThread before each test and rebuilds it lazily without a lock, so tool work still
// running on a pool thread (font generation progress, for one) can create it against the wrong
// platform during that window and leave the next test's window rendering and hit-testing nothing.
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerAssembly)]

namespace Gum.Avalonia.Tests;

/// <summary>Boots the head's <see cref="App"/> on Avalonia's headless platform for the xunit tests.</summary>
public static class TestAppBuilder
{
    /// <summary>The container the headless app runs on; shared by every test in the assembly.</summary>
    public static IServiceProvider Services { get; } = HeadTestServices.Build();

    /// <summary>Called by the Avalonia xunit integration.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure(() => new App(Services, new HeadOptions(null, null)))
            // Real Skia text rendering rather than the headless stub, which cannot load the icon
            // font (FluentIcons) the Variables tab and States tree use.
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            // The tool's own startup steps, before any test body can raise a plugin event at a
            // manager that has not been initialized yet. The head runs them once the window opens,
            // which the headless lifetime never does.
            .AfterSetup(_ => ToolStartup.EnsureInitialized());
}
