using System.Reflection;
using System.Runtime.InteropServices;
using GumPreview;
using Shouldly;

namespace Gum.Presentation.Tests.GumPreview;

public class SdlLibraryTests
{
    // This project references the same MonoGame.Library.SDL natives GumPreview ships, so this loads
    // the real file on each CI OS.
    [Fact]
    public void Resolve_Sdl2_LoadsShippedLibraryWithRaiseWindow()
    {
        Assembly assembly = typeof(SdlLibraryTests).Assembly;

        IntPtr handle = SdlLibrary.Resolve(SdlLibrary.ImportName, assembly, null);

        handle.ShouldNotBe(IntPtr.Zero);
        NativeLibrary.TryGetExport(handle, "SDL_RaiseWindow", out _).ShouldBeTrue();
    }

    [Fact]
    public void Resolve_OtherLibrary_ReturnsZeroForDefaultProbing()
    {
        Assembly assembly = typeof(SdlLibraryTests).Assembly;

        IntPtr handle = SdlLibrary.Resolve("libc", assembly, null);

        handle.ShouldBe(IntPtr.Zero);
    }
}
