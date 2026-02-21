using Gum.Commands;
using Gum.DataTypes;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe.Editors;
using HarfBuzzSharp;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.RightsManagement;
using System.Windows.Forms;
using System.Windows.Input;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;
using WinCursor = System.Windows.Forms.Cursor;

namespace Gum.Wireframe;

public interface ISelectionManager
{
    bool IsOverBody { get; set; }
    void DeselectAll();
    void ToggleSelection(GraphicalUiElement element);
    void Select(IEnumerable<GraphicalUiElement> elements);
}

public class SelectionManager : ISelectionManager
{
    #region GetFocusedControl interop implementation

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
    internal static extern IntPtr GetFocus();

    private Control GetFocusedControl()
    {
        Control focusedControl = null;
        // To get hold of the focused control:
        IntPtr focusedHandle = GetFocus();
        if (focusedHandle != IntPtr.Zero)
            // Note that if the focused Control is not a .Net control, then this will return null.
            focusedControl = Control.FromHandle(focusedHandle);
        return focusedControl;
    }



    #endregion

    #region Fields

    LayerService _layerService;

    public WireframeEditor? WireframeEditor;

    private RectangleSelector? _rectangleSelector;

    List<GraphicalUiElement> mSelectedIpsos = new List<GraphicalUiElement>();
    IPositionedSizedObject? mHighlightedIpso;

    public event Action<IPositionedSizedObject?>? HighlightedIpsoChanged;

    GraphicalOutline mGraphicalOutline;


    HighlightManager highlightManager;

    #endregion

    #region Properties


    public InputLibrary.Cursor Cursor
    {
        get
        {
            return InputLibrary.Cursor.Self;
        }
    }


    private readonly ISelectedState _selectedState;
    private readonly IEditingManager _editingManager;
    private readonly IUndoManager _undoManager;
    private readonly IDialogService _dialogService;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IVariableInCategoryPropagationLogic _variableInCategoryPropagationLogic;
    private readonly IProjectManager _projectManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IElementCommands _elementCommands;
    private readonly IFileCommands _fileCommands;
    private readonly ISetVariableLogic _setVariableLogic;
    private readonly IUiSettingsService _uiSettingsService;

    public virtual bool IsOverBody
    {
        get;
        set;
    }

    public GraphicalUiElement? SelectedGue
    {
        get
        {
            if (mSelectedIpsos.Count == 0)
            {
                return null;
            }
            else
            {
                return mSelectedIpsos[0];
            }
        }
        set
        {
            mSelectedIpsos.Clear();

            if (value != null)
            {
                mSelectedIpsos.Add(value);
            }
            UpdateEditorsToSelection();
        }
    }

    public List<GraphicalUiElement> SelectedGues
    {
        get
        {
            return mSelectedIpsos;
        }
        private set
        {
            mSelectedIpsos.Clear();
            mSelectedIpsos.AddRange(value);
            UpdateEditorsToSelection();
        }
    }

    public IPositionedSizedObject? HighlightedIpso
    {
        get
        {
            return mHighlightedIpso;
        }
        set
        {
            highlightManager.HighlightedIpso = value;
            if (mHighlightedIpso != value)
            {
                if (mHighlightedIpso != null)
                {
                    highlightManager.UnhighlightIpso(mHighlightedIpso as GraphicalUiElement);
                }

                mHighlightedIpso = value;

                mGraphicalOutline.HighlightedIpso = mHighlightedIpso as GraphicalUiElement;

                HighlightedIpsoChanged?.Invoke(value);
            }
        }
    }

    public bool HasSelection
    {
        get
        {
            return mSelectedIpsos.Any();
        }
    }

    public bool IsShiftDown
    {
        get
        {
            return ((Control.ModifierKeys & Keys.Shift) != 0);
        }
    }

    bool restrictToUnitValues;
    public bool RestrictToUnitValues
    {
        get { return restrictToUnitValues; }
        set
        {
            restrictToUnitValues = value;
            if (WireframeEditor != null)
            {
                WireframeEditor.RestrictToUnitValues = value;
            }
        }
    }

    public bool AreHighlightsVisible
    {
        get => highlightManager.AreHighlightsVisible;
        set => highlightManager.AreHighlightsVisible = value;
    }

    // This is used to punch through the selected and go back up to the top. More info here:
    // https://github.com/vchelaru/Gum/issues/1810
    public bool IsComponentNoInstanceSelected => _selectedState.SelectedInstance == null && _selectedState.SelectedComponent != null;

    #endregion

    #region Methods

    public SelectionManager(ISelectedState selectedState,
        IUndoManager undoManager,
        IEditingManager editingManager,
        IDialogService dialogService,
        IHotkeyManager hotkeyManager,
        IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        IWireframeObjectManager wireframeObjectManager,
        IProjectManager projectManager,
        IGuiCommands guiCommands,
        IElementCommands elementCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        IUiSettingsService uiSettingsService)
    {
        _selectedState = selectedState;
        _editingManager = editingManager;
        _undoManager = undoManager;
        _dialogService = dialogService;
        _hotkeyManager = hotkeyManager;
        _wireframeObjectManager = wireframeObjectManager;
        _variableInCategoryPropagationLogic = variableInCategoryPropagationLogic;
        _projectManager = projectManager;
        _guiCommands = guiCommands;
        _elementCommands = elementCommands;
        _fileCommands = fileCommands;
        _setVariableLogic = setVariableLogic;
        _uiSettingsService = uiSettingsService;
    }

