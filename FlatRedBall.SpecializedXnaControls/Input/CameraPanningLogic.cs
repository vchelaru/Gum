using System;
using XnaAndWinforms;
using RenderingLibrary;
using Cursor = InputLibrary.Cursor;
using InputLibrary;

namespace FlatRedBall.SpecializedXnaControls.Input
{
    public class CameraPanningLogic
    {
        Camera mCamera;
        GraphicsDeviceControl mControl;

        Cursor mCursor;
        Keyboard mKeyboard;

        SystemManagers mManagers;

        public event Action? Panning;

        public bool IsHotkeyPanningEnabled
        {
            get;
            set;
        } = true;

        public CameraPanningLogic(GraphicsDeviceControl graphicsControl, SystemManagers managers, Cursor cursor, Keyboard keyboard)
        {
            mManagers = managers;

            mKeyboard = keyboard;

            mCursor = cursor;
            mCursor.Initialize(graphicsControl);
            mCamera = managers.Renderer.Camera;
            mControl = graphicsControl;
            graphicsControl.XnaUpdate += new Action(Activity);

        }

        void Activity()
        {
            if (mKeyboard != null && IsHotkeyPanningEnabled)
            {
                DoHotkeyPanning();
            }

            if (mCursor.MiddleDown &&
                mCursor.IsInWindow &&
                (mCursor.XChange != 0 || mCursor.YChange != 0)
                )
            {


                mCamera.X -= mCursor.XChange / mManagers.Renderer.Camera.Zoom;
                mCamera.Y -= mCursor.YChange / mManagers.Renderer.Camera.Zoom;
                if (Panning != null)
                {
                    Panning();
                }

            }
        }

        private void DoHotkeyPanning()
        {
            bool isCtrlDown = mKeyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) ||
                mKeyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl);

            if (isCtrlDown)
            {
                const int movementCoefficient = 20;
                if (mKeyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Up))
                {
                    mCamera.Y -= movementCoefficient / mCamera.Zoom;
                    if (Panning != null)
                    {
                        Panning();
                    }
                }
                else if (mKeyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Down))
                {
                    mCamera.Y += movementCoefficient / mCamera.Zoom;
                    if (Panning != null)
                    {
                        Panning();
                    }
                }
                else if (mKeyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Left))
                {
                    mCamera.X -= movementCoefficient / mCamera.Zoom;
                    if (Panning != null)
                    {
                        Panning();
                    }
                }
                else if (mKeyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Right))
                {
                    mCamera.X += movementCoefficient / mCamera.Zoom;
                    if (Panning != null)
                    {
                        Panning();
                    }
                }

                //else if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
                //{
                //    mWireframeEditControl.ZoomIn();
                //}
                //else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
                //{
                //    mWireframeEditControl.ZoomOut();
                //}
            }
        }
    }
}
