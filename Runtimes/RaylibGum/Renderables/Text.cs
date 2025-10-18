using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;
public class Text : InvisibleRenderable, IText
{
    Font _font;

    public Font Font
    {
        get
        {
            if (_font.BaseSize == 0)
            {
                _font = GetFontDefault();
            }

            return _font;
        }
        set => _font = value;
    }

    public Color Color
    {
        get; set;
    } = Color.DarkGray;

    public string RawText
    {
        get; set;
    } = "Hello World";

    public float DescenderHeight => 2;

    public float FontScale => 1;

    public int FontSize { get; set; } = 12;

    public float WrappedTextWidth => MeasureTextEx(Font, RawText, FontSize, 1).X;

    public float WrappedTextHeight => MeasureTextEx(Font, RawText, FontSize, 1).Y;

    public TextOverflowVerticalMode TextOverflowVerticalMode { get; set; }
    float? IText.Width { get; set; }

    public HorizontalAlignment HorizontalAlignment
    {
        get; set;
    }

    public VerticalAlignment VerticalAlignment
    {
        get; set;
    }

    static Text()
    {
        GraphicalUiElement.UpdateFontFromProperties += HandleUpdateFontValues;
    }

    public Text() : this(null)
    {
    }

    public Text(ISystemManagers? managers)
    {
    }

    private static void HandleUpdateFontValues(IText text, GraphicalUiElement element)
    {
        var asText = text as Text;
        if(element is TextRuntime asTextRuntime)
        {
            asText.FontSize = asTextRuntime.FontSize;

        }
    }

    public override void Render(ISystemManagers managers)
    {
        if (!Visible) return;

        var position = new Vector2(
            this.GetAbsoluteLeft(),
            this.GetAbsoluteTop());
        var origin = new Vector2(
            0, // todo - handle horizontal alignment
            0); // todo - handle vertical alignment


        if(HorizontalAlignment == HorizontalAlignment.Center)
        {
            position.X += this.Width / 2;
            origin.X = MeasureTextEx(Font, RawText, FontSize, 1).X/2;
        }
        else if (HorizontalAlignment == HorizontalAlignment.Right)
        {
            position.X += this.Width;
            origin.X = MeasureTextEx(Font, RawText, FontSize, 1).X;
        }

        if (VerticalAlignment == VerticalAlignment.Center)
        {
            position.Y += this.Height / 2;
            origin.Y = MeasureTextEx(Font, RawText, FontSize, 1).Y / 2;
        }
        if (VerticalAlignment == VerticalAlignment.Bottom)
        {
            position.Y += this.Height;
            origin.Y = MeasureTextEx(Font, RawText, FontSize, 1).Y;
        }

        // todo - handle alignment
        //DrawText(RawText, x, y, 20, Color.DarkGray);
        //DrawTextEx(Font, RawText, position, FontSize, 0, Color);
        const float spacing = 1;
        DrawTextPro(Font, RawText, position, origin, 0, FontSize, spacing, Color);

    }

    public void SetNeedsRefreshToTrue()
    {
    }

    public void UpdatePreRenderDimensions()
    {
    }


}
