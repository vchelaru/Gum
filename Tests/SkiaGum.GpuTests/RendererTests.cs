using Shouldly;
using Xunit.Abstractions;

namespace SkiaGum.GpuTests;

/// <summary>
/// Prints which GL renderer the GPU tests ran on, so a CI log shows whether it was Mesa llvmpipe or a real driver.
/// </summary>
[Collection(GpuCollection.Name)]
public class RendererTests
{
    readonly WglGpuContext _gpu;
    readonly ITestOutputHelper _output;

    public RendererTests(WglGpuContext gpu, ITestOutputHelper output)
    {
        _gpu = gpu;
        _output = output;
    }

    [Fact]
    public void Context_ReportsRenderer()
    {
        _output.WriteLine($"GL_RENDERER: {_gpu.Renderer}");
        _output.WriteLine($"GL_VERSION: {_gpu.Version}");
        _gpu.Renderer.ShouldNotBeNullOrWhiteSpace();
    }
}
