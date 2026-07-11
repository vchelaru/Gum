using Gum;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests;

/// <summary>
/// Characterizes the platform-partial split of GumService (issue #3608) on the XNALIKE side:
/// members and seams that moved into GumService.XnaLike.cs must keep their pre-split behavior.
/// These are headless (constructor-only) pins for the divergent seams; the Initialize / Update /
/// Uninitialize paths that need a GraphicsDevice are pinned by MonoGameGum.IntegrationTests
/// (XNALIKE) and RaylibGum.Tests (RAYLIB).
/// </summary>
public class GumServicePartialSeamTests : BaseTestClass
{
    [Fact]
    public void GameTime_IsNull_BeforeFirstUpdate()
    {
        // IGumService.GameTime lives in the XNALIKE partial and returns null while the XNA GameTime
        // property is still null (i.e. before any Update). A relocation bug in the seam would flip
        // this to 0 or throw.
        IGumService service = new GumService();

        service.GameTime.ShouldBeNull();
    }

    [Fact]
    public void NativeTextInput_IsAssigned_OnMonoGame()
    {
        // The constructor calls the AssignNativeTextInput() partial seam; on MonoGame/KNI it is
        // implemented (not elided), so a fresh service exposes the native modal text-input dialog.
        GumService service = new GumService();

        service.NativeTextInput.ShouldNotBeNull();
    }
}
