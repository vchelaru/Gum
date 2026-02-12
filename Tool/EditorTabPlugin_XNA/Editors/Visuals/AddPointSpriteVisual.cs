using Gum.Input;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ToolsUtilities;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Visual component that displays an "add point" icon when hovering
/// between polygon vertices. Clicking adds a new vertex at that location.
/// </summary>
public class AddPointSpriteVisual : EditorVisualBase
{
    private readonly Sprite _addPointSprite;
    private static Texture2D? _addPointTexture;

    private const float SizeAtNoZoom = 16;
    private const int MaxPixelsForAddPoint = 15;

    /// <summary>
    /// The index of the line segment the add button is positioned at.
    /// The new point would be inserted after this index.
    /// </summary>
    public int InsertAfterIndex { get; private set; } = -1;

    /// <summary>
    /// Whether the add point sprite is currently being hovered over.
    /// </summary>
    public bool IsHovered { get; private set; }

    /// <summary>
    /// Whether point addition is currently enabled (no grab in progress).
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    public AddPointSpriteVisual(EditorContext context, Layer layer) : base(context)
    {
        // Load texture if not already loaded
        if (_addPointTexture == null)
        {
            var gumExePath = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetEntryAssembly()!.Location)!
                .ToLower().Replace("/", "\\") + "\\";

            var fileName = gumExePath + "Content/AddPoint.png";

            using (var stream = FileManager.GetStreamForFile(fileName))
            {
                _addPointTexture = Texture2D.FromStream(
                    SystemManagers.Default.Renderer.GraphicsDevice, stream);
                _addPointTexture.Name = fileName;
            }
        }

        _addPointSprite = new Sprite(_addPointTexture);
        _addPointSprite.Name = "Add point sprite";
        _addPointSprite.Visible = false;
        SpriteManager.Self.Add(_addPointSprite, layer);
    }

    protected override void OnVisibilityChanged(bool isVisible)
    {
        _addPointSprite.Visible = isVisible;
    }

    public override void Update()
    {
        if (!IsEnabled || Context.SelectedObjects.Count == 0)
        {
            _addPointSprite.Visible = false;
            IsHovered = false;
            return;
        }

        var selectedPolygon = Context.SelectedObjects
            .FirstOrDefault()?.RenderableComponent as LinePolygon;

        if (selectedPolygon == null || selectedPolygon.PointCount < 2)
        {
            _addPointSprite.Visible = false;
            IsHovered = false;
            return;
        }

        var worldX = InputLibrary.Cursor.Self.GetWorldX();
        var worldY = InputLibrary.Cursor.Self.GetWorldY();

        UpdatePosition(selectedPolygon, worldX, worldY);
    }

    private void UpdatePosition(LinePolygon polygon, float cursorX, float cursorY)
    {
        var size = SizeAtNoZoom / Zoom;
        _addPointSprite.Width = _addPointSprite.Height = size;

        var closestResult = GetClosestLineSegment(polygon, cursorX, cursorY);
        var maxDistance = MaxPixelsForAddPoint / Zoom;

        _addPointSprite.Visible = closestResult.Distance < maxDistance;
        InsertAfterIndex = closestResult.SegmentIndex;

        if (_addPointSprite.Visible)
        {
            // Give preferential treatment to existing points
            var pointNodesVisual = Context.SelectedObjects
                .FirstOrDefault()?.RenderableComponent as LinePolygon;
            // Check if over an existing point - if so, hide the add button
            // This is handled by the input handler, so we just position the sprite

            // Calculate midpoint of the closest segment
            var p1 = polygon.AbsolutePointAt(closestResult.SegmentIndex);
            var p2 = polygon.AbsolutePointAt(closestResult.SegmentIndex + 1);
            var midpoint = (p1 + p2) / 2.0f;

            _addPointSprite.X = midpoint.X - size / 2.0f;
            _addPointSprite.Y = midpoint.Y - size / 2.0f;
        }

        // Check if cursor is over the sprite
        IsHovered = _addPointSprite.Visible && IsPointOver(cursorX, cursorY);
    }

    private (float Distance, int SegmentIndex) GetClosestLineSegment(
        LinePolygon polygon, float cursorX, float cursorY)
    {
        float minDistance = float.MaxValue;
        int closestIndex = 0;
        Vector2 cursorPosition = new Vector2(cursorX, cursorY);

        for (int i = 0; i < polygon.PointCount - 1; i++)
        {
            var p1 = polygon.AbsolutePointAt(i);
            var p2 = polygon.AbsolutePointAt(i + 1);
            var average = (p1 + p2) / 2.0f;

            float distance = (cursorPosition - average).Length();

            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        return (minDistance, closestIndex);
    }

    /// <summary>
    /// Returns true if the specified point is over the add point sprite.
    /// </summary>
    public bool IsPointOver(float x, float y)
    {
        return _addPointSprite.Visible &&
               x > _addPointSprite.X && x < _addPointSprite.X + _addPointSprite.Width &&
               y > _addPointSprite.Y && y < _addPointSprite.Y + _addPointSprite.Height;
    }

    public override void Destroy()
    {
        SpriteManager.Self.Remove(_addPointSprite);
    }
}
