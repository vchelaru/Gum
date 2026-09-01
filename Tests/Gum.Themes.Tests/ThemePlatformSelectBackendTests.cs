// ThemePlatform (internal) is linked from the same Themes/Shared/ThemePlatform.cs source file into
// every theme .MonoGame assembly this project references, so an unaliased reference is ambiguous --
// see the Aliases="global,ThemePlatformHost" on the Bubblegum ProjectReference in this project's
// .csproj. Any one theme assembly's copy is equally valid to test against; Bubblegum is arbitrary.
extern alias ThemePlatformHost;

using KernSmith;
using Shouldly;
using ThemePlatform = ThemePlatformHost::Gum.Themes.ThemePlatform;

// Deliberately NOT namespace Gum.Themes.Tests (this project's usual convention): a namespace nested
// under Gum.Themes makes the compiler search that enclosing namespace across EVERY referenced
// assembly for an unqualified name -- reintroducing the exact CS0433 ambiguity the extern alias
// above exists to avoid.
namespace GumThemePlatformTests;

/// <summary>
/// Pins #4564: on browser-wasm, <c>ThemePlatform.WireInMemoryFontCreator</c> must select the
/// StbTrueType rasterizer backend instead of the native-only FreeType default, or every theme font
/// fails to bake (KernSmith throws, and -- before #4563's caching fix -- the failure retried on every
/// request, freezing the page on theme switch). <c>SelectBackend</c> is the extracted, pure decision
/// so this is testable without a real browser host or GraphicsDevice.
/// </summary>
public class ThemePlatformSelectBackendTests
{
    [Fact]
    public void SelectBackend_WhenNotBrowser_ReturnsNull()
    {
        var (savedIsBrowser, savedResolve, savedForce) = SaveSeams();
        try
        {
            ThemePlatform.IsBrowserPlatform = () => false;
            ThemePlatform.ResolveStbTrueTypeRasterizerType = () =>
                throw new System.InvalidOperationException("Should not be consulted off-browser.");

            ThemePlatform.SelectBackend().ShouldBeNull();
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
        System.Type? forcedType = null;
        try
        {
            ThemePlatform.IsBrowserPlatform = () => true;
            ThemePlatform.ResolveStbTrueTypeRasterizerType = () => typeof(ThemePlatformSelectBackendTests);
            ThemePlatform.ForceStaticConstructor = type => forcedType = type;

            RasterizerBackend? backend = ThemePlatform.SelectBackend();

            backend.ShouldBe(RasterizerBackend.StbTrueType);
            forcedType.ShouldBe(typeof(ThemePlatformSelectBackendTests));
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
            ThemePlatform.IsBrowserPlatform = () => true;
            ThemePlatform.ResolveStbTrueTypeRasterizerType = () => null;
            ThemePlatform.ForceStaticConstructor = _ => forceCalled = true;

            ThemePlatform.SelectBackend().ShouldBeNull(
                "so the existing PlatformNotSupportedException from GumFontGenerator.Generate still " +
                "fires and names the missing package, instead of silently misbehaving");
            forceCalled.ShouldBeFalse();
        }
        finally
        {
            RestoreSeams(savedIsBrowser, savedResolve, savedForce);
        }
    }

    private static (System.Func<bool>, System.Func<System.Type?>, System.Action<System.Type>) SaveSeams() =>
        (ThemePlatform.IsBrowserPlatform, ThemePlatform.ResolveStbTrueTypeRasterizerType, ThemePlatform.ForceStaticConstructor);

    private static void RestoreSeams(
        System.Func<bool> isBrowser, System.Func<System.Type?> resolve, System.Action<System.Type> force)
    {
        ThemePlatform.IsBrowserPlatform = isBrowser;
        ThemePlatform.ResolveStbTrueTypeRasterizerType = resolve;
        ThemePlatform.ForceStaticConstructor = force;
    }
}
