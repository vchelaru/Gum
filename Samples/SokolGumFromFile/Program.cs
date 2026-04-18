using System.Runtime.InteropServices;
using Gum.DataTypes;
using Gum.Managers;
using GumRuntime;
using RenderingLibrary.Graphics;
using SokolGum;
using ToolsUtilities;
using static Sokol.SApp;
using static Sokol.SG;
using static Sokol.SGP;
using static Sokol.SGlue;
using static Sokol.SLog;

namespace SokolGumFromFile;

/// <summary>
/// .gumx-loading sample — SilkNet-parity. Loads Content/GumProject/GumProject.gumx
/// at startup, instantiates the MainScreen's GraphicalUiElement tree, and
/// adds it to the root layer. Every property on every instance — including
/// <c>SourceFile</c> for textures and <c>Font</c> strings for TTF paths —
/// is routed through <see cref="CustomSetPropertyOnRenderable"/>.
///
/// Pair with <c>Samples/SokolGum</c> (code-only) to compare the two
/// construction paths for the same backend.
/// </summary>
public static unsafe class Program
{
    private static sg_pass_action _passAction;
    private static SystemManagers? _systemManagers;

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

        _systemManagers = new SystemManagers();
        SystemManagers.Default = _systemManagers;
        _systemManagers.Initialize();

        // Pass the .gumx as an absolute path and point FileManager.RelativeDirectory
        // at Content/. CustomSetPropertyOnRenderable prepends RelativeDirectory to
        // any relative SourceFile / Font value before handing it to the ContentLoader
        // — matches Gum's editor model where the project root is the asset root.
        var contentRoot = Path.Combine(Directory.GetCurrentDirectory(), "Content");
        FileManager.RelativeDirectory = contentRoot + Path.DirectorySeparatorChar;

        var gumxPath = Path.Combine(contentRoot, "GumProject", "GumProject.gumx");
        LoadGumProjectAndAddToRoot(gumxPath, "MainScreen", _systemManagers.Renderer.MainLayer);
    }

    /// <summary>
    /// Loads a .gumx, hydrates any standard-element stubs (their companion
    /// .gutx files aren't on disk but <see cref="StandardElementsManager"/>
    /// already has the in-memory schemas), registers with <see cref="ObjectFinder"/>
    /// so BaseType lookups succeed, then builds the screen's GraphicalUiElement
    /// tree and flattens its top-level children into the supplied layer.
    /// </summary>
    private static void LoadGumProjectAndAddToRoot(string gumxPath, string screenName, Layer layer)
    {
        var project = GumProjectSave.Load(gumxPath, out var result)
            ?? throw new InvalidOperationException($"Failed to load {gumxPath}: {result.ErrorMessage}");

        // GumProjectSave.Load creates bare stubs for any <StandardElementReference>
        // whose companion .gutx file is missing on disk. Hydrate each with its
        // in-memory default state so DefaultState is non-null when Gum walks
        // instances during ToGraphicalUiElement.
        foreach (var std in project.StandardElements)
        {
            if (std.States.Count == 0
                && StandardElementsManager.Self.DefaultStates.TryGetValue(std.Name, out var defaultState))
            {
                std.Initialize(defaultState);
            }
        }
        ObjectFinder.Self.GumProjectSave = project;

        var screen = project.Screens.FirstOrDefault(s => s.Name == screenName)
            ?? throw new InvalidOperationException($"Screen '{screenName}' not found in {gumxPath}");

        // ScreenSave children are intentionally NOT parented to the screen GUE
        // (see ElementSaveExtensions.CreateChildrenRecursively — the isScreen
        // check). They live in ContainedElements for variable-name lookup, but
        // the render walker only traverses Children — so each top-level
        // instance must be added to the layer as a peer.
        var screenGue = screen.ToGraphicalUiElement(_systemManagers!, addToManagers: false);
        foreach (var contained in screenGue.ContainedElements)
            layer.Add(contained);
    }

    [UnmanagedCallersOnly]
    private static void Frame()
    {
        var width = sapp_width();
        var height = sapp_height();

        sg_begin_pass(new sg_pass { action = _passAction, swapchain = sglue_swapchain() });

        _systemManagers!.Renderer.BeginFrame(width, height);
        _systemManagers.Renderer.Draw(_systemManagers);
        _systemManagers.Renderer.EndFrame(_systemManagers);

        sg_end_pass();
        sg_commit();
    }

    [UnmanagedCallersOnly]
    private static void Cleanup()
    {
        _systemManagers?.Dispose();
        sgp_shutdown();
        sg_shutdown();
    }
}
