using System.Numerics;
using RenderingLibrary.Graphics;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Matrix = System.Numerics.Matrix4x4;
using System;

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

        public float AbsoluteBottom => AbsoluteTop + ClientHeight / Zoom;

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

        public float AbsoluteRight => AbsoluteLeft + ClientWidth / Zoom;


        public float X
        {
            get => Position.X;
            set => Position.X = value;
        }

        public float Y
        {
            get => Position.Y; 
            set => Position.Y = value; 
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

        public static float PixelPerfectOffsetX = .0f;
        public static float PixelPerfectOffsetY = .0f;

        public int ClientWidth { get; set; }

        public int ClientHeight { get; set; }

        public int ClientLeft { get; set; }
        public int ClientTop { get; set; }

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


        public Camera()
        {
            Zoom = 1;
        }

        public Matrix GetTransformationMatrix(bool forRendering = false)
        {

            var x = X;
            var y = Y;
            var zoom = Zoom;
            var width = ClientWidth;
            var height = ClientHeight;

            //var effectiveX = x;
            //var effectiveY = y;
            //if(CameraCenterOnScreen != RenderingLibrary.CameraCenterOnScreen.Center)
            //{
            //    effectiveX = x + width / (2.0f * zoom);
            //    effectiveY = y + height / (2.0f * zoom);
            //}

            //effectiveX = ((int)(effectiveX * zoom)) / zoom;
            //effectiveY = ((int)(effectiveY * zoom)) / zoom;

            //return Camera.GetTransformationMatrix(effectiveX, effectiveY, zoom, width, height, forRendering);
            x = ((int)(x * zoom)) / zoom;
            y = ((int)(y * zoom)) / zoom;

            if (CameraCenterOnScreen == RenderingLibrary.CameraCenterOnScreen.Center)
            {
                if(ClientWidth % 2 == 1)
                {
                    x += .5f / zoom;
                }
                if(ClientHeight % 2 == 1)
                {
                    y += .5f / zoom;
                }
                // make local vars to make stepping in faster if debugging
                return GetTransformationMatrix(x, y, zoom, width, height, forRendering);
            }
            else
            {
                return Matrix.CreateTranslation(-x, -y, 0) *
                                         Matrix.CreateScale(new Vector3(zoom, zoom, 1));
            }
        }

        public static Matrix GetTransformationMatrix(float x, float y, float zoom, int clientWidth, int clientHeight, bool forRendering = false)
        {
            // Vic says - I don't know exactly why this code is needed. I don't understand it 
            // well enough to address it now, but I need to make this compile so I'm going to
            // refactor out the Renderer usage.
            if (RendererSettings.UsingEffect)
            {
                return
                    Matrix.CreateTranslation(new Vector3(-x, -y, 0)) *
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


        /// <summary>
        /// Overwrites this camera's <see cref="X"/>, <see cref="Y"/>, and <see cref="Zoom"/> by
        /// decomposing the supplied world-to-screen transform. This is the inverse of
        /// <see cref="GetTransformationMatrix(bool)"/> and is used by
        /// <c>GumService.Draw(Matrix)</c> so that callers can drive Gum's camera from a
        /// platform-native transform (raylib's <c>Camera2D</c> via <c>GetCameraMatrix2D</c>,
        /// MonoGame's view matrix, etc.). The current <see cref="CameraCenterOnScreen"/> mode
        /// selects which decomposition formula is applied, so set it before calling this if you
        /// are not using the default <see cref="CameraCenterOnScreen.Center"/>.
        ///
        /// Only translation and uniform scale are extracted. Rotation, shear, and non-uniform
        /// scale in the matrix render correctly (BeginMode2D/SpriteBatch consume the matrix
        /// verbatim), but Gum's layout, hit-testing, and <c>ScreenPixel</c> stroke resolution
        /// do not see rotation. If you need rotated input compensation, set the cursor transform
        /// separately.
        /// </summary>
        public void SetFromMatrix(Matrix matrix)
        {
            float zoom = matrix.M11;
            if (zoom == 0)
            {
                zoom = 1;
            }

            // Mirror the conditional in GetTransformationMatrix: the center translation
            // T(ClientWidth/2, ClientHeight/2) is only baked into the matrix in Center mode
            // when RendererSettings.UsingEffect is false. Otherwise the matrix is just
            // T(-x,-y) * S(zoom). Invert whichever form was produced.
            bool matrixIncludesCenterTranslation =
                CameraCenterOnScreen == CameraCenterOnScreen.Center &&
                !RendererSettings.UsingEffect;

            if (matrixIncludesCenterTranslation)
            {
                X = (ClientWidth * 0.5f - matrix.M41) / zoom;
                Y = (ClientHeight * 0.5f - matrix.M42) / zoom;
            }
            else
            {
                X = -matrix.M41 / zoom;
                Y = -matrix.M42 / zoom;
            }

            Zoom = zoom;
        }

        public void ScreenToWorld(float screenX, float screenY, out float worldX, out float worldY)
        {
            Matrix.Invert(GetTransformationMatrix(), out var matrix);

            Vector3 position = new Vector3(screenX, screenY, 0);
            Vector3 transformed = Vector3.Transform(position, matrix);

#if FRB
// FRB handles its own client offsets, so don't update those here:
            worldX = transformed.X;
            worldY = transformed.Y;
#else
            worldX = transformed.X - this.ClientLeft/this.Zoom;
            worldY = transformed.Y - this.ClientTop/this.Zoom;
#endif
        }

        public void WorldToScreen(float worldX, float worldY, out float screenX, out float screenY)
        {
            Matrix matrix = GetTransformationMatrix();

            Vector3 position = new Vector3(worldX + ClientLeft/Zoom, worldY + ClientTop/Zoom, 0);
            Vector3 transformed = Vector3.Transform(position, matrix);

            screenX = transformed.X;
            screenY = transformed.Y;
        }
#endregion
    }

    /// <summary>
    /// Extensions that compute screen-space scissor rectangles from world-space
    /// renderable bounds. Lives in this file (rather than its own) to avoid having
    /// to add a new linked-Compile entry to every consumer csproj (FRB, RaylibGum,
    /// SokolGum, SkiaGum, etc.). Takes IRenderableIpso/Layer so kept off the Camera
    /// class itself — Camera should remain a pure transform primitive.
    /// </summary>
    public static class CameraScissorExtensions
    {
        /// <summary>
        /// When true (the default), the render walk skips any renderable that falls entirely outside
        /// the active clip rectangle, along with its whole subtree — avoiding draw and render-state
        /// work for scrolled-off content in ListBoxes, ScrollViewers, ComboBox dropdowns, and any
        /// clipping container. Set false to render all clipped content (e.g. if a renderable's
        /// visuals intentionally bleed far past its own bounds). See #2998.
        /// </summary>
        public static bool CullOffscreenWhenClipped = true;

        /// <summary>
        /// Pixels by which a renderable's bounds are expanded before the off-screen cull test, so
        /// content that bleeds slightly past its own bounds (drop shadows, borders) is not
        /// prematurely culled. See #2998.
        /// </summary>
        public const int OffscreenCullMarginInPixels = 15;

        /// <summary>
        /// Returns true when <paramref name="bounds"/> lies entirely outside <paramref name="clip"/>,
        /// expanded outward by <paramref name="margin"/> pixels on every side. Used by the off-screen
        /// render cull (#2998) to skip renderables that fall completely outside the active clip
        /// rectangle. The margin absorbs content that bleeds slightly past its own bounds (drop
        /// shadows, borders) so it is not prematurely culled.
        /// </summary>
        public static bool IsFullyOutside(System.Drawing.Rectangle bounds, System.Drawing.Rectangle clip, int margin)
        {
            return bounds.Right < clip.Left - margin
                || bounds.Left > clip.Right + margin
                || bounds.Bottom < clip.Top - margin
                || bounds.Top > clip.Bottom + margin;
        }

        public static System.Drawing.Rectangle GetScissorRectangleFor(this Camera camera, Layer layer, IRenderableIpso ipso)
        {
            if (ipso == null)
            {
                return new System.Drawing.Rectangle(camera.ClientLeft, camera.ClientTop, camera.ClientWidth, camera.ClientHeight);
            }

            float worldX = ipso.GetAbsoluteLeft();
            float worldY = ipso.GetAbsoluteTop();

            float screenX, screenY;
            if (layer != null)
            {
                layer.WorldToScreen(camera, worldX, worldY, out screenX, out screenY);
            }
            else
            {
                camera.WorldToScreen(worldX, worldY, out screenX, out screenY);
            }

#if FRB
            // Layer.WorldToScreen / Camera.WorldToScreen intentionally skip the ClientLeft/Top
            // offset under FRB so FRB's own renderer doesn't double-apply it for sprite geometry.
            // But GraphicsDevice.ScissorRectangle is backbuffer-relative, not viewport-local, so
            // we have to add the offset back here or the scissor lands at backbuffer (0,0) when
            // the FRB camera DestinationRectangle is letterboxed away from the origin.
            screenX += camera.ClientLeft;
            screenY += camera.ClientTop;
#endif

            int left = global::RenderingLibrary.Math.MathFunctions.RoundToInt(screenX);
            int top = global::RenderingLibrary.Math.MathFunctions.RoundToInt(screenY);

            worldX = ipso.GetAbsoluteRight();
            worldY = ipso.GetAbsoluteBottom();
            if (layer != null)
            {
                layer.WorldToScreen(camera, worldX, worldY, out screenX, out screenY);
            }
            else
            {
                camera.WorldToScreen(worldX, worldY, out screenX, out screenY);
            }

#if FRB
            screenX += camera.ClientLeft;
            screenY += camera.ClientTop;
#endif

            int right = global::RenderingLibrary.Math.MathFunctions.RoundToInt(screenX);
            int bottom = global::RenderingLibrary.Math.MathFunctions.RoundToInt(screenY);

            int minX = camera.ClientLeft;
            int maxX = camera.ClientLeft + camera.ClientWidth;
            int minY = camera.ClientTop;
            int maxY = camera.ClientTop + camera.ClientHeight;

            left = System.Math.Clamp(left, minX, maxX);
            right = System.Math.Clamp(right, minX, maxX);
            top = System.Math.Clamp(top, minY, maxY);
            bottom = System.Math.Clamp(bottom, minY, maxY);

            int width = System.Math.Max(0, right - left);
            int height = System.Math.Max(0, bottom - top);

            return new System.Drawing.Rectangle(left, top, width, height);
        }
    }

    /// <summary>
    /// The camera-visible rectangle of a render-target container: its world-space bounds clamped to
    /// the camera's visible extent, plus the pixel size of that clamped region at the current zoom.
    /// A render-target container bakes and composites only this visible portion. When it is degenerate
    /// (zero natural size) or positioned entirely off-camera, the clamp collapses to a non-positive
    /// pixel size and <see cref="HasVisibleArea"/> is false — there is nothing to bake or composite,
    /// so the container's subtree renders nothing. Produced by
    /// <see cref="CameraRenderTargetExtensions.GetRenderTargetBounds"/>.
    /// </summary>
    public readonly struct RenderTargetBounds
    {
        public RenderTargetBounds(float left, float top, float right, float bottom, int width, int height)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
            Width = width;
            Height = height;
        }

        /// <summary>World-space left edge, clamped to the camera's visible left.</summary>
        public float Left { get; }
        /// <summary>World-space top edge, clamped to the camera's visible top.</summary>
        public float Top { get; }
        /// <summary>World-space right edge, clamped to the camera's visible right.</summary>
        public float Right { get; }
        /// <summary>World-space bottom edge, clamped to the camera's visible bottom.</summary>
        public float Bottom { get; }
        /// <summary>Pixel width of the clamped region at the current camera zoom.</summary>
        public int Width { get; }
        /// <summary>Pixel height of the clamped region at the current camera zoom.</summary>
        public int Height { get; }

        /// <summary>
        /// True when the clamped region has a positive pixel size — i.e. there is something to bake
        /// and composite. False for a degenerate (zero-size) or entirely off-camera container, whose
        /// render-target subtree renders nothing. This is the single skip-decision both backends
        /// share, replacing the copy-pasted <c>width &lt;= 0 || height &lt;= 0</c> guards.
        /// </summary>
        public bool HasVisibleArea => Width > 0 && Height > 0;
    }

    /// <summary>
    /// Computes the camera-visible bounds of a render-target container (#3478). Kept here in Camera.cs
    /// — alongside <see cref="CameraScissorExtensions"/> and for the same reason — so every backend
    /// (MonoGame, raylib, Sokol, Skia) shares one clamp + pixel-size implementation via the GumCommon
    /// source reference, instead of copy-pasting the Max/Min clamp math into each renderer. That
    /// duplication is what let the MonoGame and raylib render-target paths diverge in #3478; a single
    /// shared helper makes the consistency structural. Kept off the Camera class itself so Camera
    /// stays a pure transform primitive.
    /// </summary>
    public static class CameraRenderTargetExtensions
    {
        /// <summary>
        /// Clamps <paramref name="renderable"/>'s world-space bounds to <paramref name="camera"/>'s
        /// visible extent and returns that rectangle plus its pixel size at the current zoom. A
        /// render-target container bakes and composites only this visible portion; an off-camera or
        /// degenerate container yields a non-positive size (<see cref="RenderTargetBounds.HasVisibleArea"/>
        /// is false).
        /// </summary>
        public static RenderTargetBounds GetRenderTargetBounds(this Camera camera, IRenderableIpso renderable)
        {
            float left = System.Math.Max(camera.AbsoluteLeft, renderable.GetAbsoluteLeft());
            float right = System.Math.Min(camera.AbsoluteRight, renderable.GetAbsoluteRight());
            float top = System.Math.Max(camera.AbsoluteTop, renderable.GetAbsoluteTop());
            float bottom = System.Math.Min(camera.AbsoluteBottom, renderable.GetAbsoluteBottom());

            int width = global::RenderingLibrary.Math.MathFunctions.RoundToInt((right - left) * camera.Zoom);
            int height = global::RenderingLibrary.Math.MathFunctions.RoundToInt((bottom - top) * camera.Zoom);

            return new RenderTargetBounds(left, top, right, bottom, width, height);
        }
    }
}
