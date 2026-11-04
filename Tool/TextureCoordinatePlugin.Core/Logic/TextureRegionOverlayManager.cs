using System.Collections.Generic;
using Gum.Managers;
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

    // A dark line hugging the outside of each white outline, so it stays visible on light sprites and
    // on the checkerboard alike.
    private readonly List<LineRectangle> _underlays = new List<LineRectangle>();
    private SystemManagers? _systemManagers;
    private SolidRectangle? _tint;
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

    /// <summary>The label's top-left in screen pixels and its size on screen, while it is drawn, or null.</summary>
    public (float X, float Y, float Height, float FontScale)? VisibleLabelPlacement =>
        _labelText?.Visible == true ? (_labelText.X, _labelText.Y, _labelText.WrappedTextHeight, _labelText.FontScale) : null;

    public void Initialize(SystemManagers systemManagers)
    {
        _systemManagers = systemManagers;

        _tint = new SolidRectangle();
        _tint.Z = OutlineZ - 0.5f;
        _tint.Visible = false;
        systemManagers.ShapeManager.Add(_tint);


        // The editor's own tool font, drawn with its built-in outline like the ruler text, so the name
        // reads on any background without a box behind it.
        IToolFontService fonts = new ToolFontService();
        try
        {
            fonts.Initialize();
        }
        catch (System.IO.IOException)
        {
            // No tool font beside the executable: the default font still draws the name.
        }

        _labelText = new Text(systemManagers);
        _labelText.BitmapFont = fonts.ToolFont;
        _labelText.RenderBoundary = false;
        _labelText.Width = null;
        _labelText.Z = OutlineZ + 0.6f;
        _labelText.Color = Color.White;
        _labelText.Visible = false;

        // Screen space, like the ruler text: the same pixel size and crispness at every zoom.
        Layer labelLayer = systemManagers.Renderer.AddLayer();
        labelLayer.LayerCameraSettings = new LayerCameraSettings();
        labelLayer.LayerCameraSettings.IsInScreenSpace = true;
        labelLayer.Name = "Texture region label layer";
        systemManagers.TextManager.Add(_labelText, labelLayer);
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

            LineRectangle underlay = new LineRectangle(_systemManagers);
            underlay.IsDotted = false;
            underlay.Z = OutlineZ - 0.1f;
            underlay.Visible = false;
            _systemManagers.ShapeManager.Add(underlay);
            _underlays.Add(underlay);
        }

        // One screen pixel in texture units: how far the dark line sits outside the white one.
        float outside = pixel / _systemManagers.Renderer.Camera.Zoom;
        for (int i = 0; i < _outlines.Count; i++)
        {
            LineRectangle outline = _outlines[i];
            LineRectangle underlay = _underlays[i];
            if (i >= regions.Count)
            {
                outline.Visible = false;
                underlay.Visible = false;
                continue;
            }

            underlay.X = regions[i].X - outside;
            underlay.Y = regions[i].Y - outside;
            underlay.Width = regions[i].Width + 2 * outside;
            underlay.Height = regions[i].Height + 2 * outside;
            underlay.LinePixelWidth = regions[i] == _hovered ? pixel * 2 : pixel;
            underlay.Color = Color.FromArgb(faint ? 170 : 215, 0, 0, 0);
            underlay.Visible = true;

            TextureRegion region = regions[i];
            bool isCurrent = IsCurrent(region);
            bool emphasized = region == _hovered || isCurrent;
            outline.X = region.X;
            outline.Y = region.Y;
            outline.Width = region.Width;
            outline.Height = region.Height;
            outline.LinePixelWidth = isCurrent ? pixel * 2 : pixel;
            outline.Color = Color.FromArgb(emphasized ? 255 : faint ? 215 : 240, 255, 255, 255);
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
        if (_labelText == null || _systemManagers == null)
        {
            return;
        }

        bool show = _hovered != null;
        _labelText.Visible = show;
        if (_hovered is not TextureRegion region)
        {
            return;
        }

        // The label layer is in screen space, so positions are screen pixels and the size never follows the zoom.
        Camera camera = _systemManagers.Renderer.Camera;
        float scale = _displayScale.DisplayScale;
        float gap = 6 * scale;
        _labelText.RawText = region.Name;
        _labelText.FontScale = scale;
        float left = (region.X - camera.X) * camera.Zoom;
        float top = (region.Y - camera.Y) * camera.Zoom;
        float bottom = top + region.Height * camera.Zoom;
        float textHeight = _labelText.WrappedTextHeight;
        _labelText.X = left;
        // Above the region so the sprite stays visible; below it when there is no room above.
        _labelText.Y = top - gap - textHeight >= 0 ? top - gap - textHeight : bottom + gap;
    }
}
