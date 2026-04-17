using Sokol;
using static Sokol.SG;
using static Sokol.SGP;

namespace SokolGum;

/// <summary>
/// A 2D texture backed by sokol_gfx. Holds the <see cref="sg_image"/> plus the
/// <see cref="sg_view"/> needed to sample it from sokol_gp's textured draw calls.
///
/// The sampler is not owned here — renderables pick one from
/// <see cref="SystemManagers"/> so samplers stay shared (one sg_sampler
/// per filter mode instead of one per texture).
/// </summary>
public sealed class Texture2D : IDisposable
{
    public sg_image Image { get; }
    public sg_view View { get; }
    public int Width { get; }
    public int Height { get; }

    private bool _disposed;

    public Texture2D(sg_image image, sg_view view, int width, int height)
    {
        Image = image;
        View = view;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Build a Texture2D from tightly-packed RGBA8 pixel data.
    /// Expects <paramref name="pixels"/>.Length == width * height * 4.
    /// </summary>
    public static unsafe Texture2D FromRgba8(ReadOnlySpan<byte> pixels, int width, int height, string label = "")
    {
        if (pixels.Length != width * height * 4)
            throw new ArgumentException($"Expected {width * height * 4} bytes of RGBA8 data, got {pixels.Length}.");

        sg_image image;
        sg_view view;
        fixed (byte* p = pixels)
        {
            var desc = new sg_image_desc
            {
                width = width,
                height = height,
                pixel_format = sg_pixel_format.SG_PIXELFORMAT_RGBA8,
                label = label,
            };
            desc.data.mip_levels[0] = new sg_range { ptr = p, size = (nuint)pixels.Length };
            image = sg_make_image(desc);
            view = sgp_make_texture_view_from_image(image, label);
        }

        return new Texture2D(image, view, width, height);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        sg_destroy_view(View);
        sg_destroy_image(Image);
    }
}
