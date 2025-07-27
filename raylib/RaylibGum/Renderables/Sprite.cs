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
    public Texture2D Texture { get; set; }
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

    public Color Color
    {
        get; set;
    } = Color.White;

    public float? TextureWidth => Texture.Width;

    public float? TextureHeight => Texture.Height;

    public float AspectRatio => TextureHeight > 0 && TextureWidth != null ?
        (float)TextureWidth.Value / TextureHeight.Value : 1;

    bool ITextureCoordinate.Wrap{ get; set; }

    public override void Render(ISystemManagers managers)
    {
        if (!Visible) return;

        int x = (int)this.GetAbsoluteLeft();
        int y = (int)this.GetAbsoluteTop();

        if(SourceRectangle == null)
        {
            // todo - support scaling
            DrawTextureEx(Texture, new Vector2(x, y), -Rotation, 1, Color);
        }
        else
        {
            var destinationRectangle = new Rectangle(
                x, y, this.Width, this.Height);

            DrawTexturePro(Texture, SourceRectangle.Value, destinationRectangle, Vector2.Zero, -Rotation, Color);
        }



    }
}
