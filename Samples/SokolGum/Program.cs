using System.Runtime.InteropServices;
using RenderingLibrary.Content;
using SokolGum;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Renderables;
using GumKeys = Gum.Forms.Input.Keys;
using static Sokol.SApp;
using static Sokol.SG;
using static Sokol.SGP;
using static Sokol.SGlue;
using static Sokol.SLog;

namespace SokolGumSample;

/// <summary>
/// Minimal code-only Gum sample running on the SokolGum backend. Mirrors the
/// shape of Samples/raylib — no .gumx project, just instantiate runtime
/// wrappers and add them to the main layer.
///
/// Left/Right arrow toggles between Screen1 (renderable exercises) and
/// Screen2 (empty — fill in with custom code).
/// </summary>
public static unsafe class Program
{
    private static sg_pass_action _passAction;
    private static Texture2D? _gradientTexture;
    private static Texture2D? _logoTexture;
    private static Texture2D? _nineSliceTexture;
    private static Font? _font;
    private static AnimationChainList? _characterAnimations;
    private static Screen1? _screen1;
    private static Screen2? _screen2;

    public static void Main()
    {
        // When launched outside a debugger, the working directory is the
        // binary's location — so Content relative paths resolve correctly.
        if (!System.Diagnostics.Debugger.IsAttached)
        {
            var appPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName)!;
            Directory.SetCurrentDirectory(appPath);
        }

