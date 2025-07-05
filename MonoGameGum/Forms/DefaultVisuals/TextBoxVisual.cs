using MonoGameGum.Forms.Controls;

namespace MonoGameGum.Forms.DefaultVisuals
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

        public TextBox FormsControl => FormsControlAsObject as TextBox;
    }
}
