using Gum.Forms.Controls;

namespace Gum.Forms.DefaultVisuals
{
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

        public PasswordBox FormsControl => FormsControlAsObject as PasswordBox;
    }
}
