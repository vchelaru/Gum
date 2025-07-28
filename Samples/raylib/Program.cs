using Gum.Converters;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Gum.GueDeriving;
using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Renderables;
using Gum.Wireframe;
using Raylib_cs;
using RaylibGum;
using RaylibGum.Input;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Raylib_cs.Raylib;

namespace Examples.Shapes;




public class BasicShapes
{

    static Texture2D texture;

    static GumService GumUI => GumService.Default;


    public static void Main()
    {
        // Initialization
        //--------------------------------------------------------------------------------------
        const int screenWidth = 800;
        const int screenHeight = 450;

        // This tells Gum to use the entire screen
        GraphicalUiElement.CanvasWidth = screenWidth;
        GraphicalUiElement.CanvasHeight = screenHeight;

        
        InitWindow(screenWidth, screenHeight, "Basic shape and image drawing");

        //Load the image into the cpu
        Image image = LoadImage("resources/gum-logo-normal-64.png");

        //Transform it as a texture
        texture = LoadTextureFromImage(image);



        //Unload/release it
        UnloadImage(image);
        //--------------------------------------------------------------------------------------

        GumUI.Initialize();
        var standardTexture = SystemManagers.Default.LoadEmbeddedTexture2d("UISpriteSheet.png");

        InitializeStyling();
        

        //var texture2 = LoadTextureFromImage(imageFromFile);

        var container = new GraphicalUiElement(new InvisibleRenderable());
        container.AddToRoot();
        container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        container.WrapsChildren = true;
        container.StackSpacing = 2;
        container.Width = 0;
        container.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;

        container.Height = 0;
        container.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;

        container.ClipsChildren = true;

        //var sprite = new SpriteRuntime();
        //sprite.Texture = standardTexture.Value;
        //container.AddChild(sprite);

        var button = new Button();
        button.Width = 200;
        button.Text = "I'm a button";
        container.AddChild(button.Visual);

        var checkbox = new CheckBox();
        checkbox.Width = 200;
        checkbox.Text = "Check me";
        container.AddChild(checkbox.Visual);


        var slider = new Slider();
        container.AddChild(slider.Visual);

        slider.Minimum = 0;
        slider.Maximum = 30;
        slider.TicksFrequency = 1;
        slider.IsSnapToTickEnabled = true;
        slider.Width = 250;
        slider.ValueChanged += (_, _) =>
            Debug.WriteLine($"Value: {slider.Value}");
        slider.ValueChangeCompleted += (_, _) =>
            Debug.WriteLine($"Finished setting Value: {slider.Value}");

        var label = new Label();
        container.AddChild(label.Visual);
        label.Text = "This is a Gum label";

        var scrollViewer = new ScrollViewer();
        container.AddChild(scrollViewer.Visual);
        scrollViewer.Width = 200;
        scrollViewer.Height = 200;
        scrollViewer.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        scrollViewer.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        scrollViewer.InnerPanel.StackSpacing = 2;

        for (int i = 0; i < 30; i++)
        {
            var innerButton = new Button();
            scrollViewer.AddChild(innerButton);
            innerButton.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            innerButton.Width = 0;
            innerButton.Text = "Button " + i;
            innerButton.Click += (_, _) =>
                innerButton.Text = DateTime.Now.ToString();
        }



        var stackPanel = new StackPanel();
        container.AddChild(stackPanel.Visual);

        var easyRadioButton = new RadioButton();
        stackPanel.AddChild(easyRadioButton);
        easyRadioButton.Text = "Easy";

        var mediumRadioButton = new RadioButton();
        stackPanel.AddChild(mediumRadioButton);
        mediumRadioButton.Text = "Medium";

        var hardRadioButton = new RadioButton();
        stackPanel.AddChild(hardRadioButton);
        hardRadioButton.Text = "Hard";

        var listBox = new ListBox();
        container.AddChild(listBox.Visual);
        listBox.Width = 300;
        listBox.Height = 200;

        var addButton = new Button();
        container.AddChild(addButton.Visual);
        addButton.Text = "Add to ListBox";
        addButton.Click += (s, e) =>
        {
            var newItem = $"Item {listBox.Items.Count} @ {DateTime.Now}";
            listBox.Items.Add(newItem);
            listBox.ScrollIntoView(newItem);
        };

        var comboBox = new ComboBox();
        container.AddChild(comboBox.Visual);
        for (int i = 0; i < 10; i++)
        {
            comboBox.Items.Add($"Item {i}");
        }


        var window = new Window();
        window.Anchor(Gum.Wireframe.Anchor.Center);
        window.Width = 300;
        window.Height = 200;
        window.AddToRoot();

        var textInstance = new Label();
        textInstance.Dock(Gum.Wireframe.Dock.Top);
        textInstance.Y = 24;
        textInstance.Text = "Hello I am a message box";
        window.AddChild(textInstance);

        var windowButton = new Button();
        windowButton.Anchor(Gum.Wireframe.Anchor.Bottom);
        windowButton.Y = -10;
        windowButton.Text = "Close";
        window.AddChild(windowButton.Visual);
        windowButton.Click += (_, _) =>
        {
            window.RemoveFromRoot();
        };


        var splitterStackPanel = new StackPanel();
        container.AddChild(splitterStackPanel);
        splitterStackPanel.Spacing = 1;

        var listBox1 = new ListBox();
        splitterStackPanel.AddChild(listBox1);
        for (int i = 0; i < 10; i++)
        {
            listBox1.Items.Add("List Item " + i);
        }

        var splitter = new Splitter();
        splitterStackPanel.AddChild(splitter);
        splitter.Dock(Dock.FillHorizontally);
        splitter.Height = 5;

        var listBox2 = new ListBox();
        splitterStackPanel.AddChild(listBox2);
        for (int i = 0; i < 10; i++)
        {
            listBox2.Items.Add("List Item " + i);
        }

        // Main game loop
        while (!WindowShouldClose())
        {
            // Update
            //----------------------------------------------------------------------------------
            // TODO: Update your variables here
            //----------------------------------------------------------------------------------

            // Draw
            //----------------------------------------------------------------------------------
            BeginDrawing();
            ClearBackground(Color.SkyBlue);

            /* Raylib supports drawing simple 2d shapes with internal functions 
            so uncomment the following lines to see it in action */

            // DrawLine(18, 42, screenWidth - 18, 42, Color.Black);

            // DrawCircle(screenWidth / 4, 120, 35, Color.DarkBlue);
            // DrawRectangle(screenWidth / 4 * 2 - 60, 100, 120, 60, Color.Red);
            // DrawTriangle(
            //     new Vector2(screenWidth / 4 * 3, 80),
            //     new Vector2(screenWidth / 4 * 3 - 60, 150),
            //     new Vector2(screenWidth / 4 * 3 + 60, 150), Color.Violet
            // );


            GumUI.Update(0);

            GumUI.Draw();


                EndDrawing();
            //----------------------------------------------------------------------------------
        }

        // De-Initialization
        //--------------------------------------------------------------------------------------
        CloseWindow();
        //--------------------------------------------------------------------------------------

    }

    private static void InitializeStyling()
    {
        var font = LoadFontEx("resources/04B_30_.TTF", 24, null, 0);
        Styling.ActiveStyle.Text.Normal.SetValue("Font", font);
        Styling.ActiveStyle.Text.Strong.SetValue("Font", font);
        Styling.ActiveStyle.Text.Emphasis.SetValue("Font", font);
    }
}