        sapp_run(new sapp_desc
        {
            init_cb = &Init,
            frame_cb = &Frame,
            event_cb = &Event,
            cleanup_cb = &Cleanup,
            width = 1280,
            height = 720,
            sample_count = 4,
            // Opt into the native framebuffer resolution on Retina / hi-DPI
            // displays. Without this, sokol_app asks the OS for a 1280×720
            // backing store and the compositor bilinearly upscales it 2× to
            // the physical display — which blurs every pixel we draw. With
            // it on, sapp_width() / sapp_height() return physical pixels and
            // BeginFrame's sgp_project below matches, so text + geometry
            // sample 1:1 against the display's native grid.
            high_dpi = true,
            window_title = "SokolGum Sample — Gum UI via sokol_gp",
            icon = { sokol_default = true },
            logger = { func = &slog_func },
        });
    }

    [UnmanagedCallersOnly]
    private static void Init()
    {
        sg_setup(new sg_desc
        {
            environment = sglue_environment(),
            logger = { func = &slog_func },
        });
        sgp_setup(new sgp_desc());

        _passAction = default;
        _passAction.colors[0].load_action = sg_load_action.SG_LOADACTION_CLEAR;
        _passAction.colors[0].clear_value = new sg_color { r = 0.10f, g = 0.12f, b = 0.15f, a = 1.0f };

        GumService.Default.Initialize();

        _gradientTexture = BuildGradientTexture(128, 128);
        _nineSliceTexture = BuildNineSliceTestTexture();
        _logoTexture = LoaderManager.Self.ContentLoader.LoadContent<Texture2D>("Assets/sokol_logo.png");
        _font = LoaderManager.Self.ContentLoader.LoadContent<Font>("Assets/DroidSerif-Regular.ttf");
        _characterAnimations = LoaderManager.Self.ContentLoader.LoadContent<AnimationChainList>("Assets/CharacterAnimations.achx");

        var mainLayer = GumService.Default.SystemManagers.Renderer.MainLayer;

        _screen1 = new Screen1(mainLayer, _gradientTexture, _nineSliceTexture, _logoTexture, _font, _characterAnimations);
        _screen2 = new Screen2();
        _screen1.AddToRoot();

        // Uncomment to demo camera Zoom / Position (applied globally to
        // every layer via Renderer.BeginFrame's sgp_project call):
        //   _systemManagers.Renderer.Camera.Zoom = 1.15f;   // zoom in 15%
        //   _systemManagers.Renderer.Camera.Position = new Vector2(-40, 0); // pan right 40px
    }

    /// <summary>
    /// Build a diagonal gradient texture at runtime so the sprite test doesn't
    /// depend on asset files. Red varies along X, green along Y, blue is half.
    /// </summary>
    private static Texture2D BuildGradientTexture(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                pixels[i + 0] = (byte)(x * 255 / (width - 1));
                pixels[i + 1] = (byte)(y * 255 / (height - 1));
                pixels[i + 2] = 128;
                pixels[i + 3] = 255;
            }
        }
        return Texture2D.FromRgba8(pixels, width, height, "gradient");
    }

    /// <summary>
    /// 48×48 nine-slice test texture with distinctly colored regions:
    /// corners red, top/bottom orange (stretch horizontally), left/right
    /// green (stretch vertically), centre blue (stretch both ways).
    /// </summary>
    private static Texture2D BuildNineSliceTestTexture()
    {
        const int size = 48;
        const int border = 16;
        var pixels = new byte[size * size * 4];
        Span<byte> corner         = stackalloc byte[] { 220, 80, 80, 255 };
        Span<byte> horizontalEdge = stackalloc byte[] { 255, 180, 80, 255 };
        Span<byte> verticalEdge   = stackalloc byte[] { 80, 200, 100, 255 };
        Span<byte> center         = stackalloc byte[] { 80, 130, 255, 255 };

        for (int y = 0; y < size; y++)
        {
            bool inTop = y < border;
            bool inBottom = y >= size - border;
            bool inMidV = !inTop && !inBottom;
            for (int x = 0; x < size; x++)
            {
                bool inLeft = x < border;
                bool inRight = x >= size - border;
                bool inMidH = !inLeft && !inRight;

                Span<byte> c =
                    (inTop || inBottom) && (inLeft || inRight) ? corner
                    : (inTop || inBottom) && inMidH ? horizontalEdge
                    : inMidV && (inLeft || inRight) ? verticalEdge
                    : center;

                int i = (y * size + x) * 4;
                pixels[i + 0] = c[0];
                pixels[i + 1] = c[1];
                pixels[i + 2] = c[2];
                pixels[i + 3] = c[3];
            }
        }
        return Texture2D.FromRgba8(pixels, size, size, "nineslice-test");
    }

    [UnmanagedCallersOnly]
    private static void Event(sapp_event* ev)
    {
        GumService.Default.HandleSokolEvent(*ev);
    }

    [UnmanagedCallersOnly]
    private static void Frame()
    {
        sg_begin_pass(new sg_pass { action = _passAction, swapchain = sglue_swapchain() });

        // Toggle screens before Update() — FormsUtilities.Update clears
        // _keysPushedThisFrame, so "was just pressed" checks must happen first.
        var keyboard = GumService.Default.Keyboard;
        if (keyboard.KeyPushed(GumKeys.Left) || keyboard.KeyPushed(GumKeys.Right))
        {
            if (_screen1 is not null && _screen2 is not null)
            {
                // Toggle by swapping which screen is in the scene. Each
                // screen adds/removes its whole subtree (GUE children +
                // any raw Layer renderables) so nothing from the inactive
                // screen participates in layout, input, or rendering.
                bool screen1Active = _screen1.Parent is not null;
                if (screen1Active)
                {
                    _screen1.RemoveFromRoot();
                    _screen2.AddToRoot();
                }
                else
                {
                    _screen2.RemoveFromRoot();
                    _screen1.AddToRoot();
                }
            }
        }

        GumService.Default.Update();
        GumService.Default.Draw();

        sg_end_pass();
        sg_commit();
    }

    [UnmanagedCallersOnly]
    private static void Cleanup()
    {
        // Font has no per-font Dispose — its TTF buffer is owned by the
        // FontAtlas which the SystemManagers disposes below.
        _gradientTexture?.Dispose();
        _nineSliceTexture?.Dispose();
        _logoTexture?.Dispose();
        GumService.Default.SystemManagers?.Dispose();
        sgp_shutdown();
        sg_shutdown();
    }
}
