using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>A rectangle in world units, as edges.</summary>
public readonly record struct CanvasWorldBounds(float Left, float Top, float Right, float Bottom);

/// <summary>The zoom step and top-left camera position that frame some bounds.</summary>
public readonly record struct ZoomToFitResult(int ZoomPercent, float CameraX, float CameraY);

/// <summary>What a zoom to fit applied, as the camera reads back afterward.</summary>
public sealed record CanvasZoomToFitReport(
    string ElementName, CanvasWorldBounds Bounds, int ZoomPercent, float CameraX, float CameraY, int ViewWidth, int ViewHeight)
{
    /// <summary>
    /// One line for an unattended run's stdout. The sample sweep parses it to place world (0,0)
    /// in the screenshot, so keep the format stable.
    /// </summary>
    public override string ToString() => string.Format(
        CultureInfo.InvariantCulture,
        "Zoom to fit: {0} bounds ({1:0.##}, {2:0.##})-({3:0.##}, {4:0.##}), zoom {5}%, camera ({6:0.##}, {7:0.##}), view {8}x{9} px",
        ElementName, Bounds.Left, Bounds.Top, Bounds.Right, Bounds.Bottom, ZoomPercent, CameraX, CameraY, ViewWidth, ViewHeight);
}

/// <summary>
/// Asks the Editor tab to frame the selected element (see <see cref="CanvasZoomToFit"/>). The reply
/// is null when nothing is selected, the canvas isn't initialized, or nothing visible is selected.
/// The defaults frame the whole element without zooming past 100%, which is what an unattended
/// <c>--zoom-to-fit</c> run needs; <see cref="FocusOnSelection"/> is the interactive variant.
/// </summary>
public sealed class ZoomCanvasToFitSelectionMessage : RequestMessage<CanvasZoomToFitReport?>
{
    /// <summary>The largest zoom step the fit may use.</summary>
    public int MaxZoomPercent { get; init; } = 100;

    /// <summary>Frame the selected instances instead of their element, when any are selected.</summary>
    public bool FrameSelectedInstances { get; init; }

    /// <summary>The request behind the F hotkey and a tree double-click (#5981).</summary>
    public static ZoomCanvasToFitSelectionMessage FocusOnSelection() =>
        new ZoomCanvasToFitSelectionMessage { MaxZoomPercent = int.MaxValue, FrameSelectedInstances = true };
}

/// <summary>
/// Frames an element in the wireframe canvas (#5142): the largest zoom step, up to 100%, at which
/// the element and its visible descendants fit inside the view, centered.
/// </summary>
public static class CanvasZoomToFit
{
    /// <summary>Screen pixels kept clear on every side, which also clears the rulers.</summary>
    public const int MarginPixels = 40;

    /// <summary>
    /// The world bounds of <paramref name="element"/> and its visible descendants. A screen has no
    /// size of its own in the editor, so only its instances count. False when nothing is visible.
    /// </summary>
    public static bool TryGetVisibleBounds(GraphicalUiElement element, bool isScreen, out CanvasWorldBounds bounds)
    {
        if (!isScreen)
        {
            return TryGetVisibleBounds(new[] { element }, out bounds);
        }

        // Nested instances are reached through their parents, so a hidden parent hides them.
        return TryGetVisibleBounds(
            element.ContainedElements.Where(instance => instance.Parent == null || instance.Parent == element),
            out bounds);
    }

    /// <summary>
    /// The union of the world bounds of <paramref name="elements"/> and their visible descendants.
    /// False when none of them is visible.
    /// </summary>
    public static bool TryGetVisibleBounds(IEnumerable<GraphicalUiElement> elements, out CanvasWorldBounds bounds)
    {
        float left = float.PositiveInfinity;
        float top = float.PositiveInfinity;
        float right = float.NegativeInfinity;
        float bottom = float.NegativeInfinity;

        foreach (GraphicalUiElement element in elements)
        {
            Include(element, ref left, ref top, ref right, ref bottom);
        }

        bounds = new CanvasWorldBounds(left, top, right, bottom);
        return left <= right && top <= bottom;
    }

    private static void Include(IRenderableIpso item, ref float left, ref float top, ref float right, ref float bottom)
    {
        if (!item.Visible)
        {
            return;
        }

        left = Math.Min(left, item.GetAbsoluteLeft());
        top = Math.Min(top, item.GetAbsoluteTop());
        right = Math.Max(right, item.GetAbsoluteRight());
        bottom = Math.Max(bottom, item.GetAbsoluteBottom());

        foreach (IRenderableIpso child in item.Children)
        {
            Include(child, ref left, ref top, ref right, ref bottom);
        }
    }

    /// <summary>
    /// The zoom step from <paramref name="zoomPercents"/> and the top-left camera position that
    /// center <paramref name="bounds"/> in a view of the given pixel size. Never zooms past
    /// <paramref name="maxZoomPercent"/> (100% by default); uses the smallest step when nothing fits.
    /// </summary>
    public static ZoomToFitResult Calculate(
        CanvasWorldBounds bounds, int viewportWidth, int viewportHeight, IReadOnlyList<int> zoomPercents, int maxZoomPercent = 100)
    {
        float availableWidth = Math.Max(1, viewportWidth - 2 * MarginPixels);
        float availableHeight = Math.Max(1, viewportHeight - 2 * MarginPixels);
        float boundsWidth = bounds.Right - bounds.Left;
        float boundsHeight = bounds.Bottom - bounds.Top;

        int? best = null;
        int smallest = int.MaxValue;
        foreach (int percent in zoomPercents)
        {
            smallest = Math.Min(smallest, percent);
            float zoom = percent / 100f;
            bool fits = percent <= maxZoomPercent &&boundsWidth * zoom <= availableWidth && boundsHeight * zoom <= availableHeight;
            if (fits && (best == null || percent > best))
            {
                best = percent;
            }
        }

        int chosen = best ?? smallest;
        float chosenZoom = chosen / 100f;
        float centerX = (bounds.Left + bounds.Right) / 2;
        float centerY = (bounds.Top + bounds.Bottom) / 2;
        return new ZoomToFitResult(
            chosen,
            centerX - viewportWidth / 2f / chosenZoom,
            centerY - viewportHeight / 2f / chosenZoom);
    }
}
