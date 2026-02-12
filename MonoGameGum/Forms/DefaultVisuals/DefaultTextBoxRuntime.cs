using Gum.Converters;
using Gum.Wireframe;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals;

public class DefaultTextBoxRuntime : DefaultTextBoxBaseRuntime
{
    protected override string CategoryName => "TextBoxCategory";

    public DefaultTextBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(fullInstantiation, tryCreateFormsObject)
    {
        this.HasEvents = true;
        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new TextBox(this);
        }
    }

    public TextBox FormsControl => FormsControlAsObject as TextBox;
}