    public void Initialize(LayerService layerService)
    {
        _layerService = layerService;
        var overlayLayer = layerService.OverlayLayer;

        mGraphicalOutline = new GraphicalOutline(overlayLayer);

        highlightManager = new HighlightManager(overlayLayer);

        // Initialize rectangle selector for drag-to-select functionality
        _rectangleSelector = new RectangleSelector(
            _hotkeyManager,
            _wireframeObjectManager,
            this,
            _guiCommands,
            overlayLayer);
    }

    /// <summary>
    /// Attempts to perform a delete given the current selection. This allows
    /// for custom deletes, such as deleting a point on a polygon. Most of
    /// the time, this does nothing and returns false.
    /// </summary>
    /// <returns>Whether something was deleted in the selection.</returns>
    public bool TryHandleDelete()
    {
        var toReturn = false;
        if (WireframeEditor != null)
        {
            toReturn = WireframeEditor.TryHandleDelete();
        }
        return toReturn;
    }

    public void Activity(bool forceNoHighlight)
    {
        try
        {
            // Always check this even if the cursor isn't over the window because other windows (like
            // the texture coordinate seleciton plugin window) can change the texture coordinates and we
            // want the highlight to update:
            //if (Cursor.IsInWindow && _selectedState.SelectedElement != null)
            if (_selectedState.SelectedElement != null)
            {
                HighlightActivity(forceNoHighlight);

                SelectionActivity();
            }
            //else if (!Cursor.IsInWindow)
            //{
            // the element view window can also highlight, so we don't want to do this:
            //HighlightedIpso = null;
            //}
        }
        catch (Exception e)
        {
            _dialogService.ShowMessage("Error in SelectionManager.Activity:\n\n" + e.ToString());
        }
    }

    public void LateActivity(SystemManagers systemManagers)
    {
        // Only update visuals here - input processing happens in SelectionActivity via ProcessInputForSelection
        WireframeEditor?.UpdateVisuals(SelectedGues);

        // Update rectangle selector visual
        // Pass handler active state to ensure rectangle doesn't show when handles are being used
        bool isHandlerActive = WireframeEditor?.IsAnyHandlerActive == true;
        _rectangleSelector?.Update(isHandlerActive);
    }

    public void Deselect()
    {
        SelectedGue = null;
    }

    /// <summary>
    /// Deselects all currently selected elements.
    /// </summary>
    public virtual void DeselectAll()
    {
        // Clear the underlying state first (important for multiple selections)
        _selectedState.SelectedInstance = null;

        // Then clear the local selection and update editors
        SelectedGue = null;
    }

    /// <summary>
    /// Selects the specified elements, replacing the current selection.
    /// </summary>
    public void Select(IEnumerable<GraphicalUiElement> elements)
    {
        if (elements == null || !elements.Any())
        {
            DeselectAll();
            return;
        }

        var elementList = elements.ToList();

        // Convert GraphicalUiElements to InstanceSaves
        var instances = new List<InstanceSave>();
        foreach (var element in elementList)
        {
            if (element.Tag is InstanceSave instance)
            {
                instances.Add(instance);
            }
        }

        if (instances.Any())
        {
            _selectedState.SelectedInstances = instances;

            var elementStack = _selectedState.GetTopLevelElementStack();
            var selectedGues = new List<GraphicalUiElement>();
            foreach (var instance in instances)
            {
                var gue = _wireframeObjectManager.GetRepresentation(instance, elementStack);
                if (gue != null)
                {
                    selectedGues.Add(gue);
                }
            }
            SelectedGues = selectedGues;
        }
    }

    /// <summary>
    /// Toggles the selection state of the specified element.
    /// If selected, deselects it. If not selected, selects it.
    /// </summary>
    public void ToggleSelection(GraphicalUiElement element)
    {
        if (element?.Tag is InstanceSave instance)
        {
            var currentInstances = _selectedState.SelectedInstances.ToList();

            if (currentInstances.Contains(instance))
            {
                // Deselect
                currentInstances.Remove(instance);
            }
            else
            {
                // Select
                currentInstances.Add(instance);
            }

            if (currentInstances.Any())
            {
                _selectedState.SelectedInstances = currentInstances;

                var elementStack = _selectedState.GetTopLevelElementStack();
                var selectedGues = new List<GraphicalUiElement>();
                foreach (var inst in currentInstances)
                {
                    var gue = _wireframeObjectManager.GetRepresentation(inst, elementStack);
                    if (gue != null)
                    {
                        selectedGues.Add(gue);
                    }
                }
                SelectedGues = selectedGues;
            }
            else
            {
                DeselectAll();
            }
        }
    }

