using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGumFromFile.ComponentRuntimes;

internal class ClickableButton : InteractiveGue
{
    GraphicalUiElement textInstance;

    public ClickableButton() : base() 
    {
        this.Click += HandleClick;
    }

    int NumberOfClicks;

    public void HandleClick(object sender, EventArgs args)
    {
        if(textInstance == null)
        {
            textInstance = GetGraphicalUiElementByName("TextInstance");
        }

        NumberOfClicks++;

        textInstance.SetProperty("Text", "Clicked " + NumberOfClicks + " times");
    }
}
