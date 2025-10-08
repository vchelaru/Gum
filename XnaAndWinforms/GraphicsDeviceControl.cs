#region File Description
//-----------------------------------------------------------------------------
// GraphicsDeviceControl.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace XnaAndWinforms
{
    // System.Drawing and the XNA Framework both define Color and Rectangle
    // types. To avoid conflicts, we specify exactly which ones to use.
    using Color = System.Drawing.Color;
    using Rectangle = Microsoft.Xna.Framework.Rectangle;


    /// <summary>
    /// Custom control uses the XNA Framework GraphicsDevice to render onto
    /// a Windows Form. Derived classes can override the Initialize and Draw
    /// methods to add their own drawing code.
    /// </summary>
    public class GraphicsDeviceControl : Control
    {
        #region Fields

        const PixelFormat BmpFormat = PixelFormat.Format32bppPArgb;
        const SurfaceFormat RtFormat = SurfaceFormat.Bgra32;
        const int PreferredMultiSampleCount = 1;

        // However many GraphicsDeviceControl instances you have, they all share
        // the same underlying GraphicsDevice, managed by this helper service.
        GraphicsDeviceService graphicsDeviceService;

        Timer mTimer;

        ServiceContainer services = new ServiceContainer();

        float mDesiredFramesPerSecond = 30;

        RenderingError mRenderError = new RenderingError();

        private RenderTarget2D renderTarget;
        byte[] rawImage;
        Bitmap bitmap;

        #endregion

        #region Properties

        public float DesiredFramesPerSecond
        {
            get { return mDesiredFramesPerSecond; }
            set
            {
                mDesiredFramesPerSecond = value;
                // If this is not null, then we're post-initialize
                // so set the timer right away.  If it is null then
                // initialize hasn't happened yet.  We'll save off the
                // value in mDesiredFramesPerSecond which will get applied
                // in initialization.
                if (mTimer != null)
                {
                    mTimer.Interval = (int)(1000 / value);
                }
            }
        }

        /// <summary>
        /// Gets a GraphicsDevice that can be used to draw onto this control.
        /// </summary>
        public GraphicsDevice GraphicsDevice
        {
            get { return graphicsDeviceService.GraphicsDevice; }
        }


        public RenderTarget2D DefaultRenderTarget
        {
            get { return renderTarget; }
        }



        /// <summary>
        /// Gets an IServiceProvider containing our IGraphicsDeviceService.
        /// This can be used with components such as the ContentManager,
        /// which use this service to look up the GraphicsDevice.
        /// </summary>
        public ServiceContainer Services
        {
            get { return services; }
        }



        #endregion

        #region Constructor/Initialization

        public GraphicsDeviceControl()
        {

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            // Don't initialize the graphics device if we are running in the designer.
            if (!DesignMode)
            {
                graphicsDeviceService = GraphicsDeviceService.AddRef(Handle,
                                                                     ClientSize.Width,
                                                                     ClientSize.Height);

                // Register the service, so components like ContentManager can find it.
                services.AddService<IGraphicsDeviceService>(graphicsDeviceService);

                // We used to just invalidate on idle, which ate up the CPU.
                // Instead, I'm going to put it on a 30 fps timer
                //Application.Idle += delegate { Invalidate(); };
                mTimer = new Timer();
                // If the user hasn't set DesiredFramesPerSecond
                // this will just set it to 30 and it will set the
                // interval.  If the user has, then this will use the
                // custom value set.
                DesiredFramesPerSecond = mDesiredFramesPerSecond;
                mTimer.Tick += delegate { Invalidate(); };
                mTimer.Start();

                // Give derived classes a chance to initialize themselves.
                Initialize();
            }
        }


        /// <summary>
        /// Disposes the control.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (bitmap != null)
            {
                bitmap.Dispose();
                bitmap = null;
            }
            if (renderTarget != null)
            {
                renderTarget.Dispose();
                renderTarget = null;
            }
            if (graphicsDeviceService != null)
            {
                graphicsDeviceService.Release(disposing);
                graphicsDeviceService = null;
            }

            rawImage = null;

            base.Dispose(disposing);
        }


        #endregion

        #region Events

        public event Action XnaDraw;
        public event Action XnaUpdate;
        public event Action<Exception> ErrorOccurred;

        #endregion

        #region Paint

        /// <summary>
        /// Forces the display to redraw.  This can be performed if a control has a low or 0 FramesPerSecond value.  Continually
        /// calling this does not have any negative consequences - it will simply update at maximum framerate.
        /// </summary>
        public void RefreshDisplay()
        {
            Invalidate();
        }

        int simultaneousPaints = 0;

        /// <summary>
        /// Redraws the control in response to a WinForms paint message.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (DesignMode)
            {
                e.Graphics.FillRectangle(Brushes.BlanchedAlmond, ClientRectangle);
                return;
            }

            // This prevents multiple invalidates from happening at the same time.
            // It can fix some bugs and improves performance.
            if(simultaneousPaints > 0)
            {
                return;
            }

            simultaneousPaints++;

            int clientWidth = ClientSize.Width;
            int clientHeight = ClientSize.Height;

            if (clientWidth > 0 && clientHeight > 0)
            {
                PreDrawUpdate();

                if (string.IsNullOrEmpty(mRenderError.Message))
                {
                    BeginDraw(mRenderError);
                }


                lock (this)
                {
                    if (string.IsNullOrEmpty(mRenderError.Message))
                    {
                        try
                        {
                            if (XnaUpdate != null)
                            {
                                XnaUpdate();
                            }
                            Draw();
                            EndDraw();

                            PaintRendertarget(e.Graphics);
                        }
                        catch (Exception exception)
                        {
                            ErrorOccurred?.Invoke(exception);
                            mRenderError.Message = exception.ToString();
                            
                        }
                    }
                    //else if (!mRenderError.GraphicsDeviceResetFailed)
                    //{
                    //    TryHandleDeviceReset(mRenderError);

                    //} 
                    else
                    {
                        // If BeginDraw failed, show an error message using System.Drawing.
                        PaintUsingSystemDrawing(e.Graphics, mRenderError.ProcessedMessage);

                        DesiredFramesPerSecond = 0.5f;
                    }
                }
            }

            base.OnPaint(e);
            simultaneousPaints--;
        }


        /// <summary>
        /// Attempts to begin drawing the control. Returns an error message string
        /// if this was not possible, which can happen if the graphics device is
        /// lost, or if we are running inside the Form designer.
        /// </summary>
        void BeginDraw(RenderingError error)
        {
            // If we have no graphics device, we must be running in the designer.
            if (graphicsDeviceService == null)
            {
                error.Message = Text + "\n\n" + GetType();
            }

            if (error.HasErrors == false || error.GraphicsDeviceNeedsReset)
            {
                TryHandleDeviceReset(error);
            }
            if (!error.HasErrors)
            {
                GraphicsDevice.SetRenderTarget(renderTarget);
                GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);

                // Many GraphicsDeviceControl instances can be sharing the same
                // GraphicsDevice. The device backbuffer will be resized to fit the
                // largest of these controls. But what if we are currently drawing
                // a smaller control? To avoid unwanted stretching, we set the
                // viewport to only use the top left portion of the full backbuffer.
                Viewport viewport = new Viewport();

                viewport.X = 0;
                viewport.Y = 0;

                viewport.Width = ClientSize.Width;
                viewport.Height = ClientSize.Height;

                viewport.MinDepth = 0;
                viewport.MaxDepth = 1;

                GraphicsDevice.Viewport = viewport;
            }
        }


        /// <summary>
        /// Ends drawing the control. This is called after derived classes
        /// have finished their Draw method, and is responsible for presenting
        /// the finished image onto the screen, using the appropriate WinForms
        /// control handle to make sure it shows up in the right place.
        /// </summary>
        void EndDraw()
        {
            try
            {
                // resolve RenderTarget
                this.GraphicsDevice.SetRenderTarget(null);

                renderTarget.GetData(rawImage);

            }
            catch
            {
                // Present might throw if the device became lost while we were
                // drawing. The lost device will be handled by the next BeginDraw,
                // so we just swallow the exception.
            }
        }


        /// <summary>
        /// Helper used by BeginDraw. This checks the graphics device status,
        /// making sure it is big enough for drawing the current control, and
        /// that the device is not lost. Returns an error string if the device
        /// could not be reset.
        /// </summary>
        void TryHandleDeviceReset(RenderingError error)
        {
            // Don't attempt to reset if we failed resetting before
            if (error.GraphicsDeviceResetFailed)
                return;

            switch (GraphicsDevice.GraphicsDeviceStatus)
            {
                case GraphicsDeviceStatus.Lost:
                    // If the graphics device is lost, but we can try to reset it
                    error.Message = "Graphics device lost";
                    error.GraphicsDeviceLost = true;
                    break;
                case GraphicsDeviceStatus.NotReset:
                    // If device is in the not-reset state, we should try to reset it.
                    error.GraphicsDeviceNeedsReset = true;
                    error.Message = "Graphics device needs reset";

                    break;

                default:
                    // If the device state is ok, check whether it is big enough.
                    PresentationParameters pp = GraphicsDevice.PresentationParameters;

                    bool deviceNeedsReset = (ClientSize.Width > pp.BackBufferWidth) ||
                                       (ClientSize.Height > pp.BackBufferHeight);
                    if(deviceNeedsReset)
                    {
                        error.Message = "Resolution has changed, needs reset";
                        error.GraphicsDeviceNeedsReset = true;
                    }
                    break;
            }

            // Do we need to reset the device?
            if (error.GraphicsDeviceNeedsReset)
            {
                try
                {
                    graphicsDeviceService.ResetDevice(ClientSize.Width,
                                                      ClientSize.Height);

                    error.Message = null;
                    error.GraphicsDeviceNeedsReset = false;
                }
                catch (Exception e)
                {
                    error.GraphicsDeviceResetFailed = true;
                    error.GraphicsDeviceNeedsReset = false;
                    error.Message = "Graphics device reset failed\n\n" + e;
                }
            }

            int w = Math.Max(1, ClientSize.Width);
            int h = Math.Max(1, ClientSize.Height);

            // check whether _swapChainRenderTarget is big enough.
            if (renderTarget != null)
            {
                if (w != renderTarget.Width || h != renderTarget.Height)
                {
                    renderTarget.Dispose();
                    renderTarget = null;
                    bitmap.Dispose();
                    bitmap = null;
                }
            }

            // recreate RenderTarget
            if (renderTarget == null)
            {
                renderTarget = new RenderTarget2D(
                    this.GraphicsDevice, w, h,
                    false, RtFormat, DepthFormat.Depth24Stencil8, PreferredMultiSampleCount,
                    // needed for rendering IsRenderTarget containers
                    RenderTargetUsage.PreserveContents
                    );

                bitmap = new Bitmap(w, h, BmpFormat);
            }
            int rawImageLen = w * h * 4;

            if (rawImage == null || rawImage.Length != rawImageLen)
            {
                rawImage = new byte[rawImageLen];
            }

        }

        /// <summary>
        /// If we do not have a valid graphics device (for instance if the device
        /// is lost, or if we are running inside the Form designer), we must use
        /// regular System.Drawing method to display a status message.
        /// </summary>
        protected virtual void PaintUsingSystemDrawing(Graphics graphics, string text)
        {
            graphics.Clear(Color.CornflowerBlue);

            using (Brush brush = new SolidBrush(Color.Black))
            {
                using (StringFormat format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;

                    graphics.DrawString(text, Font, brush, ClientRectangle, format);
                }
            }
        }

        private void PaintRendertarget(Graphics graphics)
        {
            int w = Math.Max(1, ClientSize.Width);
            int h = Math.Max(1, ClientSize.Height);

            var rect = new System.Drawing.Rectangle(0, 0, w, h);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            if (renderTarget.Format == SurfaceFormat.Color && (bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format32bppPArgb))
            {
                CopyAndConvertRGBATOBGRA(w, h, rawImage, bmpData.Scan0, bmpData.Stride);
            }
            else if (renderTarget.Format == SurfaceFormat.Bgra32 && (bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format32bppPArgb))
            {
                int rowSize = w * 4;
                int rowStride = bmpData.Stride;

                Parallel.For(0, h, (y) =>
                {
                    int srcOffset = y * rowSize;
                    int dstOffset = y * rowStride;
                    Marshal.Copy(rawImage, srcOffset, bmpData.Scan0 + dstOffset, rowSize);
                });
            }
            else
            {
                throw new NotSupportedException();
            }
            bitmap.UnlockBits(bmpData);
            var cm = graphics.CompositingMode;
            var im = graphics.InterpolationMode;
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            graphics.DrawImageUnscaled(bitmap, 0, 0);
            graphics.CompositingMode = cm;
            graphics.InterpolationMode = im;
        }

        private unsafe void CopyAndConvertRGBATOBGRA(int w, int h, byte[] data, IntPtr buffer, int rowStride)
        {
            int rowSize = w * 4;

            fixed (void* pData = &data[0])
            {
                byte* src = (byte*)pData;
                byte* dst = (byte*)buffer;

                Parallel.For(0, h, (y) =>
                {
                    int srcOffset = y * rowSize;
                    int dstOffset = y * rowStride;

                    for (int x = 0; x < w; x++)
                    {
                        int i = x * 4;
                        dst[dstOffset + i + 0] = src[srcOffset + i + 2];
                        dst[dstOffset + i + 1] = src[srcOffset + i + 1];
                        dst[dstOffset + i + 2] = src[srcOffset + i + 0];
                        dst[dstOffset + i + 3] = src[srcOffset + i + 3];
                    }
                });
            }
        }

        /// <summary>
        /// Ignores WinForms paint-background messages. The default implementation
        /// would clear the control to the current background color, causing
        /// flickering when our OnPaint implementation then immediately draws some
        /// other color over the top using the XNA Framework GraphicsDevice.
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }


        #endregion

        #region Protected Virtual Methods


        /// <summary>
        /// Derived classes override this to initialize their drawing code.
        /// </summary>
        protected virtual void Initialize()
        {
        }

        protected virtual void PreDrawUpdate()
        {
        }

        /// <summary>
        /// Derived classes override this to draw themselves using the GraphicsDevice.
        /// </summary>
        protected virtual void Draw()
        {
            try
            {
                if (XnaDraw != null)
                {
                    XnaDraw();
                }
            }
            catch (Exception e)
            {
                this.mRenderError.Message = e.ToString();
            }

        }


        #endregion
        protected override bool IsInputKey(Keys keyData)
        {
            return true;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            this.SetStyle(ControlStyles.Selectable, true);

            this.TabStop = true;
            this.Focus();
            this.Select();
            base.OnMouseDown(e);
        }
    }
}
