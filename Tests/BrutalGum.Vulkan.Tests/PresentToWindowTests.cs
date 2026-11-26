using Gum;
using Shouldly;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace BrutalGum.Tests;

public class PresentToWindowTests
{
    private readonly ITestOutputHelper _output;

    public PresentToWindowTests(ITestOutputHelper output) => _output = output;

    [SkippableFact]
    public void Draw_PresentToWindowFalseBeforeInitialize_RendersIntoImageWithoutASurface()
    {
        VulkanTestDevice.Run(vulkan =>
        {
            string? loader = System.Diagnostics.Process.GetCurrentProcess().Modules.Cast<System.Diagnostics.ProcessModule>()
                .FirstOrDefault(m => m.ModuleName.StartsWith("vulkan-1", StringComparison.OrdinalIgnoreCase) || m.ModuleName.StartsWith("libvulkan", StringComparison.Ordinal))?.FileName;
            _output.WriteLine($"Vulkan device: {vulkan.DeviceName}, loader {loader}, validation {(vulkan.ValidationEnabled ? "on" : "not installed")}");
            GumService gum = GumService.Default;
            try
            {
                gum.PresentToWindow = false;
                FakeBrutalInput input = new FakeBrutalInput { WindowWidth = 64, WindowHeight = 64, FramebufferWidth = 64, FramebufferHeight = 64 };
                gum.Initialize(input, vulkan.Instance, vulkan.PhysicalDevice, vulkan.Device, vulkan.Queue, vulkan.QueueFamily, default);

                gum.Update(0);
                gum.Draw();

                gum.PresentToWindow.ShouldBeFalse();
                gum.RenderTargetImage.ShouldNotBeNull();
            }
            finally
            {
                gum.Uninitialize();
                gum.PresentToWindow = true;
            }
        });
    }
}
