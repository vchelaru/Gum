using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGumFromFile.ScreenRuntimes
{
    internal class StartScreenRuntime : GraphicalUiElement
    {
        public StartScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
            : base()
        {
            
        }

        public override void AfterFullCreation()
        {
            base.AfterFullCreation();

            var exposedVariableInstance = GetGraphicalUiElementByName("ComponentWithExposedVariableInstance");
            exposedVariableInstance.SetProperty("Text", "I'm set in code");
        }
    }
}
