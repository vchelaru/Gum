# Phase 2A: Extract Polygon Visual Components - Detailed Implementation Plan

This document provides a step-by-step implementation plan for extracting visual components from `PolygonWireframeEditor` into separate, reusable classes implementing `IEditorVisual`.

---

## Overview

Phase 2A is a subset of Phase 2 focused specifically on the `PolygonWireframeEditor`. This smaller scope allows for incremental progress and earlier validation of the visual component architecture.

**Prerequisite:** Phase 1 (Input Handlers) should be complete before Phase 2A.

**Scope:**
- Interface and base class for visual components
- All polygon-specific visuals: origin display, point nodes, add point sprite, selected point highlight
- Integration with PolygonWireframeEditor only

**Out of Scope (deferred to Phase 2B):**
- ResizeHandlesVisual
- RotationHandleVisual  
- DimensionDisplayVisual
- StandardWireframeEditor integration

---

## Step 2A.1: Create the IEditorVisual Interface

**Purpose:** Define a common contract for all visual components that display editor overlays.

**File:** `Editors/Visuals/IEditorVisual.cs`

```csharp
using RenderingLibrary.Graphics;
using System.Collections.Generic;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Defines a visual component that renders editor overlays
/// (e.g., resize handles, dimension displays, origin markers).
/// </summary>
public interface IEditorVisual
{
    /// <summary>
    /// Whether this visual is currently visible.
    /// </summary>
    bool Visible { get; set; }
    
    /// <summary>
    /// Update the visual's state every frame.
    /// Called regardless of selection state.
    /// </summary>
    void Update();
    
    /// <summary>
    /// Update the visual to reflect the current selection.
    /// Called when selection changes.
    /// </summary>
    /// <param name="selectedObjects">The currently selected GraphicalUiElements.</param>
    void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects);
    
    /// <summary>
    /// Clean up resources (shapes, sprites, text, etc.).
    /// </summary>
    void Destroy();
}
```

---

## Step 2A.2: Create EditorVisualBase Abstract Class

**Purpose:** Provide common functionality shared by all visual components.

**File:** `Editors/Visuals/EditorVisualBase.cs`

```csharp
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Collections.Generic;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Base class for visual components providing common functionality.
/// </summary>
public abstract class EditorVisualBase : IEditorVisual
{
    protected EditorContext Context { get; }
    protected Layer OverlayLayer { get; }
    
    private bool _visible = true;
    
    protected EditorVisualBase(EditorContext context)
    {
        Context = context;
        OverlayLayer = context.OverlayLayer;
    }
    
    public virtual bool Visible
    {
        get => _visible;
        set
        {
            if (_visible != value)
            {
                _visible = value;
                OnVisibilityChanged(value);
            }
        }
    }
    
    /// <summary>
    /// Called when visibility changes. Override to update child shape visibility.
    /// </summary>
    protected virtual void OnVisibilityChanged(bool isVisible) { }
    
    public virtual void Update() { }
    
    public virtual void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects) { }
    
    public abstract void Destroy();
    
    #region Helper Methods
    
    /// <summary>
    /// Returns the current camera zoom level.
    /// </summary>
    protected float Zoom => Renderer.Self.Camera.Zoom;
    
    /// <summary>
    /// Scales a size value to be consistent regardless of zoom level.
    /// </summary>
    protected float ScaleByZoom(float sizeAtNoZoom) => sizeAtNoZoom / Zoom;
    
    #endregion
}
```

---

## Step 2A.3: Create OriginDisplayVisual

**Purpose:** Display the origin marker and line connecting to parent origin.

**File:** `Editors/Visuals/OriginDisplayVisual.cs`

**Source code to reference:**
- `PolygonWireframeEditor.originDisplay` (line 25)
- `PolygonWireframeEditor` constructor (line 145)
- `Views/OriginDisplay.cs` (entire file, 220 lines)

