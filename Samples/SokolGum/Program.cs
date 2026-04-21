using System.Runtime.InteropServices;
using Gum.Wireframe;
using SokolGum;
using GumKeys = Gum.Forms.Input.Keys;
using static Sokol.SApp;
using static Sokol.SG;
using static Sokol.SGP;
using static Sokol.SGlue;
using static Sokol.SLog;
using static Sokol.STM;

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
    private static Screen1? _screen1;
    private static Screen2? _screen2;
    // sokol_time baseline — captured in Init after stm_setup, subtracted on
    // every Frame tick so GumService.Update sees total seconds since startup.
    // Matches Raylib's GetTime() convention so the two samples have identical
    // "pass cumulative game time to Update" shape.
    private static ulong _startTicks;

    static GumService GumUi => GumService.Default;

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
        stm_setup();
        _startTicks = stm_now();

        _passAction = default;
        _passAction.colors[0].load_action = sg_load_action.SG_LOADACTION_CLEAR;
        _passAction.colors[0].clear_value = new sg_color { r = 0.10f, g = 0.12f, b = 0.15f, a = 1.0f };

        GumUi.Initialize();
        GumUi.UseKeyboardDefaults();

        _screen1 = new Screen1(GumUi.SystemManagers.Renderer.MainLayer);
        _screen2 = new Screen2();
        _screen1.AddToRoot();
    }

    [UnmanagedCallersOnly]
    private static void Event(sapp_event* ev)
    {
        GumUi.HandleSokolEvent(*ev);
    }

    [UnmanagedCallersOnly]
    private static void Frame()
    {
        sg_begin_pass(new sg_pass { action = _passAction, swapchain = sglue_swapchain() });

        // Toggle screens before Update() — FormsUtilities.Update clears
        // _keysPushedThisFrame, so "was just pressed" checks must happen first.
        // Skip the swap when a Forms control holds keyboard focus (e.g. TextBox
        // consuming arrow keys for caret movement) so typing into a TextBox
        // doesn't accidentally flip screens.
        var keyboard = GumUi.Keyboard;
        if (InteractiveGue.CurrentInputReceiver is null
            && (keyboard.KeyPushed(GumKeys.Left) || keyboard.KeyPushed(GumKeys.Right)))
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

        GumUi.Update(stm_sec(stm_since(_startTicks)));
        GumUi.Draw();

        sg_end_pass();
        sg_commit();
    }

    [UnmanagedCallersOnly]
    private static void Cleanup()
    {
        _screen1?.Dispose();
        GumUi.SystemManagers?.Dispose();
        sgp_shutdown();
        sg_shutdown();
    }
}
