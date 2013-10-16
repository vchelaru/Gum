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

        SolidRectangle mOverlaySolidRectangle;
        Sprite mOverlaySprite;
        NineSlice mOverlayNineSlice;


        ResizeHandles mResizeHandles;

        List<IPositionedSizedObject> mSelectedIpsos = new List<IPositionedSizedObject>();
        IPositionedSizedObject mHighlightedIpso;

        GraphicalOutline mGraphicalOutline;
        Layer mUiLayer;

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

        public ResizeSide SideOver
        {
            get;
            private set;
        }

        public bool IsOverBody
        {
            get;
            set;
        }

        public IPositionedSizedObject SelectedIpso
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
                if (value != null)
                {
                    mResizeHandles.Visible = true;
                    mResizeHandles.SetValuesFrom(value);
                }
                else
                {
                    mResizeHandles.Visible = false;
                }

                mSelectedIpsos.Clear();

                if (value != null)
                {
                    mSelectedIpsos.Add(value);
                }
            }
        }

        public List<IPositionedSizedObject> SelectedIpsos
        {
            get
            {
                return mSelectedIpsos;
            }
            private set
            {

                if (value.Count == 1)
                {
                    SelectedIpso = value[0];
                }
                else if (value.Count > 1)
                {
                    mSelectedIpsos.Clear();
                    mSelectedIpsos.AddRange(value);
                    mResizeHandles.Visible = true;
                    mResizeHandles.SetValuesFrom(mSelectedIpsos);
                }
                else
                {
                    mSelectedIpsos.Clear();
                }

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
                if (mHighlightedIpso != value)
                {
                    if (mHighlightedIpso != null)
                    {
                        UnhighlightIpso(mHighlightedIpso as GraphicalUiElement);
                    }



                    mHighlightedIpso = value;

                    mGraphicalOutline.HighlightedIpso = mHighlightedIpso as GraphicalUiElement;

                }
            }
        }

        private void UnhighlightIpso(GraphicalUiElement highlightedIpso)
        {
            if (highlightedIpso.Component is Sprite)
            {
                mOverlaySprite.Visible = false;
            }
            else if (highlightedIpso.Component is NineSlice)
            {
                mOverlayNineSlice.Visible = false;
            }
            else if (highlightedIpso.Component is LineRectangle)
            {
                mOverlaySolidRectangle.Visible = false;
            }
        }

        public Sprite HighlightedSprite
        {
            get
            {
                if (HighlightedIpso == null)
                {
                    return null;
                }
                else
                {
                    return (HighlightedIpso as GraphicalUiElement).Component as Sprite;
                }
            }
        }

        public NineSlice HighlightedNineSlice
        {
            get
            {
                if (HighlightedIpso == null)
                {
                    return null;
                }
                else
                {
                    return (HighlightedIpso as GraphicalUiElement).Component as NineSlice;
                }
            }
        }

        public LineRectangle HighlightedLineRectangle
        {
            get
            {
                if (HighlightedIpso == null)
                {
                    return null;
                }
                else
                {
                    return (HighlightedIpso as GraphicalUiElement).Component as LineRectangle;
                }
            }
        }

        public ResizeHandles ResizeHandles
        {
            get
            {
                return mResizeHandles;
            }
        }

        public bool HasSelection
        {
            get
            {
                return mResizeHandles.Visible;
            }
        }

        public bool IsShiftDown
        {
            get
            {
                return ((Control.ModifierKeys & Keys.Shift) != 0);
            }
        }
        #endregion


        public SelectionManager()
        {

            // We used to have this set to 2, but now that we have dotted lines, I just do a value of 0
            mUiLayer = Renderer.Self.AddLayer();
            mUiLayer.Name = "UI Layer";

            mGraphicalOutline = new GraphicalOutline(mUiLayer);

            mOverlaySolidRectangle = new SolidRectangle();
            mOverlaySolidRectangle.Color = Color.LightGreen;
            mOverlaySolidRectangle.Color.A = 100;
            mOverlaySolidRectangle.Visible = false;
            ShapeManager.Self.Add(mOverlaySolidRectangle, mUiLayer);

            mOverlaySprite = new Sprite(null);
            mOverlaySprite.BlendState = BlendState.Additive;
            mOverlaySprite.Visible = false;
            SpriteManager.Self.Add(mOverlaySprite, mUiLayer);

            mOverlayNineSlice = new NineSlice();
            mOverlayNineSlice.BlendState = BlendState.Additive;
            mOverlayNineSlice.Visible = false;
            SpriteManager.Self.Add(mOverlayNineSlice, mUiLayer);

            mResizeHandles = new ResizeHandles(mUiLayer);
            mResizeHandles.Visible = false;
        }

        public void Activity(System.Windows.Forms.Control container)
        {
            try
            {
                ProjectVerifier.Self.AssertSelectedIpsosArePartOfRenderer();

                if (Cursor.IsInWindow && SelectedState.Self.SelectedElement != null)
                {
                    HighlightActivity(container);
                    ProjectVerifier.Self.AssertSelectedIpsosArePartOfRenderer();

                    SelectionActivity();
                    ProjectVerifier.Self.AssertSelectedIpsosArePartOfRenderer();
                }
                else if (!Cursor.IsInWindow)
                {
                    HighlightedIpso = null;
                }

                ProjectVerifier.Self.AssertSelectedIpsosArePartOfRenderer();

            }
            catch (Exception e)
            {
                MessageBox.Show("Error in SelectionManager.Activity:\n\n" + e.ToString());
            }
        }

        public void LateActivity()
        {
            UpdateResizeHandles();

        }

        public void Deselect()
        {
            mResizeHandles.Visible = false;
        }

        void HighlightActivity(System.Windows.Forms.Control container)
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

                if (EditingManager.Self.ContextMenuStrip != null && EditingManager.Self.ContextMenuStrip.Visible)
                {
                    // do nothing!
                }
                else
                {
                    cursorToSet = CursorOverHandlesHighlightActivity(cursorToSet, worldXAt, worldYAt);

                    #region Selecting element activity

                    IPositionedSizedObject representation = null;

                    if (SideOver != ResizeSide.None)
                    {
                        representation = WireframeObjectManager.Self.GetSelectedRepresentation();
                        IsOverBody = false;
                    }
                    else
                    {
                        if (IsOverBody && Cursor.PrimaryDown)
                        {
                            cursorToSet = Cursors.SizeAll;
                            representation = WireframeObjectManager.Self.GetSelectedRepresentation();
                        }
                        else
                        {
                            List<ElementWithState> elementStack = new List<ElementWithState>();
                            elementStack.Add(new ElementWithState(SelectedState.Self.SelectedElement));


                            representation =
                                GetRepresentationAt(worldXAt, worldYAt, false, elementStack);

                            if (representation != null)
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

                    // We don't want to show the highlight when the user is performing some kind of editing.
                    // Therefore make sure the cursor isn't down.
                    if (representation != null && Cursor.PrimaryDown == false)
                    {
                        HighlightedIpso = representation;
                    }
                    else
                    {
                        HighlightedIpso = null;
                    }

                    #endregion
                }



                Cursor.SetWinformsCursor(cursorToSet);
            }
            else if(InputLibrary.Cursor.Self.PrimaryDown)
            {
                // We only want to hide it if the user is holding the cursor down over the wireframe window.
                HighlightedIpso = null;
            }

            
            UpdateHighlightObjects();
        }

        /// <summary>
        /// Updates additional UI used to highlight objects, such
        /// as a solid rectangle for highlighted containers or overlaying
        /// an additive Sprite over the highlighted Sprite
        /// </summary>
        private void UpdateHighlightObjects()
        {
            if (HighlightedSprite != null)
            {
                mOverlaySprite.Visible = true;
                mOverlaySprite.X = HighlightedSprite.GetAbsoluteX();
                mOverlaySprite.Y = HighlightedSprite.GetAbsoluteY();

                mOverlaySprite.Width = HighlightedSprite.Width;
                mOverlaySprite.Height = HighlightedSprite.Height;
                mOverlaySprite.Texture = HighlightedSprite.Texture;


                mOverlaySprite.Wrap = HighlightedSprite.Wrap;

                mOverlaySprite.SourceRectangle = HighlightedSprite.SourceRectangle;

                mOverlaySprite.FlipHorizontal = HighlightedSprite.FlipHorizontal;
                mOverlaySprite.FlipVertical = HighlightedSprite.FlipVertical;
            }
            else if (HighlightedNineSlice != null)
            {
                mOverlayNineSlice.Visible = true;
                mOverlayNineSlice.X = HighlightedNineSlice.GetAbsoluteX();
                mOverlayNineSlice.Y = HighlightedNineSlice.GetAbsoluteY();

                mOverlayNineSlice.Width = HighlightedNineSlice.Width;
                mOverlayNineSlice.Height = HighlightedNineSlice.Height;
                mOverlayNineSlice.TopLeftTexture = HighlightedNineSlice.TopLeftTexture;
                mOverlayNineSlice.TopTexture = HighlightedNineSlice.TopTexture;
                mOverlayNineSlice.TopRightTexture = HighlightedNineSlice.TopRightTexture;

                mOverlayNineSlice.LeftTexture = HighlightedNineSlice.LeftTexture;
                mOverlayNineSlice.CenterTexture = HighlightedNineSlice.CenterTexture;
                mOverlayNineSlice.RightTexture = HighlightedNineSlice.RightTexture;

                mOverlayNineSlice.BottomLeftTexture = HighlightedNineSlice.BottomLeftTexture;
                mOverlayNineSlice.BottomTexture = HighlightedNineSlice.BottomTexture;
                mOverlayNineSlice.BottomRightTexture = HighlightedNineSlice.BottomRightTexture;

            }
            else if (HighlightedLineRectangle != null)
            {
                SolidRectangle overlay = mOverlaySolidRectangle;

                overlay.Visible = true;
                overlay.X = HighlightedLineRectangle.GetAbsoluteX();
                overlay.Y = HighlightedLineRectangle.GetAbsoluteY();

                overlay.Width = HighlightedLineRectangle.Width;
                overlay.Height = HighlightedLineRectangle.Height;
            }
        }




        public IPositionedSizedObject GetRepresentationAt(float x, float y, bool skipSelected, List<ElementWithState> elementStack)
        {

            IPositionedSizedObject ipsoOver = null;

            if (InputLibrary.Cursor.Self.PrimaryPush)
            {
                int m = 3;
            }
            // First check if we're over the current
            GraphicalUiElement selectedRepresentation = WireframeObjectManager.Self.GetSelectedRepresentation();
            if (InputLibrary.Keyboard.Self.KeyDown(Microsoft.Xna.Framework.Input.Keys.Space))
            {
                int m = 3;
            }
            int indexToStartAt = -1;
            if (skipSelected)
            {
                if (selectedRepresentation != null)
                {
                    indexToStartAt = WireframeObjectManager.Self.AllIpsos.IndexOf(selectedRepresentation);
                }
            }
            else
            {
                if (selectedRepresentation != null)
                {
                    if (selectedRepresentation.HasCursorOver(x, y))
                    {
                        ipsoOver = selectedRepresentation;
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

        private IPositionedSizedObject ReverseLoopToFindIpso(float x, float y, int indexToStartAt, int indexToEndAt, bool visibleToCheck, List<ElementWithState> elementStack)
        {
            IPositionedSizedObject ipsoOver = null;

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
                
                GraphicalUiElement ipso = WireframeObjectManager.Self.AllIpsos[i];

                bool visible = IsIpsoVisible(ipso);


                if (visible == visibleToCheck && ipso.HasCursorOver(x, y) && (WireframeObjectManager.Self.IsRepresentation(ipso)))
                {

                    // hold on, even though this is a valid IPSO and the cursor is over it, we gotta see if
                    // it's an instance that is locked.  If so, we shouldn't select it!
                    InstanceSave instanceSave = ipso.Tag as InstanceSave;
                    if (instanceSave == null || instanceSave.Locked == false)
                    {
                        ipsoOver = ipso;
                        break;
                    }
                }

            }

            return ipsoOver;
        }

        private static bool IsIpsoVisible(IPositionedSizedObject ipso)
        {
            bool isVisible = true;
            if(ipso is IVisible)
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


        private Cursor CursorOverHandlesHighlightActivity(Cursor cursorToSet, float worldXAt, float worldYAt)
        {
            if (mResizeHandles.Visible == false)
            {
                SideOver = ResizeSide.None;
            }
            else
            {
                // If the user is already dragging then there's
                // no need to re-check which side the user is over
                if (!Cursor.PrimaryDown && !Cursor.PrimaryClick)
                {
                    SideOver = mResizeHandles.GetSideOver(worldXAt, worldYAt);
                }
            }

            switch (SideOver)
            {
                case ResizeSide.TopLeft:
                case ResizeSide.BottomRight:
                    cursorToSet = Cursors.SizeNWSE;
                    break;
                case ResizeSide.TopRight:
                case ResizeSide.BottomLeft:
                    cursorToSet = Cursors.SizeNESW;
                    break;
                case ResizeSide.Top:
                case ResizeSide.Bottom:
                    cursorToSet = Cursors.SizeNS;
                    break;
                case ResizeSide.Left:
                case ResizeSide.Right:
                    cursorToSet = Cursors.SizeWE;
                    break;
                case ResizeSide.None:

                    break;
            }
            return cursorToSet;
        }


        /// <summary>
        /// Updates the resize handles according to the current object.  We need to do this every
        /// frame because the selected IPSO may be a Sprite that is continually updating itself.
        /// </summary>
        private void UpdateResizeHandles()
        {
            if (SelectedIpsos.Count != 0)
            {
                mResizeHandles.SetValuesFrom(SelectedIpsos);

                mResizeHandles.UpdateHandleRadius();
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
                

                if (Cursor.PrimaryClick)
                {
                    SideOver = ResizeSide.None;
                }
            }
        }

        private void PushAndDoubleClickSelectionActivity()
        {
            try
            {
                // If the SideOver is a non-None
                // value, that means that the object
                // is already selected
                if (SideOver == ResizeSide.None)
                {
                    float x = Cursor.GetWorldX();
                    float y = Cursor.GetWorldY();

                    List<ElementWithState> elementStack = new List<ElementWithState>();
                    elementStack.Add(new ElementWithState(SelectedState.Self.SelectedElement));


                    IPositionedSizedObject representation =
                        GetRepresentationAt(x, y, Cursor.PrimaryDoubleClick, elementStack);
                    ProjectVerifier.Self.AssertIsPartOfRenderer(representation);
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
                            List<IPositionedSizedObject> selectedIpsos = new List<IPositionedSizedObject>();
                            foreach (var instance in SelectedState.Self.SelectedInstances)
                            {
                                selectedIpsos.Add(WireframeObjectManager.Self.GetRepresentation(instance, elementStack));
                            }
                            SelectedIpsos = selectedIpsos;
                        }
                        else
                        {
                            SelectedIpso = representation;
                        }
                    }
                    ProjectVerifier.Self.AssertSelectedIpsosArePartOfRenderer();

                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error in PushAndDoubleClickSelectionActivity: " + e.ToString());
                throw e;
            }

        }

        private static void GetElementOrInstanceForIpso(IPositionedSizedObject representation, List<ElementWithState> elementStack, out InstanceSave selectedInstance, out ElementSave selectedElement)
        {
            selectedInstance = null;
            selectedElement = null;

            IPositionedSizedObject ipsoToUse = representation;

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

        public void ShowSizeHandlesFor(IPositionedSizedObject representation)
        {
            mResizeHandles.Visible = true;
            mResizeHandles.SetValuesFrom(representation);
        }

        public void Refresh()
        {
            Clear();

            List<IPositionedSizedObject> representations = new List<IPositionedSizedObject>();

            var elementStack = SelectedState.Self.GetTopLevelElementStack();
            if (SelectedState.Self.SelectedInstances.GetCount() != 0)
            {
                
                foreach (var instance in SelectedState.Self.SelectedInstances)
                {
                    IPositionedSizedObject toAdd = 
                        WireframeObjectManager.Self.GetRepresentation(instance, elementStack);
                    if (toAdd != null)
                    {
                        representations.Add(toAdd);
                    }
                }
            }
            else if (SelectedState.Self.SelectedElement != null)
            {
                IPositionedSizedObject toAdd =
                    WireframeObjectManager.Self.GetRepresentation(SelectedState.Self.SelectedElement);

                if (toAdd != null)
                {
                    representations.Add(toAdd);
                }
            }

            SelectedIpsos = representations;
        }



        private void Clear()
        {
            HighlightedIpso = null;

            mResizeHandles.Visible = false;
        }
    }
}
