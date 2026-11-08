using Stride.Core.Mathematics;
using Stride.Graphics;

namespace StrideGum.GpuTests;

// The Direct3D 12 half of the smoke test, as in SkiaGameRendering's Tests.Stride.D3D12: a window-less
// device on WARP, and command lists the caller creates, closes and submits.
public sealed partial class StrideGumGpuSmokeTests
{
    static class StrideBackend
    {
        // One device for the process: SkiaGameRendering keeps a static Skia context keyed to the device.
        static readonly Lazy<GraphicsDevice> LazyDevice = new(() =>
        {
            // Stride's own switch: enumerate only the WARP adapter, so a dev box with a GPU and CI agree.
            Environment.SetEnvironmentVariable("STRIDE_GRAPHICS_SOFTWARE_RENDERING", "1");
            GraphicsDevice device = GraphicsDevice.New();
            device.ColorSpace = ColorSpace.Gamma;
            return device;
        });

        internal static GraphicsDevice Device => LazyDevice.Value;

        /// <summary>What <see cref="GraphicsDevice.Platform"/> must be, so this project never silently tests another API.</summary>
        internal static GraphicsPlatform ExpectedPlatform => GraphicsPlatform.Direct3D12;

        internal static void Frame(GraphicsDevice device, Texture target, Action<GraphicsContext> draw)
        {
            using CommandList commandList = CommandList.New(device);
            commandList.SetRenderTargetAndViewport(null, target);
            commandList.Clear(target, new Color4(0f, 0f, 0f, 0f));
            draw(new GraphicsContext(device, commandList: commandList));
            device.ExecuteCommandList(commandList.Close());
        }

        internal static byte[] ReadRgba(GraphicsDevice device, Texture texture)
        {
            using CommandList commandList = CommandList.New(device);
            Color[] pixels = texture.GetData<Color>(commandList);
            device.ExecuteCommandList(commandList.Close());

            byte[] rgba = new byte[pixels.Length * 4];
            for (int i = 0; i < pixels.Length; i++)
            {
                rgba[i * 4] = pixels[i].R;
                rgba[i * 4 + 1] = pixels[i].G;
                rgba[i * 4 + 2] = pixels[i].B;
                rgba[i * 4 + 3] = pixels[i].A;
            }
            return rgba;
        }
    }
}
