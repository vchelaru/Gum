using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using Raylib_cs;
using RaylibGum;
using RenderingLibrary;
using static Raylib_cs.Raylib;

namespace Examples.Shapes;

public class BasicShapes
{
    static Texture2D texture;

    static GumService GumUI => GumService.Default;

    static RawVisualsScreen rawVisualsScreen;
    static FormsControlsScreen formsControlsScreen;
    static FrameworkElement? activeScreen;

    public static void Main()
    {
        const int screenWidth = 1280;
        const int screenHeight = 720;

        GumUI.CanvasWidth = screenWidth;
        GumUI.CanvasHeight = screenHeight;

        InitWindow(screenWidth, screenHeight, "Basic shape and image drawing");

        GumUI.Initialize();
        var standardTexture = SystemManagers.Default.LoadEmbeddedTexture2d("UISpriteSheet.png");

        InitializeStyling();

        rawVisualsScreen = new RawVisualsScreen();
        formsControlsScreen = new FormsControlsScreen();

        ShowScreen(rawVisualsScreen);

        while (!WindowShouldClose())
        {
            if (IsKeyPressed(KeyboardKey.Space))
            {
                ShowScreen(activeScreen == rawVisualsScreen ? formsControlsScreen : rawVisualsScreen);
            }

            BeginDrawing();
            ClearBackground(Color.SkyBlue);

            GumUI.Update(GetTime());
            GumUI.Draw();

            EndDrawing();

            Thread.Sleep(12);
        }

        CloseWindow();
    }

    private static void ShowScreen(FrameworkElement screen)
    {
        if (activeScreen != null)
        {
            activeScreen.RemoveFromRoot();
        }
        activeScreen = screen;
        activeScreen.AddToRoot();
    }

    private static void InitializeStyling()
    {
        var font = LoadFontEx("resources/04B_30_.TTF", 24, null, 0);
        Styling.ActiveStyle.Text.Normal.SetValue("Font", font);
        Styling.ActiveStyle.Text.Strong.SetValue("Font", font);
        Styling.ActiveStyle.Text.Emphasis.SetValue("Font", font);
    }
}
