using System.Drawing;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SokolGum.Animation;
using static Sokol.SGP;

namespace SokolGum.Renderables;

/// <summary>
/// Nine-slice renderable. Divides the source texture (or SourceRectangle)
/// into a 3×3 grid by default borders of one-third each, then emits nine
/// sgp_draw_textured_rect calls — corners preserved, edges stretched along
/// one axis, center stretched along both. Explicit border overrides are
/// exposed via <see cref="CustomFrameTextureCoordinateWidth"/>.
///
/// Also supports <c>.achx</c> animation chains (matching the shared XNA
/// NineSlice): assign <see cref="AnimationChains"/> + <see cref="CurrentChainName"/>
/// and let <see cref="Renderer"/> drive <see cref="AnimateSelf"/> per frame.
/// Texture / SourceRectangle are swapped from the current chain frame;
/// frame flip flags on nine-slice are interpreted as <see cref="SourceRectangle"/>
/// negation the same way <see cref="Sprite"/> handles them.
/// </summary>
public sealed class NineSlice : RenderableBase, ITextureCoordinate
{
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

    // Animation state. Mirrors Sprite's implementation — NineSlice is the
    // only other renderable Gum's shared XNA code animates via .achx chains.

    public AnimationChainList? AnimationChains { get; set; }

    private string? _currentChainName;

    /// <summary>
    /// Name of the chain currently playing. Setting this resets frame state
    /// and immediately applies the first frame's texture + source rect.
    /// </summary>
    public string? CurrentChainName
    {
        get => _currentChainName;
        set
        {
            if (_currentChainName == value) return;
            _currentChainName = value;
            _currentFrameIndex = 0;
            _frameElapsed = 0;
            UpdateToCurrentAnimationFrame();
        }
    }

    public bool  Animate        { get; set; } = true;
    public float AnimationSpeed { get; set; } = 1f;

    private int _currentFrameIndex;
    private double _frameElapsed;

    private AnimationChain? CurrentChain
    {
        get
        {
            if (AnimationChains is null || _currentChainName is null) return null;
            return AnimationChains[_currentChainName];
        }
    }

    /// <summary>Advances the active chain by <paramref name="secondsSinceLastFrame"/>.</summary>
    public void AnimateSelf(double secondsSinceLastFrame)
    {
        if (!Animate) return;
        var chain = CurrentChain;
        if (chain is null || chain.Count == 0) return;

        _frameElapsed += secondsSinceLastFrame * AnimationSpeed;

        bool frameChanged = false;
        int safety = chain.Count + 1;
        while (safety-- > 0)
        {
            var currentFrame = chain[_currentFrameIndex];
            if (currentFrame.FrameLength <= 0f)
            {
                _currentFrameIndex = (_currentFrameIndex + 1) % chain.Count;
                frameChanged = true;
                continue;
            }
            if (_frameElapsed < currentFrame.FrameLength) break;
            _frameElapsed -= currentFrame.FrameLength;
            _currentFrameIndex = (_currentFrameIndex + 1) % chain.Count;
            frameChanged = true;
        }
        if (safety < 0 && chain.TotalLength > 0f)
        {
            // Ran out of iterations with time still banked — modulo into
            // chain length so wildly high AnimationSpeed lands correctly.
            _frameElapsed = (float)(_frameElapsed % chain.TotalLength);
        }

        if (frameChanged) UpdateToCurrentAnimationFrame();
    }

    /// <summary>Pushes the current frame's texture + source rect onto render state.</summary>
    public void UpdateToCurrentAnimationFrame()
    {
        var chain = CurrentChain;
        if (chain is null || chain.Count == 0) return;
        if (_currentFrameIndex >= chain.Count) _currentFrameIndex = 0;

        var frame = chain[_currentFrameIndex];
        if (frame.Texture is not null) Texture = frame.Texture;
        SourceRectangle = frame.ToPixelSourceRectangle();
        // FlipHorizontal on RenderableBase is handled via the base field;
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
        sgp_set_sampler(0, systemManagers.LinearSampler);

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