```csharp
using RenderingLibrary.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Visual component that displays the origin marker and the line
/// connecting from the object's origin to its parent's origin point.
/// Used primarily for polygon editing.
/// </summary>
public class OriginDisplayVisual : EditorVisualBase
{
    private readonly OriginDisplay _originDisplay;
    
    public OriginDisplayVisual(EditorContext context) : base(context)
    {
        _originDisplay = new OriginDisplay(OverlayLayer);
    }
    
    protected override void OnVisibilityChanged(bool isVisible)
    {
        _originDisplay.Visible = isVisible;
    }
    
    public override void Update()
    {
        if (!Visible || Context.SelectedObjects.Count == 0) return;
        
        _originDisplay.UpdateTo(Context.SelectedObjects.First());
    }
    
    public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        if (selectedObjects.Count == 0)
        {
            Visible = false;
            return;
        }
        
        Visible = true;
        _originDisplay.UpdateTo(selectedObjects.First());
    }
    
    /// <summary>
    /// Set the color of the origin display lines.
    /// </summary>
    public void SetColor(System.Drawing.Color color)
    {
        _originDisplay.SetColor(color);
    }
    
    public override void Destroy()
    {
        _originDisplay.Destroy();
    }
}
```

---

## Step 2A.4: Create PolygonPointNodesVisual

**Purpose:** Display the draggable point nodes for polygon vertices.

**File:** `Editors/Visuals/PolygonPointNodesVisual.cs`

**Source code to extract from:**
- `PolygonWireframeEditor.pointNodes` (line 35)
- `PolygonWireframeEditor.UpdatePointNodes()` (lines 159-196)
- `PolygonWireframeEditor.HandlesActivity()` - point highlighting (lines 408-433)

```csharp
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Visual component that displays draggable nodes for polygon vertices.
/// Each vertex of the polygon gets a small rectangle that can be grabbed.
/// </summary>
public class PolygonPointNodesVisual : EditorVisualBase
{
    private readonly List<SolidRectangle> _pointNodes = new();
    private readonly Layer _layer;
    
    private const float RadiusAtNoZoom = 5;
    
    /// <summary>
    /// The index of the currently highlighted node (cursor over), or null.
    /// </summary>
    public int? HighlightedIndex { get; set; }
    
    /// <summary>
    /// The index of the currently grabbed node (being dragged), or null.
    /// </summary>
    public int? GrabbedIndex { get; set; }
    
    /// <summary>
    /// The index of the currently selected node, or null.
    /// </summary>
    public int? SelectedIndex { get; set; }
    
    private float NodeDisplayWidth => RadiusAtNoZoom * 2 / Zoom;
    
    public PolygonPointNodesVisual(EditorContext context, Layer layer) : base(context)
    {
        _layer = layer;
    }
    
    protected override void OnVisibilityChanged(bool isVisible)
    {
        foreach (var node in _pointNodes)
        {
            node.Visible = isVisible;
        }
    }
    
    public override void Update()
    {
        if (!Visible) return;
        
        UpdateNodePositions();
        UpdateNodeColors();
    }
    
    public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        var selectedPolygon = selectedObjects
            .FirstOrDefault()?.RenderableComponent as LinePolygon;
        
        UpdateNodeCount(selectedPolygon?.PointCount ?? 0);
        
        if (selectedPolygon != null)
        {
            Visible = true;
            UpdateNodePositions();
        }
        else
        {
            Visible = false;
        }
    }
    
    private void UpdateNodeCount(int neededCount)
    {
        // Create needed nodes
        while (_pointNodes.Count < neededCount)
        {
            var rectangle = new SolidRectangle();
            rectangle.Width = NodeDisplayWidth;
            rectangle.Height = NodeDisplayWidth;
            ShapeManager.Self.Add(rectangle, _layer);
            _pointNodes.Add(rectangle);
        }
        
        // Destroy excess nodes
        while (_pointNodes.Count > neededCount)
        {
            var node = _pointNodes.Last();
            ShapeManager.Self.Remove(node);
            _pointNodes.Remove(node);
        }
    }
    
    private void UpdateNodePositions()
    {
        var selectedPolygon = Context.SelectedObjects
            .FirstOrDefault()?.RenderableComponent as LinePolygon;
        
        if (selectedPolygon == null) return;
        
        var nodeDimension = NodeDisplayWidth;
        
        for (int i = 0; i < selectedPolygon.PointCount && i < _pointNodes.Count; i++)
        {
            var point = selectedPolygon.AbsolutePointAt(i);
            
            _pointNodes[i].X = point.X - nodeDimension / 2;
            _pointNodes[i].Y = point.Y - nodeDimension / 2;
            _pointNodes[i].Width = nodeDimension;
            _pointNodes[i].Height = nodeDimension;
        }
    }
    
    private void UpdateNodeColors()
    {
        for (int i = 0; i < _pointNodes.Count; i++)
        {
            bool isHighlighted = i == HighlightedIndex || i == GrabbedIndex;
            
            // Also highlight last point if first is highlighted (for closed polygons)
            if (HighlightedIndex == 0 && i == _pointNodes.Count - 1)
            {
                isHighlighted = true;
            }
            
            _pointNodes[i].Color = isHighlighted ? Color.Yellow : Color.Gray;
        }
    }
    
    public override void Destroy()
    {
        foreach (var node in _pointNodes)
        {
            ShapeManager.Self.Remove(node);
        }
        _pointNodes.Clear();
    }
    
    #region Query Methods (for PolygonPointInputHandler)
    
    /// <summary>
    /// Gets the index of the point at the specified coordinates, or null.
    /// </summary>
    public int? GetIndexOver(float worldX, float worldY)
    {
        for (int i = 0; i < _pointNodes.Count; i++)
        {
            var node = _pointNodes[i];
            if (worldX >= node.X && worldX <= node.X + node.Width &&
                worldY >= node.Y && worldY <= node.Y + node.Height)
            {
                return i;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Gets the number of point nodes.
    /// </summary>
    public int PointCount => _pointNodes.Count;
    
    #endregion
}
```

