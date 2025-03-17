using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using MonoGameGum.GueDeriving;

namespace MonoGameGumFromFile.Screens
{
    partial class StartScreenRuntime
    {
        partial void CustomInitialize()
        {
            var exposedVariableInstance = GetGraphicalUiElementByName("ComponentWithExposedVariableInstance");
            exposedVariableInstance.SetProperty("Text", 
                "I'm set in code. I even [IsBold=true]support BBCode[/IsBold] for [Color=Pink]inline[/Color] styling.");
        }
    }
}
