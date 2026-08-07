using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
#if RAYLIB
using Color = Raylib_cs.Color;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Color = Microsoft.Xna.Framework.Color;
#endif

#if RAYLIB
namespace Examples.Shapes;
#elif SKIA
namespace SilkNetGum.Screens;
#else
namespace MonoGameGumInCode.Screens;
#endif

// Issue #4367 manual-test rig (mirrored across MonoGame/raylib/SilkNetGum -- see the gum-samples
// skill): before the fix, raylib's and SkiaGum's Renderer both built ONE camera matrix for the
// whole frame and applied it to every layer, so a Layer.LayerCameraSettings override was honored
// by Layer.ScreenToWorld/WorldToScreen (hit-testing) but never consulted when actually drawing --
// only the XNALIKE renderer respected it. This screen puts the SAME content on two layers side by
// side so the difference is visible with the naked eye:
//
//   - The crimson "World content" square is an ordinary child of this screen -- it renders through
//     the main camera, so the zoom slider and pan button both move/scale it.
//   - The blue "HUD" panel is added directly to a dedicated Layer with
//     LayerCameraSettings.IsInScreenSpace = true (NOT a child of this screen -- see AddToManagers
//     below). Before the fix, on raylib/SkiaGum, it moved/scaled right along with the world content
//     (the bug). After the fix, it must stay pixel-fixed at the bottom-left corner on ALL THREE
//     backends no matter how far the slider is zoomed or whether the camera is panned.
//
// The HUD panel is intentionally non-interactive (no Slider/Button on it) -- the fix is about the
// RENDER transform. Cursor hit-testing already routed through Layer.ScreenToWorld correctly even
// before this fix (that's what made the bug a silent rendering/hit-testing DISAGREEMENT rather than
// an obvious total break), so there's nothing new to prove on the input side.
internal class LayerCameraSettingsScreen : FrameworkElement
{
    // Tracks the screen-space HUD layer created by the most recently-constructed instance of this
    // screen. A fresh instance is created every time the user navigates back to this screen (see
    // each sample's ShowScreen/LoadScreen), and RemoveFromRoot (called on the OLD screen when
    // navigating away) only detaches that screen's own visual-tree parent link -- it has no idea
    // about a renderable added directly to a Layer via AddToManagers, since that's a top-level layer
    // member, not a child of the screen. Without this, the HUD panel from every previous visit would
    // keep piling up as an extra Layer + extra renderables.
    private static Layer? _hudLayer;

    public LayerCameraSettingsScreen() : base(new ContainerRuntime())
    {
        if (_hudLayer != null)
        {
            SystemManagers.Default.Renderer.RemoveLayer(_hudLayer);
            _hudLayer = null;
        }

        Dock(Gum.Wireframe.Dock.Fill);

        // --- World content: a normal child of this screen, rendered through the main camera. ---
        var worldRectangle = new RectangleRuntime();
        worldRectangle.X = 300;
        worldRectangle.Y = 160;
        worldRectangle.Width = 140;
        worldRectangle.Height = 140;
        worldRectangle.IsFilled = true;
        worldRectangle.FillColor = new Color(220, 20, 60, 255); // Crimson
        this.AddChild(worldRectangle);

        var worldLabel = new TextRuntime();
        worldLabel.Text = "World content\n(moves & scales with the camera)";
        worldLabel.X = 300;
        worldLabel.Y = 310;
        worldLabel.WidthUnits = DimensionUnitType.Absolute;
        worldLabel.Width = 220;
        worldLabel.Red = 255;
        worldLabel.Green = 255;
        worldLabel.Blue = 255;
        worldLabel.Alpha = 255;
        this.AddChild(worldLabel);

        // --- Camera controls: ordinary Forms controls, also rendered through the main camera. ---
        var controlsPanel = new StackPanel();
        controlsPanel.Orientation = Orientation.Vertical;
        controlsPanel.Spacing = 6;
        controlsPanel.Visual.X = 16;
        controlsPanel.Visual.Y = 16;
        this.AddChild(controlsPanel);

        var zoomLabel = new Label();
        zoomLabel.Width = 260;
        controlsPanel.AddChild(zoomLabel);

        var zoomSlider = new Slider();
        zoomSlider.Width = 260;
        zoomSlider.Minimum = 1;
        zoomSlider.Maximum = 3;
        zoomSlider.Value = 1;
        controlsPanel.AddChild(zoomSlider);

        void UpdateZoomLabel(double value) => zoomLabel.Text = $"Camera Zoom: {value:0.00}x";

        zoomSlider.ValueChanged += (_, _) => UpdateZoomLabel(zoomSlider.Value);
        // Commit on release only -- see ZoomScreen's own comment for why: the slider's own drag math
        // divides the cursor position by the very Camera.Zoom this handler would otherwise write on
        // every intermediate tick, which is a real feedback loop, not just cosmetic jitter.
        zoomSlider.ValueChangeCompleted += (_, _) =>
        {
            SystemManagers.Default.Renderer.Camera.Zoom = (float)zoomSlider.Value;
        };
        UpdateZoomLabel(zoomSlider.Value);

        var panButton = new Button();
        panButton.Width = 260;
        bool isPanned = false;
        void UpdatePanButtonText() => panButton.Text = isPanned ? "Pan Camera (panned - click to reset)" : "Pan Camera";
        panButton.Click += (_, _) =>
        {
            isPanned = !isPanned;
            SystemManagers.Default.Renderer.Camera.X = isPanned ? 220 : 0;
            SystemManagers.Default.Renderer.Camera.Y = isPanned ? 140 : 0;
            UpdatePanButtonText();
        };
        UpdatePanButtonText();
        controlsPanel.AddChild(panButton);

        // --- HUD content: added directly to a dedicated screen-space Layer, NOT a child of this
        // screen, so LayerCameraSettings.IsInScreenSpace actually applies to it (see class comment
        // above for why nesting under `this` wouldn't work -- Layers are a frame-level render pass,
        // not something a nested child can independently opt into). ---
        _hudLayer = SystemManagers.Default.Renderer.AddLayer();
        _hudLayer.Name = "HUD (screen space)";
        _hudLayer.LayerCameraSettings = new LayerCameraSettings { IsInScreenSpace = true };

        var hudBackground = new RectangleRuntime();
        hudBackground.X = 16;
        hudBackground.Y = 520;
        hudBackground.Width = 260;
        hudBackground.Height = 90;
        hudBackground.IsFilled = true;
        hudBackground.FillColor = new Color(20, 90, 160, 220);
        hudBackground.AddToManagers(SystemManagers.Default, _hudLayer);

        var hudLabel = new TextRuntime();
        hudLabel.Text = "HUD layer (IsInScreenSpace = true)\nMust stay fixed regardless of zoom/pan.";
        hudLabel.X = 24;
        hudLabel.Y = 528;
        hudLabel.WidthUnits = DimensionUnitType.Absolute;
        hudLabel.Width = 244;
        hudLabel.Red = 255;
        hudLabel.Green = 255;
        hudLabel.Blue = 255;
        hudLabel.Alpha = 255;
        hudLabel.AddToManagers(SystemManagers.Default, _hudLayer);
    }
}