---

## Step 2A.5: Create AddPointSpriteVisual

**Purpose:** Display the "add point" button that appears when hovering between polygon vertices.

**File:** `Editors/Visuals/AddPointSpriteVisual.cs`

**Source code to extract from:**
- `PolygonWireframeEditor.addPointSprite` (line 37)
- `PolygonWireframeEditor.addPointTexture` (line 36)
- `PolygonWireframeEditor` constructor (lines 118-139)
- `PolygonWireframeEditor.UpdateAddPointSprite()` (lines 254-291)
- `PolygonWireframeEditor.IsPointOverAddPointSprite()` (lines 91-95)

```csharp
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
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
        
        for (int i = 0; i < polygon.PointCount - 1; i++)
        {
            var p1 = polygon.AbsolutePointAt(i);
            var p2 = polygon.AbsolutePointAt(i + 1);
            
            float distance = DistanceToLineSegment(cursorX, cursorY, p1, p2);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }
        
        return (minDistance, closestIndex);
    }
    
    private static float DistanceToLineSegment(float px, float py, Vector2 p1, Vector2 p2)
    {
        var dx = p2.X - p1.X;
        var dy = p2.Y - p1.Y;
        var lengthSquared = dx * dx + dy * dy;
        
        if (lengthSquared == 0)
        {
            return (float)System.Math.Sqrt((px - p1.X) * (px - p1.X) + (py - p1.Y) * (py - p1.Y));
        }
        
        var t = System.Math.Max(0, System.Math.Min(1, 
            ((px - p1.X) * dx + (py - p1.Y) * dy) / lengthSquared));
        
        var projX = p1.X + t * dx;
        var projY = p1.Y + t * dy;
        
        return (float)System.Math.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
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
```

---

## Step 2A.6: Create SelectedPointHighlightVisual

**Purpose:** Display a highlight rectangle around the currently selected polygon point.

**File:** `Editors/Visuals/SelectedPointHighlightVisual.cs`

**Source code to extract from:**
- `PolygonWireframeEditor.selectedPointLineRectangle` (line 39)
- `PolygonWireframeEditor` constructor (lines 141-144)
- `PolygonWireframeEditor.UpdateSelectedNodeLineRectangle()` (lines 230-251)

