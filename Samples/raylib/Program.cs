using Gum.Converters;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using Gum;
using KernSmith.Gum;
using Raylib_cs;
using RaylibGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Numerics;
using System.Threading;
using static Raylib_cs.Raylib;

namespace Examples.Shapes;

public class BasicShapes
{
    static GumService GumUI => GumService.Default;

    static StackPanel? navStrip;
    static FrameworkElement? activeScreen;

    // Set when a screen is shown so its initial gamepad focus is applied after the next
    // GumUI.Update — ShowScreen runs inside a nav button's Click handler (during Update), and
    // applying focus there gets cleared by the rest of that frame's input processing.
    static bool _applyInitialFocusAfterUpdate;

    // Manual-camera demo state. When _useManualCamera is true the render loop calls
    // GumUI.Draw(_manualCamera) instead of GumUI.Draw(), exercising the new
    // Draw(Camera2D) overload (#2846). Arrow keys pan; mouse wheel zooms.
    static bool _useManualCamera;
    static Camera2D _manualCamera = new Camera2D
    {
        Target = Vector2.Zero,
        Offset = Vector2.Zero,
        Rotation = 0,
        Zoom = 1f,
    };

    public static void Main()
    {
        // Sized to match the MonoGameGumInCode feature sample (1024x768) so the two galleries
        // render at the same scale side by side. The window is resizable, so screens whose
        // content exceeds 768 (e.g. Polygons' bottom row) can still be seen by dragging it larger.
        const int screenWidth = 1024;
        const int screenHeight = 768;

        GumUI.CanvasWidth = screenWidth;
        GumUI.CanvasHeight = screenHeight;

        // 4x MSAA enables framebuffer-level antialiasing — raylib has no per-shape AA, so
        // this is the only path to smooth circle/ring edges. Must be set BEFORE InitWindow
        // (raylib only consults config flags at GL context creation).
        SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        InitWindow(screenWidth, screenHeight, "Gum raylib gallery");

        GumUI.Initialize();

        CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

        // Issue #3465: Gum core ships no shader loader, so the app registers a resolver that turns a
        // ContainerRuntime.SourceShaderFile (.fs path) into a Raylib_cs.Shader. raylib loads GLSL
        // directly, so this is a one-liner — no runtime compiler needed (unlike the MonoGame side's
        // ShadowDusk). With no resolver registered, SourceShaderFile is a graceful no-op.
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path => LoadShader(null, path);

        // Enable gamepad + keyboard navigation for Forms controls (see
        // https://docs.flatredball.com/gum/code/events-and-interactivity/gamepad-support).
        // GumUI.Update reads the connected controller into these gamepads each frame; the
        // D-pad / left stick then move focus between controls and A activates the focused
        // control. UseKeyboardDefaults registers the keyboard so Tab / Shift+Tab navigate the
        // same way. The starting control is given focus in FormsControlsScreen's constructor.
        FrameworkElement.GamePadsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.AddRange(GumUI.Gamepads);

        GumUI.UseKeyboardDefaults();

        // Demo the auto-fit helpers — flip via the Zoom/Expand radio buttons in the nav strip.
        GumUI.EnableZoomToWindow();
        var standardTexture = SystemManagers.Default.LoadEmbeddedTexture2d("UISpriteSheet.png");

        InitializeStyling();
        BuildNavStrip();
        ShowScreen(() => new RawVisualsScreen());

        while (!WindowShouldClose())
        {
            GumUI.Update(GetTime());

            // Apply a freshly-shown screen's initial gamepad focus now that this frame's
            // input (including the nav-button click that swapped screens) has been processed.
            if (_applyInitialFocusAfterUpdate)
            {
                _applyInitialFocusAfterUpdate = false;
                (activeScreen as FormsControlsScreen)?.FocusInitialControl();
            }

            if (_useManualCamera)
            {
                UpdateManualCameraFromInput();
            }

            BeginDrawing();
            // Matches SilkNetGum's clear color so visual diffs across galleries
            // stay attributable to the shape code, not the page background.
            ClearBackground(new Color(51, 76, 204, 255));

            if (_useManualCamera)
            {
                GumUI.Draw(_manualCamera);
            }
            else
            {
                GumUI.Draw();
            }

            EndDrawing();

            Thread.Sleep(12);
        }

        CloseWindow();
    }

