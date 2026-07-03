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
using static Raylib_cs.Raylib;

namespace Gum.Renderables;
public class Sprite : InvisibleRenderable, IAspectRatio, ITextureCoordinate, IAnimatable
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

    public float? TextureWidth => RenderTargetTextureSource?.Width ?? Texture?.Width;

    public float? TextureHeight => RenderTargetTextureSource?.Height ?? Texture?.Height;

    public float AspectRatio => TextureHeight > 0 && TextureWidth != null ?
        (float)TextureWidth.Value / TextureHeight.Value : 1;

    bool ITextureCoordinate.Wrap{ get; set; }


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
            // the container-to-screen composite in Renderer.TryCompositeRenderTarget.
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

        if (Blend.HasValue)
        {
            global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter.BeginBlendMode(Blend.Value.ToRaylibBlendMode());
        }

        DrawTexturePro(textureToDraw, srcRect, destinationRectangle, Vector2.Zero, -absoluteRotation, Color);

        if (Blend.HasValue)
        {
            global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter.EndBlendMode();
        }
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
