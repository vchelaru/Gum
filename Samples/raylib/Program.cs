using Gum.Converters;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using Raylib_cs;
using RaylibGum;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Threading;
using static Raylib_cs.Raylib;

namespace Examples.Shapes;

public class BasicShapes
{
    private const float NavStripHeight = 40;

    static GumService GumUI => GumService.Default;

    static StackPanel? navStrip;
    static FrameworkElement? activeScreen;

    public static void Main()
    {
        const int screenWidth = 1280;
        const int screenHeight = 720;

        GumUI.CanvasWidth = screenWidth;
        GumUI.CanvasHeight = screenHeight;

        // 4x MSAA enables framebuffer-level antialiasing — raylib has no per-shape AA, so
        // this is the only path to smooth circle/ring edges. Must be set BEFORE InitWindow
        // (raylib only consults config flags at GL context creation).
        SetConfigFlags(ConfigFlags.Msaa4xHint);
        InitWindow(screenWidth, screenHeight, "Gum raylib gallery");

        GumUI.Initialize();
        var standardTexture = SystemManagers.Default.LoadEmbeddedTexture2d("UISpriteSheet.png");

        InitializeStyling();
        BuildNavStrip();
        ShowScreen(() => new RawVisualsScreen());

        while (!WindowShouldClose())
        {
            BeginDrawing();
            // Matches MonoGameGumShapesGallery's clear color so visual diffs across galleries
            // stay attributable to the shape code, not the page background.
            ClearBackground(new Color(51, 76, 204, 255));

            GumUI.Update(GetTime());
            GumUI.Draw();

            EndDrawing();

            Thread.Sleep(12);
        }

        CloseWindow();
    }

    // Mirrors MonoGameGumShapesGallery/Game1.BuildNavStrip — horizontal Forms Button strip
    // across the top swaps the active screen at runtime. Replaces the prior Space-key toggle
    // so adding a screen (e.g. CirclesScreen for #2757) is a one-line registration.
    private static void BuildNavStrip()
    {
        navStrip = new StackPanel();
        navStrip.Orientation = Orientation.Horizontal;
        navStrip.Spacing = 4;
        navStrip.Visual.X = 4;
        navStrip.Visual.Y = 4;
        navStrip.AddToRoot();

        AddNavButton("Raw visuals", () => new RawVisualsScreen());
        AddNavButton("Forms controls", () => new FormsControlsScreen());
        AddNavButton("Circles", () => new CirclesScreen());
    }

    private static void AddNavButton(string text, Func<FrameworkElement> factory)
    {
        Button button = new Button();
        button.Text = text;
        button.Click += (_, _) => ShowScreen(factory);
        navStrip!.AddChild(button);
    }

    private static void ShowScreen(Func<FrameworkElement> factory)
    {
        if (activeScreen != null)
        {
            activeScreen.RemoveFromRoot();
        }

        activeScreen = factory();
        // Offset the screen so it doesn't sit underneath the nav strip — same trick as
        // MonoGameGumShapesGallery/Game1.ShowScreen.
        activeScreen.Visual.YOrigin = VerticalAlignment.Top;
        activeScreen.Visual.YUnits = GeneralUnitType.PixelsFromSmall;
        activeScreen.Visual.Y = NavStripHeight;
        activeScreen.Visual.Height = -NavStripHeight;
        activeScreen.AddToRoot();
    }

    private static void InitializeStyling()
    {
        // Arial 18 across the board — the prior 04B_30_ pixel bubble font at 24 px made nav
        // buttons too tall and looked out of place next to the shape gallery. raylib's
        // LoadFontEx pulls glyph atlases from any TTF/OTF; Arial ships with Windows. If
        // missing (non-Windows / locked-down boxes), fall back to the bundled pixel font so
        // the sample still runs.
        const int fontSize = 18;
        Font font = LoadFontEx(@"C:\Windows\Fonts\arial.ttf", fontSize, null, 0);
        if (font.BaseSize == 0)
        {
            font = LoadFontEx("resources/04B_30_.TTF", fontSize, null, 0);
        }

        // Drives Forms text (buttons, labels, etc.).
        Styling.ActiveStyle.Text.Normal.SetValue("Font", font);
        Styling.ActiveStyle.Text.Strong.SetValue("Font", font);
        Styling.ActiveStyle.Text.Emphasis.SetValue("Font", font);

        // Drives non-Forms TextRuntime (e.g. raw section headers in CirclesScreen) — without
        // this they fall back to raylib's GetFontDefault, which is the chunky pixel font.
        Gum.Renderables.Text.DefaultFont = font;
    }
}
