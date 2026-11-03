using System.Collections.Generic;
using Gum.Plugins;
using Gum.Services;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Color = System.Drawing.Color;

namespace TextureCoordinateSelectionPlugin.Logic;

/// <summary>
/// Outlines every region a plugin reported for the shown texture (#7354), tints the hovered one and
/// the one equal to the current rectangle, and names only the hovered one. Draws nothing while
/// there are no regions, so a tab with no region-reporting plugin looks exactly as it did before.
/// </summary>
public class TextureRegionOverlayManager : IVisualOverlayManager
{
    // Above the snap grid and nine-slice guides (Z 1), below the current rectangle's handles.
    private const float OutlineZ = 2;

    // Faint outlines keep a sheet with dozens of regions readable.
    private const int ManyRegions = 30;

    private readonly ICanvasDisplayScale _displayScale;
    private readonly List<LineRectangle> _outlines = new List<LineRectangle>();
    private SystemManagers? _systemManagers;
    private SolidRectangle? _tint;
    private SolidRectangle? _labelBackground;
    private Text? _labelText;

    private TextureRegionSet _regions = TextureRegionSet.Empty;
    private TextureRegion? _hovered;
    private (int X, int Y, int Width, int Height)? _current;

    public TextureRegionOverlayManager(ICanvasDisplayScale displayScale)
    {
        _displayScale = displayScale;
    }

    /// <summary>The regions to draw; replacing them redraws everything once.</summary>
    public TextureRegionSet Regions
    {
        get => _regions;
        set
        {
            _regions = value;
            _hovered = null;
            Refresh();
        }
    }

    /// <summary>The region under the pointer, whose name is shown, or null.</summary>
    public TextureRegion? Hovered
    {
        get => _hovered;
        set
        {
            if (_hovered != value)
            {
                _hovered = value;
                Refresh();
            }
        }
    }

    /// <summary>The current texture rectangle; a region equal to it is drawn emphasized.</summary>
    public (int X, int Y, int Width, int Height)? Current
    {
        get => _current;
        set
        {
            if (_current != value)
            {
                _current = value;
                Refresh();
            }
        }
    }

    /// <summary>How many outlines are drawn right now.</summary>
    public int VisibleOutlineCount
    {
        get
        {
            int count = 0;
            foreach (LineRectangle outline in _outlines)
            {
                if (outline.Visible)
                {
                    count++;
                }
            }
            return count;
        }
    }

    /// <summary>The hovered region's name while its label is drawn, or null.</summary>
    public string? VisibleLabel => _labelText?.Visible == true ? _labelText.RawText : null;

    public void Initialize(SystemManagers systemManagers)
    {
        _systemManagers = systemManagers;

        _tint = new SolidRectangle();
        _tint.Z = OutlineZ - 0.5f;
        _tint.Visible = false;
        systemManagers.ShapeManager.Add(_tint);

        _labelBackground = new SolidRectangle();
        _labelBackground.Z = OutlineZ + 0.5f;
        _labelBackground.Color = Color.FromArgb(217, 0, 0, 0);
        _labelBackground.Visible = false;
        systemManagers.ShapeManager.Add(_labelBackground);

        _labelText = new Text(systemManagers);
        _labelText.RenderBoundary = false;
        _labelText.Width = null;
        _labelText.Z = OutlineZ + 0.6f;
        _labelText.Color = Color.White;
        _labelText.Visible = false;
        systemManagers.TextManager.Add(_labelText);
    }

    public void Refresh()
    {
        if (_systemManagers == null)
        {
            // Not initialized yet: the canvas has no graphics device until the head's render surface is ready.
            return;
        }

        IReadOnlyList<TextureRegion> regions = _regions.Regions;
        bool faint = regions.Count > ManyRegions;
        float pixel = _displayScale.DisplayScale;

        // One pooled outline per region, restyled in a single pass; extras stay hidden.
        while (_outlines.Count < regions.Count)
        {
            LineRectangle outline = new LineRectangle(_systemManagers);
            outline.IsDotted = false;
            outline.Z = OutlineZ;
            outline.Visible = false;
            _systemManagers.ShapeManager.Add(outline);
            _outlines.Add(outline);
        }

        for (int i = 0; i < _outlines.Count; i++)
        {
            LineRectangle outline = _outlines[i];
            if (i >= regions.Count)
            {
                outline.Visible = false;
                continue;
            }

            TextureRegion region = regions[i];
            bool isCurrent = IsCurrent(region);
            bool emphasized = region == _hovered || isCurrent;
            outline.X = region.X;
            outline.Y = region.Y;
            outline.Width = region.Width;
            outline.Height = region.Height;
            outline.LinePixelWidth = isCurrent ? pixel * 2 : pixel;
            outline.Color = Color.FromArgb(emphasized ? 242 : faint ? 102 : 178, 255, 255, 255);
            outline.Visible = true;
        }

        RefreshTint();
        RefreshLabel();
    }

    private bool IsCurrent(TextureRegion region) =>
        _current is { } current && current.X == region.X && current.Y == region.Y && current.Width == region.Width && current.Height == region.Height;

    private TextureRegion? FindCurrentRegion()
    {
        foreach (TextureRegion region in _regions.Regions)
        {
            if (IsCurrent(region))
            {
                return region;
            }
        }
        return null;
    }

    private void RefreshTint()
    {
        if (_tint == null)
        {
            return;
        }

        TextureRegion? tinted = _hovered ?? FindCurrentRegion();
        _tint.Visible = tinted != null;
        if (tinted is TextureRegion region)
        {
            _tint.X = region.X;
            _tint.Y = region.Y;
            _tint.Width = region.Width;
            _tint.Height = region.Height;
            // The hover is brighter than the "this is the current rectangle" tint.
            _tint.Color = Color.FromArgb(_hovered != null ? 56 : 31, 255, 255, 255);
        }
    }

    private void RefreshLabel()
    {
        if (_labelText == null || _labelBackground == null || _systemManagers == null)
        {
            return;
        }

        bool show = _hovered != null;
        _labelText.Visible = show;
        _labelBackground.Visible = show;
        if (_hovered is not TextureRegion region)
        {
            return;
        }

        // Constant on-screen size however far the canvas is zoomed.
        float scale = _displayScale.DisplayScale / _systemManagers.Renderer.Camera.Zoom;
        float inset = 2 * scale;
        _labelText.RawText = region.Name;
        // The default font is 18 px tall; the name is meant to read at about 11 px.
        _labelText.FontScale = scale * 0.6f;
        _labelText.X = region.X + inset + 3 * scale;
        _labelText.Y = region.Y + inset + 1 * scale;
        _labelBackground.X = region.X + inset;
        _labelBackground.Y = region.Y + inset;
        _labelBackground.Width = _labelText.WrappedTextWidth + 6 * scale;
        _labelBackground.Height = _labelText.WrappedTextHeight + 2 * scale;
    }
}
