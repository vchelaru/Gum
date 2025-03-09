using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;
using static Raylib_cs.Raylib;

namespace Examples.Shapes;


public class RaylibSprite : InvisibleRenderable
{ }

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

        // 
        Root.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        Root.Width = 0;
        Root.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        Root.Height = 0;

        var container = new GraphicalUiElement(new InvisibleRenderable());
        Root.Children.Add(container);
        container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        // let's set a top to bottom stack
        for(int i = 0; i < 6; i++)
        {
            var child = new GraphicalUiElement(new RaylibSprite());
            child.Height = 50;
            child.Width = 50;
            container.Children.Add(child);
        }

        
        InitWindow(screenWidth, screenHeight, "Basic shape and image drawing");

        //Load the image into the cpu
        Image image = LoadImage("resources/gum-logo-normal-64.png");

        //Transform it as a texture
        texture = LoadTextureFromImage(image);

        //Unload/release it
        UnloadImage(image);
        //--------------------------------------------------------------------------------------

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

            // DrawText("some basic shapes available on raylib", 20, 20, 20, Color.DarkGray);

            // DrawLine(18, 42, screenWidth - 18, 42, Color.Black);

            // DrawCircle(screenWidth / 4, 120, 35, Color.DarkBlue);
            // DrawRectangle(screenWidth / 4 * 2 - 60, 100, 120, 60, Color.Red);
            // DrawTriangle(
            //     new Vector2(screenWidth / 4 * 3, 80),
            //     new Vector2(screenWidth / 4 * 3 - 60, 150),
            //     new Vector2(screenWidth / 4 * 3 + 60, 150), Color.Violet
            // );
            Root.UpdateLayout();
            DrawGumRecursively(Root);




            EndDrawing();
            //----------------------------------------------------------------------------------
        }

        // De-Initialization
        //--------------------------------------------------------------------------------------
        CloseWindow();
        //--------------------------------------------------------------------------------------

    }

    private static void DrawGumRecursively(GraphicalUiElement element)
    {
        var shouldDrawSelf = element.RenderableComponent is RaylibSprite;

        if(shouldDrawSelf)
        {
            int x = (int)element.AbsoluteLeft;
            int y = (int)element.AbsoluteTop;

            //This is where we draw the texture
            DrawTexture(texture, x, y, Color.White);
        }

        if(element.Children != null)
        {
            foreach(var child in element.Children)
            {
                if(child is GraphicalUiElement childGue)
                {
                    DrawGumRecursively(childGue);
                }
            }
        }

    }
}