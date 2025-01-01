using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using MonoGameGum.GueDeriving;
using static MonoGameGumCodeGeneration.Components.ComponentWithStatesRuntime;

namespace MonoGameGumCodeGeneration.Screens
{
    partial class MainMenuFullGenerationRuntime : GraphicalUiElement
    {
        partial void CustomInitialize()
        {
            ComponentWithStatesInstance.ColorCategoryState = ColorCategory.RedState;
            PopupInstance.TextInstance.Text = "I'm setting the text here in custom code.";

        }
    }
}
