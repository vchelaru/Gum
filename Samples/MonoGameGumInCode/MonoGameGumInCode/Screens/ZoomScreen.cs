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
// Drives the SHARED main Camera.Zoom directly (not a per-layer override) -- simplest option, and
// safe because Game1/Program's ShowScreen resets Camera.Zoom back to 1 on every navigation, so
// leaving this screen zoomed in never leaks into any other screen.
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

        void UpdateZoomLabel()
        {
            zoomLabel.Text = $"Camera Zoom: {SystemManagers.Default.Renderer.Camera.Zoom:0.00}x";
        }

        zoomSlider.ValueChanged += (_, _) =>
        {
            SystemManagers.Default.Renderer.Camera.Zoom = (float)zoomSlider.Value;
            UpdateZoomLabel();
        };
        UpdateZoomLabel();

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
        previewText.Text = "Zoom in on this text -- crisp with oversampling on, blurry with it off (MonoGame/raylib).";
        previewText.WidthUnits = DimensionUnitType.Absolute;
        previewText.Width = 260;
        previewText.Red = 255;
        previewText.Green = 255;
        previewText.Blue = 255;
        previewText.Alpha = 255;
        controlsPanel.AddChild(previewText);
    }
}