    /// <summary>
    /// Performs every-frame highlight activity.
    /// </summary>
    /// <param name="forceNoHighlight">If force no highlight, the highlight will not act as if it's over anything. This is used
    /// to un-highlight anything if the cursor is outside of the window</param>
    void HighlightActivity(bool forceNoHighlight)
    {
        if (!InputLibrary.Cursor.Self.PrimaryDownIgnoringIsInWindow)
        {
            // There is currently a known
            // bug where the user can click+drag
            // on the divider and move "in" to the
            // wireframe window and the cursor will
            // change.

            var cursorToSet = System.Windows.Forms.Cursors.Arrow;

            float worldXAt = Cursor.GetWorldX();
            float worldYAt = Cursor.GetWorldY();

            IPositionedSizedObject representationOver = null;

            if (_editingManager.ContextMenuStrip?.Visible == true)
            {
                // do nothing!
            }
            else
            {
                // Check rectangle selector cursor first (for shift+drag mode indication)
                if (_rectangleSelector != null)
                {
                    var rectangleCursor = _rectangleSelector.GetCursorToShow();
                    if (rectangleCursor != null)
                    {
                        cursorToSet = rectangleCursor;
                    }
                }

                if (WireframeEditor != null)
                {
                    cursorToSet = WireframeEditor.GetWindowsCursorToShow(cursorToSet, worldXAt, worldYAt);
                }

                #region Selecting element activity

                if (forceNoHighlight)
                {
                    IsOverBody = false;
                }

                if (forceNoHighlight == false)
                {

                    if (WireframeEditor?.HasCursorOverHandles == true)
                    {
                        representationOver = _wireframeObjectManager.GetSelectedRepresentation();
                        IsOverBody = false;
                    }
                    else
                    {
                        if (IsOverBody && Cursor.PrimaryDown)
                        {
                            cursorToSet = System.Windows.Forms.Cursors.SizeAll;
                            representationOver = _wireframeObjectManager.GetSelectedRepresentation();
                        }
                        else
                        {
                            List<ElementWithState> elementStack = new List<ElementWithState>();
                            elementStack.Add(new ElementWithState(_selectedState.SelectedElement));

                            representationOver =
                                GetRepresentationAt(worldXAt, worldYAt, IsComponentNoInstanceSelected, elementStack);

                            if (representationOver != null)
                            {
                                cursorToSet = System.Windows.Forms.Cursors.SizeAll;
                                IsOverBody = true;
                            }
                            else
                            {
                                IsOverBody = false;
                            }
                        }
                    }
                }
                #endregion
            }


            if (representationOver != null && representationOver is NineSlice)
            {
                // This function updates the sizes and texture coordinates of the 
                // highlighted representation if it's a NineSlice.  This is needed before
                // we set the HighlightedIpso and before we update the highlight objects
                (representationOver as NineSlice).RefreshTextureCoordinatesAndSpriteSizes();
            }



            // We used to not check this, but we have to now because the cursor might be 
            if (Cursor.IsInWindow)
            {
                Cursor.SetWinformsCursor(cursorToSet);

                // We don't want to show the highlight when the user is performing some kind of editing.
                // Therefore make sure the cursor isn't down.
                if (representationOver != null && Cursor.PrimaryDown == false)
                {
                    HighlightedIpso = representationOver;

                    mGraphicalOutline.UpdateHighlightElements();
                }
                else
                {
                    HighlightedIpso = null;
                }
            }
        }
        else if (InputLibrary.Cursor.Self.PrimaryDown && Cursor.IsInWindow)
        {
            // We only want to hide it if the user is holding the cursor down over the wireframe window.
            HighlightedIpso = null;
        }

        if (forceNoHighlight)
        {
            HighlightedIpso = null;
        }

        highlightManager.UpdateHighlightObjects();
    }

