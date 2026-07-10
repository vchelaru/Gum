#pragma warning disable CS0618, GUM001 // Default visuals intentionally use deprecated MonoGameGum.GueDeriving shim types for backward compatibility until V1/V2/V3 visuals are retired. See issue #2715.
using Gum.Wireframe;

using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


#if XNALIKE
using Microsoft.Xna.Framework;
#if XNALIKE
using MonoGameGum.GueDeriving;
#else
using Gum.GueDeriving;
#endif
#else
#if XNALIKE
using MonoGameGum.GueDeriving;
#else
using Gum.GueDeriving;
#endif
#endif
using Gum.Forms.Controls;
#if SKIA
using Color = SkiaSharp.SKColor;
#endif

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
