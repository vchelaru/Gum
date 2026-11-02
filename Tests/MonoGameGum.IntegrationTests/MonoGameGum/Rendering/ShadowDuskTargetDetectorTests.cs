using Gum;
using ShadowDusk.Core;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// UseShadowDusk() takes no argument, so the compile target comes from the graphics device (#7053). The
/// mapping is a pure function over what the device reports, so every backend is covered here without
/// a device for it; the real GL device is covered below and the DirectX devices in the GumShadowDusk.Tests.* projects.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ShadowDuskTargetDetectorTests : BaseTestClass
{
    [Theory]
    [InlineData("OpenGL", PlatformTarget.OpenGL)]
    [InlineData("GLES", PlatformTarget.OpenGL)]
    [InlineData("WebGL", PlatformTarget.OpenGL)]
    [InlineData("DirectX11", PlatformTarget.DirectX)]
    [InlineData("DirectX", PlatformTarget.DirectX)]
    [InlineData("DirectX12", PlatformTarget.DirectX12)]
    [InlineData("Vulkan", PlatformTarget.Vulkan)]
    public void FromBackendName_MapsEachSupportedBackend(string backendName, PlatformTarget expected)
    {
        ShadowDuskTargetDetector.FromBackendName(backendName).ShouldBe(expected);
    }

    [Fact]
    public void FromBackendName_UnsupportedBackend_ThrowsNamingIt()
    {
        NotSupportedException exception = Should.Throw<NotSupportedException>(
            () => ShadowDuskTargetDetector.FromBackendName("Metal"));

        exception.Message.ShouldContain("Metal");
    }

    [Fact]
    public void FromBackendName_WebGPU_ThrowsNamingIt()
    {
        Should.Throw<NotSupportedException>(() => ShadowDuskTargetDetector.FromBackendName("WebGPU"))
            .Message.ShouldContain("WebGPU");
    }

    [Fact]
    public void Detect_RealDesktopGLDevice_IsOpenGL()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        ShadowDuskTargetDetector.Detect(game.GraphicsDevice).ShouldBe(PlatformTarget.OpenGL);
    }

    private sealed class MinimalGame : SharedGameLease
    {
    }
}
