using Microsoft.Playwright;
using Xunit;

namespace KniGumInCode.PlaywrightTests;

/// <summary>
/// Starts the installed Chrome, opens the sample page and waits until the Gum automation bridge
/// (window.gumAutomation) reports the game is running. The sample must already be served; see the README.
/// </summary>
public class GumBrowserFixture : IAsyncLifetime
{
    // Where the published sample is served. Override with the GUM_SAMPLE_URL environment variable.
    public static string Url => Environment.GetEnvironmentVariable("GUM_SAMPLE_URL") ?? "http://localhost:5088/";

    IPlaywright? _playwright;
    IBrowser? _browser;

    public IPage Page { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();

        // Channel = "chrome" uses the Chrome that is already installed, so nothing is downloaded
        // (no "playwright install"). SwiftShader gives headless Chrome a software WebGL.
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "chrome",
            Headless = Environment.GetEnvironmentVariable("GUM_HEADED") != "1",
            Args = new[] { "--enable-unsafe-swiftshader" },
        });

        Page = await _browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 768 },
        });
        await Page.GotoAsync(Url);

        // The Blazor runtime downloads, then the first frame creates the game, then the bridge installs.
        await Page.WaitForFunctionAsync(
            "() => { try { return window.gumAutomation && window.gumAutomation.ready(); } catch { return false; } }",
            null,
            new PageWaitForFunctionOptions { Timeout = 120_000 });
    }

    public async Task DisposeAsync()
    {
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }
}
