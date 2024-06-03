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
        this.RollOn += (_, _) => System.Diagnostics.Debug.WriteLine($"Roll On {Name}");
        this.RollOff += (_, _) => System.Diagnostics.Debug.WriteLine($"Roll Off @ {Name}");
    }

    int NumberOfClicks;

    public override void AfterFullCreation()
    {
            textInstance = GetGraphicalUiElementByName("TextInstance");
    }

    public void HandleClick(object sender, EventArgs args)
    {

        NumberOfClicks++;

        textInstance.SetProperty("Text", "Clicked " + NumberOfClicks + " times");
    }
}