    public GraphicalUiElement GetRepresentationAt(float x, float y, bool trySkipSelected, List<ElementWithState> elementStack)
    {
        GraphicalUiElement ipsoOver = null;

        // First check if we're over the current
        var selectedRepresentations = _wireframeObjectManager.GetSelectedRepresentations();

        int indexToStartAt = -1;
        if (trySkipSelected)
        {
            if (selectedRepresentations?.Length > 0)
            {
                indexToStartAt = _wireframeObjectManager.AllIpsos.IndexOf(selectedRepresentations.First());
            }
        }
        else
        {
            if ((selectedRepresentations?.FirstOrDefault()?.Tag is ScreenSave) == false)
            {
                if (selectedRepresentations != null)
                {
                    foreach (var selectedRepresentation in selectedRepresentations)
                    {
                        // If this is a container, and dotted lines are not drawn, then this has no renderable component:
                        if (selectedRepresentation?.RenderableComponent != null)
                        {
                            var hasCursorOver = false;

                            if (selectedRepresentation.RenderableComponent is LinePolygon)
                            {
                                hasCursorOver = (selectedRepresentation.RenderableComponent as LinePolygon).IsPointInside(x, y);
                            }
                            else
                            {
                                hasCursorOver = selectedRepresentation.HasCursorOver(x, y);
                            }

                            if (hasCursorOver)
                            {
                                ipsoOver = selectedRepresentation;
                                break;
                            }

                        }
                    }
                }
            }
        }

        if (ipsoOver == null)
        {
            // We used to loop through the IPSOs that are part of the WireframeObjectManager,
            // but we want to do it in the order that they appear in the
            // Renderer.  So we'll just loop through the IPositionedSizedObjects
            // in the Renderer's Layer[0], test if they're part of one of the WireframeObjectManager's
            // lists, and if so, check collision.

            if (indexToStartAt == -1)
            {
                indexToStartAt = _wireframeObjectManager.AllIpsos.Count;
            }

            #region First check only visible objects
            ipsoOver = ReverseLoopToFindIpso(x, y, indexToStartAt - 1, -1, true, elementStack);

            if (ipsoOver == null && indexToStartAt != _wireframeObjectManager.AllIpsos.Count - 1)
            {
                ipsoOver = ReverseLoopToFindIpso(x, y, _wireframeObjectManager.AllIpsos.Count - 1, indexToStartAt, true, elementStack);
            }
            #endregion

            #region If none were found, check invisible objects

            if (ipsoOver == null)
            {
                ipsoOver = ReverseLoopToFindIpso(x, y, indexToStartAt - 1, -1, false, elementStack);

                if (ipsoOver == null && indexToStartAt != _wireframeObjectManager.AllIpsos.Count - 1)
                {
                    ipsoOver = ReverseLoopToFindIpso(x, y, _wireframeObjectManager.AllIpsos.Count - 1, indexToStartAt, false, elementStack);
                }


            }

            #endregion
        }

        // If we didn't find anything and we are skipping selected, try again without skipping selected:
        if(trySkipSelected && ipsoOver == null)
        {
            ipsoOver = GetRepresentationAt(x, y, trySkipSelected: false, elementStack);
        }

        // Right now we're going to assume that we only want to select IPSOs that represent the current
        // element or its InstanceSaves - not any children.  So we're going to get the InstanceSave - if that's
        // null, get the ElementSave
        if (ipsoOver != null)
        {
            InstanceSave instance;
            ElementSave element;

            GetElementOrInstanceForIpso(ipsoOver, elementStack, out instance, out element);

            if (instance != null)
            {
                ipsoOver = _wireframeObjectManager.GetRepresentation(instance, elementStack);
            }
            else if (element != null) // both may be null if the user drag+dropped onto the wireframe window
            {
                try
                {
                    ipsoOver = _wireframeObjectManager.GetRepresentation(element);
                }
                catch (Exception)
                {
                    throw;
                }
            }

        }

        return ipsoOver;
    }

    private GraphicalUiElement ReverseLoopToFindIpso(float x, float y, int indexToStartAt, int indexToEndAt, bool visibleToCheck, List<ElementWithState> elementStack)
    {
        GraphicalUiElement ipsoOver = null;

        if (indexToEndAt < -1)
        {
            throw new Exception("Index cannot be less than -1");
        }
        if (indexToStartAt >= _wireframeObjectManager.AllIpsos.Count)
        {
            throw new Exception("Index must be less than the AllIpsos Count");
        }

        // Let's try to get visible ones first, then if we don't find anything, look at invisible ones
        for (int i = indexToStartAt; i > indexToEndAt; i--)
        {

            GraphicalUiElement graphicalUiElement = _wireframeObjectManager.AllIpsos[i];
            bool skip = graphicalUiElement.Tag is ScreenSave;
            if (!skip)
            {
                bool visible = IsIpsoVisible(graphicalUiElement);


                if (visible == visibleToCheck)
                {
                    var hasCursorOver = false;

                    if (graphicalUiElement.RenderableComponent is LinePolygon)
                    {
                        hasCursorOver = (graphicalUiElement.RenderableComponent as LinePolygon).IsPointInside(x, y);
                    }
                    else
                    {
                        hasCursorOver = graphicalUiElement.HasCursorOver(x, y);
                    }

                    if (hasCursorOver && (_wireframeObjectManager.IsRepresentation(graphicalUiElement)))
                    {

                        // hold on, even though this is a valid IPSO and the cursor is over it, we gotta see if
                        // it's an instance that is locked.  If so, we shouldn't select it!
                        var instanceSave = graphicalUiElement.Tag as InstanceSave;
                        if (instanceSave == null || instanceSave.Locked == false)
                        {
                            ipsoOver = graphicalUiElement;
                            break;
                        }
                    }
                }
            }
        }

        return ipsoOver;
    }

    private static bool IsIpsoVisible(IPositionedSizedObject ipso)
    {
        bool isVisible = true;
        if (ipso is GraphicalUiElement)
        {
            if (ipso.Tag == null || ipso.Tag is ScreenSave == false)
            {
                // If this has no object, then just treat it as true:
                try
                {
                    isVisible = ((IVisible)ipso).AbsoluteVisible;
                }
                catch
                {
                    isVisible = true;
                }
            }
            else
            {
                isVisible = false;
            }
        }
        else if (ipso is IVisible asIVisible)
        {
            isVisible = (asIVisible).AbsoluteVisible;
        }
        else if (ipso is Sprite asSprite)
        {
            isVisible = (asSprite).AbsoluteVisible;
        }
        else if (ipso is Text asText)
        {
            isVisible = (asText).AbsoluteVisible;
        }

        return isVisible;
    }

