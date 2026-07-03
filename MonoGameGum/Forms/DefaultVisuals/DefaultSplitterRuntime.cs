#pragma warning disable CS0618, GUM001 // Default visuals intentionally use deprecated MonoGameGum.GueDeriving shim types for backward compatibility until V1/V2/V3 visuals are retired. See issue #2715.
using Gum.Wireframe;
using Gum.Forms.Controls;
#if XNALIKE
using MonoGameGum.GueDeriving;
#else
using Gum.GueDeriving;
#endif
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Styling = Gum.Forms.DefaultVisuals.Styling;

#if FRB
namespace MonoGameGum.Forms.DefaultVisuals;
#else
namespace Gum.Forms.DefaultVisuals;
#endif
[Obsolete("Legacy V1 default visual. Use the V3 visuals via DefaultVisualsVersion.V3/.Newest; the V1 default visuals are slated for removal in a future release.")]
public class DefaultSplitterRuntime : InteractiveGue
{
    public DefaultSplitterRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        this.HasEvents = true;

        if (fullInstantiation)
        {
            this.Width = 8;
            this.Height = 8;

            var background = new ColoredRectangleRuntime();
            background.Name = "Background";
            background.Dock(Gum.Wireframe.Dock.Fill);
            background.Color = Styling.ActiveStyle.Colors.Gray;
            this.AddChild(background);

        }

        if(tryCreateFormsObject)
        {
            FormsControlAsObject = new Splitter(this);
        }
    }

    public Splitter FormsControl => FormsControlAsObject as Splitter;
}
