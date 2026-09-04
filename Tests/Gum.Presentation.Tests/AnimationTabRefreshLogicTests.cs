using Shouldly;
using StateAnimationPlugin;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins which on-disk file changes live-reload the Animations tab. Both sidecar extensions must
/// qualify: a JSON project's sidecar is .ganj, and recognizing only .ganx left the tab showing
/// stale animations after an external edit in every converted project.
/// </summary>
public class AnimationTabRefreshLogicTests
{
    [Theory]
    [InlineData("ganx")]
    [InlineData("ganj")]
    public void ShouldReloadAnimationsForChangedFile_IsTrue_ForTheSelectedElementsSidecar(string extension)
    {
        FilePath sidecar = new FilePath($@"C:\Project\Components\MyComponentAnimations.{extension}");

        AnimationTabRefreshLogic.ShouldReloadAnimationsForChangedFile(sidecar, sidecar).ShouldBeTrue();
    }

    [Theory]
    [InlineData("ganx")]
    [InlineData("ganj")]
    public void ShouldReloadAnimationsForChangedFile_IsFalse_ForAnotherElementsSidecar(string extension)
    {
        FilePath changed = new FilePath($@"C:\Project\Components\OtherAnimations.{extension}");
        FilePath selected = new FilePath($@"C:\Project\Components\MyComponentAnimations.{extension}");

        AnimationTabRefreshLogic.ShouldReloadAnimationsForChangedFile(changed, selected).ShouldBeFalse();
    }

    [Fact]
    public void ShouldReloadAnimationsForChangedFile_IsFalse_ForANonAnimationFile()
    {
        FilePath changed = new FilePath(@"C:\Project\Components\MyComponent.gucj");
        FilePath selected = new FilePath(@"C:\Project\Components\MyComponentAnimations.ganj");

        AnimationTabRefreshLogic.ShouldReloadAnimationsForChangedFile(changed, selected).ShouldBeFalse();
    }
}
