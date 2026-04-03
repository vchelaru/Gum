using Gum.Forms.Controls;

namespace Gum.Forms.DefaultVisuals.V3
{
    /// <summary>
    /// Default V3 visual for a PasswordBox control. Extends TextBoxBaseVisual with
    /// PasswordBox-specific state category naming.
    /// </summary>
    public class PasswordBoxVisual : TextBoxBaseVisual
    {
        protected override string CategoryName => "PasswordBoxCategory";

        public PasswordBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(fullInstantiation, tryCreateFormsObject)
        {
            this.HasEvents = true;
            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new PasswordBox(this);
            }
        }

        /// <summary>
        /// Returns the strongly-typed PasswordBox Forms control backing this visual.
        /// </summary>
        public PasswordBox FormsControl => (PasswordBox)FormsControlAsObject;
    }
}
