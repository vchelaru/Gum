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
using ToolsUtilitiesStandard.Helpers;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;
public class NineSlice : RenderableBase, ITextureCoordinate
{
    public Texture2D? Texture { get; set; }

    public Raylib_cs.Rectangle? SourceRectangle
    {
        get;
        set;
    }

    public float? TextureWidth => Texture?.Width;

    public float? TextureHeight => Texture?.Height;

    // border scale is not currently supported here because the built-in NinePatch functionality doesn't support border scaling in raylib.
    // This means we would have to break apart rendering into individual sprites. That's more than I want to do now

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

    public override void Render(ISystemManagers managers)
    {
        if (!Visible || Texture == null) return;

        var nonNullText = Texture.Value;

        int x = (int)this.GetAbsoluteLeft();
        int y = (int)this.GetAbsoluteTop();

        var destinationRectangle = new Rectangle(
            x, y, this.Width, this.Height);

        int leftSize, rightSize, topSize, bottomSize;

        if (SourceRectangle != null)
        {
            var rect = SourceRectangle.Value;
            leftSize = (int)(rect.Width / 3);
            rightSize = (int)(rect.Width / 3);
            topSize = (int)(rect.Height / 3);
            bottomSize = (int)(rect.Height / 3);
        }
        else
        {
            leftSize = (int)(nonNullText.Width / 3);
            rightSize = (int)(nonNullText.Width / 3);
            topSize = (int)(nonNullText.Height / 3);
            bottomSize = (int)(nonNullText.Height / 3);
        }

        var nPatchInfo = new NPatchInfo
        {
            Source = SourceRectangle ?? new Raylib_cs.Rectangle(0, 0, nonNullText.Width, nonNullText.Height),
            Left = leftSize,
            Top = topSize,
            Right = rightSize,
            Bottom = bottomSize,
            Layout = NPatchLayout.NinePatch
        };

        var absoluteRotation = this.GetAbsoluteRotation();

        DrawTextureNPatch(
            nonNullText,
            nPatchInfo,
            destinationRectangle,
            Vector2.Zero,
            -absoluteRotation,
            Color);
    }


}
