using Gum.Graphics.Animation;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Animation;
using RenderingLibrary.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToolsUtilitiesStandard.Helpers;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;
public class Sprite : InvisibleRenderable, IAspectRatio, ITextureCoordinate, IAnimatable, IRenderableIpso
{
    public Texture2D? Texture { get; set; }

    /// <summary>
    /// The render-target container whose baked offscreen texture this sprite displays instead of
    /// <see cref="Texture"/>. Resolved each <see cref="Render"/> via
    /// <see cref="global::RenderingLibrary.Graphics.Renderer.TryGetBakedRenderTargetFor"/>, so it
    /// always reflects the source's latest bake (including after a resize).
    /// </summary>
    public IRenderableIpso? RenderTargetTextureSource { get; set; }
    public Raylib_cs.Rectangle? SourceRectangle
    { 
        get; 
        set; 
    }
    System.Drawing.Rectangle? ITextureCoordinate.SourceRectangle 
    { 
        get
        {
            if(SourceRectangle == null)
            {
                return null;
            }
            else
            {
                var rRect = SourceRectangle.Value;
                return new System.Drawing.Rectangle(
                    (int)rRect.X,
                    (int)rRect.Y,
                    (int)rRect.Width,
                    (int)rRect.Height
                    );
            }
        }
        set
        {
            if(value == null)
            {
                SourceRectangle = null;
            }
            else
            {
                SourceRectangle = new Rectangle(
                    value.Value.X,
                    value.Value.Y,
                    value.Value.Width,
                    value.Value.Height);
            }
        }
    }

    public bool FlipVertical { get; set; }

    public int Alpha
    {
        get
        {
            return Color.A;
        }
        set
        {
            if (value != Color.A)
            {
                Color = new Color(Color.R, Color.G, Color.B, (byte)value);
            }
        }
    }

    public int Red
    {
        get
        {
            return Color.R;
        }
        set
        {
            if (value != Color.R)
            {
                Color = new Color((byte)value, Color.G, Color.B, Color.A);
            }
        }
    }

    public int Green
    {
        get
        {
            return Color.G;
        }
        set
        {
            if (value != Color.G)
            {
                Color = new Color(Color.R, (byte)value, Color.B, Color.A);
            }
        }
    }

    public int Blue
    {
        get
        {
            return Color.B;
        }
        set
        {
            if (value != Color.B)
            {
                Color = new Color(Color.R, Color.G, (byte)value, Color.A);
            }
        }
    }

    public Color Color
    {
        get; set;
    } = Color.White;

    public global::Gum.RenderingLibrary.Blend? Blend { get; set; }

    /// <summary>
    /// How the sprite's tint <see cref="Color"/> combines with its texture, matching MonoGame's
    /// <see cref="ColorOperation"/> (issue #3486). <see cref="ColorOperation.Modulate"/> (the default)
    /// multiplies texture RGBA by the tint; <see cref="ColorOperation.ColorTextureAlpha"/> uses the
    /// texture only as an alpha mask and fills with the tint color, via
    /// <see cref="global::RenderingLibrary.Graphics.Renderer.ColorTextureAlphaShader"/>.
    /// </summary>
    public ColorOperation ColorOperation { get; set; } = ColorOperation.Modulate;

    // Re-implement the interface getter so it reports this sprite's real ColorOperation. The base
    // RenderableBase explicitly implements IRenderableIpso.ColorOperation as a hardcoded Modulate
    // (correct for Text/NineSlice/shapes, which never vary it); a Sprite is the one renderable that does.
    ColorOperation IRenderableIpso.ColorOperation => ColorOperation;

    public float? TextureWidth => RenderTargetTextureSource?.Width ?? Texture?.Width;

    public float? TextureHeight => RenderTargetTextureSource?.Height ?? Texture?.Height;

    public float AspectRatio => TextureHeight > 0 && TextureWidth != null ?
        (float)TextureWidth.Value / TextureHeight.Value : 1;

    bool ITextureCoordinate.Wrap { get; set; }

