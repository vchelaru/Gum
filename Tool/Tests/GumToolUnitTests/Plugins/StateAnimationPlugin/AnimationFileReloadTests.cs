using Shouldly;
using StateAnimationPlugin;
using ToolsUtilities;
using Xunit;

namespace GumToolUnitTests.Plugins.StateAnimationPlugin;

/// <summary>
/// Pins the decision the Animations tab uses to live-reload when a <c>.ganx</c> file is edited on
/// disk (issue #3410): only the currently-selected element's animation sidecar should trigger a
/// repaint, and only for the <c>.ganx</c> extension. The plugin subscribes to the file-watch
/// <c>ReactToFileChanged</c> event for every changed file, so the extension and per-element gates
/// both live in <see cref="MainStateAnimationPlugin.ShouldReloadAnimationsForChangedFile"/>.
/// </summary>
public class AnimationFileReloadTests
{
    [Fact]
    public void ShouldReload_WhenChangedFileIsSelectedElementsAnimationFile()
    {
        FilePath changed = new FilePath(@"C:\Proj\Components\MyButtonAnimations.ganx");
        FilePath selectedElementAnimationFile = new FilePath(@"C:\Proj\Components\MyButtonAnimations.ganx");

        MainStateAnimationPlugin.ShouldReloadAnimationsForChangedFile(changed, selectedElementAnimationFile)
            .ShouldBeTrue();
    }

    [Fact]
    public void ShouldNotReload_WhenChangedGanxBelongsToADifferentElement()
    {
        FilePath changed = new FilePath(@"C:\Proj\Components\OtherAnimations.ganx");
        FilePath selectedElementAnimationFile = new FilePath(@"C:\Proj\Components\MyButtonAnimations.ganx");

        MainStateAnimationPlugin.ShouldReloadAnimationsForChangedFile(changed, selectedElementAnimationFile)
            .ShouldBeFalse();
    }

    [Fact]
    public void ShouldNotReload_WhenChangedFileIsNotAGanx()
    {
        FilePath changed = new FilePath(@"C:\Proj\Components\MyButton.gucx");
        FilePath selectedElementAnimationFile = new FilePath(@"C:\Proj\Components\MyButtonAnimations.ganx");

        MainStateAnimationPlugin.ShouldReloadAnimationsForChangedFile(changed, selectedElementAnimationFile)
            .ShouldBeFalse();
    }

    [Fact]
    public void ShouldNotReload_WhenNoElementHasAnAnimationFile()
    {
        FilePath changed = new FilePath(@"C:\Proj\Components\MyButtonAnimations.ganx");

        MainStateAnimationPlugin.ShouldReloadAnimationsForChangedFile(changed, null)
            .ShouldBeFalse();
    }
}
