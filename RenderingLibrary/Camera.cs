using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace RenderingLibrary
{
    #region Enums

    public enum CameraCenterOnScreen
    {
        Center,
        TopLeft
    }

    #endregion

    public class Camera
    {
        #region Fields

        public Vector2 Position;
        SystemManagers mManagers;

        #endregion

        #region Properties

        public float AbsoluteTop
        {
            get
            {
                if(this.CameraCenterOnScreen == CameraCenterOnScreen.Center)
                {
                    return Y - (ClientHeight / 2.0f) / Zoom;
                }
                else
                {
                    return Y;
                }
            }
            set
            {
                if(this.CameraCenterOnScreen == CameraCenterOnScreen.Center)
                {
                    Y = value + (ClientHeight / 2.0f) / Zoom;
                }
                else
                {
                    Y = value;
                }
            }
        }

        public float AbsoluteBottom
        {
            get
            {
                return AbsoluteTop + ClientHeight / Zoom;
            }
        }

        public float AbsoluteLeft
        {
            get
            {
                if(this.CameraCenterOnScreen == CameraCenterOnScreen.Center)
                {
                    return X - (ClientWidth / 2.0f) / Zoom;
                }
                else
                {
                    return X;
                }
            }
            set
            {
                if(this.CameraCenterOnScreen == CameraCenterOnScreen.Center)
                {
                    X = value + (ClientWidth / 2.0f) / Zoom;
                }
                else
                {
                    X = value;
                }
            }
        }

        public float AbsoluteRight
        {
            get
            {

                return AbsoluteLeft + ClientWidth / Zoom;
            }
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

        /// <summary>
        /// The zoom value for everything on this camera. Default value of 1.
        /// A value of 2 will make everything appear twice as large.
        /// </summary>
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

        public Matrix GetTransformationMatrix(bool forRendering = false)
        {
            if (CameraCenterOnScreen == RenderingLibrary.CameraCenterOnScreen.Center)
            {
                // make local vars to make stepping in faster if debugging
                var x = X;
                var y = Y;
                var zoom = Zoom;
                var width = ClientWidth;
                var height = ClientHeight;
                return Camera.GetTransformationMatrix(x, y, zoom, width, height, forRendering);
            }
            else
            {
                return Matrix.CreateTranslation(-X,-Y,0) *
                                         Matrix.CreateScale(new Vector3(Zoom, Zoom, 1));
            }
        }

        public static Matrix GetTransformationMatrix(float x, float y, float zoom, int clientWidth, int clientHeight, bool forRendering = false)
        {
            if(Renderer.UseBasicEffectRendering && forRendering)
            {
                return
                    Matrix.CreateTranslation(new Vector3(-x, -y, 0)) *
                    Matrix.CreateTranslation(new Vector3(0, 0, 0))*
                    Matrix.CreateScale(new Vector3(zoom, zoom, 1)) 
                   ;
            }
            else
            {

                return 
                    Matrix.CreateTranslation(new Vector3(-x, -y, 0)) *
                    Matrix.CreateScale(new Vector3(zoom, zoom, 1)) *
                    Matrix.CreateTranslation(new Vector3(clientWidth * 0.5f, clientHeight * 0.5f, 0));
            }
        }


        public void ScreenToWorld(float screenX, float screenY, out float worldX, out float worldY)
        {
            Matrix matrix = Matrix.Invert(GetTransformationMatrix());

            Vector3 position = new Vector3(screenX, screenY, 0);
            Vector3 transformed;
            Vector3.Transform(ref position, ref matrix, out transformed);

            worldX = transformed.X;
            worldY = transformed.Y;
        }

        public void WorldToScreen(float worldX, float worldY, out float screenX, out float screenY)
        {
            Matrix matrix = GetTransformationMatrix();

            Vector3 position = new Vector3(worldX, worldY, 0);
            Vector3 transformed;
            Vector3.Transform(ref position, ref matrix, out transformed);

            screenX = transformed.X;
            screenY = transformed.Y;
        }

        // Not sure why but for some reason the GraphicsDevice would
        // return its viewport as different managers- perhaps there is
        // only one graphics device and the viewport is switched when it
        // renders?  Hard to say.
        internal void UpdateClient()
        {
#if MONOGAME
            if (Renderer.GraphicsDevice != null)
            {
                ClientWidth = Renderer.GraphicsDevice.Viewport.Width;
                ClientHeight = Renderer.GraphicsDevice.Viewport.Height;
            }
#endif
        }

        #endregion
    }
}