```csharp
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Visual component that displays a highlight rectangle around
/// the currently selected polygon vertex.
/// </summary>
public class SelectedPointHighlightVisual : EditorVisualBase
{
    private readonly LineRectangle _highlightRectangle;
    
    private const float RadiusAtNoZoom = 5;
    private const float PaddingAtNoZoom = 6;
    private const float LinePixelWidth = 3;
    
    /// <summary>
    /// The index of the currently selected point, or null for no selection.
    /// </summary>
    public int? SelectedIndex { get; set; }
    
    private float NodeDisplayWidth => RadiusAtNoZoom * 2 / Zoom;
    
    public SelectedPointHighlightVisual(EditorContext context, Layer layer) : base(context)
    {
        _highlightRectangle = new LineRectangle();
        _highlightRectangle.Color = Color.Magenta;
        _highlightRectangle.IsDotted = false;
        _highlightRectangle.LinePixelWidth = LinePixelWidth;
        _highlightRectangle.Visible = false;
        
        ShapeManager.Self.Add(_highlightRectangle, layer);
    }
    
    protected override void OnVisibilityChanged(bool isVisible)
    {
        _highlightRectangle.Visible = isVisible;
    }
    
    public override void Update()
    {
        var selectedPolygon = Context.SelectedObjects
            .FirstOrDefault()?.RenderableComponent as LinePolygon;
        
        var hasSelection = SelectedIndex != null && 
                          selectedPolygon != null && 
                          SelectedIndex < selectedPolygon.PointCount;
        
        _highlightRectangle.Visible = hasSelection;
        
        if (hasSelection)
        {
            UpdatePosition(selectedPolygon!, SelectedIndex!.Value);
        }
    }
    
    private void UpdatePosition(LinePolygon polygon, int index)
    {
        var padding = PaddingAtNoZoom / Zoom;
        var highlightSize = NodeDisplayWidth + padding;
        
        _highlightRectangle.Width = highlightSize;
        _highlightRectangle.Height = highlightSize;
        
        var vertexPosition = polygon.AbsolutePointAt(index);
        
        _highlightRectangle.X = vertexPosition.X - highlightSize / 2;
        _highlightRectangle.Y = vertexPosition.Y - highlightSize / 2;
    }
    
    public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        // Clear selection when selection changes
        SelectedIndex = null;
        _highlightRectangle.Visible = false;
    }
    
    public override void Destroy()
    {
        ShapeManager.Self.Remove(_highlightRectangle);
    }
}
```

---

## Step 2A.7: Add Visual Support to WireframeEditor Base Class

**File:** `Editors/WireframeEditor.cs`

Add visual component collection and lifecycle management:

```csharp
using Gum.Wireframe.Editors.Visuals;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Wireframe;

public abstract class WireframeEditor
{
    protected EditorContext Context { get; }
    protected List<IInputHandler> InputHandlers { get; } = new();
    protected List<IEditorVisual> Visuals { get; } = new();
    
    private IInputHandler? _activeHandler;
    
    protected WireframeEditor(EditorContext context)
    {
        Context = context;
    }
    
    // ... existing HasCursorOver from Phase 1 ...
    
    public virtual void Activity(ICollection<GraphicalUiElement> selectedObjects, SystemManagers systemManagers)
    {
        if (selectedObjects.Count == 0) return;
        
        var cursor = InputLibrary.Cursor.Self;
        var worldX = cursor.GetWorldX();
        var worldY = cursor.GetWorldY();
        
        // Update all hover states (handlers)
        foreach (var handler in InputHandlers)
        {
            handler.UpdateHover(worldX, worldY);
        }
        
        // Handle push - find handler to activate
        if (cursor.PrimaryPush)
        {
            foreach (var handler in InputHandlers.OrderByDescending(h => h.Priority))
            {
                if (handler.HandlePush(worldX, worldY))
                {
                    _activeHandler = handler;
                    break;
                }
            }
        }
        
        // Handle drag
        if (cursor.PrimaryDown && _activeHandler != null)
        {
            _activeHandler.HandleDrag();
        }
        
        // Handle click (release)
        if (cursor.PrimaryClick && _activeHandler != null)
        {
            _activeHandler.HandleClick();
            _activeHandler = null;
        }
        
        // Update all visual components
        foreach (var visual in Visuals)
        {
            visual.Update();
        }
    }
    
    public virtual void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        Context.SelectedObjects.Clear();
        Context.SelectedObjects.AddRange(selectedObjects);
        
        // Notify handlers
        foreach (var handler in InputHandlers)
        {
            handler.OnSelectionChanged();
        }
        
        // Notify visuals
        foreach (var visual in Visuals)
        {
            visual.UpdateToSelection(selectedObjects);
        }
    }
    
    public virtual void Destroy()
    {
        foreach (var visual in Visuals)
        {
            visual.Destroy();
        }
    }
    
    // ... rest of existing methods ...
}
```

---

