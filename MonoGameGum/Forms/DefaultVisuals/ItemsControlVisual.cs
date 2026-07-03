using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Forms.DefaultVisuals;
[Obsolete("Legacy V2 default visual. Use the V3 visuals via DefaultVisualsVersion.V3/.Newest; the V2 default visuals are slated for removal in a future release.")]
public class ItemsControlVisual : ScrollViewerVisual
{
    public ItemsControlVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(
        fullInstantiation, 
        // do not create the forms control here, because we want it to be of type ItemsControl
        tryCreateFormsObject:false)
    {
        this.HasEvents = true;
        if (tryCreateFormsObject)
        {
            this.FormsControlAsObject = new ItemsControl(this);
        }
    }

    public new ItemsControl? FormsControl => this.FormsControlAsObject as ItemsControl;
}
