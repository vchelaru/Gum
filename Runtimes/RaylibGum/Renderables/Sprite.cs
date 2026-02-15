using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;
public class Sprite : InvisibleRenderable, IAspectRatio, ITextureCoordinate
{
    public Texture2D? Texture { get; set; }
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

    public float? TextureWidth => Texture?.Width;

    public float? TextureHeight => Texture?.Height;

    public float AspectRatio => TextureHeight > 0 && TextureWidth != null ?
        (float)TextureWidth.Value / TextureHeight.Value : 1;

    bool ITextureCoordinate.Wrap{ get; set; }


    public override void Render(ISystemManagers managers)
    {
        if (!Visible || Texture == null) return;

        int x = (int)this.GetAbsoluteLeft();
        int y = (int)this.GetAbsoluteTop();

        var absoluteRotation = this.GetAbsoluteRotation();
        var destinationRectangle = new Rectangle(
            x, y, this.Width, this.Height);

        // if we don't have a source rectangle, the source is the entire texture
        var srcRect = SourceRectangle ?? new Rectangle(0, 0, TextureWidth.Value, TextureHeight.Value);

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

        DrawTexturePro(Texture.Value, srcRect, destinationRectangle, Vector2.Zero, -absoluteRotation, Color);
    }

    public Sprite(Texture2D? texture = null)
    {
        this.Texture = texture;
    }
}
