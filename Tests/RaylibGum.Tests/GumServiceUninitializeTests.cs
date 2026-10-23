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

        // Uninitialize is a no-op on a service that was never initialized (#6066), so this test must not
        // depend on an earlier test class having initialized the default service.
        GumService.Default.InitializeForTesting();

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

    // #6491: Uninitialize used to set RelativeDirectory to "Content/" whether or not a project had
    // pointed it anywhere. Raylib never uses "Content/" (SystemManagers leaves it alone), so every text
    // after an Uninitialize resolved "Arial 18" to a Content/FontCache .fnt instead of system Arial.
    [Fact]
    public void Uninitialize_WithNoProjectLoaded_LeavesRelativeDirectoryUnchanged()
    {
        string original = ToolsUtilities.FileManager.RelativeDirectory;
        Font originalFont = Text.DefaultFont;
        try
        {
            GumService.Default.InitializeForTesting();

            GumService.Default.Uninitialize();

            ToolsUtilities.FileManager.RelativeDirectory.ShouldBe(original);
        }
        finally
        {
            ToolsUtilities.FileManager.RelativeDirectory = original;
            Text.DefaultFont = originalFont;
            TestAssemblyInitialize.ApplyDefaultTestState();
        }
    }
}
