using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.Screens
{
    internal class FrameworkElementExampleScreen
    {
        public void Initialize(GraphicalUiElement root)
        {
            root.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            root.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            root.Width = 0;
            root.Height = 0;
            FrameworkElement.DefaultFormsComponents[typeof(Button)] =
                typeof(FullyCustomizedButton);

            var currentY = 0;

            var button = new Button();
            root.Children.Add(button.Visual);
            button.X = 0;
            button.Y = 0;
            button.Width = 100;
            button.Height = 50;
            button.Text = "Hello MonoGame!";
            int clickCount = 0;
            button.Click += (_, _) =>
            {
                clickCount++;
                button.Text = $"Clicked {clickCount} times";
            };

            currentY += 50;

            var checkbox = new CheckBox();
            root.Children.Add(checkbox.Visual);
            checkbox.X = 0;
            checkbox.Y = currentY;
            checkbox.Text = "Checkbox";

            currentY += 50;

            var comboBox = new ComboBox();
            root.Children.Add(comboBox.Visual);
            comboBox.Name = "Hello";
            comboBox.Width = 140;
            comboBox.X = 0;
            comboBox.Y = currentY;
            for(int i = 0; i < 20; i++)
            {
                comboBox.Items.Add($"Item {i}");
            }

            currentY += 120;

            // We can also create buttons through the creation of the default controls:
            var buttonRuntime = new DefaultButtonRuntime();
            root.Children.Add(buttonRuntime);
            buttonRuntime.X = 0;
            buttonRuntime.Y = currentY;
            buttonRuntime.Width = 100;
            buttonRuntime.Height = 50;
            buttonRuntime.TextInstance.Text = "Other Button!";
            var formsButton = buttonRuntime.FormsControl;
            formsButton.Click += (_, _) =>
            {
                clickCount++;
                formsButton.Text = $"Clicked {clickCount} times";
            };

            currentY += 50;
            var listBox = new ListBox();
            root.Children.Add(listBox.Visual);
            listBox.X = 0;
            listBox.Y = currentY;
            listBox.Width = 200;
            listBox.Height = 200;
            for (int i = 0; i < 20; i++)
            {
                listBox.Items.Add($"Item {i}");
            }


            var scrollBar = new ScrollBar();
            root.Children.Add(scrollBar.Visual);
            scrollBar.Width = 24;
            scrollBar.Height = 200;
            scrollBar.X = 200;
            scrollBar.Minimum = 0;
            scrollBar.Maximum = 150;
            scrollBar.ViewportSize = 50;

            var scrollViewer = new ScrollViewer();
            root.Children.Add(scrollViewer.Visual);
            scrollViewer.X = 300;
            scrollViewer.Y = 0;
            scrollViewer.Width = 200;
            scrollViewer.Height = 200;
            scrollViewer.InnerPanel.StackSpacing = 2;
            for (int i = 0; i < 20; i++)
            {
                var innerButton = new Button();
                innerButton.X = 1;
                innerButton.Visual.Width = -2;
                innerButton.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                innerButton.Text = $"Button {i}";
                innerButton.Click += (_, _) =>
                {
                    ShowPopup($"You just clicked on {innerButton.Text}. This is a modal popup (no other UI has input)");
                };
                scrollViewer.InnerPanel.Children.Add(innerButton.Visual);


            }



            //var viewer = new DefaultScrollViewerRuntime(true, false);
            //this.Root.Children.Add(viewer);
            //viewer.X = 300;
            //viewer.Y = 0;
            //viewer.Width = 200;
            //viewer.Height = 200;

            var textBox = new TextBox();
            root.Children.Add(textBox.Visual);
            textBox.X = 220;
            textBox.Y = 220;
            textBox.Width = 200;
            textBox.Height = 34;
            textBox.Placeholder = "Placeholder Text...";

            var textBox2 = new TextBox();
            root.Children.Add(textBox2.Visual);
            textBox2.X = 220;
            textBox2.Y = 260;
            textBox2.Width = 200;
            textBox2.Height = 34;
            textBox2.Placeholder = "Placeholder Text...";

            var passwordBox = new PasswordBox();
            root.Children.Add(passwordBox.Visual);
            passwordBox.X = 220;
            passwordBox.Y = 300;
            passwordBox.Width = 200;
            passwordBox.Height = 34;
            passwordBox.Placeholder = "Enter Password";

            var slider = new Slider();
            root.Children.Add(slider.Visual);
            slider.X = 220;
            slider.Y = 340;
            slider.Minimum = 0;
            slider.Maximum = 10;
            slider.TicksFrequency = 1;
            slider.IsSnapToTickEnabled = true;
            slider.Width = 200;


            var customizedButton = new Button();
            root.Children.Add(customizedButton.Visual);

            //customizedButton.Width = 200;
            //customizedButton.Height = 50;

            customizedButton.X = 450;
            customizedButton.Y = 300;

            //var spriteRuntime = new SpriteRuntime();
            //spriteRuntime.SourceFileName = "button_square_gradient.png";
            //spriteRuntime.X = 100;
            //spriteRuntime.Y = 100;
            //spriteRuntime.Width = 500;
            //spriteRuntime.Height = 600;
            //this.Root.Children.Add(spriteRuntime);

            //var innerTextBox = new TextBox();
            //innerTextBox.X = 100;
            //innerTextBox.Y = 200;
            //innerTextBox.Width = 200;
            //innerTextBox.Height = 40;

            //spriteRuntime.Children.Add(innerTextBox.Visual);

            //// ButtonCategory is the category that all Buttons must have
            //var category = customizedButton.Visual.Categories["ButtonCategory"];

            //// Highlighted state is applied when the button is hovered over
            //var highlightedState = category.States.Find(item => item.Name == "Highlighted");
            //// remove all old styling:
            //highlightedState.Variables.Clear();
            //// Add the new color:
            //highlightedState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
            //{
            //    Name = "ButtonBackground.Color",
            //    Value = Color.Yellow
            //});

            //highlightedState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
            //{
            //    Name = "TextInstance.Color",
            //    Value = Color.Black
            //});

            //highlightedState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
            //{
            //    Name = "TextInstance.FontScale",
            //    // FontScale expects a float value, so use 2.0f instead of 2
            //    Value = 2.0f
            //});

            //var enabledState = category.States.Find(item => item.Name == "Enabled");
            //enabledState.Variables.Clear();
            //enabledState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
            //{
            //    Name = "ButtonBackground.Color",
            //    Value = new Color(0, 0, 128),
            //});

            //enabledState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
            //{
            //    Name = "TextInstance.Color",
            //    Value = Color.White
            //});

            //enabledState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
            //{
            //    Name = "TextInstance.FontScale",
            //    // FontScale expects a float value, so use 2.0f instead of 2
            //    Value = 1.0f
            //});
        }

        private void ShowPopup(string text)
        {
            var container = new ContainerRuntime();
            container.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            container.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            container.X = 0;
            container.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            container.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            container.Y = 0;

            FrameworkElement.ModalRoot.Children.Add(container);

            container.Width = 300;
            container.Height = 200;

            var background = new ColoredRectangleRuntime();
            background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            background.Width = 0;
            background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            background.Height = 0;
            background.Color = Color.DarkGray;
            container.Children.Add(background);

            var textInstance = new TextRuntime();
            textInstance.X = 4;
            textInstance.Y = 4;
            textInstance.Text = text;
            textInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            textInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            textInstance.Width = -8;
            textInstance.Height = -8;
            container.Children.Add(textInstance);

            var button = new Button();
            button.Text = "Close";
            var buttonVisual = button.Visual;
            buttonVisual.Y = -10;
            buttonVisual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Bottom;
            buttonVisual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
            buttonVisual.X = 0;
            buttonVisual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            buttonVisual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            container.Children.Add(button.Visual);
            button.Click += (_, _) =>
            {
                container.RemoveFromManagers();
                container.Parent = null;
            };
        }
    }
}
