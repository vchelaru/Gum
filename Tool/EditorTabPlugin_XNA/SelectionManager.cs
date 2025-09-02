using Gum.DataTypes;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe.Editors;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.RightsManagement;
using System.Windows.Forms;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;
using WinCursor = System.Windows.Forms.Cursor;

namespace Gum.Wireframe;

public class SelectionManager
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

    public WireframeEditor WireframeEditor;

    List<GraphicalUiElement> mSelectedIpsos = new List<GraphicalUiElement>();
    IPositionedSizedObject mHighlightedIpso;

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
    private readonly EditingManager _editingManager;
    private readonly IUndoManager _undoManager;
    private readonly IDialogService _dialogService;
    private readonly HotkeyManager _hotkeyManager;
    private readonly IVariableInCategoryPropagationLogic _variableInCategoryPropagationLogic;

    public bool IsOverBody
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

    public IPositionedSizedObject HighlightedIpso
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


    #endregion

    #region Methods

    internal SelectionManager(ISelectedState selectedState, 
        IUndoManager undoManager, 
        EditingManager editingManager, 
        IDialogService dialogService,
        HotkeyManager hotkeyManager,
        IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic)
    {
        _selectedState = selectedState;
        _editingManager = editingManager;
        _undoManager = undoManager;
        _dialogService = dialogService;
        _hotkeyManager = hotkeyManager;
        _variableInCategoryPropagationLogic = variableInCategoryPropagationLogic;
    }

    public void Initialize(LayerService layerService)
    {
        _layerService = layerService;
        var overlayLayer = layerService.OverlayLayer;

        mGraphicalOutline = new GraphicalOutline(overlayLayer);

        highlightManager = new HighlightManager(overlayLayer);

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

    public void LateActivity()
    {
        WireframeEditor?.Activity(SelectedGues);
    }

    public void Deselect()
    {
        SelectedGue = null;
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

                    if (WireframeEditor?.HasCursorOver == true)
                    {
                        representationOver = WireframeObjectManager.Self.GetSelectedRepresentation();
                        IsOverBody = false;
                    }
                    else
                    {
                        if (IsOverBody && Cursor.PrimaryDown)
                        {
                            cursorToSet = System.Windows.Forms.Cursors.SizeAll;
                            representationOver = WireframeObjectManager.Self.GetSelectedRepresentation();
                        }
                        else
                        {
                            List<ElementWithState> elementStack = new List<ElementWithState>();
                            elementStack.Add(new ElementWithState(_selectedState.SelectedElement));

                            representationOver =
                                GetRepresentationAt(worldXAt, worldYAt, false, elementStack);

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

    public GraphicalUiElement GetRepresentationAt(float x, float y, bool skipSelected, List<ElementWithState> elementStack)
    {
        GraphicalUiElement ipsoOver = null;

        // First check if we're over the current
        var selectedRepresentations = WireframeObjectManager.Self.GetSelectedRepresentations();

        int indexToStartAt = -1;
        if (skipSelected)
        {
            if (selectedRepresentations?.Length > 0)
            {
                indexToStartAt = WireframeObjectManager.Self.AllIpsos.IndexOf(selectedRepresentations.First());
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
                indexToStartAt = WireframeObjectManager.Self.AllIpsos.Count;
            }

            #region First check only visible objects
            ipsoOver = ReverseLoopToFindIpso(x, y, indexToStartAt - 1, -1, true, elementStack);

            if (ipsoOver == null && indexToStartAt != WireframeObjectManager.Self.AllIpsos.Count - 1)
            {
                ipsoOver = ReverseLoopToFindIpso(x, y, WireframeObjectManager.Self.AllIpsos.Count - 1, indexToStartAt, true, elementStack);
            }
            #endregion

            #region If none were found, check invisible objects

            if (ipsoOver == null)
            {
                ipsoOver = ReverseLoopToFindIpso(x, y, indexToStartAt - 1, -1, false, elementStack);

                if (ipsoOver == null && indexToStartAt != WireframeObjectManager.Self.AllIpsos.Count - 1)
                {
                    ipsoOver = ReverseLoopToFindIpso(x, y, WireframeObjectManager.Self.AllIpsos.Count - 1, indexToStartAt, false, elementStack);
                }


            }

            #endregion
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
                ipsoOver = WireframeObjectManager.Self.GetRepresentation(instance, elementStack);
            }
            else if (element != null) // both may be null if the user drag+dropped onto the wireframe window
            {
                try
                {
                    ipsoOver = WireframeObjectManager.Self.GetRepresentation(element);
                }
                catch (Exception e)
                {
                    int m = 3;
                    throw e;
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
        if (indexToStartAt >= WireframeObjectManager.Self.AllIpsos.Count)
        {
            throw new Exception("Index must be less than the AllIpsos Count");
        }

        // Let's try to get visible ones first, then if we don't find anything, look at invisible ones
        for (int i = indexToStartAt; i > indexToEndAt; i--)
        {

            GraphicalUiElement graphicalUiElement = WireframeObjectManager.Self.AllIpsos[i];
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

                    if (hasCursorOver && (WireframeObjectManager.Self.IsRepresentation(graphicalUiElement)))
                    {

                        // hold on, even though this is a valid IPSO and the cursor is over it, we gotta see if
                        // it's an instance that is locked.  If so, we shouldn't select it!
                        InstanceSave instanceSave = graphicalUiElement.Tag as InstanceSave;
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
        else if (ipso is IVisible)
        {
            isVisible = ((IVisible)ipso).AbsoluteVisible;
        }
        else if (ipso is Sprite)
        {
            isVisible = ((Sprite)ipso).AbsoluteVisible;
        }
        else if (ipso is Text)
        {
            isVisible = ((Text)ipso).AbsoluteVisible;
        }

        return isVisible;
    }

    List<GraphicalUiElement> emptyGraphicalUiElementList = new List<GraphicalUiElement>();
    private void UpdateEditorsToSelection()
    {
        if (SelectedGues.Count == 1 &&
            SelectedGue?.Tag is InstanceSave &&
            ((InstanceSave)SelectedGue.Tag).BaseType == "Polygon")
        {
            // use the Polygon wireframe editor
            if (WireframeEditor is PolygonWireframeEditor == false)
            {
                if (WireframeEditor != null)
                {
                    WireframeEditor.Destroy();
                }
                WireframeEditor = new PolygonWireframeEditor(
                    _layerService.OverlayLayer,
                    _hotkeyManager,
                    this,
                    _selectedState);
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
                WireframeEditor = new PolygonWireframeEditor(
                    _layerService.OverlayLayer,
                    _hotkeyManager,
                    this,
                    _selectedState);
            }
            else
            {
                if (WireframeEditor is StandardWireframeEditor == false)
                {
                    if (WireframeEditor != null)
                    {
                        WireframeEditor.Destroy();
                    }

                    var lineColor = Color.FromArgb(255, GumState.Self.ProjectState.GeneralSettings.GuideLineColorR,
                        GumState.Self.ProjectState.GeneralSettings.GuideLineColorG,
                        GumState.Self.ProjectState.GeneralSettings.GuideLineColorB);

                    var textColor = Color.FromArgb(255, GumState.Self.ProjectState.GeneralSettings.GuideTextColorR,
                        GumState.Self.ProjectState.GeneralSettings.GuideTextColorG,
                        GumState.Self.ProjectState.GeneralSettings.GuideTextColorB);

                    WireframeEditor = new StandardWireframeEditor(
                        _layerService.OverlayLayer,
                        lineColor, textColor, 
                        _hotkeyManager,
                        this,
                        _selectedState,
                        _variableInCategoryPropagationLogic);
                }
            }
        }
        else if (WireframeEditor != null)
        {
            if (WireframeEditor != null)
            {
                WireframeEditor.Destroy();
            }
            WireframeEditor = null;
        }

        if (WireframeEditor != null)
        {
            if (_selectedState.CustomCurrentStateSave != null)
            {
                WireframeEditor.UpdateToSelection(emptyGraphicalUiElementList);
            }
            else
            {
                WireframeEditor.UpdateToSelection(mSelectedIpsos);
            }
            WireframeEditor.RestrictToUnitValues = RestrictToUnitValues;
        }
    }

    void SelectionActivity()
    {
        if (_editingManager.ContextMenuStrip?.Visible != true)
        {
            if (Cursor.PrimaryPush || Cursor.SecondaryPush || Cursor.PrimaryDoubleClick)
            {
                PushAndDoubleClickSelectionActivity();
            }

            //if (Cursor.PrimaryClick)
            //{
            //    SideOver = ResizeSide.None;
            //}
        }
    }

    private void PushAndDoubleClickSelectionActivity()
    {
        try
        {
            // If the SideOver is a non-None
            // value, that means that the object
            // is already selected
            if (WireframeEditor?.HasCursorOver != true)
            {
                float x = Cursor.GetWorldX();
                float y = Cursor.GetWorldY();

                List<ElementWithState> elementStack = new List<ElementWithState>();
                elementStack.Add(new ElementWithState(_selectedState.SelectedElement));


                IRenderableIpso representation =
                    GetRepresentationAt(x, y, Cursor.PrimaryDoubleClick, elementStack);
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
                            representation = WireframeObjectManager.Self.GetRepresentation(selectedInstance, elementStack);
                        }
                    }
                    else
                    {
                        _selectedState.SelectedInstance = null;
                        _selectedState.SelectedElement = selectedElement;

                        representation = WireframeObjectManager.Self.GetRepresentation(selectedElement);
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
                            selectedIpsos.Add(WireframeObjectManager.Self.GetRepresentation(instance, elementStack));
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
            MessageBox.Show("Error in PushAndDoubleClickSelectionActivity: " + e.ToString());
            throw e;
        }

    }

    private static void GetElementOrInstanceForIpso(IRenderableIpso representation, List<ElementWithState> elementStack,
                                                    out InstanceSave selectedInstance, out ElementSave selectedElement)
    {
        selectedInstance = null;
        selectedElement = null;

        IRenderableIpso ipsoToUse = representation;

        while (ipsoToUse != null && ipsoToUse.Parent != null && WireframeObjectManager.Self.AllIpsos.Contains(ipsoToUse) == false)
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
                    WireframeObjectManager.Self.GetRepresentation(instance, elementStack);
                if (toAdd != null)
                {
                    representations.Add(toAdd);
                }
            }
        }
        else if (_selectedState.SelectedElement != null)
        {
            GraphicalUiElement toAdd =
                WireframeObjectManager.Self.GetRepresentation(_selectedState.SelectedElement);

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
