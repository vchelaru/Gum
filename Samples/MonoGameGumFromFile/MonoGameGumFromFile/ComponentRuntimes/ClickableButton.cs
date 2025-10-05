using Gum.Wireframe;
using Gum.Forms.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGumFromFile.ComponentRuntimes;

internal class ClickableButton : InteractiveGue
{
    public ClickableButton(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base() 
    {
        if(fullInstantiation)
        {
            // no need to do anything here, we are fully instantiated by the Gum object
        }

        if(tryCreateFormsObject)
        {
            FormsControlAsObject = new Button(this);
        }
    }

    public Button FormsControl => FormsControlAsObject as Button;
}