    List<GraphicalUiElement> _emptyGraphicalUiElementList = new List<GraphicalUiElement>();
    private void UpdateEditorsToSelection()
    {
        if (SelectedGues.Count == 1 &&
            SelectedGue?.Tag is InstanceSave instanceSaveTag &&
            ObjectFinder.Self.GetRootStandardElementSave(instanceSaveTag)?.Name == "Polygon")
        {
            // use the Polygon wireframe editor
            if (WireframeEditor is PolygonWireframeEditor == false)
            {
                if (WireframeEditor != null)
                {
                    WireframeEditor.Destroy();
                }
                CreatePolygonWireframeEditor();
            }
        }
        else if (SelectedGues.Count > 0 && SelectedGue?.Tag is ScreenSave == false)
        {
            var tag = SelectedGue.Tag as ElementSave;

            var isPolygon = false;
            if (ObjectFinder.Self.GetRootStandardElementSave(tag)?.Name == "Polygon")
            {
                isPolygon = true;
            }

            if (isPolygon)
            {
                if (WireframeEditor != null)
                {
                    WireframeEditor.Destroy();
                }
                CreatePolygonWireframeEditor();
            }
            else
            {
                if (WireframeEditor is StandardWireframeEditor == false)
                {
                    if (WireframeEditor != null)
                    {
                        WireframeEditor.Destroy();
                    }

                    var lineColor = Color.FromArgb(255, _projectManager.GeneralSettingsFile.GuideLineColorR,
                        _projectManager.GeneralSettingsFile.GuideLineColorG,
                        _projectManager.GeneralSettingsFile.GuideLineColorB);

                    var textColor = Color.FromArgb(255, _projectManager.GeneralSettingsFile.GuideTextColorR,
                        _projectManager.GeneralSettingsFile.GuideTextColorG,
                        _projectManager.GeneralSettingsFile.GuideTextColorB);

                    WireframeEditor = new StandardWireframeEditor(
                        _layerService.OverlayLayer,
                        lineColor,
                        textColor,
                        _hotkeyManager,
                        this,
                        _selectedState,
                        _elementCommands,
                        _guiCommands,
                        _fileCommands,
                        _setVariableLogic,
                        _undoManager,
                        _variableInCategoryPropagationLogic,
                        _wireframeObjectManager,
                        _uiSettingsService);
                }
            }
        }
        else if (WireframeEditor != null)
        {
            WireframeEditor.Destroy();
            WireframeEditor = null;
        }

        if (WireframeEditor != null)
        {
            if (_selectedState.CustomCurrentStateSave != null)
            {
                WireframeEditor.UpdateToSelection(_emptyGraphicalUiElementList);
            }
            else
            {
                WireframeEditor.UpdateToSelection(mSelectedIpsos);
            }
            WireframeEditor.RestrictToUnitValues = RestrictToUnitValues;
        }
    }

    private void CreatePolygonWireframeEditor()
    {
        WireframeEditor = new PolygonWireframeEditor(
            _layerService.OverlayLayer,
            _hotkeyManager,
            this,
            _selectedState,
            _elementCommands,
            _guiCommands,
            _fileCommands,
            _setVariableLogic,
            _undoManager,
            _variableInCategoryPropagationLogic,
            _wireframeObjectManager,
            _uiSettingsService);
    }

    #region New Explicit Input Processing System

    /// <summary>
    /// Context about what's under the cursor and the current state.
    /// Everything needed to make an input decision.
    /// </summary>
    private struct InputContext
    {
        public bool IsOverHandle;           // Is cursor over a handle (resize, rotate, polygon point)?
        public bool IsHandlerCurrentlyActive; // Is a handler already active (mid-drag)?
        public bool IsOverElementBody;      // Is cursor over an element's body?
        public bool IsShiftHeld;           // Is shift key held (multi-select)?
        public IRenderableIpso? ElementUnderCursor; // What element is under cursor (if any)?
        public float WorldX;               // World X coordinate
        public float WorldY;               // World Y coordinate

        // For debugging and logging
        public string DebugInfo;
    }

    /// <summary>
    /// Possible input handling decisions, in priority order.
    /// </summary>
    private enum InputDecision
    {
        None,                    // Don't handle input this frame
        HandleSelection,         // Let handles (resize, rotate, polygon) handle it (HIGHEST PRIORITY)
        RectangleSelection,      // Let rectangle selector handle it (MEDIUM PRIORITY)
        NormalClickSelection,    // Do normal click selection (LOWEST PRIORITY)
    }

