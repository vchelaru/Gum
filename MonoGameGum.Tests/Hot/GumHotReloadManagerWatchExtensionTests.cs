using Gum;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// Pins the set of file extensions <see cref="GumHotReloadManager"/> reacts to (issue #4182).
/// The runtime reload pipeline (<c>GumProjectSave.Load</c>, <c>GumAnimationLoader.LoadAnimationsFromProvider</c>)
/// already handles both XML and JSON project formats - only the watch-extension gate itself needed
/// the JSON siblings added.
/// </summary>
public class GumHotReloadManagerWatchExtensionTests
{
    [Theory]
    [InlineData(".gumx")]
    [InlineData(".gumj")]
    [InlineData(".gucx")]
    [InlineData(".gucj")]
    [InlineData(".gusx")]
    [InlineData(".gusj")]
    [InlineData(".gutx")]
    [InlineData(".gutj")]
    [InlineData(".ganx")]
    [InlineData(".ganj")]
    [InlineData(".behx")]
    [InlineData(".behj")]
    [InlineData(".fnt")]
    public void IsWatchedExtension_ShouldReturnTrue_ForEveryRecognizedGumExtension(string extension)
    {
        GumHotReloadManager.IsWatchedExtension(extension).ShouldBeTrue();
    }

    [Theory]
    [InlineData(".png")]
    [InlineData(".txt")]
    [InlineData("")]
    public void IsWatchedExtension_ShouldReturnFalse_ForUnrecognizedExtension(string extension)
    {
        GumHotReloadManager.IsWatchedExtension(extension).ShouldBeFalse();
    }
}