## Step 2A.8: Refactor PolygonWireframeEditor to Use Visuals

**File:** `Editors/PolygonWireframeEditor.cs`

After applying visual extractions:

```csharp
using Gum.Wireframe.Editors.Handlers;
using Gum.Wireframe.Editors.Visuals;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Collections.Generic;

namespace Gum.Wireframe.Editors;

public class PolygonWireframeEditor : WireframeEditor
{
    // Visual references for handler access
    private readonly PolygonPointNodesVisual _pointNodesVisual;
    private readonly AddPointSpriteVisual _addPointSpriteVisual;
    private readonly SelectedPointHighlightVisual _selectedPointHighlightVisual;
    private readonly OriginDisplayVisual _originDisplayVisual;
    
    public PolygonWireframeEditor(EditorContext context, Layer layer) : base(context)
    {
        // Create visual components
        _pointNodesVisual = new PolygonPointNodesVisual(context, layer);
        _addPointSpriteVisual = new AddPointSpriteVisual(context, layer);
        _selectedPointHighlightVisual = new SelectedPointHighlightVisual(context, layer);
        _originDisplayVisual = new OriginDisplayVisual(context);
        
        // Register visuals
        Visuals.Add(_pointNodesVisual);
        Visuals.Add(_addPointSpriteVisual);
        Visuals.Add(_selectedPointHighlightVisual);
        Visuals.Add(_originDisplayVisual);
        
        // Create and register input handlers
        // Polygon point handler receives visual references for hit testing
        InputHandlers.Add(new PolygonPointInputHandler(
            context, _pointNodesVisual, _addPointSpriteVisual, _selectedPointHighlightVisual));
        InputHandlers.Add(new MoveInputHandler(context));
    }
    
    public override bool TryHandleDelete()
    {
        foreach (var handler in InputHandlers)
        {
            if (handler.TryHandleDelete())
            {
                return true;
            }
        }
        return false;
    }
}
```

---

## Testing Checklist

After implementing Phase 2A, verify:

- [ ] **OriginDisplayVisual**
  - [ ] Line connects to parent origin
  - [ ] Handles rotated parents correctly
  - [ ] Updates with stacked layouts
  
- [ ] **PolygonPointNodesVisual**
  - [ ] Correct number of nodes displayed
  - [ ] Nodes position at vertices
  - [ ] Highlight on hover
  - [ ] First/last node sync for closed polygons
  
- [ ] **AddPointSpriteVisual**
  - [ ] Appears when hovering between vertices
  - [ ] Hides when dragging
  - [ ] Correct position at segment midpoint
  
- [ ] **SelectedPointHighlightVisual**
  - [ ] Appears around selected vertex
  - [ ] Follows vertex during drag
  - [ ] Clears on selection change

---

## New Files Summary

| File | Purpose |
|------|---------|
| `Editors/Visuals/IEditorVisual.cs` | Interface for all visual components |
| `Editors/Visuals/EditorVisualBase.cs` | Base class with common functionality |
| `Editors/Visuals/OriginDisplayVisual.cs` | Origin to parent line |
| `Editors/Visuals/PolygonPointNodesVisual.cs` | Polygon vertex nodes |
| `Editors/Visuals/AddPointSpriteVisual.cs` | Add point button |
| `Editors/Visuals/SelectedPointHighlightVisual.cs` | Selected point highlight |

---

## Estimated Effort

| Step | Estimated Time | Risk |
|------|---------------|------|
| Step 2A.1-2A.2 (Interfaces) | 30 min | Low |
| Step 2A.3 (OriginDisplayVisual) | 30 min | Low |
| Step 2A.4 (PolygonPointNodesVisual) | 1 hour | Medium |
| Step 2A.5 (AddPointSpriteVisual) | 1 hour | Medium |
| Step 2A.6 (SelectedPointHighlightVisual) | 30 min | Low |
| Step 2A.7-2A.8 (Integration) | 1.5 hours | Medium |
| Testing & Bug Fixes | 1-2 hours | Medium |
| **Total** | **5-7 hours** | |

---

## Next Phase

After Phase 2A is complete, Phase 2B will extract the remaining visual components for `StandardWireframeEditor`:
- ResizeHandlesVisual
- RotationHandleVisual
- DimensionDisplayVisual
- StandardWireframeEditor integration
