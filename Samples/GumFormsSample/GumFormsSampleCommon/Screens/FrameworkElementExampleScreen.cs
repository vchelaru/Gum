using Gum.Wireframe;
using GumFormsSample.CustomRuntimes;
using Microsoft.Xna.Framework;
using MonoGameGum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GumFormsSample.Screens
{
    internal class FrameworkElementExampleScreen : ContainerRuntime, IUpdateScreen
    {
        MenuItem FileMenuItem;
        MenuItem EditMenuItem;
        MenuItem CustomMenuItem;

        public FrameworkElementExampleScreen()
        {
            //FileManager.RelativeDirectory = "Content/";
            this.Dock(Gum.Wireframe.Dock.Fill);

            FrameworkElement.DefaultFormsTemplates[typeof(Button)] =
                new VisualTemplate(typeof(FullyCustomizedButton));

            CreateMenu();

            CreateColumn1Ui();

            CreateColumn2Ui();

            CreateColumn3Ui();

            // This requires custom roots which adds a lot of complexity so removing this for now
            //CreateLayeredUi();

        }

        private void CreateMenu()
        {
            var menu = new Menu();

            FileMenuItem = new MenuItem();
            FileMenuItem.Header = "File";

            for(int i = 0; i < 10; i++)
            {
                var subItem = new MenuItem();
                subItem.Header = $"File Item {i}";
                for(int j = 0; j < 5; j++)
                {
                    var jItem = new MenuItem();
                    for(int k = 0; k < 5; k++)
                    {
                        var kItem = new MenuItem();
                        kItem.Header = $"Sub Sub Item {k}";
                        jItem.Items.Add(kItem);
                    }
                    jItem.Header = $"Sub Item {j}";
                    subItem.Items.Add(jItem);

                }
                FileMenuItem.Items.Add(subItem);
            }

            menu.Items.Add(FileMenuItem);

            EditMenuItem = new MenuItem();
            EditMenuItem.Header = "Edit";
            for (int i = 0; i < 10; i++)
            {
                EditMenuItem.Items.Add($"Edit Item {i}");
            }
            menu.Items.Add(EditMenuItem);



            CustomMenuItem = new MenuItem();


            CustomMenuItem.Header = "Custom Dropdown";
            var customScrollViewerVisualTemplate = new VisualTemplate(() =>
            {
                var toReturn = new MonoGameGum.Forms.DefaultVisuals.DefaultScrollViewerRuntime();
                toReturn.MakeSizedToChildren();
                var background = toReturn.GetGraphicalUiElementByName("Background")
                    as ColoredRectangleRuntime;

                background.Color = Color.Orange;

                return toReturn;
            });
            CustomMenuItem.ScrollViewerVisualTemplate = customScrollViewerVisualTemplate;


            for (int i = 0; i < 10; i++)
            {
                var customMenuItemRuntime = new CustomMenuItemRuntime();
                customMenuItemRuntime.FormsControl.Header = $"Custom dropdown item {i}";

                CustomMenuItem.Items.Add(customMenuItemRuntime.FormsControl);
            }
            menu.Items.Add(CustomMenuItem);


            menu.Items.Add("Help");

            this.AddChild(menu);

        }

        private void CreateColumn1Ui()
        {
            var stackPanel = new StackPanel();
            stackPanel.Spacing = 4;
            this.AddChild(stackPanel);
            stackPanel.Y = 40;

            var normalLabel = new Label();
            normalLabel.Text = "This is a normal label";
            stackPanel.AddChild(normalLabel);

            var labelWithBbCode = new Label();
            labelWithBbCode.Text = "This is [IsBold=true]bold text[/IsBold] and\n[IsItalic=true]italic text[/IsItalic] on this label";
            stackPanel.AddChild(labelWithBbCode);

            var scrollBar = new ScrollBar();
            scrollBar.Width = 24;
            scrollBar.Height = 200;
            scrollBar.X = 200;

            scrollBar.Minimum = 0;
            scrollBar.Maximum = 150;
            scrollBar.ViewportSize = 50;
            stackPanel.AddChild(scrollBar);


            var button = new Button();
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
            stackPanel.AddChild(button);

            var checkbox = new CheckBox();
            checkbox.Text = "Checkbox";
            stackPanel.AddChild(checkbox);

            var comboBox = new ComboBox();
            comboBox.Name = "Hello";
            comboBox.Width = 140;
            for (int i = 0; i < 20; i++)
            {
                comboBox.Items.Add($"Item {i}");
            }
            stackPanel.AddChild(comboBox);

            // We can also create buttons through their visual type:
            var buttonRuntime = new MonoGameGum.Forms.DefaultVisuals.DefaultButtonRuntime();
            buttonRuntime.Width = 100;
            buttonRuntime.Height = 50;
            buttonRuntime.TextInstance.Text = "Other Button!";
            var formsButton = buttonRuntime.FormsControl;
            formsButton.Click += (_, _) =>
            {
                clickCount++;
                formsButton.Text = $"Clicked {clickCount} times";
            };
            stackPanel.AddChild(buttonRuntime);

            var listBox = new ListBox();
            listBox.Width = 200;
            listBox.Height = 200;

            for (int i = 0; i < 20; i++)
            {
                listBox.Items.Add($"Item {i}");
            }
            stackPanel.AddChild(listBox);

        }

        private void CreateColumn2Ui()
        {
            var stackPanel = new StackPanel();
            stackPanel.Y = 40;
            stackPanel.X = 260;
            stackPanel.Spacing = 4;
            this.AddChild(stackPanel);


            var scrollViewer = new ScrollViewer();
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollViewer.Width = 200;
            scrollViewer.Height = 200;
            scrollViewer.InnerPanel.StackSpacing = 2;
            stackPanel.AddChild(scrollViewer);

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
            stackPanel.AddChild(addButton);


            var textBox = new TextBox();
            textBox.Width = 200;
            textBox.Height = 34;
            textBox.Placeholder = "Placeholder Text...";
            stackPanel.AddChild(textBox);

            var wrappedTextBox = new TextBox();
            wrappedTextBox.Width = 200;
            
            // Make it no wrap to still accept return but not automatically wrap
            //wrappedTextBox.TextWrapping = MonoGameGum.Forms.TextWrapping.NoWrap;
            wrappedTextBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
            wrappedTextBox.AcceptsReturn = true;
            wrappedTextBox.Height = 140;
            wrappedTextBox.Placeholder = "Placeholder Text...";
            stackPanel.AddChild(wrappedTextBox);

            var passwordBox = new PasswordBox();
            passwordBox.Width = 200;
            passwordBox.Height = 34;
            passwordBox.Placeholder = "Enter Password";
            stackPanel.AddChild(passwordBox);

            var slider = new Slider();
            slider.Minimum = 0;
            slider.Maximum = 10;
            slider.TicksFrequency = 1;
            slider.IsSnapToTickEnabled = true;
            slider.Width = 200;
            stackPanel.AddChild(slider);

            var showPopupButton = new Button();

            showPopupButton.Visual.RollOn += (_, _) =>
            {
                Debug.WriteLine($"Roll on at {DateTime.Now}");
            };

            showPopupButton.Visual.RollOff += (_, _) =>
            {
                Debug.WriteLine($"Roll off at {DateTime.Now}");
            };
            showPopupButton.Width = 200;

            showPopupButton.Text = "Show Non-Modal Popup";
            showPopupButton.Click += (_, _) =>
            {
                ShowPopup("This is a non-modal popup", isModal: false);
                // create a popup here
            };
            stackPanel.AddChild(showPopupButton);

            var showModalPopupButton = new Button();
            showModalPopupButton.Width = 200;

            showModalPopupButton.Text = "Show Modal Popup";
            showModalPopupButton.Click += (_,_) =>
            {
                ShowPopup("This is a modal popup", isModal:true);
            };
            stackPanel.AddChild(showModalPopupButton);
        }

        private void CreateColumn3Ui()
        {
            var stackPanel = new StackPanel();
            stackPanel.Y = 40;
            stackPanel.X = 520;
            stackPanel.Spacing = 4;
            this.AddChild(stackPanel);

            var panelWithSplitter = new StackPanel();
            stackPanel.AddChild(panelWithSplitter);

            var button = new Button();
            button.Width = 200;
            button.Height = 200;
            panelWithSplitter.AddChild(button);
            button.Text = "Button above splitter";

            var splitter = new Splitter();
            panelWithSplitter.AddChild(splitter);
            splitter.Dock(Gum.Wireframe.Dock.FillHorizontally);
            splitter.Height = 5;

            var button2 = new Button();
            button2.Width = 200;
            button2.Height = 200;
            panelWithSplitter.AddChild(button2);
            button2.Text = "Button below splitter";


            var text = new TextRuntime();
            // Set this value before changing width-related properties.
            // This is a global value, so it is used by all TextRuntime
            // instances:
            RenderingLibrary.Graphics.Text.IsMidWordLineBreakEnabled = true;
            text.Width = 100;
            text.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            text.Text = "abcdefghijklmnopqrstuvwxyz";
            stackPanel.AddChild(text);
        }

        void CreateLayeredUi()
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
            layeredContainer.Y = 40;
            layeredContainer.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
            layeredContainer.AddToManagers(SystemManagers.Default, layer);
            layeredContainer.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
            layeredContainer.XOrigin = HorizontalAlignment.Right;
            layeredContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.AddChild(layeredContainer);

            var zoomInButton = new Button();
            zoomInButton.Text = "Zoom layer in";
            zoomInButton.Width = 0;
            zoomInButton.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            zoomInButton.Height = 100;
            zoomInButton.Click += (_,_) =>
            {
                layerCameraSettings.Zoom += 0.1f;
            };
            layeredContainer.AddChild(zoomInButton);


            var zoomOutButton = new Button();
            zoomOutButton.Text = "Zoom layer out";
            zoomOutButton.Width = 0;
            zoomOutButton.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            zoomOutButton.Height = 100;
            zoomOutButton.Click += (_, _) =>
            {
                layerCameraSettings.Zoom -= 0.1f;
            };
            layeredContainer.AddChild(zoomOutButton);
            //button.Visual.AddToManagers(SystemManagers.Default, null);
        }


        private void ShowPopup(string text, bool isModal)
        {
            var window = new Window();
            window.Anchor(Gum.Wireframe.Anchor.Center);

            if(isModal)
            {
                FrameworkElement.ModalRoot.AddChild(window);
            }
            else
            {
                FrameworkElement.PopupRoot.AddChild(window);
            }
            window.Width = 300;
            window.Height = 200;

            var textInstance = new Label();
            textInstance.Dock(Gum.Wireframe.Dock.Top);
            textInstance.Y = 24;

            textInstance.Text = text;
            window.AddChild(textInstance);

            var button = new Button();
            button.Anchor(Gum.Wireframe.Anchor.Bottom);
            button.Y = -10;
            button.Text = "Close";
            window.AddChild(button.Visual);
            button.Click += (_, _) =>
            {
                window.RemoveFromRoot();
            };
        }

        public void Update(GameTime gameTime)
        {
            var keyboard = FormsUtilities.Keyboard;

            if(keyboard.IsAltDown)
            {
                if(keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.F))
                {
                    FileMenuItem.IsSelected = true;
                }
                else if(keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.E))
                {
                    EditMenuItem.IsSelected = true;
                }
                else if(keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.C))
                {
                    CustomMenuItem.IsSelected = true;
                }
            }
        }
    }
}
