#pragma warning disable CS0618, GUM001 // Default visuals intentionally use deprecated MonoGameGum.GueDeriving shim types for backward compatibility until V1/V2/V3 visuals are retired. See issue #2715.
using Gum.Wireframe;

using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if XNALIKE
using MonoGameGum.GueDeriving;
#else
using MonoGameGum.GueDeriving;
#endif
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals;

public class SplitterVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }

    public SplitterVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        this.HasEvents = true;
        Width = 8;
        Height = 8;

        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        Background = new NineSliceRuntime();
        Background.Name = "Background";
        Background.Dock(Gum.Wireframe.Dock.Fill);
        Background.Color = Styling.ActiveStyle.Colors.DarkGray;
        Background.Texture = uiSpriteSheetTexture;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);
        this.AddChild(Background);

        if(tryCreateFormsObject)
        {
            FormsControlAsObject = new Splitter(this);
        }
    }

    public Splitter FormsControl => FormsControlAsObject as Splitter;
}
