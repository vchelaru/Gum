using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using MonoGameGumCodeGeneration.Components;
using RenderingLibrary.Graphics;
using System.Linq;

using static MonoGameGumCodeGeneration.Components.ComponentWithStates;

namespace MonoGameGumCodeGeneration.Screens
{
    partial class MainMenuFullGeneration : global::Gum.Forms.Controls.FrameworkElement
    {
        partial void CustomInitialize()
        {
            ComponentWithStatesInstance.ColorCategoryState = ColorCategory.RedState;
            PopupInstance.TextInstance.Text = "I'm setting the text here in custom code.";

        }
    }
}
