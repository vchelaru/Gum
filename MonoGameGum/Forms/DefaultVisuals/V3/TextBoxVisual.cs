using Gum.Forms.Controls;

namespace Gum.Forms.DefaultVisuals.V3
{
    public class TextBoxVisual : TextBoxBaseVisual
    {
        protected override string CategoryName => "TextBoxCategory";

        public TextBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(fullInstantiation, tryCreateFormsObject)
        {
            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new TextBox(this);
            }
        }

        public TextBox FormsControl => (TextBox)FormsControlAsObject;
    }
}
