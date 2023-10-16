using RenderingLibrary.Graphics;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Matrix = System.Numerics.Matrix4x4;

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

        public float X
        {
            get => Position.X; 
            set => Position.X = value;
        }

        public float Y
        {
            get { return Position.Y; }
            set
            {
                Position.Y = value;
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


        public Camera(SystemManagers managers)
        {
            Zoom = 1;
            mManagers = managers;
            UpdateClient();
        }

        public static Matrix GetTransformationMatrix(float x, float y, float zoom, int clientWidth, int clientHeight, bool forRendering = false)
        {
            if (Renderer.UseBasicEffectRendering && forRendering)
            {
                return
                    Matrix.CreateTranslation(new Vector3(-x, -y, 0)) *
                    Matrix.CreateTranslation(new Vector3(0, 0, 0)) *
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
    }
}
