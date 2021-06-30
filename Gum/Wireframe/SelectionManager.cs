using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Math.Geometry;
using Gum.ToolStates;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Gum.Input;
using Gum.DataTypes;
using System.Windows.Forms;
using WinCursor = System.Windows.Forms.Cursor;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using Gum.Undo;
using Gum.Debug;
using Gum.Wireframe.Editors;

namespace Gum.Wireframe
{
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
        
        static SelectionManager mSelf;



        public WireframeEditor WireframeEditor;

        List<GraphicalUiElement> mSelectedIpsos = new List<GraphicalUiElement>();
        IPositionedSizedObject mHighlightedIpso;

        GraphicalOutline mGraphicalOutline;
        Layer mUiLayer;

        HighlightManager highlightManager;

        #endregion

        #region Properties

        public Layer UiLayer
        {
            get
            {
                return mUiLayer;
            }
        }

        public InputLibrary.Cursor Cursor
        {
            get
            {
                return InputLibrary.Cursor.Self;
            }
        }

        public static SelectionManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new SelectionManager();
                }
                return mSelf;
            }
        }

        public bool IsOverBody
        {
            get;
            set;
        }

        public GraphicalUiElement SelectedGue
        {
            get
            {
                if (mSelectedIpsos.Count == 0)
                {
                    return null;
                }
                else
                {
                    return mSelectedIpsos[0] as GraphicalUiElement;
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
                return  mSelectedIpsos.Any();
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
                if(WireframeEditor != null)
                {
                    WireframeEditor.RestrictToUnitValues = value;
                }
            }
        }
        #endregion

        #region Methods

        public SelectionManager()
        {

            // We used to have this set to 2, but now that we have dotted lines, I just do a value of 0
            mUiLayer = Renderer.Self.AddLayer();
            mUiLayer.Name = "UI Layer";

            mGraphicalOutline = new GraphicalOutline(mUiLayer);

            highlightManager = new HighlightManager(mUiLayer);
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
                //if (Cursor.IsInWindow && SelectedState.Self.SelectedElement != null)
                if (SelectedState.Self.SelectedElement != null)
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
                GumCommands.Self.GuiCommands.ShowMessage("Error in SelectionManager.Activity:\n\n" + e.ToString());
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

                Cursor cursorToSet = Cursors.Arrow;

                float worldXAt = Cursor.GetWorldX();
                float worldYAt = Cursor.GetWorldY();

                IPositionedSizedObject representationOver = null;

                if (EditingManager.Self.ContextMenuStrip != null && EditingManager.Self.ContextMenuStrip.Visible)
                {
                    // do nothing!
                }
                else
                {
                    if(WireframeEditor != null)
                    {
                        cursorToSet = WireframeEditor.GetWindowsCursorToShow(cursorToSet, worldXAt, worldYAt);
                    }

                    #region Selecting element activity

                    if(forceNoHighlight)
                    {
                        IsOverBody = false;
                    }

                    if(forceNoHighlight == false)
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
                                cursorToSet = Cursors.SizeAll;
                                representationOver = WireframeObjectManager.Self.GetSelectedRepresentation();
                            }
                            else
                            {
                                List<ElementWithState> elementStack = new List<ElementWithState>();
                                elementStack.Add(new ElementWithState(SelectedState.Self.SelectedElement));

                                representationOver =
                                    GetRepresentationAt(worldXAt, worldYAt, false, elementStack);

                                if (representationOver != null)
                                {
                                    cursorToSet = Cursors.SizeAll;
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


                if(representationOver != null && representationOver is NineSlice)
                {
                    // This function updates the sizes and texture coordinates of the 
                    // highlighted representation if it's a NineSlice.  This is needed before
                    // we set the HighlightedIpso and before we update the highlight objects
                    (representationOver as NineSlice).RefreshTextureCoordinatesAndSpriteSizes();
                }



                // We used to not check this, but we have to now because the cursor might be 
                if(Cursor.IsInWindow)
                {
                    Cursor.SetWinformsCursor(cursorToSet);

                    // We don't want to show the highlight when the user is performing some kind of editing.
                    // Therefore make sure the cursor isn't down.
                    if (representationOver != null && Cursor.PrimaryDown == false)
                    {
                        HighlightedIpso = representationOver;
                    }
                    else
                    {
                        HighlightedIpso = null;
                    }
                }
            }
            else if(InputLibrary.Cursor.Self.PrimaryDown && Cursor.IsInWindow)
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
                    if(selectedRepresentations != null)
                    {
                        foreach(var selectedRepresentation in selectedRepresentations)
                        {
                            // If this is a container, and dotted lines are not drawn, then this has no renderable component:
                            if(selectedRepresentation?.RenderableComponent != null)
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
                else if(element != null) // both may be null if the user drag+dropped onto the wireframe window
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

                        if(graphicalUiElement.RenderableComponent is LinePolygon)
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
            if(ipso is GraphicalUiElement)
            {
                 if(ipso.Tag == null || ipso.Tag is ScreenSave == false)
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
            else if(ipso is IVisible)
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

        private void UpdateEditorsToSelection()
        {
            if(SelectedGues.Count == 1 &&
                SelectedGue?.Tag is InstanceSave && 
                ((InstanceSave)SelectedGue.Tag).BaseType == "Polygon")
            {
                // use the Polygon wireframe editor
                if(WireframeEditor is PolygonWireframeEditor == false)
                {
                    if (WireframeEditor != null)
                    {
                        WireframeEditor.Destroy();
                    }
                    WireframeEditor = new PolygonWireframeEditor(UiLayer);
                }
                WireframeEditor.UpdateToSelection(mSelectedIpsos);
            }
            else if(SelectedGues.Count > 0 && SelectedGue?.Tag is ScreenSave == false)
            {
                if(WireframeEditor is StandardWireframeEditor == false)
                {
                    if(WireframeEditor != null)
                    {
                        WireframeEditor.Destroy();
                    }
                    WireframeEditor = new StandardWireframeEditor(UiLayer);
                }
                WireframeEditor.UpdateToSelection(mSelectedIpsos);
            }
            else if(WireframeEditor != null)
            {
                if (WireframeEditor != null)
                {
                    WireframeEditor.Destroy();
                }
                WireframeEditor = null;
            }

            if(WireframeEditor != null)
            {
                WireframeEditor.RestrictToUnitValues = RestrictToUnitValues;
            }
        }

        void SelectionActivity()
        {
            if (EditingManager.Self.ContextMenuStrip == null ||
                !EditingManager.Self.ContextMenuStrip.Visible)
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
                    elementStack.Add(new ElementWithState(SelectedState.Self.SelectedElement));


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
                            bool isAlreadySelected = SelectedState.Self.SelectedInstances.Contains(selectedInstance);

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
                                    instances.AddRange(SelectedState.Self.SelectedInstances);
                                    instances.Add(selectedInstance);
                                    SelectedState.Self.SelectedInstances = instances;
                                }
                                else
                                {
                                    SelectedState.Self.SelectedInstance = selectedInstance;
                                }
                                // See comment above on why we do this
                                representation = WireframeObjectManager.Self.GetRepresentation(selectedInstance, elementStack);
                            }
                        }
                        else
                        {
                            SelectedState.Self.SelectedInstance = null;
                            SelectedState.Self.SelectedElement = selectedElement;

                            representation = WireframeObjectManager.Self.GetRepresentation(selectedElement);
                        }
                        UndoManager.Self.RecordUndo();

                    }
                    else
                    {
                        SelectedState.Self.SelectedInstance = null;
                    }
                    
                    if (hasChanged)
                    {
                        if (SelectedState.Self.SelectedInstances.GetCount() > 1)
                        {
                            List<GraphicalUiElement> selectedIpsos = new List<GraphicalUiElement>();
                            foreach (var instance in SelectedState.Self.SelectedInstances)
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

            var elementStack = SelectedState.Self.GetTopLevelElementStack();
            if (SelectedState.Self.SelectedInstances.GetCount() != 0)
            {
                
                foreach (var instance in SelectedState.Self.SelectedInstances)
                {
                    GraphicalUiElement toAdd = 
                        WireframeObjectManager.Self.GetRepresentation(instance, elementStack);
                    if (toAdd != null)
                    {
                        representations.Add(toAdd);
                    }
                }
            }
            else if (SelectedState.Self.SelectedElement != null)
            {
                GraphicalUiElement toAdd =
                    WireframeObjectManager.Self.GetRepresentation(SelectedState.Self.SelectedElement);

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
}
