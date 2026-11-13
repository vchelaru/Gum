using Gum.Forms.Controls;

namespace Gum.Themes.Editor;

/// <summary>An ItemsControl that paints the same recessed well as the Editor ScrollViewer and ListBox.</summary>
public class ItemsControlVisual : ScrollViewerVisual
{
    public ItemsControlVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        // the Forms control is created below so that it is an ItemsControl, not a ScrollViewer
        base(fullInstantiation, tryCreateFormsObject: false)
    {
        this.HasEvents = true;
        if (tryCreateFormsObject)
        {
            this.FormsControlAsObject = new ItemsControl(this);
        }
    }

    /// <summary>Returns the strongly-typed ItemsControl Forms control backing this visual.</summary>
    public new ItemsControl? FormsControl => (ItemsControl?)FormsControlAsObject;
}