    public override void Render(ISystemManagers managers)
    {
        if (!Visible) return;

        Texture2D textureToDraw;
        Rectangle defaultSrcRect;

        if (RenderTargetTextureSource != null)
        {
            RenderTexture2D? renderTexture =
                global::RenderingLibrary.Graphics.Renderer.Self.TryGetBakedRenderTargetFor(RenderTargetTextureSource);
            // Source container isn't a baked render target (never became one, or its bake was
            // skipped this frame) — draw nothing rather than guess at stale content.
            if (renderTexture == null) return;

            textureToDraw = renderTexture.Value.Texture;
            // An RT is stored bottom-up in GL; a negative source height flips it upright, matching
            // the container-to-screen composite in Renderer.CompositeRenderTarget.
            defaultSrcRect = new Rectangle(0, 0, textureToDraw.Width, -textureToDraw.Height);
        }
        else if (Texture != null)
        {
            textureToDraw = Texture.Value;
            defaultSrcRect = new Rectangle(0, 0, TextureWidth.Value, TextureHeight.Value);
        }
        else
        {
            return;
        }

        int x = (int)this.GetAbsoluteLeft();
        int y = (int)this.GetAbsoluteTop();

        var absoluteRotation = this.GetAbsoluteRotation();
        var destinationRectangle = new Rectangle(
            x, y, this.Width, this.Height);

        // if we don't have a source rectangle, the source is the entire texture
        var srcRect = SourceRectangle ?? defaultSrcRect;

        // ColorTextureAlpha (issue #3486): bind the mask shader around every draw path below so the
        // texture supplies only alpha and the sprite fills with its tint Color, matching MonoGame. The
        // shader's non-premultiplied output composites identically to a normal Modulate draw, so it is
        // correct both on screen and inside a render-target bake. Modulate (the default) draws unshaded.
        bool useColorTextureAlpha = ColorOperation == ColorOperation.ColorTextureAlpha;
        if (useColorTextureAlpha)
        {
            global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter.BeginShaderMode(
                global::RenderingLibrary.Graphics.Renderer.Self.ColorTextureAlphaShader.Shader);
        }

        if (Blend.HasValue)
        {
            global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter.BeginBlendMode(Blend.Value);
        }

        // RenderTargetTextureSource is excluded: its source rect already encodes the bottom-up GL
        // flip via a negative height, which the tiling/clamping loops' positive-range math does
        // not expect.
        bool canSoftwareAddress = RenderTargetTextureSource == null
            && srcRect.Width > 0 && srcRect.Height > 0;

        if (canSoftwareAddress && ((ITextureCoordinate)this).Wrap)
        {
            RenderTiled(textureToDraw, srcRect, destinationRectangle, absoluteRotation);
        }
        else if (canSoftwareAddress && IsOutOfTextureBounds(srcRect, textureToDraw))
        {
            RenderClamped(textureToDraw, srcRect, destinationRectangle, absoluteRotation);
        }
        else
        {
            // Apply flipping by adjusting the source rectangle
            if (FlipHorizontal)
            {
                srcRect.X += srcRect.Width;
                srcRect.Width = -srcRect.Width;
            }

            if (FlipVertical)
            {
                srcRect.Y += srcRect.Height;
                srcRect.Height = -srcRect.Height;
            }

            DrawTexturePro(textureToDraw, srcRect, destinationRectangle, Vector2.Zero, -absoluteRotation, Color);
        }

        if (Blend.HasValue)
        {
            global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter.EndBlendMode();
        }

        if (useColorTextureAlpha)
        {
            global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter.EndShaderMode();
        }
    }

    // Repeats sourceRectangle's texture area across destinationRectangle instead of stretching it,
    // one DrawTexturePro call per texture-bounded tile. Mirrors RenderTiledSprite in the MonoGame
    // Sprite renderable (RenderingLibrary/Graphics/Sprite.cs) but always tiles by software draw
    // calls — raylib has no XNA-style hardware address-mode sampler to fall back to.
    private void RenderTiled(Texture2D texture, Rectangle sourceRectangle, Rectangle destinationRectangle,
        float absoluteRotationDegrees)
    {
        int textureWidth = texture.Width;
        int textureHeight = texture.Height;
        if (textureWidth <= 0 || textureHeight <= 0) return;

        float textureWidthScale = destinationRectangle.Width / sourceRectangle.Width;
        float textureHeightScale = destinationRectangle.Height / sourceRectangle.Height;

        var matrix = this.GetRotationMatrix();

        int startX = (int)sourceRectangle.X;
        int startY = (int)sourceRectangle.Y;
        int endX = startX + (int)sourceRectangle.Width;
        int endY = startY + (int)sourceRectangle.Height;

        float offsetYFromTopLeft = 0;
        for (int y = startY; y < endY;)
        {
            int texTop = ((y % textureHeight) + textureHeight) % textureHeight;
            int texHeight = System.Math.Min(textureHeight - texTop, endY - y);
            float destHeight = texHeight * textureHeightScale;

            float offsetXFromTopLeft = 0;
            for (int x = startX; x < endX;)
            {
                int texLeft = ((x % textureWidth) + textureWidth) % textureWidth;
                int texWidth = System.Math.Min(textureWidth - texLeft, endX - x);
                float destWidth = texWidth * textureWidthScale;

                var tileSourceRect = new Rectangle(texLeft, texTop, texWidth, texHeight);

                if (FlipHorizontal)
                {
                    tileSourceRect.X += tileSourceRect.Width;
                    tileSourceRect.Width = -tileSourceRect.Width;
                }

                if (FlipVertical)
                {
                    tileSourceRect.Y += tileSourceRect.Height;
                    tileSourceRect.Height = -tileSourceRect.Height;
                }

                Vector3 tileOffset = matrix.Right() * offsetXFromTopLeft + matrix.Up() * offsetYFromTopLeft;
                var tileDestinationRect = new Rectangle(
                    destinationRectangle.X + tileOffset.X,
                    destinationRectangle.Y + tileOffset.Y,
                    destWidth,
                    destHeight);

                DrawTexturePro(texture, tileSourceRect, tileDestinationRect, Vector2.Zero, -absoluteRotationDegrees, Color);

                offsetXFromTopLeft += destWidth;
                x += texWidth;
            }

            offsetYFromTopLeft += destHeight;
            y += texHeight;
        }
    }

