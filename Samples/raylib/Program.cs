using Gum.Forms.Controls;
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

        //for(int i = 0; i < 10; i++)
        //{
        //    var nineSlice = new NineSliceRuntime();
        //    nineSlice.Texture = standardTexture.Value;
        //    nineSlice.TextureLeft = 24;
        //    nineSlice.TextureTop = 3 * 16;
        //    nineSlice.TextureWidth = 24;
        //    nineSlice.TextureHeight = 24;
        //    nineSlice.TextureAddress = Gum.Managers.TextureAddress.Custom;
        //    nineSlice.Width = 15 + 15*i;
        //    nineSlice.Height = 15 + 15 * i;
        //    container.AddChild(nineSlice);

        //}

        // let's set a top to bottom stack
        //for (int i = 0; i < 3; i++)
        //{
        //    //var sprite = new Sprite();
        //    //sprite.Texture = texture;
        //    //var child = new GraphicalUiElement(sprite);

        //    var child = new SpriteRuntime();
        //    child.Texture = texture;
        //    child.Height = 100;
        //    child.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        //    child.Width = 100;
        //    child.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        //    container.Children.Add(child);
        //}

        //for (int i = 0; i < 2; i++)
        //{
        //    var partialSprite = new SpriteRuntime();
        //    partialSprite.Name = "Partial sprite";
        //    partialSprite.Texture = texture;
        //    partialSprite.Height = 300;
        //    partialSprite.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        //    partialSprite.Width = 300;
        //    partialSprite.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        //    partialSprite.TextureLeft = 0;
        //    partialSprite.TextureWidth = 32;
        //    partialSprite.TextureTop = 0;
        //    partialSprite.TextureHeight = 16;
        //    partialSprite.TextureAddress = Gum.Managers.TextureAddress.Custom;
        //    container.Children.Add(partialSprite);

        //}

        //for (int i = 0; i < 5; i++)

        //{
        //    var text = new Text();
        //    text.RawText = "Hello World";
        //    var child = new GraphicalUiElement(text);
        //    child.Height = 0;
        //    child.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        //    child.Width = 0;
        //    child.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        //    child.FontSize = 12 + i * 4;

        //    container.Children.Add(child);
        //}

        for (int i = 0; i < 3; i++)
        {
            var rectangle = new SolidRectangle();
            rectangle.Color = Color.Green;

            var child = new InteractiveGue(rectangle);
            child.Name = "Rectangle " + i;
            child.Height = 30;
            child.Width = 60;
            container.AddChild(child);

            child.Click += (_,_) =>
            {
                child.X += 10;
            };
        }

        //var buttonBackground = new ColoredRectangleRuntime();
        //buttonBackground.Color = Color.Blue;
        //buttonBackground.Dock(Dock.Fill);
        //var buttonVisual = new ContainerRuntime();
        //buttonVisual.AddChild(buttonBackground);

        //var button = new Button(buttonVisual);
        //button.Click += (_,_) =>
        //{
        //   // This will be called when the button is clicked
        //    Console.WriteLine("Button clicked!");
        //};
        //container.AddChild(buttonVisual);

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
            ClearBackground(Color.Gray);

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

}