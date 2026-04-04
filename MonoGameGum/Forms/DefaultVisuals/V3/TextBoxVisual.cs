using Gum.Forms.Controls;

namespace Gum.Forms.DefaultVisuals.V3
{
    /// <summary>
    /// Default V3 visual for a TextBox control. Extends TextBoxBaseVisual with
    /// TextBox-specific state category naming.
    /// </summary>
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

        /// <summary>
        /// Returns the strongly-typed TextBox Forms control backing this visual.
        /// </summary>
        public TextBox FormsControl => (TextBox)FormsControlAsObject;
    }
}
