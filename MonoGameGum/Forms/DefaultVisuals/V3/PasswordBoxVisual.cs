using Gum.Forms.Controls;

namespace Gum.Forms.DefaultVisuals.V3
{
    public class PasswordBoxVisual : TextBoxBaseVisual
    {
        protected override string CategoryName => "PasswordBoxCategory";

        public PasswordBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(fullInstantiation, tryCreateFormsObject)
        {
            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new PasswordBox(this);
            }
        }

        public PasswordBox FormsControl => FormsControlAsObject as PasswordBox;
    }
}
