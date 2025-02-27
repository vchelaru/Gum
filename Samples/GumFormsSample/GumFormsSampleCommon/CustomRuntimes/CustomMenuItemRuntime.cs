using MonoGameGum.Forms.DefaultVisuals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.CustomRuntimes;
internal class CustomMenuItemRuntime : DefaultMenuItemRuntime
{
    public CustomMenuItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(fullInstantiation, tryCreateFormsObject)
    {
        var category = this.Categories["MenuItemCategory"];


        // We can customize the TextInstance outside of states as long
        // as states don't overwrite these values:
        TextInstance.FontScale = 2;

        var enabledState = category.States.Find(item => item.Name == "Enabled");
        enabledState.Variables.Clear();
        enabledState.Variables.Add(
            new Gum.DataTypes.Variables.VariableSave()
            {
                Value = false,
                Name = "Background.Visible"
            });

        var highlightedState = category.States.Find(item => item.Name == "Highlighted");
        highlightedState.Variables.Clear();
        highlightedState.Variables.Add(
            new Gum.DataTypes.Variables.VariableSave()
            {
                Value = true,
                Name = "Background.Visible"
            });
        highlightedState.Variables.Add(
            new Gum.DataTypes.Variables.VariableSave()
            {
                Value = new Microsoft.Xna.Framework.Color(0, 200, 0),
                Name = "Background.Color"
            });


        var selectedState = category.States.Find(item => item.Name == "Selected");
        selectedState.Variables.Clear();
        selectedState.Variables.Add(
            new Gum.DataTypes.Variables.VariableSave()
            {
                Value = true,
                Name = "Background.Visible"
            });
        selectedState.Variables.Add(
            new Gum.DataTypes.Variables.VariableSave()
            {
                Value = new Microsoft.Xna.Framework.Color(0, 200, 100),
                Name = "Background.Color"
            });

    }
}
