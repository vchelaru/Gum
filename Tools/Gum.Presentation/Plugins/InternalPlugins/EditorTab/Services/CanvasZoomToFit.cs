using System;
using System.Collections.Generic;
using System.Globalization;
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
/// </summary>
public sealed class ZoomCanvasToFitSelectionMessage : RequestMessage<CanvasZoomToFitReport?>
{
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
        float left = float.PositiveInfinity;
        float top = float.PositiveInfinity;
        float right = float.NegativeInfinity;
        float bottom = float.NegativeInfinity;

        if (isScreen)
        {
            foreach (GraphicalUiElement instance in element.ContainedElements)
            {
                // Nested instances are reached through their parents, so a hidden parent hides them.
                if (instance.Parent == null || instance.Parent == element)
                {
                    Include(instance, ref left, ref top, ref right, ref bottom);
                }
            }
        }
        else
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
    /// center <paramref name="bounds"/> in a view of the given pixel size. Never zooms past 100%;
    /// uses the smallest step when nothing fits.
    /// </summary>
    public static ZoomToFitResult Calculate(CanvasWorldBounds bounds, int viewportWidth, int viewportHeight, IReadOnlyList<int> zoomPercents)
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
            bool fits = percent <= 100 && boundsWidth * zoom <= availableWidth && boundsHeight * zoom <= availableHeight;
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
