using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gum.Input;
using RenderingLibrary;
using FlatRedBall.AnimationEditorForms.Controls;
using RenderingLibrary.Graphics;

namespace Gum.Wireframe
{
    public class CameraController : Gum.Managers.Singleton<CameraController>
    {
        InputLibrary.Cursor Cursor
        {
            get;
            set;
        }

        global::RenderingLibrary.Camera Camera
        {
            get;
            set;
        }

        WireframeEditControl mWireframeEditControl;

        public void Initialize(InputLibrary.Cursor cursor, Camera camera, WireframeEditControl wireframeEditControl)
        {
            this.Cursor = cursor;
            this.Camera = camera;
            this.mWireframeEditControl = wireframeEditControl;


            Renderer.Self.Camera.X = Renderer.Self.GraphicsDevice.Viewport.Width / 2 - 30;
            Renderer.Self.Camera.Y = Renderer.Self.GraphicsDevice.Viewport.Height / 2 - 30;
        }


        public void HandleMouseWheel(object sender, MouseEventArgs e)
        {
            float worldX = this.Cursor.GetWorldX();
            float worldY = this.Cursor.GetWorldY();

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

        }

        public void CameraMovementAndZoomActivity()
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


        internal void HandleKeyPress(KeyEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (e.KeyCode == System.Windows.Forms.Keys.Up)
                {
                    SystemManagers.Default.Renderer.Camera.Y -= 10 / SystemManagers.Default.Renderer.Camera.Zoom;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Down)
                {
                    SystemManagers.Default.Renderer.Camera.Y += 10 / SystemManagers.Default.Renderer.Camera.Zoom;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Left)
                {
                    SystemManagers.Default.Renderer.Camera.X -= 10 / SystemManagers.Default.Renderer.Camera.Zoom;

                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Right)
                {
                    SystemManagers.Default.Renderer.Camera.X += 10 / SystemManagers.Default.Renderer.Camera.Zoom;

                }
                else if(e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
                {
                    mWireframeEditControl.ZoomIn();
                }
                else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
                {
                    mWireframeEditControl.ZoomOut();
                }
            }

        }
    }
}
