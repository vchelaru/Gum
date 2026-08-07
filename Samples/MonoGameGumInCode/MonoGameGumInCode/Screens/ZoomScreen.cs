using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;

#if RAYLIB
namespace Examples.Shapes;
#elif SKIA
namespace SilkNetGum.Screens;
#else
namespace MonoGameGumInCode.Screens;
#endif

// Issue #4330: a dedicated screen (mirrored across MonoGame/raylib/SilkNetGum -- see the
// gum-samples skill) to manually verify camera-zoom font crispness on every backend side by side.
// Shared by all three the same way TextScreen.cs is (namespace switch above + <Compile Include Link>
// in the raylib/SilkNetGum csprojs) rather than three drifting copies.
//
// Drives the SHARED main Camera.Zoom directly (not a per-layer override -- raylib's and SkiaGum's
// Renderer both build ONE camera matrix for the whole frame and apply it to every layer, ignoring
// LayerCameraSettings.IsInScreenSpace entirely for actual drawing; only the XNALIKE renderer
// respects it. A dedicated screen-space layer therefore can't isolate this screen's own controls
// from the zoom it's driving on 2 of 3 backends, so this screen doesn't attempt that). Game1/
// Program's ShowScreen/LoadScreen resets Camera.Zoom back to 1 on every navigation, so leaving this
// screen zoomed in never leaks into any other screen.
//
// The zoom slider commits to Camera.Zoom only on ValueChangeCompleted (mouse release), not on every
// intermediate ValueChanged during the drag. This screen's own Slider is rendered through the same
// camera it controls, and Slider's drag math (UpdateThumbPositionToCursorDrag) converts the cursor's
// screen position back to canvas space via ICursor.XRespectingGumZoomAndBounds(), which divides by
// the CURRENT Camera.Zoom. Writing a new Camera.Zoom on every intermediate ValueChanged feeds that
// new zoom back into the very next frame's cursor conversion -- a real, not merely cosmetic, circular
// dependency: a perfectly static mouse position converts to a DIFFERENT canvas-space ratio once the
// zoom it's divided by has changed, which computes yet another new zoom, compounding every frame the
// drag continues. Confirmed to spiral into unbounded zoom on Silk.NET (continuing even after
// releasing the mouse -- the runaway state corrupts the thumb's own push/release hit-testing) and to
// jitter without ever settling far from 1x on MonoGame/raylib (which is also why oversampling never
// visibly appeared to work there -- the achieved zoom rarely moved far enough from 1x to cross
// UpdateAutomaticFontOversampling's 1px raster-delta threshold). Committing only once per completed
// gesture makes Camera.Zoom -- and therefore the cursor conversion depending on it -- constant for
// the ENTIRE drag, which cannot loop. The label still updates live from the slider's own Value on
// every ValueChanged for immediate numeric feedback; only the actual camera zoom (and the UI's own
// on-screen scale, since there's no way to isolate it -- see above) waits for release.
//
// TextRuntime.UseFontOversampling is a single project-wide static flag (see its own doc comment --
// deliberately not a per-instance override), so this screen can only demonstrate ALL text going
// crisp or ALL text staying blurry together, not a permanent side-by-side blurry/crisp pair. The
// checkbox toggles that shared flag live so the SAME text can be compared blurry vs. crisp at the
// same zoom level. On MonoGame/raylib this actually changes what's drawn (TextRuntime.
// RegenerateOversampledFont re-rasterizes via IInMemoryFontCreator/IRaylibFontCreator, #4317/#4330).
// On Skia there is no oversampling machinery at all -- SkiaSharp rasterizes text natively at
// whatever size it's drawn, so it never blurs under zoom and the checkbox has nothing to toggle.
internal class ZoomScreen : FrameworkElement
{
    public ZoomScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

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
        zoomSlider.Maximum = 6;
        zoomSlider.Value = 1;
        controlsPanel.AddChild(zoomSlider);

        void UpdateZoomLabel(double value)
        {
            zoomLabel.Text = $"Camera Zoom: {value:0.00}x";
        }

        zoomSlider.ValueChanged += (_, _) => UpdateZoomLabel(zoomSlider.Value);
        zoomSlider.ValueChangeCompleted += (_, _) =>
        {
            SystemManagers.Default.Renderer.Camera.Zoom = (float)zoomSlider.Value;
        };
        UpdateZoomLabel(zoomSlider.Value);

#if !SKIA
        var oversamplingCheckBox = new CheckBox();
        oversamplingCheckBox.Text = "Use Font Oversampling";
        oversamplingCheckBox.Width = 260;
        oversamplingCheckBox.IsChecked = TextRuntime.UseFontOversampling;
        oversamplingCheckBox.Checked += (_, _) => TextRuntime.UseFontOversampling = true;
        oversamplingCheckBox.Unchecked += (_, _) => TextRuntime.UseFontOversampling = false;
        controlsPanel.AddChild(oversamplingCheckBox);
#else
        var oversamplingNote = new Label();
        oversamplingNote.Text = "(Skia rasterizes text natively -- always crisp, no oversampling toggle needed)";
        oversamplingNote.Width = 260;
        controlsPanel.AddChild(oversamplingNote);
#endif

        var previewText = new TextRuntime();
        previewText.Font = "Arial";
        previewText.FontSize = 24;
        previewText.Text = "Drag the slider and release -- crisp with oversampling on, blurry with it off (MonoGame/raylib).";
        previewText.WidthUnits = DimensionUnitType.Absolute;
        previewText.Width = 260;
        previewText.Red = 255;
        previewText.Green = 255;
        previewText.Blue = 255;
        previewText.Alpha = 255;
        controlsPanel.AddChild(previewText);
    }
}
