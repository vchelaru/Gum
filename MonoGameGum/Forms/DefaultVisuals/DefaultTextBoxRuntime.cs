#pragma warning disable CS0618, GUM001 // Default visuals intentionally use deprecated MonoGameGum.GueDeriving shim types for backward compatibility until V1/V2/V3 visuals are retired. See issue #2715.
using Gum.Converters;
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

#if FRB
namespace MonoGameGum.Forms.DefaultVisuals;
#else
namespace Gum.Forms.DefaultVisuals;
#endif

[Obsolete("Legacy V1 default visual. Use the V3 visuals via DefaultVisualsVersion.V3/.Newest; the V1 default visuals are slated for removal in a future release.")]
public class DefaultTextBoxRuntime : DefaultTextBoxBaseRuntime
{
    protected override string CategoryName => "TextBoxCategory";

    public DefaultTextBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(fullInstantiation, tryCreateFormsObject)
    {
        this.HasEvents = true;
        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new TextBox(this);
        }
    }

    public TextBox FormsControl => FormsControlAsObject as TextBox;
}
