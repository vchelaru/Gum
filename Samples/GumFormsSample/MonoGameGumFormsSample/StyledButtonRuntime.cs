using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms.DefaultVisuals;

namespace GumFormsSample
{
    class StyledButtonRuntime : DefaultButtonRuntime
    {
        public StyledButtonRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : 
            base(fullInstantiation, tryCreateFormsObject)
        {
            if(fullInstantiation)
            {
                var category = this.Categories["ButtonCategory"];

                var highlightedState = category.States.Find(item => item.Name == "Highlighted");
                highlightedState.Variables.Clear();
                highlightedState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
                {
                    Name = "ButtonBackground.Color",
                    Value = new Color(255,0,191)
                });

                var enabledState = category.States.Find(item => item.Name == "Enabled");
                enabledState.Variables.Clear();
                enabledState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
                {
                    Name = "ButtonBackground.Color",
                    Value = new Color(255, 100, 194),
                });

            }
        }
    }
}
