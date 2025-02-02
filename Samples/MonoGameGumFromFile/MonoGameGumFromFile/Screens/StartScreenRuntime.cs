using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using MonoGameGum.GueDeriving;

namespace MonoGameGumFromFile.Screens
{
    partial class StartScreenRuntime : Gum.Wireframe.InteractiveGue
    {
        partial void CustomInitialize()
        {
            var exposedVariableInstance = GetGraphicalUiElementByName("ComponentWithExposedVariableInstance");
            exposedVariableInstance.SetProperty("Text", "I'm set in code");
        }
    }
}
