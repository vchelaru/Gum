using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XnaAndWinforms;
using Microsoft.Xna.Framework;
using System.Windows.Forms;
using RenderingLibrary.Math.Geometry;
using RenderingLibrary.Graphics;
using RenderingLibrary;
using Gum.DataTypes;
using Gum.ToolStates;
using Gum.Input;
using RenderingLibrary.Content;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.ToolCommands;
using System.ComponentModel.Composition;
using FlatRedBall.AnimationEditorForms.Controls;
using Gum.Debug;
using Microsoft.Xna.Framework.Graphics;

using WinCursor = System.Windows.Forms.Cursor;

namespace Gum.Wireframe
{
    #region WireframeControlPlugin Class

    [Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
    public class WireframeControlPlugin : InternalPlugin
    {

        public override void StartUp()
        {
            this.ProjectLoad += new Action<GumProjectSave>(OnProjectLoad);
        }

        void OnProjectLoad(GumProjectSave obj)
        {
            GuiCommands.Self.RefreshWireframeDisplay();
        }
    }

    #endregion

    public class WireframeControl : GraphicsDeviceControl
    {
        #region Fields

        WireframeEditControl mWireframeEditControl;

        LineRectangle mScreenBounds;

        public Color BackgroundColor = Color.DimGray;
        public Color ScreenBoundsColor = Color.LightBlue;

        bool mHasInitialized = false;

        Ruler mTopRuler;
        Ruler mLeftRuler;

        #endregion

        #region Properties

        new InputLibrary.Cursor Cursor
        {
            get
            {
                return InputLibrary.Cursor.Self;
            }
        }

        Camera Camera
        {
            get { return Renderer.Self.Camera; }
        }

        #endregion

        public event EventHandler AfterXnaInitialize;

        #region Event Methods


        void OnKeyDown(object sender, KeyEventArgs e)
        {
            HandleCopyCutPaste(e);

            HandleDelete(e);

            HandleNudge(e);
        }


        void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            int m = 3;

        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            int nudgeX = 0;
            int nudgeY = 0;

            if (keyData == Keys.Up)
            {
                nudgeY = -1;
            }
            if (keyData == Keys.Down)
            {
                nudgeY = 1;
            }
            if (keyData == Keys.Right)
            {
                nudgeX = 1;
            }
            if (keyData == Keys.Left)
            {
                nudgeX = -1;
            }

            if (nudgeX != 0 || nudgeY != 0)
            {
                EditingManager.Self.MoveSelectedObjectsBy(nudgeX, nudgeY);
                //e.Handled = true;
                //e.SuppressKeyPress = true;
                return true;
            }
            else
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }
        //protected override bool ProcessKeyEventArgs(ref Message m, Keys keyData)
        //{
        //    if (keyData == Keys.Left)
        //    {
        //        Console.WriteLine("left");
        //        return true;
        //    }
        //    // etc..
        //    return base.ProcessCmdKey(ref msg, keyData);
        //}

        private void HandleNudge(KeyEventArgs e)
        {
            int nudgeX = 0;
            int nudgeY = 0;

            if (e.KeyCode == Keys.Up)
            {
                nudgeY = -1;
            }
            if (e.KeyCode == Keys.Down)
            {
                nudgeY = 1;
            }
            if (e.KeyCode == Keys.Right)
            {
                nudgeX = 1;
            }
            if (e.KeyCode == Keys.Left)
            {
                nudgeX = -1;
            }

            if (nudgeX != 0 || nudgeY != 0)
            {
                EditingManager.Self.MoveSelectedObjectsBy(nudgeX, nudgeY);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            
        }

        public void HandleDelete(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                EditingManager.Self.OnDelete();

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        public void HandleCopyCutPaste(KeyEventArgs e)
        {
            if ((e.Modifiers & Keys.Control) == Keys.Control)
            {
                // copy, ctrl c, ctrl + c
                if (e.KeyCode == Keys.C)
                {
                    EditingManager.Self.OnCopy(CopyType.Instance);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                // paste, ctrl v, ctrl + v
                else if (e.KeyCode == Keys.V)
                {
                    EditingManager.Self.OnPaste(CopyType.Instance);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                // cut, ctrl x, ctrl + x
                else if (e.KeyCode == Keys.X)
                {
                    EditingManager.Self.OnCut(CopyType.Instance);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }
        

        #endregion

        #region Initialize Methods

        public void Initialize(WireframeEditControl wireframeEditControl)
        {
            try
            {
                XnaUpdate += HandleXnaUpdate;
                mWireframeEditControl = wireframeEditControl;

                mWireframeEditControl.ZoomChanged += HandleZoomChanged;

                Renderer.Self.Initialize(GraphicsDevice, null);

                Renderer.Self.SamplerState = SamplerState.PointWrap;

                LoaderManager.Self.Initialize(null, "content/TestFont.fnt", Services, null);

                InputLibrary.Cursor.Self.Initialize(this);


                mScreenBounds = new LineRectangle();
                mScreenBounds.Width = 800;
                mScreenBounds.Height = 600;
                mScreenBounds.Color = ScreenBoundsColor;
                ShapeManager.Self.Add(mScreenBounds, SelectionManager.Self.UiLayer);


                Renderer.Self.Camera.X = Renderer.Self.GraphicsDevice.Viewport.Width / 2 - 30;
                Renderer.Self.Camera.Y = Renderer.Self.GraphicsDevice.Viewport.Height / 2 - 30;
                

                this.KeyDown += new KeyEventHandler(OnKeyDown);
                this.KeyPress += new KeyPressEventHandler(OnKeyPress);
                this.MouseWheel += new MouseEventHandler(HandleMouseWheel);

                this.mTopRuler = new Ruler(this, null, InputLibrary.Cursor.Self);
                mLeftRuler = new Ruler(this, null, InputLibrary.Cursor.Self);
                mLeftRuler.RulerSide = RulerSide.Left;

                if (AfterXnaInitialize != null)
                {
                    AfterXnaInitialize(this, null);
                }

                UpdateWireframeToProject();

                mHasInitialized = true;
            }
            catch(Exception exception)
            {
                MessageBox.Show("Error initializing the wireframe control\n\n" + exception);
                int m = 3;

            }
        }

        void HandleZoomChanged(object sender, EventArgs e)
        {

            this.mLeftRuler.ZoomValue = mWireframeEditControl.PercentageValue / 100.0f;
            this.mTopRuler.ZoomValue = mWireframeEditControl.PercentageValue / 100.0f;
        }

        void HandleXnaUpdate()
        {

        }

        void HandleMouseWheel(object sender, MouseEventArgs e)
        {
            float worldX = Cursor.GetWorldX();
            float worldY = Cursor.GetWorldY();

            float differenceX = Camera.X - worldX;
            float differenceY = Camera.Y - worldY;

            float oldZoom = Camera.Zoom;

            if (e.Delta < 0)
            {
                mWireframeEditControl.ZoomOut();
            }
            else
            {
                mWireframeEditControl.ZoomIn();
            }

            float newDifferenceX = differenceX * oldZoom / Camera.Zoom;
            float newDifferenceY = differenceY * oldZoom / Camera.Zoom;

            Camera.X = worldX + newDifferenceX;
            Camera.Y = worldY + newDifferenceY;


            //Renderer.Self.Camera.X = worldX;
            //Renderer.Self.Camera.Y = worldY;
        }

        #endregion

        


        void Activity()
        {
#if DEBUG
            try
#endif
            {
                InputLibrary.Cursor.Self.StartCursorSettingFrameStart();
                ProjectVerifier.Self.AssertSelectedIpsosArePartOfRenderer();
                TimeManager.Self.Activity();

                SpriteManager.Self.Activity(TimeManager.Self.CurrentTime);

                DragDropManager.Self.Activity();

                InputLibrary.Cursor.Self.Activity(TimeManager.Self.CurrentTime);
                CameraMovementAndZoomActivity();
                if (Cursor.PrimaryPush)
                {
                    int m = 3;
                }

                bool isOver = this.mTopRuler.HandleXnaUpdate(InputLibrary.Cursor.Self.IsInWindow) ||
                    mLeftRuler.HandleXnaUpdate(InputLibrary.Cursor.Self.IsInWindow);



                // But we want the selection to update the handles to the selected object
                // after editing is done.  SelectionManager.LateActivity lets us do that.  LateActivity must
                // come after EidtingManager.Activity.
                if (mTopRuler.IsCursorOver == false && mLeftRuler.IsCursorOver == false)
                {
                    SelectionManager.Self.Activity(this);
                    // EditingManager activity must happen after SelectionManager activity
                    EditingManager.Self.Activity();


                    SelectionManager.Self.LateActivity();
                }
                InputLibrary.Cursor.Self.EndCursorSettingFrameStart();

            }
#if DEBUG
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
#endif
        }


        public void UpdateWireframeToProject()
        {
            if (mScreenBounds != null && ProjectManager.Self.GumProjectSave != null)
            {
                mScreenBounds.Width = ProjectManager.Self.GumProjectSave.DefaultCanvasWidth;
                mScreenBounds.Height = ProjectManager.Self.GumProjectSave.DefaultCanvasHeight;
            }
        }


        private void CameraMovementAndZoomActivity()
        {

            if (Cursor.IsInWindow)
            {
                if (Cursor.MiddleDown)
                {
                    Renderer.Self.Camera.Position.X -= InputLibrary.Cursor.Self.XChange / Renderer.Self.Camera.Zoom;
                    Renderer.Self.Camera.Position.Y -= InputLibrary.Cursor.Self.YChange / Renderer.Self.Camera.Zoom;
                }
            }
        }



        protected override void Draw()
        {
            try
            {
                if (mHasInitialized)
                {
                    Activity();

                    GraphicsDevice.Clear(BackgroundColor);

                    Renderer.Self.Draw(null);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw e;
            }
            
        }


    }
}