    private static bool IsOutOfTextureBounds(Rectangle sourceRectangle, Texture2D texture)
    {
        return sourceRectangle.X < 0 || sourceRectangle.Y < 0
            || sourceRectangle.X + sourceRectangle.Width > texture.Width
            || sourceRectangle.Y + sourceRectangle.Height > texture.Height;
    }

    // Stretches the nearest edge row/column/corner texel of an oversized source rectangle across
    // its out-of-bounds portion instead of letting raylib's GL default TextureWrap.Repeat sample
    // repeat it (#3459). Splits source and destination into up to a 3x3 grid at the texture's
    // 0/width and 0/height bounds - in-bounds cells draw straight through, out-of-bounds cells draw
    // a single clamped edge/corner texel stretched to fill, the same edge-stretching idea as
    // nine-slice. Never calls SetTextureWrap: raylib's negative-source-dimension flip trick (below)
    // samples a single clamped texel across the whole quad under hardware TextureWrap.Clamp, which
    // is why the earlier hardware-wrap attempt for this issue was reverted.
    //
    // Unlike RenderTiled, cells are reordered (not just content-flipped) when flipping, because a
    // clamped edge and its in-bounds neighbor are not interchangeable the way repeating tiles are -
    // flipping must swap which side carries the stretched edge, not just mirror each cell in place.
    private void RenderClamped(Texture2D texture, Rectangle sourceRectangle, Rectangle destinationRectangle,
        float absoluteRotationDegrees)
    {
        int textureWidth = texture.Width;
        int textureHeight = texture.Height;
        if (textureWidth <= 0 || textureHeight <= 0) return;

        float textureWidthScale = destinationRectangle.Width / sourceRectangle.Width;
        float textureHeightScale = destinationRectangle.Height / sourceRectangle.Height;

        var matrix = this.GetRotationMatrix();

        var xSegments = GetClampSegments(sourceRectangle.X, sourceRectangle.Width, textureWidth);
        var ySegments = GetClampSegments(sourceRectangle.Y, sourceRectangle.Height, textureHeight);

        if (FlipHorizontal) xSegments.Reverse();
        if (FlipVertical) ySegments.Reverse();

        float offsetYFromTopLeft = 0;
        foreach (var ySegment in ySegments)
        {
            float destHeight = ySegment.Length * textureHeightScale;

            float offsetXFromTopLeft = 0;
            foreach (var xSegment in xSegments)
            {
                float destWidth = xSegment.Length * textureWidthScale;

                var tileSourceRect = new Rectangle(xSegment.TexCoordinate, ySegment.TexCoordinate,
                    xSegment.TexLength, ySegment.TexLength);

                // Mirroring a single-texel span is a visual no-op (only one column/row is ever
                // sampled), so skip the negate-and-shift for it. This also sidesteps a raylib
                // DrawTexturePro quirk: negating a 1-wide/tall source rect pushes its X/Y to sit
                // exactly on the texture's far edge (X == textureWidth), which samples garbage
                // (observed: solid wrong-color fill) instead of the intended edge texel.
                if (FlipHorizontal && tileSourceRect.Width > 1)
                {
                    tileSourceRect.X += tileSourceRect.Width;
                    tileSourceRect.Width = -tileSourceRect.Width;
                }

                if (FlipVertical && tileSourceRect.Height > 1)
                {
                    tileSourceRect.Y += tileSourceRect.Height;
                    tileSourceRect.Height = -tileSourceRect.Height;
                }

                Vector3 tileOffset = matrix.Right() * offsetXFromTopLeft + matrix.Up() * offsetYFromTopLeft;
                var tileDestinationRect = new Rectangle(
                    destinationRectangle.X + tileOffset.X,
                    destinationRectangle.Y + tileOffset.Y,
                    destWidth,
                    destHeight);

                DrawTexturePro(texture, tileSourceRect, tileDestinationRect, Vector2.Zero, -absoluteRotationDegrees, Color);

                offsetXFromTopLeft += destWidth;
            }

            offsetYFromTopLeft += destHeight;
        }
    }

