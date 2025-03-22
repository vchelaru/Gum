using Gum.Wireframe;
using GumTest.Renderables;
using Raylib_cs;
using RaylibGum;
using RaylibGum.Renderables;
using RenderingLibrary.Graphics;
using System.Runtime.CompilerServices;
using static Raylib_cs.Raylib;

namespace Examples.Shapes;




public class BasicShapes
{

    static Texture2D texture;

    public static void Main()
    {
        // Initialization
        //--------------------------------------------------------------------------------------
        const int screenWidth = 800;
        const int screenHeight = 450;

        // This tells Gum to use the entire screen
        GraphicalUiElement.CanvasWidth = screenWidth;
        GraphicalUiElement.CanvasHeight = screenHeight;

        // Our root contains everything:
        GraphicalUiElement Root = new GraphicalUiElement(new InvisibleRenderable());



        
        InitWindow(screenWidth, screenHeight, "Basic shape and image drawing");

        //Load the image into the cpu
        Image image = LoadImage("resources/gum-logo-normal-64.png");

        //Transform it as a texture
        texture = LoadTextureFromImage(image);

        //Unload/release it
        UnloadImage(image);
        //--------------------------------------------------------------------------------------

        GumService.Default.Initialize();
        // 


        var container = new GraphicalUiElement(new InvisibleRenderable());
        container.AddToRoot();
        container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        container.StackSpacing = 2;
        container.Width = 100;
        container.Height = 0;
        container.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        container.ClipsChildren = true;
        container.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;

        // let's set a top to bottom stack
        for (int i = 0; i < 3; i++)
        {
            var sprite = new Sprite();
            sprite.Texture = texture;
            var child = new GraphicalUiElement(sprite);
            child.Height = 100;
            child.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            child.Width = 100;
            child.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            container.Children.Add(child);
        }

        for(int i = 0; i < 5; i++)

        {
            var text = new Text();
            text.RawText = "Hello World";
            var child = new GraphicalUiElement(text);
            child.Height = 0;
            child.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            child.Width = 0;
            child.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            child.FontSize = 12 + i * 4;

            container.Children.Add(child);
        }

        for(int i = 0; i < 3; i++)
        {
            var rectangle = new SolidRectangle();
            rectangle.Color = Color.Green;

            var child = new InteractiveGue(rectangle);
            child.Name = "Rectangle " + i;
            child.Height = 30;
            child.Width = 60;
            container.Children.Add(child);

            child.Click += (_,_) =>
            {
                child.X += 10;
            };
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
            ClearBackground(Color.RayWhite);

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


            GumService.Default.Update(0);

            GumService.Default.Draw();

            EndDrawing();
            //----------------------------------------------------------------------------------
        }

        // De-Initialization
        //--------------------------------------------------------------------------------------
        CloseWindow();
        //--------------------------------------------------------------------------------------

    }

}