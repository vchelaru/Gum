using System.Drawing;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SokolGum.Animation;
using static Sokol.SGP;

namespace SokolGum.Renderables;

/// <summary>
/// Textured quad renderable. Supports <see cref="SourceRectangle"/> for
/// sprite-sheet sampling and horizontal/vertical flipping via UV inversion.
/// Rotation is handled centrally in <see cref="Renderer.DrawGumRecursively"/>
/// via sgp_push_transform + sgp_rotate_at so each renderable draws in
/// local coordinates.
///
/// Also implements <c>.achx</c> animation chains: assign
/// <see cref="AnimationChains"/> + <see cref="CurrentChainName"/> and set
/// <see cref="Animate"/> = true, then let <see cref="Renderer"/> call
/// <see cref="AnimateSelf"/> each frame. Texture / SourceRectangle /
/// flip flags are updated in-place from the current chain frame.
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

    // Animation state. AnimationChains holds every named chain; CurrentChainName
    // drives which one plays. When Animate is true the Renderer ticks AnimateSelf
    // each frame, advancing the frame index by the elapsed time.

    public AnimationChainList? AnimationChains { get; set; }

    private string? _currentChainName;

    /// <summary>
    /// Name of the chain currently playing. Setting this resets the frame
    /// index / elapsed-time counters and immediately applies the first
    /// frame's texture + source rect + flip flags to this sprite. Assigning
    /// null or an unknown name stops animation.
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

    /// <summary>Play/pause toggle. Defaults to true once a chain is assigned.</summary>
    public bool Animate { get; set; } = true;

    /// <summary>Playback-rate multiplier. 1.0 = realtime; 2.0 = double speed; 0.5 = half.</summary>
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

    public Sprite(Texture2D? texture = null)
    {
        Texture = texture;
    }

    /// <summary>
    /// Advances the active chain by <paramref name="secondsSinceLastFrame"/>,
    /// crossing frame boundaries as needed (handles multi-frame skips when
    /// a big dt arrives, e.g. after a pause). Loops at chain end. Called
    /// by <see cref="Renderer"/> once per frame for every animated sprite.
    /// </summary>
    public void AnimateSelf(double secondsSinceLastFrame)
    {
        if (!Animate) return;
        var chain = CurrentChain;
        if (chain is null || chain.Count == 0) return;

        _frameElapsed += secondsSinceLastFrame * AnimationSpeed;

        // Burn through complete frames in a loop so a dt larger than one
        // frame's duration still lands on the correct absolute frame
        // rather than stuttering to a neighbour. A cap on iterations
        // protects against a malformed .achx whose frames all have
        // FrameLength<=0 — without it the loop never consumes dt and
        // spins the render thread forever.
        bool frameChanged = false;
        int safety = chain.Count * 8;
        while (safety-- > 0)
        {
            var currentFrame = chain[_currentFrameIndex];
            if (currentFrame.FrameLength <= 0f)
            {
                // Skip-only — don't spend time on it, but don't decrement
                // _frameElapsed either (nothing to subtract). Advancing
                // past is enough for the chain to still cycle.
                _currentFrameIndex = (_currentFrameIndex + 1) % chain.Count;
                frameChanged = true;
                continue;
            }
            if (_frameElapsed < currentFrame.FrameLength) break;
            _frameElapsed -= currentFrame.FrameLength;
            _currentFrameIndex = (_currentFrameIndex + 1) % chain.Count;
            frameChanged = true;
        }
        if (frameChanged) UpdateToCurrentAnimationFrame();
    }

    /// <summary>
    /// Pushes the current frame's texture / source rect / flip flags onto
    /// this sprite's render state. Called automatically on
    /// <see cref="CurrentChainName"/> assignment and after each
    /// <see cref="AnimateSelf"/> frame advance; can also be called
    /// manually after mutating a frame in place.
    /// </summary>
    public void UpdateToCurrentAnimationFrame()
    {
        var chain = CurrentChain;
        if (chain is null || chain.Count == 0) return;
        if (_currentFrameIndex >= chain.Count) _currentFrameIndex = 0;

        var frame = chain[_currentFrameIndex];
        if (frame.Texture is not null) Texture = frame.Texture;
        SourceRectangle = frame.ToPixelSourceRectangle();
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
