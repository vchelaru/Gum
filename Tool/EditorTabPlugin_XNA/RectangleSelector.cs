using System.Collections.Generic;
using System.Linq;
using Gum.Commands;
using Gum.Input;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Color = System.Drawing.Color;

namespace Gum;

/// <summary>
/// Handles rectangle selection (drag-to-select) functionality at the SelectionManager level.
/// Allows users to select multiple elements by dragging a rectangle over them.
/// </summary>
public class RectangleSelector
{
    #region Fields

    private readonly HotkeyManager _hotkeyManager;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly SelectionManager _selectionManager;
    private readonly IGuiCommands _guiCommands;
    private readonly Layer _overlayLayer;

    private readonly LineRectangle _selectionRectangle;

    private bool _isActive;
    private bool _hasMovedEnough;
    private float _startX;
    private float _startY;
    private float _currentX;
    private float _currentY;
    private bool _isAdditive;

    private const float MinimumDragDistance = 3; // pixels before considering it a drag

    #endregion

    #region Properties

    public bool IsActive => _isActive;

    public (float Left, float Top, float Right, float Bottom) Bounds { get; private set; }

    #endregion

    public RectangleSelector(
        HotkeyManager hotkeyManager,
        IWireframeObjectManager wireframeObjectManager,
        SelectionManager selectionManager,
        IGuiCommands guiCommands,
        Layer overlayLayer)
    {
        _hotkeyManager = hotkeyManager;
        _wireframeObjectManager = wireframeObjectManager;
        _selectionManager = selectionManager;
        _guiCommands = guiCommands;
        _overlayLayer = overlayLayer;

        // Create visual rectangle
        _selectionRectangle = new LineRectangle();
        _selectionRectangle.Color = Color.DodgerBlue;
        _selectionRectangle.IsDotted = true;
        _selectionRectangle.LinePixelWidth = 1;
        _selectionRectangle.Visible = false;

        ShapeManager.Self.Add(_selectionRectangle, _overlayLayer);
    }

    public void HandlePush(float worldX, float worldY)
    {
        // Only activate if shift is held OR if not over any element body
        // (SelectionManager.IsOverBody tells us if we're over a selected element)
        bool shouldActivate = _hotkeyManager.MultiSelect.IsPressedInControl() || !_selectionManager.IsOverBody;

        if (shouldActivate)
        {
            System.Diagnostics.Debug.WriteLine("RectangleSelector.HandlePush - Starting rectangle selection");

            _isActive = true;
            _hasMovedEnough = false;
            _startX = worldX;
            _startY = worldY;
            _currentX = worldX;
            _currentY = worldY;
            _isAdditive = _hotkeyManager.MultiSelect.IsPressedInControl();

            UpdateBounds();
        }
    }

    public void HandleDrag()
    {
        if (!_isActive) return;

        var cursor = InputLibrary.Cursor.Self;
        _currentX = cursor.GetWorldX();
        _currentY = cursor.GetWorldY();

        // Check if moved enough to consider it a drag
        if (!_hasMovedEnough)
        {
            var screenDragDistance = System.Math.Sqrt(
                System.Math.Pow((_currentX - _startX) * Renderer.Self.Camera.Zoom, 2) +
                System.Math.Pow((_currentY - _startY) * Renderer.Self.Camera.Zoom, 2));

            if (screenDragDistance >= MinimumDragDistance)
            {
                _hasMovedEnough = true;
                System.Diagnostics.Debug.WriteLine("RectangleSelector - Drag threshold reached, showing rectangle");
            }
        }

        UpdateBounds();
    }

    public void HandleRelease()
    {
        if (!_isActive) return;

        System.Diagnostics.Debug.WriteLine($"RectangleSelector.HandleRelease - HasMovedEnough={_hasMovedEnough}");

        if (!_hasMovedEnough)
        {
            // No drag occurred - treat as a click to deselect all (unless shift held)
            if (!_isAdditive)
            {
                _selectionManager.DeselectAll();
            }
        }
        else
        {
            // Find all elements within the rectangle
            var elementsInBounds = GetElementsInRectangle();

            System.Diagnostics.Debug.WriteLine($"RectangleSelector - Found {elementsInBounds.Count} elements in bounds");

            if (_isAdditive)
            {
                // Add/toggle selection
                foreach (var element in elementsInBounds)
                {
                    _selectionManager.ToggleSelection(element);
                }
            }
            else
            {
                // Replace selection
                _selectionManager.Select(elementsInBounds);
            }

            // Refresh UI
            _guiCommands.RefreshVariables();
        }

        _isActive = false;
        _hasMovedEnough = false;
    }

    public void Update()
    {
        // Update visual visibility
        _selectionRectangle.Visible = _isActive && _hasMovedEnough;

        if (_selectionRectangle.Visible)
        {
            var (left, top, right, bottom) = Bounds;
            _selectionRectangle.X = left;
            _selectionRectangle.Y = top;
            _selectionRectangle.Width = right - left;
            _selectionRectangle.Height = bottom - top;

            System.Diagnostics.Debug.WriteLine($"RectangleSelector.Update - Rectangle at L={left:F1}, T={top:F1}, W={right - left:F1}, H={bottom - top:F1}");
        }
    }

    public System.Windows.Forms.Cursor? GetCursorToShow()
    {
        // Show crosshair when shift is held to indicate rectangle select mode
        if (_hotkeyManager.MultiSelect.IsPressedInControl())
        {
            return System.Windows.Forms.Cursors.Cross;
        }
        return null;
    }

    public void Destroy()
    {
        ShapeManager.Self.Remove(_selectionRectangle);
    }

    #region Private Methods

    private void UpdateBounds()
    {
        var left = System.Math.Min(_startX, _currentX);
        var right = System.Math.Max(_startX, _currentX);
        var top = System.Math.Min(_startY, _currentY);
        var bottom = System.Math.Max(_startY, _currentY);

        Bounds = (left, top, right, bottom);
    }

    private List<GraphicalUiElement> GetElementsInRectangle()
    {
        var result = new List<GraphicalUiElement>();
        var (left, top, right, bottom) = Bounds;

        // Get all visible elements in the current screen/component
        var allElements = _wireframeObjectManager.GetAllVisibleElements();

        foreach (var element in allElements)
        {
            // Skip screens - they can't be selected via rectangle
            if (element.Tag is Gum.DataTypes.ScreenSave)
                continue;

            // Check if element bounds intersect with selection rectangle
            if (ElementIntersectsRectangle(element, left, top, right, bottom))
            {
                result.Add(element);
            }
        }

        return result;
    }

    private bool ElementIntersectsRectangle(
        GraphicalUiElement element,
        float left,
        float top,
        float right,
        float bottom)
    {
        // Get element bounds in world coordinates
        var elementLeft = element.GetAbsoluteLeft();
        var elementRight = element.GetAbsoluteRight();
        var elementTop = element.GetAbsoluteTop();
        var elementBottom = element.GetAbsoluteBottom();

        // Check for intersection (not just containment - any overlap counts)
        return !(elementRight < left ||
                 elementLeft > right ||
                 elementBottom < top ||
                 elementTop > bottom);
    }

    #endregion
}
