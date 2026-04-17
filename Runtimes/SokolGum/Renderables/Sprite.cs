using System.Drawing;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using static Sokol.SGP;

namespace SokolGum.Renderables;

/// <summary>
/// Textured quad renderable. Supports <see cref="SourceRectangle"/> for
/// sprite-sheet sampling and horizontal/vertical flipping via UV inversion.
/// Rotation (via GraphicalUiElement.Rotation) is not yet handled; use
/// sgp_push_transform + sgp_rotate_at when that lands.
/// </summary>
public sealed class Sprite : InvisibleRenderable, IAspectRatio, ITextureCoordinate
{
    public Texture2D? Texture { get; set; }
    public Rectangle? SourceRectangle { get; set; }
    public Color Tint = Color.White;
    public bool FlipVertical { get; set; }

    public int Red   { get => Tint.R; set => Tint.R = (byte)value; }
    public int Green { get => Tint.G; set => Tint.G = (byte)value; }
    public int Blue  { get => Tint.B; set => Tint.B = (byte)value; }

    public float? TextureWidth  => Texture?.Width;
    public float? TextureHeight => Texture?.Height;

    public float AspectRatio =>
        TextureWidth > 0 && TextureHeight > 0
            ? TextureWidth.Value / TextureHeight.Value
            : 1f;

    bool ITextureCoordinate.Wrap { get; set; }

    public Sprite(Texture2D? texture = null)
    {
        Texture = texture;
    }

    public override void Render(ISystemManagers? managers)
    {
        if (!Visible || Texture == null) return;

        var systemManagers = (managers as SystemManagers) ?? SystemManagers.Default;
        if (systemManagers == null) return;

        var dstX = this.GetAbsoluteLeft();
        var dstY = this.GetAbsoluteTop();
        var dstW = this.Width;
        var dstH = this.Height;

        // Default source rect covers the whole texture.
        var src = SourceRectangle
            ?? new Rectangle(0, 0, Texture.Width, Texture.Height);

        float sx = src.X;
        float sy = src.Y;
        float sw = src.Width;
        float sh = src.Height;

        // Flipping via UV inversion (negative width/height on source rect).
        if (FlipHorizontal) { sx += sw; sw = -sw; }
        if (FlipVertical)   { sy += sh; sh = -sh; }

        var a = (Tint.A / 255f) * (Alpha / 255f);
        sgp_set_color(Tint.R / 255f, Tint.G / 255f, Tint.B / 255f, a);
        sgp_set_view(0, Texture.View);
        sgp_set_sampler(0, systemManagers.LinearSampler);

        sgp_draw_textured_rect(
            0,
            new sgp_rect { x = dstX, y = dstY, w = dstW, h = dstH },
            new sgp_rect { x = sx,   y = sy,   w = sw,   h = sh   });

        sgp_reset_view(0);
        sgp_reset_sampler(0);
        sgp_reset_color();
    }
}
