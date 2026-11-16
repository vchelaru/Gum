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

    // #8137: KNIFX only from KNI 4.3 (4.2 loads plain OpenGL silently, 4.1 and older cannot read KNIFX), and only
    // on desktop OpenGL, the one backend it was checked on.
    [Theory]
    [InlineData("OpenGL", PlatformTarget.OpenGL, "4.0.9001.0", false)]
    [InlineData("OpenGL", PlatformTarget.OpenGL, "4.1.9001.0", false)]
    [InlineData("OpenGL", PlatformTarget.OpenGL, "4.2.9001.0", false)]
    [InlineData("OpenGL", PlatformTarget.OpenGL, "4.3.9001.0", true)]
    [InlineData("OpenGL", PlatformTarget.OpenGL, "5.0.0.0", true)]
    [InlineData("OpenGL", PlatformTarget.OpenGL, null, false)]
    [InlineData("GLES", PlatformTarget.OpenGL, "4.3.9001.0", false)]
    [InlineData("WebGL", PlatformTarget.OpenGL, "4.3.9001.0", false)]
    [InlineData("DirectX11", PlatformTarget.DirectX, "4.3.9001.0", false)]
    public void RecommendProfile_IsKnifxOnlyOnDesktopOpenGlOfKni43AndLater(
        string backend, PlatformTarget target, string? version, bool expectsProfile)
    {
        CapabilityProfile? profile = ShadowDuskTargetDetector.RecommendProfile(
            backend, target, version == null ? null : new Version(version));

        (profile != null).ShouldBe(expectsProfile);
        if (profile != null)
        {
            profile.KnifxVersion.ShouldBe((short)12);
        }
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