    /// <summary>
    /// Processes all mouse input for selection in a single, explicit order.
    /// This replaces the implicit timing dependencies between Activity and LateActivity.
    ///
    /// CRITICAL ORDERING:
    /// - On PrimaryPush: Selection logic runs FIRST, then handlers (so handlers see correct selection)
    /// - On PrimaryDown/PrimaryClick: Handlers run FIRST (to continue their operation)
    /// </summary>
    private void ProcessInputForSelection()
    {
        var cursor = Cursor;
        float worldX = cursor.GetWorldX();
        float worldY = cursor.GetWorldY();

        // ═══════════════════════════════════════════════════════════════
        // DIFFERENT ORDERING FOR PUSH vs DOWN/CLICK
        // ═══════════════════════════════════════════════════════════════

        if (cursor.PrimaryPush || cursor.SecondaryPush || cursor.PrimaryDoubleClick)
        {
            // ───────────────────────────────────────────────────────────
            // ON PUSH: Selection logic FIRST, then handlers
            // This ensures that clicking object B when A is selected will
            // select B first, then handlers operate on B (not A)
            // ───────────────────────────────────────────────────────────

            // PHASE 1: QUERY - What's under the cursor?
            var inputContext = DetermineInputContext(worldX, worldY, cursor);

            // PHASE 2: DECIDE - What selection logic to run?
            var decision = MakeInputDecision(inputContext, cursor);

            // PHASE 3: EXECUTE SELECTION - Update selection if needed
            ExecuteInputDecision(decision, inputContext, cursor, worldX, worldY);

            // PHASE 4: HANDLERS - Now let handlers process with correct selection
            WireframeEditor?.ProcessHandleInput(cursor, worldX, worldY);
        }
        else if (cursor.PrimaryDown || cursor.PrimaryClick)
        {
            // ───────────────────────────────────────────────────────────
            // ON DOWN/CLICK: Handlers FIRST
            // If a handler is active (dragging), it should continue its
            // operation without any selection logic interfering
            // ───────────────────────────────────────────────────────────

            // Track whether a handler owned this push-drag-release cycle.
            // If a handler was active and just released (PrimaryClick),
            // we must not run selection logic that could spuriously deselect.
            bool handlerProcessedRelease = false;

            if (WireframeEditor?.IsAnyHandlerActive == true && cursor.PrimaryClick)
            {
                handlerProcessedRelease = true;

                // Even when a handler owns the release, clean up the rectangle
                // selector in case it was partially started before the handler
                // took over.
                if (_rectangleSelector?.IsActive == true)
                {
                    _rectangleSelector.HandleRelease();
                }
            }

            // PHASE 1: HANDLERS - Let active handlers continue/release
            WireframeEditor?.ProcessHandleInput(cursor, worldX, worldY);

            if (!handlerProcessedRelease)
            {
                // PHASE 2: QUERY - Check state after handlers ran
                var inputContext = DetermineInputContext(worldX, worldY, cursor);

                // PHASE 3: DECIDE - Do we need additional logic?
                var decision = MakeInputDecision(inputContext, cursor);

                // PHASE 4: EXECUTE - Additional selection logic if no handler claimed it
                ExecuteInputDecision(decision, inputContext, cursor, worldX, worldY);

                // PHASE 5: Always give rectangle selector a chance to clean up on release.
                // The decision logic may route to NormalClickSelection (e.g., when the
                // cursor ends over an instance body), but the rectangle selector may still
                // be active from the initial push. We must release it so the visual clears.
                if (cursor.PrimaryClick
                    && decision != InputDecision.RectangleSelection
                    && _rectangleSelector?.IsActive == true)
                {
                    _rectangleSelector.HandleRelease();
                }
            }
        }
    }

    /// <summary>
    /// Determines what's under the cursor without changing any state.
    /// This is a pure query - no side effects.
    /// </summary>
    private InputContext DetermineInputContext(float worldX, float worldY, InputLibrary.Cursor cursor)
    {
        var context = new InputContext
        {
            WorldX = worldX,
            WorldY = worldY
        };

        // Check handles FIRST (highest priority)
        // Important: Call the wireframe editor directly to get CURRENT frame data
        if (WireframeEditor != null)
        {
            // Check if cursor is over any handle right now
            context.IsOverHandle = WireframeEditor.HasCursorOverHandles;

            // Check if any handler is currently active (mid-drag)
            context.IsHandlerCurrentlyActive = WireframeEditor.IsAnyHandlerActive;
        }

        // Check if over element body (for normal selection)
        context.IsOverElementBody = IsOverBody;

        // Check modifiers
        context.IsShiftHeld = _hotkeyManager.MultiSelect.IsPressedInControl();

        // Find what element is under cursor (for normal selection)
        if (!context.IsOverHandle)
        {
            var elementStack = _selectedState.GetTopLevelElementStack();
            context.ElementUnderCursor = GetRepresentationAt(
                worldX, worldY,
                cursor.PrimaryDoubleClick || IsComponentNoInstanceSelected,
                elementStack);
        }

        // Build debug info
        context.DebugInfo = $"IsOverHandle={context.IsOverHandle}, " +
                           $"IsHandlerActive={context.IsHandlerCurrentlyActive}, " +
                           $"IsOverBody={context.IsOverElementBody}, " +
                           $"HasElement={context.ElementUnderCursor != null}";

        return context;
    }

