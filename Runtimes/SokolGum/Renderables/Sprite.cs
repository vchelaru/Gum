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
/// Textured quad renderable. Supports <see cref="SourceRectangle"/> for
/// sprite-sheet sampling and horizontal/vertical flipping via UV inversion.
/// Rotation is handled centrally in <see cref="Renderer.DrawGumRecursively"/>
/// via sgp_push_transform + sgp_rotate_at so each renderable draws in
/// local coordinates.
///
/// Animation chain playback is delegated to a composed
/// <see cref="SpriteAnimationLogic"/> (same pattern as RaylibGum / Skia).
/// Assign <see cref="AnimationChains"/> + <see cref="CurrentChainName"/>
/// and keep <see cref="Animate"/> = true; the <see cref="Renderer"/> calls
/// <see cref="AnimateSelf"/> per frame and the shared playback machinery
/// dispatches <see cref="ApplyAnimationFrame"/> on every frame change —
/// which pushes the current frame's texture + source rect + flip flags
/// onto this sprite's render state.
/// </summary>
public sealed class Sprite : InvisibleRenderable, IAspectRatio, ITextureCoordinate, IAnimatable
{
    public Texture2D? Texture { get; set; }
    public Rectangle? SourceRectangle { get; set; }
    public Color Color = Color.White;
    public bool FlipVertical { get; set; }

    public int Red   { get => Color.R; set => Color.R = (byte)value; }
    public int Green { get => Color.G; set => Color.G = (byte)value; }
    public int Blue  { get => Color.B; set => Color.B = (byte)value; }

    public float? TextureWidth  => Texture?.Width;
    public float? TextureHeight => Texture?.Height;

    public float AspectRatio =>
        TextureWidth > 0 && TextureHeight > 0
            ? TextureWidth.Value / TextureHeight.Value
            : 1f;

    bool ITextureCoordinate.Wrap { get; set; }

    /// <summary>
    /// Shared animation state machine. Exposed so consumers can subscribe
    /// to <see cref="SpriteAnimationLogic.AnimationChainCycled"/> or tweak
    /// playback (speed, looping) directly. The property shortcuts below
    /// (<see cref="AnimationChains"/>, <see cref="CurrentChainName"/>,
    /// <see cref="Animate"/>, <see cref="AnimationSpeed"/>) just forward.
    /// </summary>
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

    public Sprite(Texture2D? texture = null)
    {
        Texture = texture;
        AnimationLogic.ApplyFrame = ApplyAnimationFrame;
    }

    /// <summary>
    /// IAnimatable entry point — called by <see cref="Renderer.Update"/>
    /// each frame with the elapsed seconds since the last tick. Delegates
    /// to <see cref="SpriteAnimationLogic.AnimateSelf"/> which advances the
    /// frame index and invokes <see cref="ApplyAnimationFrame"/> on change.
    /// </summary>
    public bool AnimateSelf(double secondDifference)
    {
        if (!Visible) return false;
        return AnimationLogic.AnimateSelf(secondDifference);
    }

    /// <summary>
    /// Pushes the current <see cref="AnimationFrame"/> onto this sprite's
    /// render state. Called by <see cref="AnimationLogic"/> on every frame
    /// change (and on initial chain assignment).
    /// </summary>
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

        FlipHorizontal = frame.FlipHorizontal;
        FlipVertical   = frame.FlipVertical;
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

        var a = (Color.A / 255f) * (Alpha / 255f);
        sgp_set_color(Color.R / 255f, Color.G / 255f, Color.B / 255f, a);
        sgp_set_view(0, Texture.View);
        // Point filtering by default — matches Gum core's TextureFilter.Point
        // default and keeps pixel art crisp. Callers wanting bilinear on
        // photographic textures can switch to systemManagers.LinearSampler.
        sgp_set_sampler(0, systemManagers.SpriteSampler);

        sgp_draw_textured_rect(
            0,
            new sgp_rect { x = dstX, y = dstY, w = dstW, h = dstH },
            new sgp_rect { x = sx,   y = sy,   w = sw,   h = sh   });

        sgp_reset_view(0);
        sgp_reset_sampler(0);
        sgp_reset_color();
    }
}
