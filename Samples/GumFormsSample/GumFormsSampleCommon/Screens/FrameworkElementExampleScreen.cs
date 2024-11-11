using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.Screens
{
    internal class FrameworkElementExampleScreen
    {
        public void Initialize(List<GraphicalUiElement> roots)
        {
            var root = roots[0];
            root.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            root.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            root.Width = 0;
            root.Height = 0;
            FrameworkElement.DefaultFormsComponents[typeof(Button)] =
                typeof(FullyCustomizedButton);

            CreateColumn1Ui(root);

            CreateColumn2Ui(root);

            CreateLayeredUi(roots);

        }

        private void CreateColumn1Ui(GraphicalUiElement root)
        {
            var currentY = 0;

            var button = new Button();
            root.Children.Add(button.Visual);
            button.X = 0;
            button.Y = 0;
            button.Width = 100;
            button.Height = 50;
            button.Text = "Hello MonoGame!";
            int clickCount = 0;
            button.Visual.RollOn += (_, _) =>
            {
                Debug.WriteLine($"Roll on at {DateTime.Now}");
            };
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
            for (int i = 0; i < 20; i++)
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
        }

        private void CreateColumn2Ui(GraphicalUiElement root)
        {
            var currentY = 20;

            var scrollViewer = new ScrollViewer();
            root.Children.Add(scrollViewer.Visual);
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            scrollViewer.X = 260;
            scrollViewer.Y = currentY;
            scrollViewer.Width = 200;
            scrollViewer.Height = 200;
            scrollViewer.InnerPanel.StackSpacing = 2;


            currentY += 200;
            Button addButton = new Button();
            addButton.Text = "Add Items";
            addButton.Click += (_, _) =>
            {
                var random = new System.Random();

                var child = new ColoredRectangleRuntime();
                child.Red = random.Next(255);
                child.Green = random.Next(255);
                child.Blue = random.Next(255);
                scrollViewer.InnerPanel.Children.Add(child);

            };
            addButton.X = 260;
            addButton.Y = currentY;
            root.Children.Add(addButton.Visual);

            currentY += 40;

            var textBox = new TextBox();
            root.Children.Add(textBox.Visual);
            textBox.X = 260;
            textBox.Y = currentY;
            textBox.Width = 200;
            textBox.Height = 34;
            textBox.Placeholder = "Placeholder Text...";

            currentY += 40;

            var textBox2 = new TextBox();
            root.Children.Add(textBox2.Visual);
            textBox2.X = 260;
            textBox2.Y = currentY;
            textBox2.Width = 200;
            textBox2.Height = 34;
            textBox2.Placeholder = "Placeholder Text...";

            currentY += 40;

            var passwordBox = new PasswordBox();
            root.Children.Add(passwordBox.Visual);
            passwordBox.X = 260;
            passwordBox.Y = currentY;
            passwordBox.Width = 200;
            passwordBox.Height = 34;
            passwordBox.Placeholder = "Enter Password";

            currentY += 40;

            var slider = new Slider();
            root.Children.Add(slider.Visual);
            slider.X = 260;
            slider.Y = currentY;
            slider.Minimum = 0;
            slider.Maximum = 10;
            slider.TicksFrequency = 1;
            slider.IsSnapToTickEnabled = true;
            slider.Width = 200;

            currentY += 40;

            var showPopupButton = new Button();
            root.Children.Add(showPopupButton.Visual);

            showPopupButton.Visual.RollOn += (_, _) =>
            {
                Debug.WriteLine($"Roll on at {DateTime.Now}");
            };

            showPopupButton.Visual.RollOff += (_, _) =>
            {
                Debug.WriteLine($"Roll off at {DateTime.Now}");
            };

            showPopupButton.X = 260;
            showPopupButton.Y = currentY;
            showPopupButton.Width = 200;

            showPopupButton.Text = "Show Non-Modal Popup";
            showPopupButton.Click += (_, _) =>
            {
                ShowPopup("This is a non-modal popup", isModal: false);
                // create a popup here
            };
            currentY += 40;


            var showModalPopupButton = new Button();
            root.Children.Add(showModalPopupButton.Visual);

            showModalPopupButton.X = 260;
            showModalPopupButton.Y = currentY;
            showModalPopupButton.Width = 200;

            showModalPopupButton.Text = "Show Modal Popup";
            showModalPopupButton.Click += (_,_) =>
            {
                ShowPopup("This is a modal popup", isModal:true);
                // create a popup here
            };
            currentY += 40;


        }

        void CreateLayeredUi(List<GraphicalUiElement> extraRoots)
        {
            var layer = new Layer();
            var layerCameraSettings = new LayerCameraSettings();
            layerCameraSettings.Zoom = 1;
            layerCameraSettings.IsInScreenSpace = true;
            layer.LayerCameraSettings = layerCameraSettings;
            SystemManagers.Default.Renderer.AddLayer(layer);

            var layeredContainer = new ContainerRuntime();
            layeredContainer.Name = "Layered Container";
            layeredContainer.X = 0;
            layeredContainer.Y = 0;
            layeredContainer.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
            layeredContainer.AddToManagers(SystemManagers.Default, layer);
            layeredContainer.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
            layeredContainer.XOrigin = HorizontalAlignment.Right;
            layeredContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            extraRoots.Add(layeredContainer);

            var zoomInButton = new Button();
            zoomInButton.Text = "Zoom layer in";
            zoomInButton.Width = 0;
            zoomInButton.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            zoomInButton.Height = 100;
            zoomInButton.Click += (_,_) =>
            {
                layerCameraSettings.Zoom += 0.1f;
            };
            layeredContainer.Children.Add(zoomInButton.Visual);


            var zoomOutButton = new Button();
            zoomOutButton.Text = "Zoom layer out";
            zoomOutButton.Width = 0;
            zoomOutButton.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            zoomOutButton.Height = 100;
            zoomOutButton.Click += (_, _) =>
            {
                layerCameraSettings.Zoom -= 0.1f;
            };
            layeredContainer.Children.Add(zoomOutButton.Visual);
            //button.Visual.AddToManagers(SystemManagers.Default, null);
        }


        private void ShowPopup(string text, bool isModal)
        {
            var container = new ContainerRuntime();
            container.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            container.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            container.X = 0;
            container.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            container.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            container.Y = 0;

            if(isModal)
            {
                FrameworkElement.ModalRoot.Children.Add(container);
            }
            else
            {
                FrameworkElement.PopupRoot.Children.Add(container);
            }

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
