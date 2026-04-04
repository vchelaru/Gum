using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for an ItemsControl. Extends ScrollViewerVisual to provide a scrollable
/// container for dynamically generated items.
/// </summary>
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

    /// <summary>
    /// Returns the strongly-typed ItemsControl Forms control backing this visual.
    /// </summary>
    public new ItemsControl? FormsControl => (ItemsControl)FormsControlAsObject;
}
