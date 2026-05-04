using Gum.Graphics.Animation;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Animation;
using RenderingLibrary.Math;
using SokolGum;
using static Sokol.SGP;
using Rectangle = System.Drawing.Rectangle;

namespace Gum.Renderables;

/// <summary>
/// Nine-slice renderable. Divides the source texture (or SourceRectangle)
/// into a 3×3 grid by default borders of one-third each, then emits nine
/// sgp_draw_textured_rect calls — corners preserved, edges stretched along
/// one axis, center stretched along both. Explicit border overrides are
/// exposed via <see cref="CustomFrameTextureCoordinateWidth"/>.
///
/// Animation chain playback is delegated to <see cref="SpriteAnimationLogic"/>
/// — same composition pattern as <see cref="Sprite"/>. SokolGum is currently
/// the only backend that drives animation on NineSlice; RaylibGum/Skia
/// expose <c>AnimationChains</c> on their SpriteRuntime but not their
/// NineSliceRuntime yet.
/// </summary>
public sealed class NineSlice : RenderableBase, ITextureCoordinate, IAnimatable, ICloneable
{
    public NineSlice Clone()
    {
        var newInstance = (NineSlice)this.MemberwiseClone();
        ((IRenderableIpso)newInstance).SetParentDirect(null);
        newInstance._children = new();
        return newInstance;
    }

    object ICloneable.Clone() => Clone();

    public Texture2D? Texture { get; set; }
    public Rectangle? SourceRectangle { get; set; }
    public Color Color = Color.White;

    public int Alpha { get => Color.A; set => Color.A = (byte)value; }
    public int Red   { get => Color.R; set => Color.R = (byte)value; }
    public int Green { get => Color.G; set => Color.G = (byte)value; }
    public int Blue  { get => Color.B; set => Color.B = (byte)value; }

    public float? TextureWidth  => Texture?.Width;
    public float? TextureHeight => Texture?.Height;

    /// <summary>
    /// Override for the border width on all four sides, in source-texture
    /// pixels. When null the border is computed as source/3 per side, the
    /// classical even-thirds split. Matches Gum's cross-backend convention
    /// so <c>.gumx</c> values set via this property carry across runtimes.
    /// </summary>
    public float? CustomFrameTextureCoordinateWidth { get; set; }

    bool ITextureCoordinate.Wrap { get => false; set { } }

    // Shared animation state — see Sprite for the composition pattern.

    public SpriteAnimationLogic AnimationLogic { get; } = new();

    public AnimationChainList? AnimationChains
    {
        get => AnimationLogic.AnimationChains;
        set => AnimationLogic.AnimationChains = value;
    }

    public string? CurrentChainName
    {
        get => AnimationLogic.CurrentChainName;
        set => AnimationLogic.CurrentChainName = value;
    }

    public bool Animate
    {
        get => AnimationLogic.Animate;
        set => AnimationLogic.Animate = value;
    }

    public float AnimationSpeed
    {
        get => AnimationLogic.AnimationSpeed;
        set => AnimationLogic.AnimationSpeed = value;
    }

    public NineSlice()
    {
        AnimationLogic.ApplyFrame = ApplyAnimationFrame;
    }

    public bool AnimateSelf(double secondDifference)
    {
        if (!Visible) return false;
        return AnimationLogic.AnimateSelf(secondDifference);
    }

    private void ApplyAnimationFrame(AnimationFrame frame)
    {
        if (frame.Texture is { } tex)
        {
            Texture = tex;
            var left   = MathFunctions.RoundToInt(frame.LeftCoordinate   * tex.Width);
            var right  = MathFunctions.RoundToInt(frame.RightCoordinate  * tex.Width);
            var top    = MathFunctions.RoundToInt(frame.TopCoordinate    * tex.Height);
            var bottom = MathFunctions.RoundToInt(frame.BottomCoordinate * tex.Height);
            SourceRectangle = new Rectangle(left, top, right - left, bottom - top);
        }
        else
        {
            SourceRectangle = null;
        }

        // NineSlice doesn't flip per-slice, but honouring the frame flag at
        // the container level matches Sprite's behaviour.
        FlipHorizontal = frame.FlipHorizontal;
    }

