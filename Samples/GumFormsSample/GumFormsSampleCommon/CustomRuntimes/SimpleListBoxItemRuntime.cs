using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.CustomRuntimes;

internal class SimpleListBoxItemRuntime : ListBoxItemVisual
{
    public SimpleListBoxItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(
        fullInstantiation, tryCreateFormsObject)
    {
        if(fullInstantiation)
        {
            base.Children.Remove(GetGraphicalUiElementByName("TextInstance"));

            var coloredRectangle = new RectangleRuntime();
            coloredRectangle.Width = 50;
            coloredRectangle.Height = 50;
            coloredRectangle.IsFilled = true;
            coloredRectangle.StrokeWidth = 0;
            coloredRectangle.IsAntialiased = false;
            var random = new System.Random();
            coloredRectangle.FillRed = random.Next(255);
            coloredRectangle.FillBlue = random.Next(255);
            coloredRectangle.FillGreen = random.Next(255);

            base.Children.Add(coloredRectangle);
        }
    }
}
