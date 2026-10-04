using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.EditorTab;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Editor toolbar's controls stay clear of the docked Preview button when the window is
/// narrower than the controls need (#5695).
/// </summary>
public class EditorToolbarNarrowWidthTests
{
    [AvaloniaFact]
    public void NarrowWindow_ControlsEndBeforePreviewButton()
    {
        EditorToolbar toolbar = new EditorToolbar();
        Window window = new Window { Width = 500, Height = 200, Content = toolbar };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            CheckBox snap = toolbar.GetVisualDescendants().OfType<CheckBox>().Single();
            Control controlsHost = snap.GetVisualAncestors().OfType<Control>()
                .Single(ancestor => ancestor.GetVisualParent() == toolbar);

            // The controls overflow the space beside the button, so the host must clip them to it.
            controlsHost.Bounds.Right.ShouldBeLessThanOrEqualTo(toolbar.PreviewButton.Bounds.Left);
            double snapRight = snap.TranslatePoint(new Point(snap.Bounds.Width, 0), toolbar)!.Value.X;
            if (snapRight > toolbar.PreviewButton.Bounds.Left)
            {
                controlsHost.ClipToBounds.ShouldBeTrue();
            }
        }
        finally
        {
            window.Close();
        }
    }
}
