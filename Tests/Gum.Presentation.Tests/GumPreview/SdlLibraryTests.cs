using System.Reflection;
using GumPreview;
using Shouldly;

namespace Gum.Presentation.Tests.GumPreview;

// Loading SDL itself is checked by build-and-release.yml, which runs `GumPreview --probe-sdl` on the
// published ReadyToRun and Native AOT builds on each OS.
public class SdlLibraryTests
{
    [Fact]
    public void Resolve_OtherLibrary_ReturnsZeroForDefaultProbing()
    {
        Assembly assembly = typeof(SdlLibraryTests).Assembly;

        IntPtr handle = SdlLibrary.Resolve("libc", assembly, null);

        handle.ShouldBe(IntPtr.Zero);
    }
}