    private readonly struct ClampSegment
    {
        public ClampSegment(float length, int texCoordinate, int texLength)
        {
            Length = length;
            TexCoordinate = texCoordinate;
            TexLength = texLength;
        }

        // Destination-space (pre-scale) length of this segment, in source units.
        public float Length { get; }
        // Texture column/row this segment samples: the real in-bounds coordinate for an in-bounds
        // segment, or the nearest edge texel's coordinate for an out-of-bounds segment.
        public int TexCoordinate { get; }
        // 1 for an out-of-bounds (clamped) segment; the real span for an in-bounds segment.
        public int TexLength { get; }
    }

    // Splits [start, start + length) into up to 3 segments at the texture's 0 and textureExtent
    // bounds, so each segment is either fully in-bounds (samples real texels 1:1) or fully
    // out-of-bounds (samples the single nearest edge texel, to be stretched across the segment).
    private static List<ClampSegment> GetClampSegments(float start, float length, int textureExtent)
    {
        float end = start + length;

        var breaks = new List<float> { start, end };
        if (start < 0 && end > 0) breaks.Add(0);
        if (start < textureExtent && end > textureExtent) breaks.Add(textureExtent);
        breaks.Sort();

        var segments = new List<ClampSegment>();
        for (int i = 0; i < breaks.Count - 1; i++)
        {
            float segmentStart = breaks[i];
            float segmentEnd = breaks[i + 1];
            float segmentLength = segmentEnd - segmentStart;
            if (segmentLength <= 0) continue;

            if (segmentEnd <= 0)
            {
                segments.Add(new ClampSegment(segmentLength, 0, 1));
            }
            else if (segmentStart >= textureExtent)
            {
                segments.Add(new ClampSegment(segmentLength, textureExtent - 1, 1));
            }
            else
            {
                segments.Add(new ClampSegment(segmentLength, (int)segmentStart, (int)segmentLength));
            }
        }

        return segments;
    }

    public AnimationChainLogic AnimationLogic { get; } = new AnimationChainLogic();

    // Convenience pass-throughs to AnimationLogic, mirroring the MonoGame Sprite renderable
    // (RenderingLibrary/Graphics/Sprite.cs) so shared code such as
    // CustomSetPropertyOnRenderable.AssignSourceFileOnSprite compiles identically on both.
    public AnimationChainList? AnimationChains
    {
        get => AnimationLogic.AnimationChains;
        set => AnimationLogic.AnimationChains = value;
    }

    public bool UpdateToCurrentAnimationFrame() => AnimationLogic.UpdateToCurrentAnimationFrame();

    public void RefreshCurrentChainToDesiredName() => AnimationLogic.RefreshCurrentChainToDesiredName();

    public Sprite(Texture2D? texture = null)
    {
        this.Texture = texture;
        AnimationLogic.ApplyFrame = ApplyAnimationFrame;
    }

    void ApplyAnimationFrame(Gum.Graphics.Animation.AnimationFrame frame)
    {
        Texture = frame.Texture;

        if (frame.Texture.HasValue)
        {
            var tex = frame.Texture.Value;
            var left = MathFunctions.RoundToInt(frame.LeftCoordinate * tex.Width);
            var width = MathFunctions.RoundToInt(frame.RightCoordinate * tex.Width) - left;
            var top = MathFunctions.RoundToInt(frame.TopCoordinate * tex.Height);
            var height = MathFunctions.RoundToInt(frame.BottomCoordinate * tex.Height) - top;
            SourceRectangle = new Rectangle(left, top, width, height);
        }
        else
        {
            SourceRectangle = null;
        }

        FlipHorizontal = frame.FlipHorizontal;
        FlipVertical = frame.FlipVertical;
    }

    public bool AnimateSelf(double secondDifference)
    {
        if (!Visible) return false;
        return AnimationLogic.AnimateSelf(secondDifference);
    }
}
