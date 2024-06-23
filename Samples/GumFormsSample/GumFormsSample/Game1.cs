using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using MonoGameGum.Input;
using RenderingLibrary;
using System.Diagnostics;

namespace GumFormsSample
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        ContainerRuntime Root;
        Cursor cursor;
        MonoGameGum.Input.Keyboard keyboard;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            SystemManagers.Default = new SystemManagers(); 
            SystemManagers.Default.Initialize(_graphics.GraphicsDevice, fullInstantiation: true);
            cursor = new Cursor();
            keyboard = new MonoGameGum.Input.Keyboard();

            FrameworkElement.DefaultFormsComponents[typeof(Button)] = typeof(DefaultButtonRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(CheckBox)] = typeof(DefaultCheckboxRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(ListBox)] = typeof(DefaultListBoxRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(ListBoxItem)] = typeof(DefaultListBoxItemRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(ScrollBar)] = typeof(DefaultScrollBarRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(ScrollViewer)] = typeof(DefaultScrollViewerRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(TextBox)] = typeof(DefaultTextBoxRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(PasswordBox)] = typeof(DefaultTextBoxRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(Slider)] = typeof(DefaultSliderRuntime);
            FrameworkElement.MainCursor = cursor;

            Root = new ContainerRuntime();
            Root.Width = 0;
            Root.Height = 0;
            Root.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            Root.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            Root.AddToManagers();


            var button = new Button();
            Root.Children.Add(button.Visual);
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

            var checkbox = new CheckBox();
            Root.Children.Add(checkbox.Visual);
            checkbox.X = 0;
            checkbox.Y = 50;
            checkbox.Text = "Checkbox";

            var scrollBar = new ScrollBar();
            this.Root.Children.Add(scrollBar.Visual);
            scrollBar.Width = 24;
            scrollBar.Height = 200;
            scrollBar.X = 200;
            scrollBar.Minimum = 0;
            scrollBar.Maximum = 150;
            scrollBar.ViewportSize = 50;

            var scrollViewer = new ScrollViewer();
            this.Root.Children.Add(scrollViewer.Visual);
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
                scrollViewer.InnerPanel.Children.Add(innerButton.Visual);


            }

            // We can also create buttons through the creation of the default controls:
            var buttonRuntime = new DefaultButtonRuntime();
            Root.Children.Add(buttonRuntime);
            buttonRuntime.X = 0;
            buttonRuntime.Y = 100;
            buttonRuntime.Width = 100;
            buttonRuntime.Height = 50;
            buttonRuntime.TextInstance.Text = "Other Button!";
            var formsButton = buttonRuntime.FormsControl;
            formsButton.Click += (_, _) =>
            {
                clickCount++;
                formsButton.Text = $"Clicked {clickCount} times";
            };

            var listBox = new ListBox();
            this.Root.Children.Add(listBox.Visual);
            listBox.X = 0;
            listBox.Y = 200;
            listBox.Width = 200;
            listBox.Height = 200;
            for (int i = 0; i < 20; i++)
            {
                listBox.Items.Add($"Item {i}");
            }

            //var viewer = new DefaultScrollViewerRuntime(true, false);
            //this.Root.Children.Add(viewer);
            //viewer.X = 300;
            //viewer.Y = 0;
            //viewer.Width = 200;
            //viewer.Height = 200;

            var textBox = new TextBox();
            this.Root.Children.Add(textBox.Visual);
            textBox.X = 220;
            textBox.Y = 220;
            textBox.Width = 200;
            textBox.Height = 34;
            textBox.Placeholder = "Placeholder Text...";

            var textBox2 = new TextBox();
            this.Root.Children.Add(textBox2.Visual);
            textBox2.X = 220;
            textBox2.Y = 260;
            textBox2.Width = 200;
            textBox2.Height = 34;
            textBox2.Placeholder = "Placeholder Text...";

            var passwordBox = new PasswordBox();
            this.Root.Children.Add(passwordBox.Visual);
            passwordBox.X = 220;
            passwordBox.Y = 300;
            passwordBox.Width = 200;
            passwordBox.Height = 34;
            passwordBox.Placeholder = "Enter Password";

            var slider = new Slider();
            this.Root.Children.Add(slider.Visual);
            slider.X = 220;
            slider.Y = 340;
            slider.Minimum = 0;
            slider.Maximum = 10;
            slider.TicksFrequency = 1;
            slider.IsSnapToTickEnabled = true;
            slider.Width = 200;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Microsoft.Xna.Framework.Input.Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            cursor.Activity(gameTime.TotalGameTime.TotalSeconds);
            keyboard.Activity(gameTime.TotalGameTime.TotalSeconds);
            Root.DoUiActivityRecursively(cursor, keyboard, gameTime.TotalGameTime.TotalSeconds);

            SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);
            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            SystemManagers.Default.Draw();
            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
