using System.Runtime.InteropServices;
using Gum.Managers;
using SokolGum;
using static Sokol.SApp;
using static Sokol.SG;
using static Sokol.SGP;
using static Sokol.SGlue;
using static Sokol.SLog;

namespace SokolGumFromFile;

/// <summary>
/// .gumx-loading sample. Loads Content/GumProject/GumProject.gumx at
/// startup via <see cref="GumService"/>, shows MainScreen, and renders it
/// each frame. Pair with <c>Samples/SokolGum</c> (code-only) to compare
/// the two construction paths for the same backend.
/// </summary>
public static unsafe class Program
{
    private static sg_pass_action _passAction;

    public static void Main()
    {
        if (!System.Diagnostics.Debugger.IsAttached)
        {
            var appPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName)!;
            Directory.SetCurrentDirectory(appPath);
        }

        sapp_run(new sapp_desc
        {
            init_cb = &Init,
            frame_cb = &Frame,
            cleanup_cb = &Cleanup,
            width = 1280,
            height = 720,
            sample_count = 4,
            // See comment on the equivalent flag in Samples/SokolGum for
            // why this matters — without it the OS upscales our framebuffer
            // 2× on Retina displays, blurring every pixel.
            high_dpi = true,
            window_title = "SokolGum FromFile — .gumx loader",
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

        GumService.Default.Initialize("Content/GumProject/GumProject.gumx");

        var screenSave = ObjectFinder.Self.GumProjectSave!.Screens
            .First(s => s.Name == "MainScreen");
        var screenGue = screenSave.ToGraphicalUiElement();
        screenGue.AddToRoot();
    }

    [UnmanagedCallersOnly]
    private static void Frame()
    {
        sg_begin_pass(new sg_pass { action = _passAction, swapchain = sglue_swapchain() });

        GumService.Default.Update();
        GumService.Default.Draw();

        sg_end_pass();
        sg_commit();
    }

    [UnmanagedCallersOnly]
    private static void Cleanup()
    {
        GumService.Default.SystemManagers?.Dispose();
        sgp_shutdown();
        sg_shutdown();
    }
}
