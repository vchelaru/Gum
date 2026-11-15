using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
using Gum;
using GumSamples;
using KernSmith.Gum;
using Raylib_cs;
using RaylibGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using System;
using System.Numerics;
using System.Threading;
using static Raylib_cs.Raylib;

namespace Examples.Shapes;

public class BasicShapes
{
    static GumService GumUI => GumService.Default;

    static readonly GumGallery gallery = new GumGallery();

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

    // Diagnostic screenshot mode, like SilkNetGum's: when GUM_SCREENSHOT_PATH is set, the window opens
    // without taking focus (a hidden window crashes raylib on macOS), the screen named by GUM_SCREENSHOT_SCREEN (a nav button label, default "RT Shader") is
    // shown, a few frames are drawn so layout settles, a PNG is saved and the sample exits.
    static readonly string? screenshotPath = Environment.GetEnvironmentVariable("GUM_SCREENSHOT_PATH");
    static readonly string screenshotScreenName = Environment.GetEnvironmentVariable("GUM_SCREENSHOT_SCREEN") ?? "RT Shader";

    public static void Main()
    {
        // Screens load their files (resources/...) relative to the working directory. Visual Studio
        // starts the sample in its output folder, but `dotnet run` keeps the folder it was run from.
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        // Width matches the MonoGameGum feature sample; height is tall enough to show each shape
        // screen's content on Windows. On macOS the window must also fit under the menu bar, title bar
        // and Dock: macOS shrinks a taller window, but raylib keeps drawing for the size it asked for
        // until the window is resized, so the top of the frame (the nav buttons) is cut off. The
        // window is resizable if a screen needs more room.
        const int screenWidth = 1400;
        int screenHeight = OperatingSystem.IsMacOS() ? 780 : 900;

        // 4x MSAA enables framebuffer-level antialiasing — raylib has no per-shape AA, so
        // this is the only path to smooth circle/ring edges. Must be set BEFORE InitWindow
        // (raylib only consults config flags at GL context creation).
        SetConfigFlags(screenshotPath != null
            ? ConfigFlags.UnfocusedWindow
            : ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        InitWindow(screenWidth, screenHeight, "Gum raylib gallery");

        GumUI.Initialize();

        GumUI.UseKeyboardDefaults();

        GumUI.UseGamepadDefaults();

        GumUI.UseKernSmith();

        // Issue #3465: Gum core ships no shader loader, so the app registers a resolver that turns a
        // ContainerRuntime.SourceShaderFile (.fs path) into a Raylib_cs.Shader. raylib loads GLSL
        // directly, so this is a one-liner with no runtime compiler. Wrapped in a lambda (not a bare
        // method group) because Shader is a struct: the target-typed lambda body boxes it to the
        // resolver's object? return, which a method-group conversion can't.
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver =
            path => RenderTargetShaderScreen.CompileEffectFromFile(path);

        // The Gum.raylib.ShadowDusk package adds .fx and .slang files on top of that (#7053) and
        // registers the defaults they declare. Other extensions, like .fs, still go to the resolver above.
        GumUI.UseShadowDusk();

        // Demo the auto-fit helpers — flip via the Zoom/Expand radio buttons in the nav strip.
        GumUI.EnableZoomToWindow();

        InitializeStyling();
        gallery.Build("Forms");
        AddManualCameraRadio();

        if (screenshotPath != null)
        {
            CaptureScreenshotAndExit(screenshotPath);
            return;
        }

        while (!WindowShouldClose())
        {
            GumUI.Update(GetTime());

            gallery.Tick(GetFrameTime());

            if (_useManualCamera)
            {
                UpdateManualCameraFromInput();
            }

            BeginDrawing();
            // Matches SilkNetGum's clear color so visual diffs across galleries
            // stay attributable to the shape code, not the page background.
            ClearBackground(new Color(100, 149, 237, 255));

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

    // The gallery adds the Zoom and Expand radios; this adds the raylib-only manual camera, which swaps the
    // render loop to GumUI.Draw(_manualCamera). Zoom and Expand turn it back off.
    private static void AddManualCameraRadio()
    {
        gallery.FitModeChanging += () => _useManualCamera = false;
        gallery.AddFitModeRadio("Manual camera", isChecked: false, () => _useManualCamera = true);
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

    private static void CaptureScreenshotAndExit(string path)
    {
        gallery.TryShowCodeScreen(screenshotScreenName);

        for (int frame = 0; frame < 15; frame++)
        {
            GumUI.Update(GetTime());
            BeginDrawing();
            ClearBackground(new Color(100, 149, 237, 255));
            GumUI.Draw();
            EndDrawing();
        }

        Raylib_cs.Image image = LoadImageFromScreen();
        ExportImage(image, path);
        UnloadImage(image);
        CloseWindow();
    }

    private static void InitializeStyling()
    {
        // Arial 18 across the board — the prior 04B_30_ pixel bubble font at 24 px made nav
        // buttons too tall and looked out of place next to the shape gallery. raylib's
        // TtfFontLoader pulls glyph atlases from any TTF/OTF; Arial ships with Windows. If
        // missing (non-Windows / locked-down boxes), fall back to the bundled pixel font so
        // the sample still runs.
        const int fontSize = 18;
        string fontFilePath = @"C:\Windows\Fonts\arial.ttf";
        Font font = TtfFontLoader.LoadFont(fontFilePath, fontSize);
        if (font.BaseSize == 0)
        {
            fontFilePath = "resources/04B_30_.TTF";
            font = TtfFontLoader.LoadFont(fontFilePath, fontSize);
        }

        // Drives Forms text (buttons, labels, etc.) via CustomFontFile+FontSize, NOT a raw Font
        // struct (issue #4365, manual-test finding). RegenerateOversampledFont pins whatever
        // TextRuntime.Font/FontSize currently resolves to as the "native" reference and compares its
        // measured width against a font freshly built by the same IRaylibFontCreator pipeline to
        // derive the oversample compensation ratio. Assigning the struct directly (the old
        // SetValue("Font", font) here) bypassed that pipeline, so the "native" reference was a
        // DIFFERENT font asset than the one the creator generates for oversampling -- the resulting
        // compensation ratio came out wrong and every Forms control's text visibly shrank once
        // oversampling engaged. Routing through CustomFontFile ensures both are generated by the same
        // creator, so they stay proportional (this is also why the sample's own non-Forms preview
        // TextRuntimes, which already set Font/FontSize directly, never showed this).
        void ApplyFontState(StateSave state)
        {
            state.SetValue(nameof(TextRuntime.UseCustomFont), true, "bool");
            state.SetValue(nameof(TextRuntime.CustomFontFile), fontFilePath, "string");
            state.SetValue(nameof(TextRuntime.FontSize), fontSize, "int");
        }
        ApplyFontState(Styling.ActiveStyle.Text.Normal);
        ApplyFontState(Styling.ActiveStyle.Text.Strong);
        ApplyFontState(Styling.ActiveStyle.Text.Emphasis);

        // Drives non-Forms TextRuntime (e.g. raw section headers in CirclesScreen) — without
        // this they fall back to raylib's GetFontDefault, which is the chunky pixel font.
        Gum.Renderables.Text.DefaultFont = font;
    }
}
