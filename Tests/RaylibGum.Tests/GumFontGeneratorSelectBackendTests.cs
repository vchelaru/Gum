using KernSmith;
using KernSmith.Gum;
using Shouldly;

namespace RaylibGum.Tests;

/// <summary>
/// The backend <c>UseKernSmith()</c> and the themes pick when the caller passes none (#4564, #7765): on
/// browser-wasm the native-only FreeType default cannot run, so StbTrueType is selected and its static
/// constructor forced. Moved from Gum.Themes.Tests (ThemePlatformSelectBackendTests) with the logic.
/// </summary>
public class GumFontGeneratorSelectBackendTests
{
    [Fact]
    public void SelectBackend_WhenNotBrowser_ReturnsNull()
    {
        var (savedIsBrowser, savedResolve, savedForce) = SaveSeams();
        try
        {
            GumFontGenerator.IsBrowserPlatform = () => false;
            GumFontGenerator.ResolveStbTrueTypeRasterizerType = () =>
                throw new InvalidOperationException("Should not be consulted off-browser.");

            GumFontGenerator.SelectBackend().ShouldBeNull();
        }
        finally
        {
            RestoreSeams(savedIsBrowser, savedResolve, savedForce);
        }
    }

    [Fact]
    public void SelectBackend_WhenBrowserAndStbTrueTypeIsAvailable_ReturnsStbTrueTypeAndForcesRegistration()
    {
        var (savedIsBrowser, savedResolve, savedForce) = SaveSeams();
        Type? forcedType = null;
        try
        {
            GumFontGenerator.IsBrowserPlatform = () => true;
            GumFontGenerator.ResolveStbTrueTypeRasterizerType = () => typeof(GumFontGeneratorSelectBackendTests);
            GumFontGenerator.ForceStaticConstructor = type => forcedType = type;

            RasterizerBackend? backend = GumFontGenerator.SelectBackend();

            backend.ShouldBe(RasterizerBackend.StbTrueType);
            forcedType.ShouldBe(typeof(GumFontGeneratorSelectBackendTests));
        }
        finally
        {
            RestoreSeams(savedIsBrowser, savedResolve, savedForce);
        }
    }

    [Fact]
    public void SelectBackend_WhenBrowserAndStbTrueTypeIsNotReferenced_ReturnsNull()
    {
        var (savedIsBrowser, savedResolve, savedForce) = SaveSeams();
        bool forceCalled = false;
        try
        {
            GumFontGenerator.IsBrowserPlatform = () => true;
            GumFontGenerator.ResolveStbTrueTypeRasterizerType = () => null;
            GumFontGenerator.ForceStaticConstructor = _ => forceCalled = true;

            GumFontGenerator.SelectBackend().ShouldBeNull(
                "so the existing PlatformNotSupportedException from GumFontGenerator.Generate still " +
                "fires and names the missing package, instead of silently misbehaving");
            forceCalled.ShouldBeFalse();
        }
        finally
        {
            RestoreSeams(savedIsBrowser, savedResolve, savedForce);
        }
    }

    private static (Func<bool>, Func<Type?>, Action<Type>) SaveSeams() =>
        (GumFontGenerator.IsBrowserPlatform, GumFontGenerator.ResolveStbTrueTypeRasterizerType, GumFontGenerator.ForceStaticConstructor);

    private static void RestoreSeams(Func<bool> isBrowser, Func<Type?> resolve, Action<Type> force)
    {
        GumFontGenerator.IsBrowserPlatform = isBrowser;
        GumFontGenerator.ResolveStbTrueTypeRasterizerType = resolve;
        GumFontGenerator.ForceStaticConstructor = force;
    }
}
