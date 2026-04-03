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

/// <summary>
/// Default V3 visual for a Splitter control. Contains a bordered background that can be
/// dragged to resize adjacent elements.
/// </summary>
public class SplitterVisual : InteractiveGue
{
    /// <summary>
    /// The bordered background nine-slice that fills the control.
    /// </summary>
    public NineSliceRuntime Background { get; private set; }

    Color _backgroundColor;

    /// <summary>
    /// The color applied to the background. Setting this value immediately updates the visual.
    /// </summary>
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
        this.HasEvents = true;
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

    /// <summary>
    /// Returns the strongly-typed Splitter Forms control backing this visual.
    /// </summary>
    public Splitter FormsControl => (Splitter)FormsControlAsObject;
}
