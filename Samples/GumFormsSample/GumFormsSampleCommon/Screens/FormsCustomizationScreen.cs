using Gum.Wireframe;
using GumFormsSample.CustomRuntimes;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.Screens
{
    internal class FormsCustomizationScreen
    {
        public void Initialize(GraphicalUiElement Root)
        {

            var button = new Button();

            var category = button.Visual.Categories["ButtonCategory"];
            var highlightedState = category.States.FirstOrDefault(item => item.Name == FrameworkElement.HighlightedState);

            highlightedState.Variables.Clear();
            // Add the new color:
            highlightedState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
            {
                Name = "ButtonBackground.Color",
                Value = Microsoft.Xna.Framework.Color.Yellow
            });

            Root.Children.Add(button.Visual);
            button.X = 0;
            button.Y = 10;
            button.Width = 100;
            button.Height = 42;
            button.Text = $"My highlight is yellow";


            var listBox = new ListBox();
            Root.Children.Add(listBox.Visual);
            listBox.X = 0;
            listBox.Y = 100;
            listBox.Width = 220;
            listBox.Height = 200;

            // assign the template before adding new list items
            listBox.VisualTemplate = 
                new MonoGameGum.Forms.VisualTemplate(() => 
                    // do not create a forms object because this template will be
                    // automatically added to a ListBoxItem by the ListBox:
                    new CustomListBoxItemRuntime(tryCreateFormsObject:false));

            for (int i = 0; i < 20; i++)
            {
                listBox.Items.Add($"Custom ListBoxItem [{i}]");
            }

        }
    }
}
