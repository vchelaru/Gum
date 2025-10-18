using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;
public class NineSlice : InvisibleRenderable, ITextureCoordinate
{
    public Texture2D? Texture { get; set; }

    public Raylib_cs.Rectangle? SourceRectangle
    {
        get;
        set;
    }

    public float? TextureWidth => Texture?.Width;

    public float? TextureHeight => Texture?.Height;

    System.Drawing.Rectangle? ITextureCoordinate.SourceRectangle
    {
        get
        {
            if (SourceRectangle == null)
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
            if (value == null)
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

    // This exists to satisfy the same syntax as MonoGame
    internal void SetSingleTexture(Texture2D? texture) => Texture = texture;

    bool ITextureCoordinate.Wrap 
    { 
        get => false;
        set {} 
    }

    public Color Color
    {
        get; set;
    } = Color.White;

    public override void Render(ISystemManagers managers)
    {
        if (!Visible || Texture == null) return;

        var nonNullText = Texture.Value;

        int x = (int)this.GetAbsoluteLeft();
        int y = (int)this.GetAbsoluteTop();

        var destinationRectangle = new Rectangle(
            x, y, this.Width, this.Height);


        var nPatchInfo = new NPatchInfo
        {
            Source = SourceRectangle ?? new Raylib_cs.Rectangle(0, 0, nonNullText.Width, nonNullText.Height),
            Left = nonNullText.Width/3,
            Top = nonNullText.Height/3,
            Right = nonNullText.Width - nonNullText.Width/3,
            Bottom = nonNullText.Height - nonNullText.Height/3,
            Layout = NPatchLayout.NinePatch
        };

        if(SourceRectangle != null)
        {
            var rect = SourceRectangle.Value;
            nPatchInfo.Left = (int)(rect.Width / 3);
            nPatchInfo.Right = (int)(rect.Width / 3);
            nPatchInfo.Top = (int)(rect.Height / 3);
            nPatchInfo.Bottom = (int)(rect.Height / 3);
        }

        DrawTextureNPatch(
            nonNullText,
            nPatchInfo,
            destinationRectangle,
            Vector2.Zero,
            0,
            Color);
    }


}
