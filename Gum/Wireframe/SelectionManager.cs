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

        List<LineRectangle> mHighlightRectangles = new List<LineRectangle>();
        SolidRectangle mOverlaySolidRectangle;
        Sprite mOverlaySprite;
        NineSlice mOverlayNineSlice;


        ResizeHandles mResizeHandles;

        List<IPositionedSizedObject> mSelectedIpsos = new List<IPositionedSizedObject>();
        IPositionedSizedObject mHighlightedIpso;


        Layer mUiLayer;

        #endregion

        #region Properties


        public int SelectionBorder
        {
            get;
            set;
        }

        InputLibrary.Cursor Cursor
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
                        UnhighlightIpso(mHighlightedIpso);
                    }



                    mHighlightedIpso = value;

                    if (mHighlightedIpso != null)
                    {
                        SetLineRectangleAroundIpso(mHighlightRectangles[0], mHighlightedIpso);
                    }
                    else
                    {
                        mHighlightRectangles[0].Visible = false;
                    }
                }
            }
        }

        private void UnhighlightIpso(IPositionedSizedObject highlightedIpso)
        {
            if (highlightedIpso is Sprite)
            {
                mOverlaySprite.Visible = false;
            }
            else if (highlightedIpso is NineSlice)
            {
                mOverlayNineSlice.Visible = false;
            }
            else if (highlightedIpso is LineRectangle)
            {
                mOverlaySolidRectangle.Visible = false;
            }
        }

        public Sprite HighlightedSprite
        {
            get
            {
                return HighlightedIpso as Sprite;
            }
        }

        public NineSlice HighlightedNineSlice
        {
            get
            {
                return HighlightedIpso as NineSlice;
            }
        }

        public LineRectangle HighlightedLineRectangle
        {
            get
            {
                return HighlightedIpso as LineRectangle;
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
            SelectionBorder = 0;
            mUiLayer = Renderer.Self.AddLayer();
            mUiLayer.Name = "UI Layer";

            LineRectangle selection = new LineRectangle();
            selection.Color = Color.Yellow;
            selection.Visible = false;
            mHighlightRectangles.Add(selection);
            ShapeManager.Self.Add(selection, mUiLayer);

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
                            representation =
                                GetRepresentationAt(worldXAt, worldYAt, false);

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




        public IPositionedSizedObject GetRepresentationAt(float x, float y, bool skipSelected)
        {

            IPositionedSizedObject ipsoOver = null;

            if (InputLibrary.Cursor.Self.PrimaryPush)
            {
                int m = 3;
            }
            // First check if we're over the current
            IPositionedSizedObject selectedRepresentation = WireframeObjectManager.Self.GetSelectedRepresentation();

            int indexToStartAt = -1;
            if (skipSelected)
            {
                if (selectedRepresentation != null)
                {
                    indexToStartAt = Renderer.Self.Layers[0].Renderables.IndexOf(selectedRepresentation as IRenderable);
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

                Layer layer = Renderer.Self.Layers[0];
                if (indexToStartAt == -1)
                {
                    indexToStartAt = layer.Renderables.Count;
                }

                ipsoOver = ReverseLoopToFindIpso(x, y, indexToStartAt - 1, -1);

                if (ipsoOver == null && indexToStartAt != layer.Renderables.Count - 1)
                {
                    ipsoOver = ReverseLoopToFindIpso(x, y, layer.Renderables.Count - 1, indexToStartAt);
                }
            }

            // Right now we're going to assume that we only want to select IPSOs that represent the current
            // element or its InstanceSaves - not any children.  So we're going to get the InstanceSave - if that's
            // null, get the ElementSave
            if (ipsoOver != null)
            {
                InstanceSave instance;
                ElementSave element;

                GetElementOrInstanceForIpso(ipsoOver, out instance, out element);

                if (instance != null)
                {
                    ipsoOver = WireframeObjectManager.Self.GetRepresentation(instance);
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

        private IPositionedSizedObject ReverseLoopToFindIpso(float x, float y, int indexToStartAt, int indexToEndAt)
        {
            IPositionedSizedObject ipsoOver = null;

            Layer layer = Renderer.Self.Layers[0];

            // Let's try to get visible ones first, then if we don't find anything, look at invisible ones
            for (int i = indexToStartAt; i > indexToEndAt; i--)
            {
                if (layer.Renderables[i] is IPositionedSizedObject)
                {
                    IPositionedSizedObject ipso = layer.Renderables[i] as IPositionedSizedObject;

                    bool visible = IsIpsoVisible(ipso);
                    

                    if (visible && ipso.HasCursorOver(x, y) && (WireframeObjectManager.Self.IsRepresentation(ipso)))
                    {
                        // hold on, even though this is a valid IPSO and the cursor is over it, we gotta see if
                        // it's an instance that is locked.  If so, we shouldn't select it!
                        InstanceSave instanceSave = WireframeObjectManager.Self.GetInstance(ipso, InstanceFetchType.InstanceInCurrentElement);
                        if (instanceSave == null || instanceSave.Locked == false)
                        {
                            ipsoOver = ipso;
                            break;
                        }
                    }

                }
            }

            if (ipsoOver == null)
            {
                // now invisible
                for (int i = indexToStartAt; i > indexToEndAt; i--)
                {
                    if (layer.Renderables[i] is IPositionedSizedObject)
                    {
                        IPositionedSizedObject ipso = layer.Renderables[i] as IPositionedSizedObject;
                        bool visible = IsIpsoVisible(ipso);

                        if (!visible && ipso.HasCursorOver(x, y) && (WireframeObjectManager.Self.IsRepresentation(ipso)))
                        {
                            // hold on, even though this is a valid IPSO and the cursor is over it, we gotta see if
                            // it's an instance that is locked.  If so, we shouldn't select it!
                            InstanceSave instanceSave = WireframeObjectManager.Self.GetInstance(ipso, InstanceFetchType.InstanceInCurrentElement);
                            if (instanceSave == null || instanceSave.Locked == false)
                            {
                                ipsoOver = ipso;
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
            if (ipso is Sprite)
            {
                isVisible = ((Sprite)ipso).AbsoluteVisible;
            }
            if (ipso is Text)
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

                    IPositionedSizedObject representation =
                        GetRepresentationAt(x, y, Cursor.PrimaryDoubleClick);
                    ProjectVerifier.Self.AssertIsPartOfRenderer(representation);
                    bool hasChanged = true;

                    if (representation != null)
                    {
                        InstanceSave selectedInstance;
                        ElementSave selectedElement;
                        GetElementOrInstanceForIpso(representation, out selectedInstance, out selectedElement);

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
                                representation = WireframeObjectManager.Self.GetRepresentation(selectedInstance);
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
                                selectedIpsos.Add(WireframeObjectManager.Self.GetRepresentation(instance));
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

        private static void GetElementOrInstanceForIpso(IPositionedSizedObject representation, out InstanceSave selectedInstance, out ElementSave selectedElement)
        {
            selectedInstance = null;
            selectedElement = null;

            IPositionedSizedObject topParent = representation.GetTopParent();


            InstanceSave topParentInstanceSave = WireframeObjectManager.Self.GetInstance(topParent, InstanceFetchType.InstanceInCurrentElement);

            if (topParentInstanceSave == null)
            {
                // We're inside a Component
                if (representation.Name == SelectedState.Self.SelectedElement.Name)
                {
                    selectedElement = SelectedState.Self.SelectedElement;
                }
                else
                {
                    InstanceSave representationInstance = WireframeObjectManager.Self.GetInstance(representation, InstanceFetchType.InstanceInCurrentElement);
                    selectedInstance = representationInstance;
                }
            }
            else
            {
                // We're inside a Screen
                if (representation.Parent == topParent || representation == topParent)
                {
                    selectedInstance = topParentInstanceSave;
                }
                else
                {
                    ElementSave elementSave = WireframeObjectManager.Self.GetElement(representation);

                    if (elementSave != null)
                    {
                        selectedInstance = null;
                        selectedElement = elementSave;
                    }

                }
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

            if (SelectedState.Self.SelectedInstances.GetCount() != 0)
            {
                foreach (var instance in SelectedState.Self.SelectedInstances)
                {
                    IPositionedSizedObject toAdd = 
                        WireframeObjectManager.Self.GetRepresentation(instance);
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

        private void SetLineRectangleAroundIpso(LineRectangle rectangle, IPositionedSizedObject pso )
        {
            float adjustedSelectionBorder = SelectionBorder / Renderer.Self.Camera.Zoom;

            rectangle.Visible = true;
            rectangle.X = pso.GetAbsoluteX() - adjustedSelectionBorder;
            rectangle.Y = pso.GetAbsoluteY() - adjustedSelectionBorder;


            if ((pso.Width == 0 || pso.Height == 0) && pso is Sprite)
            {
                Sprite asSprite = pso as Sprite;

                rectangle.Width = asSprite.EffectiveWidth + adjustedSelectionBorder * 2;
                rectangle.Height = asSprite.EffectiveHeight + adjustedSelectionBorder * 2;
            }
            else
            {
                rectangle.Width = pso.Width + adjustedSelectionBorder * 2;
                rectangle.Height = pso.Height + adjustedSelectionBorder * 2;
            }
        }

        private void Clear()
        {
            HighlightedIpso = null;

            mResizeHandles.Visible = false;
        }
    }
}
