using Stride.Core.Mathematics;
using Stride.Graphics;

namespace StrideGum.GpuTests;

// The Direct3D 11 half of the smoke test, as in SkiaGameRendering's Tests.Stride.D3D11: a window-less
// device on WARP, and the device's one main command list (D3D11 supports no others).
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
        internal static GraphicsPlatform ExpectedPlatform => GraphicsPlatform.Direct3D11;

        internal static void Frame(GraphicsDevice device, Texture target, Action<GraphicsContext> draw)
        {
            GraphicsContext graphicsContext = new(device);
            graphicsContext.CommandList.SetRenderTargetAndViewport(null, target);
            graphicsContext.CommandList.Clear(target, new Color4(0f, 0f, 0f, 0f));
            draw(graphicsContext);
        }

        internal static byte[] ReadRgba(GraphicsDevice device, Texture texture)
        {
            Color[] pixels = texture.GetData<Color>(new GraphicsContext(device).CommandList);
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
