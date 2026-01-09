using Gum.Wireframe;

using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


#if RAYLIB
using Gum.GueDeriving;
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
#endif
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals.V3;

public class SplitterVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }

    Color _backgroundColor;

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (!value.Equals(_backgroundColor))
            {
                _backgroundColor = value;
                //FormsControl?.UpdateState();
                if(Background != null)
                {
                    Background.Color = value;
                }
            }
        }
    }

    public SplitterVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        Width = 8;
        Height = 8;

        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        Background = new NineSliceRuntime();
        Background.Name = "Background";
        Background.Dock(Gum.Wireframe.Dock.Fill);
        Background.Texture = uiSpriteSheetTexture;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);
        this.AddChild(Background);

        BackgroundColor = Styling.ActiveStyle.Colors.InputBackground;

        if(tryCreateFormsObject)
        {
            FormsControlAsObject = new Splitter(this);
        }
    }

    public Splitter FormsControl => (Splitter)FormsControlAsObject;
}
