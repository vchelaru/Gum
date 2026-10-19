using Gum.Renderables;
using Raylib_cs;
using RaylibGum;
using Shouldly;

namespace RaylibGum.Tests;

/// <summary>
/// Exercises <see cref="GumService.Uninitialize"/> on Raylib, where the reset logic
/// mostly lives behind <c>#if XNALIKE</c> and needs a Raylib-specific mirror (#3557).
/// </summary>
public class GumServiceUninitializeTests
{
    [Fact]
    public void Uninitialize_ResetsTextDefaultFont()
    {
        Font original = Text.DefaultFont;

        try
        {
            // Uninitialize is a no-op on a service that is not initialized (#6066), so the test must
            // not depend on an earlier test having initialized it.
            GumService.Default.InitializeForTesting();

            Text.DefaultFont = Raylib.GetFontDefault();
            Text.DefaultFont.BaseSize.ShouldBeGreaterThan(0);

            GumService.Default.Uninitialize();

            Text.DefaultFont.BaseSize.ShouldBe(0);
        }
        finally
        {
            Text.DefaultFont = original;
            TestAssemblyInitialize.ApplyDefaultTestState();
        }
    }
}