    public override void Render(ISystemManagers? managers)
    {
        if (!Visible || Texture is null) return;

        var systemManagers = (managers as SystemManagers) ?? SystemManagers.Default;
        if (systemManagers is null) return;

        float sx, sy, sw, sh;
        if (SourceRectangle is { } src)
        {
            sx = src.X; sy = src.Y; sw = src.Width; sh = src.Height;
        }
        else
        {
            sx = 0; sy = 0; sw = Texture.Width; sh = Texture.Height;
        }

        // Border in source pixels: override when CustomFrameTextureCoordinateWidth
        // is set, otherwise default to source/3. Later clamped below so a
        // degenerate source (sw or sh < 3) doesn't produce negative middles.
        float bwSrc = CustomFrameTextureCoordinateWidth ?? sw / 3f;
        float bhSrc = CustomFrameTextureCoordinateWidth ?? sh / 3f;
        float bl = bwSrc, br = bwSrc;
        float bt = bhSrc, bb = bhSrc;

        float dx = this.GetAbsoluteLeft();
        float dy = this.GetAbsoluteTop();
        float dw = this.Width;
        float dh = this.Height;

        // If the destination is smaller than the combined borders, shrink them
        // so left/right (or top/bottom) meet at the center instead of overlapping.
        if (bl + br > dw)
        {
            float k = dw / (bl + br); bl *= k; br *= k;
        }
        if (bt + bb > dh)
        {
            float k = dh / (bt + bb); bt *= k; bb *= k;
        }

        var a = Color.A / 255f;
        sgp_set_color(Color.R / 255f, Color.G / 255f, Color.B / 255f, a);
        sgp_set_view(0, Texture.View);
        // See Sprite.Render — point filtering by default to match Gum core.
        sgp_set_sampler(0, systemManagers.SpriteSampler);

        // Src column/row boundaries mirror the source-pixel border width.
        float srcMidX   = sx + bwSrc;
        float srcRightX = sx + sw - bwSrc;
        float srcMidY   = sy + bhSrc;
        float srcBottomY = sy + sh - bhSrc;

        float dstMidX    = dx + bl;
        float dstRightX  = dx + dw - br;
        float dstMidY    = dy + bt;
        float dstBottomY = dy + dh - bb;

        // Row 1: top-left, top-center, top-right.
        Draw(dx,         dy, bl,                     bt,
             sx,         sy, bwSrc,                  bhSrc);
        Draw(dstMidX,    dy, dstRightX - dstMidX,    bt,
             srcMidX,    sy, srcRightX - srcMidX,    bhSrc);
        Draw(dstRightX,  dy, br,                     bt,
             srcRightX,  sy, bwSrc,                  bhSrc);

        // Row 2: middle-left, middle-center, middle-right.
        Draw(dx,         dstMidY, bl,                     dstBottomY - dstMidY,
             sx,         srcMidY, bwSrc,                  srcBottomY - srcMidY);
        Draw(dstMidX,    dstMidY, dstRightX - dstMidX,    dstBottomY - dstMidY,
             srcMidX,    srcMidY, srcRightX - srcMidX,    srcBottomY - srcMidY);
        Draw(dstRightX,  dstMidY, br,                     dstBottomY - dstMidY,
             srcRightX,  srcMidY, bwSrc,                  srcBottomY - srcMidY);

        // Row 3: bottom-left, bottom-center, bottom-right.
        Draw(dx,         dstBottomY, bl,                  bb,
             sx,         srcBottomY, bwSrc,               bhSrc);
        Draw(dstMidX,    dstBottomY, dstRightX - dstMidX, bb,
             srcMidX,    srcBottomY, srcRightX - srcMidX, bhSrc);
        Draw(dstRightX,  dstBottomY, br,                  bb,
             srcRightX,  srcBottomY, bwSrc,               bhSrc);

        sgp_reset_view(0);
        sgp_reset_sampler(0);
        sgp_reset_color();
    }

    private static void Draw(float dx, float dy, float dw, float dh,
                              float sx, float sy, float sw, float sh)
    {
        if (dw <= 0 || dh <= 0) return;
        sgp_draw_textured_rect(0,
            new sgp_rect { x = dx, y = dy, w = dw, h = dh },
            new sgp_rect { x = sx, y = sy, w = sw, h = sh });
    }
}