    /// <summary>
    /// Decides what selection logic should run based on priority rules.
    /// This makes the priority system EXPLICIT.
    ///
    /// NOTE: Timing varies by input type:
    /// - PrimaryPush: Called BEFORE handlers (selection updates first)
    /// - PrimaryDown/Click: Called AFTER handlers (handlers continue operation)
    /// </summary>
    private InputDecision MakeInputDecision(InputContext context, InputLibrary.Cursor cursor)
    {
        // ═══════════════════════════════════════════════════════════════
        // PRIORITY 1: If any handler is active (dragging),
        // skip all other selection logic to avoid interfering.
        // Exception: PrimaryDoubleClick overrides this so that
        // "punch through" (cycling to the next overlapping object)
        // works even though the second click's push activated a handler.
        // ═══════════════════════════════════════════════════════════════
        if (context.IsHandlerCurrentlyActive && !cursor.PrimaryDoubleClick)
        {
            return InputDecision.HandleSelection;
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIORITY 2: Rectangle selection (if shift held OR not over body)
        // IMPORTANT: Don't trigger rectangle selection when over a handle!
        // When over a handle, IsOverElementBody is false (intentional), but
        // we should let handlers process it, not rectangle selector.
        // Rectangle selector will only activate if user drags far enough.
        // If user just clicks without dragging, it will return early and
        // we'll handle it in ProcessRectangleSelection as a fallback.
        // ═══════════════════════════════════════════════════════════════
        if (context.IsShiftHeld || (!context.IsOverElementBody && !context.IsOverHandle))
        {
            return InputDecision.RectangleSelection;
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIORITY 3: Normal click selection
        // Handles clicking on element bodies and selecting them
        // ═══════════════════════════════════════════════════════════════
        if (cursor.PrimaryPush || cursor.SecondaryPush || cursor.PrimaryDoubleClick)
        {
            return InputDecision.NormalClickSelection;
        }

        // No selection logic needed this frame
        return InputDecision.None;
    }

    /// <summary>
    /// Executes the selection logic based on the decision.
    ///
    /// NOTE: Handler timing varies:
    /// - PrimaryPush: This runs BEFORE handlers (updates selection, then handlers operate on it)
    /// - PrimaryDown/Click: This runs AFTER handlers (handlers already processed)
    /// </summary>
    private void ExecuteInputDecision(
        InputDecision decision,
        InputContext context,
        InputLibrary.Cursor cursor,
        float worldX,
        float worldY)
    {
        switch (decision)
        {
            case InputDecision.HandleSelection:
                // A handler is active - don't run any other selection logic
                // Handlers will process separately (before or after this depending on input type)
                break;

            case InputDecision.RectangleSelection:
                ProcessRectangleSelection(cursor, worldX, worldY, context);
                break;

            case InputDecision.NormalClickSelection:
                ProcessNormalClickSelection(context, worldX, worldY);
                break;

            case InputDecision.None:
                // Nothing to do this frame
                break;
        }
    }

    /// <summary>
    /// Handles rectangle selection input.
    /// Separated into its own method for clarity.
    /// If rectangle selector doesn't activate (no drag), falls back to normal selection.
    /// </summary>
    private void ProcessRectangleSelection(InputLibrary.Cursor cursor, float worldX, float worldY, InputContext context)
    {
        if (_rectangleSelector == null)
            return;

        if (cursor.PrimaryPush)
        {
            _rectangleSelector.HandlePush(worldX, worldY);
        }
        else if (cursor.PrimaryDown)
        {
            // Pass the handler active state so rectangle selector can bail if needed
            _rectangleSelector.HandleDrag(context.IsHandlerCurrentlyActive);
        }
        else if (cursor.PrimaryClick)
        {
            // Check if rectangle selector was activated (user dragged)
            bool wasActive = _rectangleSelector.IsActive;

            _rectangleSelector.HandleRelease();

            // If rectangle selector was never activated (simple click, no drag),
            // fall back to normal click selection to handle deselection
            if (!wasActive)
            {
                ProcessNormalClickSelection(context, worldX, worldY);
            }
        }
    }

    /// <summary>
    /// Handles normal click selection (clicking on elements, not handles).
    /// Separated into its own method for clarity.
    /// </summary>
    private void ProcessNormalClickSelection(InputContext context, float worldX, float worldY)
    {
        // Don't do normal selection if rectangle selector is active
        if (_rectangleSelector?.IsActive == true)
            return;

        // Call the existing PushAndDoubleClickSelectionActivity logic
        // but with the context we already gathered
        PushAndDoubleClickSelectionActivity();
    }

    #endregion

    void SelectionActivity()
    {
        // Update hover state EVERY frame (not just when there's input)
        // This ensures hover highlights (e.g., polygon point highlights) show correctly
        var cursor = Cursor;
        float worldX = cursor.GetWorldX();
        float worldY = cursor.GetWorldY();
        WireframeEditor?.UpdateHover(worldX, worldY);

        // Process input only when no context menu is visible
        if (_editingManager.ContextMenuStrip?.Visible != true)
        {
            ProcessInputForSelection();
        }
    }

    private void PushAndDoubleClickSelectionActivity()
    {
        try
        {
            // If the SideOver is a non-None
            // value, that means that the object
            // is already selected
            if (WireframeEditor?.HasCursorOverHandles != true)
            {
                float x = Cursor.GetWorldX();
                float y = Cursor.GetWorldY();

                List<ElementWithState> elementStack = new List<ElementWithState>();
                elementStack.Add(new ElementWithState(_selectedState.SelectedElement));


                IRenderableIpso representation =
                    GetRepresentationAt(x, y, Cursor.PrimaryDoubleClick || IsComponentNoInstanceSelected, elementStack);
                bool hasChanged = true;

                if (representation != null)
                {
                    InstanceSave selectedInstance;
                    ElementSave selectedElement;
                    GetElementOrInstanceForIpso(representation, elementStack, out selectedInstance, out selectedElement);

                    if (selectedInstance == null && selectedElement == null)
                    {
                        throw new Exception("Either the selected element or instance should not be null");
                    }
                    // The representation 
                    // will become invalid
                    // in the following if/else
                    // because the Wireframe view
                    // is refreshed.  So we need to
                    // re-get the representationl
                    if (selectedInstance != null)
                    {
                        bool isAlreadySelected = _selectedState.SelectedInstances.Contains(selectedInstance);

                        if (isAlreadySelected)
                        {
                            hasChanged = false;
                        }
                        else
                        {
                            // If the user shift+clicks, then we want to select multiple
                            bool selectMultiple = IsShiftDown;

                            if (selectMultiple)
                            {
                                List<InstanceSave> instances = new List<InstanceSave>();
                                instances.AddRange(_selectedState.SelectedInstances);
                                instances.Add(selectedInstance);
                                _selectedState.SelectedInstances = instances;
                            }
                            else
                            {
                                _selectedState.SelectedInstance = selectedInstance;
                            }
                            // See comment above on why we do this
                            representation = _wireframeObjectManager.GetRepresentation(selectedInstance, elementStack);
                        }
                    }
                    else
                    {
                        _selectedState.SelectedInstance = null;
                        _selectedState.SelectedElement = selectedElement;

                        representation = _wireframeObjectManager.GetRepresentation(selectedElement);
                    }
                    _undoManager.RecordUndo();

                }
                else
                {
                    _selectedState.SelectedInstance = null;
                }

                if (hasChanged)
                {
                    if (_selectedState.SelectedInstances.Count() > 1)
                    {
                        List<GraphicalUiElement> selectedIpsos = new List<GraphicalUiElement>();
                        foreach (var instance in _selectedState.SelectedInstances)
                        {
                            selectedIpsos.Add(_wireframeObjectManager.GetRepresentation(instance, elementStack));
                        }
                        SelectedGues = selectedIpsos;
                    }
                    else
                    {
                        SelectedGue = representation as GraphicalUiElement;
                    }
                }
            }
        }
        catch (Exception e)
        {
            _dialogService.ShowMessage("Error in PushAndDoubleClickSelectionActivity: " + e.ToString());
            throw;
        }

    }

    private void GetElementOrInstanceForIpso(IRenderableIpso representation, List<ElementWithState> elementStack,
                                                    out InstanceSave selectedInstance, out ElementSave selectedElement)
    {
        selectedInstance = null;
        selectedElement = null;

        IRenderableIpso ipsoToUse = representation;

        while (ipsoToUse != null && ipsoToUse.Parent != null && _wireframeObjectManager.AllIpsos.Contains(ipsoToUse) == false)
        {
            ipsoToUse = ipsoToUse.Parent;
        }

        if (ipsoToUse != null)
        {
            if (ipsoToUse.Tag is InstanceSave)
            {
                selectedInstance = ipsoToUse.Tag as InstanceSave;
            }
            else if (ipsoToUse.Tag is ElementSave)
            {
                selectedElement = ipsoToUse.Tag as ElementSave;
            }
            else
            {
                throw new Exception("This should never happen");
            }
        }

        if (selectedInstance == null && selectedElement == null)
        {
            throw new Exception("Either the selected element or instance should not be null");
        }
    }

    public void Refresh()
    {
        Clear();

        List<GraphicalUiElement> representations = new List<GraphicalUiElement>();

        var elementStack = _selectedState.GetTopLevelElementStack();
        if (_selectedState.SelectedInstances.Count() != 0)
        {

            foreach (var instance in _selectedState.SelectedInstances)
            {
                GraphicalUiElement toAdd =
                    _wireframeObjectManager.GetRepresentation(instance, elementStack);
                if (toAdd != null)
                {
                    representations.Add(toAdd);
                }
            }
        }
        else if (_selectedState.SelectedElement != null)
        {
            GraphicalUiElement toAdd =
                _wireframeObjectManager.GetRepresentation(_selectedState.SelectedElement);

            if (toAdd != null)
            {
                representations.Add(toAdd);
            }
        }

        SelectedGues = representations;
    }

    private void Clear()
    {
        HighlightedIpso = null;
    }

    #endregion
}
