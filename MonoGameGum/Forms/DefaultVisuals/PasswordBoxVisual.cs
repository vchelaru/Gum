using MonoGameGum.Forms.Controls;

namespace MonoGameGum.Forms.DefaultVisuals
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
