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

    private readonly IHotkeyManager _hotkeyManager;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly ISelectionManager _selectionManager;
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

    public bool HasMovedEnough => _hasMovedEnough;

    public (float Left, float Top, float Right, float Bottom) Bounds { get; private set; }

    #endregion

    public RectangleSelector(
        IHotkeyManager hotkeyManager,
        IWireframeObjectManager wireframeObjectManager,
        ISelectionManager selectionManager,
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
        // Store push position but don't activate yet - wait for drag
        // This allows shift+click to work for multi-select without interfering
        _startX = worldX;
        _startY = worldY;
        _currentX = worldX;
        _currentY = worldY;
        _isAdditive = _hotkeyManager.MultiSelect.IsPressedInControl();
        _hasMovedEnough = false;
    }

    public void HandleDrag(bool isHandlerActive = false)
    {
        // Don't activate if any input handler is active (resize, rotate, polygon points, etc.)
        if (isHandlerActive) return;

        // Activate rectangle selector only when dragging
        // Check conditions: shift held OR not over element body
        bool shouldActivate = _hotkeyManager.MultiSelect.IsPressedInControl() || !_selectionManager.IsOverBody;

        if (!shouldActivate) return;

        var cursor = InputLibrary.Cursor.Self;
        _currentX = cursor.GetWorldX();
        _currentY = cursor.GetWorldY();

        // Check if moved enough to consider it a drag
        if (!_hasMovedEnough)
        {
            var zoom = 1f;
            if(SystemManagers.Default?.Renderer != null)
            {
                zoom = SystemManagers.Default.Renderer.Camera.Zoom;
            }
            var screenDragDistance = System.Math.Sqrt(
                System.Math.Pow((_currentX - _startX) * zoom, 2) +
                System.Math.Pow((_currentY - _startY) * zoom, 2));

            if (screenDragDistance >= MinimumDragDistance)
            {
                _hasMovedEnough = true;
                _isActive = true; // Activate only when drag threshold is reached
            }
        }

        UpdateBounds();
    }

    public void HandleRelease()
    {
        if (!_isActive)
        {
            // Rectangle selector was never activated (no drag occurred)
            // Reset state and let normal click handling take over
            _hasMovedEnough = false;
            return;
        }

        // Find all elements within the rectangle
        var elementsInBounds = GetElementsInRectangle();

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

        _isActive = false;
        _hasMovedEnough = false;
    }

    public void Update(bool isHandlerActive = false)
    {
        // Update visual visibility
        // Don't show if a handler is active (even if rectangle selector thinks it's active)
        _selectionRectangle.Visible = _isActive && _hasMovedEnough && !isHandlerActive;

        if (_selectionRectangle.Visible)
        {
            var (left, top, right, bottom) = Bounds;
            _selectionRectangle.X = left;
            _selectionRectangle.Y = top;
            _selectionRectangle.Width = right - left;
            _selectionRectangle.Height = bottom - top;
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
