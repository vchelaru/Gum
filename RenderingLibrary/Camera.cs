using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace RenderingLibrary
{
    public enum CameraCenterOnScreen
    {
        Center,
        TopLeft
    }


    public class Camera
    {
        #region Fields

        public Vector2 Position;
        SystemManagers mManagers;



        #endregion

        #region Properties

        public float AbsoluteTop
        {
            get { return Y - (ClientHeight / 2.0f) / Zoom; }
            set
            {
                Y = value + (ClientHeight / 2.0f) / Zoom;
            }
        }

        public float AbsoluteBottom
        {
            get { return Y + (ClientHeight / 2.0f) / Zoom; }
        }

        public float AbsoluteLeft
        {
            get { return X - (ClientWidth / 2.0f) / Zoom; }
            set
            {
                X = value + (ClientWidth / 2.0f) / Zoom;
            }
        }

        public float AbsoluteRight
        {
            get { return X + (ClientWidth / 2.0f) / Zoom; }
        }


        public float X
        {
            get { return Position.X; }
            set 
            { 
                Position.X = value; 
            }
        }

        public float Y
        {
            get { return Position.Y; }
            set 
            { 
                Position.Y = value; 
            }
        }

        public int RenderingXOffset
        {
            get
            {
                return 0;// (int)(ClientWidth / 2 - (int)Position.X);
            }
        }

        public int RenderingYOffset
        {
            get
            {
                return 0;// (int)(ClientHeight / 2 - (int)Position.Y);
            }
        }

        Renderer Renderer
        {
            get
            {
                if (mManagers == null)
                {
                    return Renderer.Self;
                }
                else
                {
                    return mManagers.Renderer;
                }
            }
        }


        public int ClientWidth
        {
            //get
            //{
            //    return Renderer.GraphicsDevice.Viewport.Width;
            //}
            get;
            private set;

        }

        public int ClientHeight
        {
            //get
            //{
            //    return Renderer.GraphicsDevice.Viewport.Height;
            //}
            get;
            private set;
        }


        public float Zoom
        {
            get;
            set;
        }

        public CameraCenterOnScreen CameraCenterOnScreen
        {
            get;
            set;
        }

        #endregion

        #region Methods


        public Camera(SystemManagers managers)
        {
            Zoom = 1;
            mManagers = managers;
            UpdateClient();
        }

        public Matrix GetTransformationMatrix()
        {
            if (CameraCenterOnScreen == RenderingLibrary.CameraCenterOnScreen.Center)
            {
                return Camera.GetTransformationMatirx(X, Y, Zoom, ClientWidth, ClientHeight);
            }
            else
            {
                return Matrix.CreateTranslation(-X,-Y,0) *
                                         Matrix.CreateScale(new Vector3(Zoom, Zoom, 1));
            }
        }

        public static Matrix GetTransformationMatirx(float x, float y, float zoom, int clientWidth, int clientHeight)
        {
            Matrix matrix;
            
            matrix =       
              Matrix.CreateTranslation(new Vector3(-x, -y, 0)) *
                                         Matrix.CreateScale(new Vector3(zoom, zoom, 1)) *
                                         Matrix.CreateTranslation(new Vector3(clientWidth * 0.5f, clientHeight * 0.5f, 0));
            return matrix;

        }


        public void ScreenToWorld(float screenX, float screenY, out float worldX, out float worldY)
        {
            Vector3 transformed = new Vector3(screenX, screenY, 0);
            Matrix matrix = GetTransformationMatrix();
            matrix = Matrix.Invert(matrix);

            TransformVector(ref transformed, ref matrix);

            worldX = transformed.X;
            worldY = transformed.Y;
        }

        public static void TransformVector(ref Vector3 vectorToTransform, ref Matrix matrixToTransformBy)
        {

            Vector3 transformed = Vector3.Zero;

            transformed.X =
                matrixToTransformBy.M11 * vectorToTransform.X +
                matrixToTransformBy.M21 * vectorToTransform.Y +
                matrixToTransformBy.M31 * vectorToTransform.Z +
                matrixToTransformBy.M41;

            transformed.Y =
                matrixToTransformBy.M12 * vectorToTransform.X +
                matrixToTransformBy.M22 * vectorToTransform.Y +
                matrixToTransformBy.M32 * vectorToTransform.Z +
                matrixToTransformBy.M42;

            transformed.Z =
                matrixToTransformBy.M13 * vectorToTransform.X +
                matrixToTransformBy.M23 * vectorToTransform.Y +
                matrixToTransformBy.M33 * vectorToTransform.Z +
                matrixToTransformBy.M43;

            vectorToTransform = transformed;
        }

        // Not sure why but for some reason the GraphicsDevice would
        // return its viewport as different managers- perhaps there is
        // only one graphics device and the viewport is switched when it
        // renders?  Hard to say.
        internal void UpdateClient()
        {
            if (Renderer.GraphicsDevice != null)
            {
                ClientWidth = Renderer.GraphicsDevice.Viewport.Width;
                ClientHeight = Renderer.GraphicsDevice.Viewport.Height;
            }
        }

        #endregion

    }
}
