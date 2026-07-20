using System.Collections.Generic;
using System.Linq;
using Gum.Commands;
using Gum.Input;
using Gum.Managers;
using Gum.Wireframe;
using Gum.Wireframe.Editors.Visuals;
using RenderingLibrary;

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
    private readonly Camera _camera;
    private readonly IGumCursorState _cursor;
    private readonly ISelectionRectangleVisual _selectionRectangleVisual;

    private bool _isActive;
    private bool _hasMovedEnough;
    private bool _hasValidPush;
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
        Camera camera,
        IGumCursorState cursor,
        ISelectionRectangleVisual selectionRectangleVisual)
    {
        _hotkeyManager = hotkeyManager;
        _wireframeObjectManager = wireframeObjectManager;
        _selectionManager = selectionManager;
        _guiCommands = guiCommands;
        _camera = camera;
        _cursor = cursor;
        _selectionRectangleVisual = selectionRectangleVisual;
    }

    public void HandlePush(float worldX, float worldY)
    {
        // Store push position but don't activate yet - wait for drag
        // This allows shift+click to work for multi-select without interfering
        _startX = worldX;
        _startY = worldY;
        _currentX = worldX;
        _currentY = worldY;
        _isAdditive = _hotkeyManager.IsPressedInControl(_hotkeyManager.MultiSelect);
        _hasMovedEnough = false;
        _hasValidPush = true;
    }

    public void HandleDrag(bool isHandlerActive = false)
    {
        // Don't activate if there was no valid in-window push to start this drag
        if (!_hasValidPush) return;

        // Don't activate if any input handler is active (resize, rotate, polygon points, etc.)
        if (isHandlerActive) return;

        // Activate rectangle selector only when dragging
        // Check conditions: shift held OR not over element body
        bool shouldActivate = _hotkeyManager.IsPressedInControl(_hotkeyManager.MultiSelect) || !_selectionManager.IsOverBody;

        if (!shouldActivate) return;

        _camera.ScreenToWorld(_cursor.X, _cursor.Y, out _currentX, out _currentY);

        // Check if moved enough to consider it a drag
        if (!_hasMovedEnough)
        {
            var zoom = _camera.Zoom;
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
        _hasValidPush = false;
    }

    public void Update(bool isHandlerActive = false)
    {
        // Update visual visibility
        // Don't show if a handler is active (even if rectangle selector thinks it's active)
        _selectionRectangleVisual.Visible = _isActive && _hasMovedEnough && !isHandlerActive;

        if (_selectionRectangleVisual.Visible)
        {
            var (left, top, right, bottom) = Bounds;
            _selectionRectangleVisual.X = left;
            _selectionRectangleVisual.Y = top;
            _selectionRectangleVisual.Width = right - left;
            _selectionRectangleVisual.Height = bottom - top;
        }
    }

    public GumCursorKind? GetCursorToShow()
    {
        // Show crosshair when shift is held to indicate rectangle select mode
        if (_hotkeyManager.IsPressedInControl(_hotkeyManager.MultiSelect))
        {
            return GumCursorKind.Cross;
        }
        return null;
    }

    public void Destroy()
    {
        _selectionRectangleVisual.Destroy();
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

            // Skip locked instances - they should not be selectable
            if (element.Tag is Gum.DataTypes.InstanceSave { Locked: true })
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
