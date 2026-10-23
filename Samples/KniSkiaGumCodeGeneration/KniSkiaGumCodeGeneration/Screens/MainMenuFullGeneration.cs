using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using Gum.GueDeriving;
using KniSkiaGumCodeGeneration.Components;
using RenderingLibrary.Graphics;
using System.Linq;

using static KniSkiaGumCodeGeneration.Components.ComponentWithStates;

namespace KniSkiaGumCodeGeneration.Screens
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