    // Mirrors MonoGameGumInCode/Game1.BuildNavStrip — horizontal Forms Button strip
    // across the top swaps the active screen at runtime. Replaces the prior Space-key toggle
    // so adding a screen (e.g. CirclesScreen for #2757) is a one-line registration.
    private static void BuildNavStrip()
    {
        navStrip = new StackPanel();
        navStrip.Orientation = Orientation.Horizontal;
        navStrip.Spacing = 4;
        navStrip.Visual.X = 4;
        navStrip.Visual.Y = 4;
        // Fill the window width and wrap the button row instead of letting buttons bleed off the
        // right edge; height grows to fit however many rows the wrap produces. Mirrors
        // MonoGameGumInCode/Game1.BuildNavStrip.
        navStrip.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        navStrip.Width = 0;
        navStrip.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        navStrip.Height = 0;
        navStrip.Visual.WrapsChildren = true;
        navStrip.AddToRoot();

        AddNavButton("Raw visuals", () => new RawVisualsScreen());
        AddNavButton("Forms controls", () => new FormsControlsScreen());
        AddNavButton("Circles", () => new CirclesScreen());
        AddNavButton("Rectangles", () => new RectanglesScreen());
        AddNavButton("Polygons", () => new PolygonsScreen());
        AddNavButton("Arcs", () => new ArcsScreen());
        AddNavButton("Sprite", () => new SpriteScreen());
        AddNavButton("NineSlice", () => new NineSliceScreen());
        AddNavButton("Text", () => new TextScreen());
        AddNavButton("Render Target", () => new RenderTargetScreen());
        AddNavButton("RT Shader", () => new RenderTargetShaderScreen());

        AddFitModeRadio("Zoom", isChecked: true, () =>
        {
            _useManualCamera = false;
            GumUI.EnableZoomToWindow();
        });
        AddFitModeRadio("Expand", isChecked: false, () =>
        {
            _useManualCamera = false;
            GumUI.EnableExpandToWindow();
        });
        AddFitModeRadio("Manual camera", isChecked: false, () =>
        {
            // Enabling manual camera mode swaps the render loop to
            // GumUI.Draw(_manualCamera) — see UpdateManualCameraFromInput for the
            // arrow-key pan / mouse-wheel zoom controls.
            _useManualCamera = true;
        });
    }

    // Reads keyboard/mouse input and mutates _manualCamera. Pan with arrows, zoom with
    // the mouse wheel. Kept in pixel-per-second / zoom-per-step terms (not per-frame) so
    // the feel is reasonable regardless of the loop's 12 ms sleep.
    private static void UpdateManualCameraFromInput()
    {
        float dt = GetFrameTime();
        float panSpeed = 400f / Math.Max(_manualCamera.Zoom, 0.01f);

        if (IsKeyDown(KeyboardKey.Left))
        {
            _manualCamera.Target.X -= panSpeed * dt;
        }
        if (IsKeyDown(KeyboardKey.Right))
        {
            _manualCamera.Target.X += panSpeed * dt;
        }
        if (IsKeyDown(KeyboardKey.Up))
        {
            _manualCamera.Target.Y -= panSpeed * dt;
        }
        if (IsKeyDown(KeyboardKey.Down))
        {
            _manualCamera.Target.Y += panSpeed * dt;
        }

        float wheel = GetMouseWheelMove();
        if (wheel != 0)
        {
            _manualCamera.Zoom = Math.Clamp(_manualCamera.Zoom * (1 + wheel * 0.1f), 0.1f, 10f);
        }
    }

    private static void AddFitModeRadio(string text, bool isChecked, Action onChecked)
    {
        RadioButton radio = new RadioButton();
        radio.GroupName = "FitMode";
        radio.Text = text;
        radio.IsChecked = isChecked;
        radio.Checked += (_, _) => onChecked();
        navStrip!.AddChild(radio);
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
        // MonoGameGumInCode/Game1.ShowScreen. Use the nav strip's actual laid-out height so a
        // wrapped (multi-row) button strip still clears the screen content below it.
        float navStripHeight = navStrip!.Visual.GetAbsoluteHeight();
        activeScreen.Visual.YOrigin = VerticalAlignment.Top;
        activeScreen.Visual.YUnits = GeneralUnitType.PixelsFromSmall;
        activeScreen.Visual.Y = navStripHeight;
        activeScreen.Visual.Height = -navStripHeight;
        activeScreen.AddToRoot();

        // Defer initial gamepad focus to just after the next GumUI.Update (see field comment).
        _applyInitialFocusAfterUpdate = true;
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
