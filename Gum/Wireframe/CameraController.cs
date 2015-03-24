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
        global::RenderingLibrary.Camera Camera
        {
            get;
            set;
        }

        WireframeEditControl mWireframeEditControl;
        System.Drawing.Point mLastMouseLocation;

        public void Initialize(Camera camera, WireframeEditControl wireframeEditControl, int defaultWidth, int defaultHeight)
        {
            Camera = camera;
            mWireframeEditControl = wireframeEditControl;

            Renderer.Self.Camera.X = defaultWidth / 2 - 30;
            Renderer.Self.Camera.Y = defaultHeight / 2 - 30;
        }

        internal void HandleMouseWheel(object sender, MouseEventArgs e)
        {
            float worldX, worldY;
            Camera.ScreenToWorld(e.X, e.Y, out worldX, out worldY);

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

        internal void HandleMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                mLastMouseLocation = e.Location;
            }
        }

        internal void HandleMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                int xChange = e.X - mLastMouseLocation.X;
                int yChange = e.Y - mLastMouseLocation.Y;

                Renderer.Self.Camera.Position.X -= xChange / Renderer.Self.Camera.Zoom;
                Renderer.Self.Camera.Position.Y -= yChange / Renderer.Self.Camera.Zoom;

                Gum.ToolCommands.GuiCommands.Self.RefreshWireframe();

                mLastMouseLocation = e.Location;
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
                else if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
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
