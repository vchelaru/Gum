using Gum.Forms.Controls;

namespace Gum.Forms.DefaultVisuals
{
    [System.Obsolete("Legacy V2 default visual. Use the V3 visuals via DefaultVisualsVersion.V3/.Newest; the V2 default visuals are slated for removal in a future release.")]
    public class TextBoxVisual : TextBoxBaseVisual
    {
        protected override string CategoryName => "TextBoxCategory";

        public TextBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(fullInstantiation, tryCreateFormsObject)
        {
            this.HasEvents = true;
            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new TextBox(this);
            }
        }

        public TextBox FormsControl => FormsControlAsObject as TextBox;
    }
}
