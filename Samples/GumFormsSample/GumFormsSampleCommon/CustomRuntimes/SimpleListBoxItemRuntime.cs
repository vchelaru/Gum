using MonoGameGum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.CustomRuntimes;

internal class SimpleListBoxItemRuntime : DefaultListBoxItemRuntime
{
    public SimpleListBoxItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(
        fullInstantiation, tryCreateFormsObject)
    {
        if(fullInstantiation)
        {
            base.Children.Remove(GetGraphicalUiElementByName("TextInstance"));

            var coloredRectangle = new ColoredRectangleRuntime();
            coloredRectangle.Width = 50;
            coloredRectangle.Height = 50;
            var random = new System.Random();
            coloredRectangle.Red = random.Next(255);
            coloredRectangle.Blue = random.Next(255);
            coloredRectangle.Green = random.Next(255);

            base.Children.Add(coloredRectangle);
        }
    }
}
