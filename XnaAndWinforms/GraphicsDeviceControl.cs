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


        // However many GraphicsDeviceControl instances you have, they all share
        // the same underlying GraphicsDevice, managed by this helper service.
        GraphicsDeviceService graphicsDeviceService;

        Timer mTimer;

        ServiceContainer services = new ServiceContainer();

        float mDesiredFramesPerSecond = 30;

        RenderingError mRenderError = new RenderingError();

        bool mIsInitialized = false;

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
            : base()
        {
        }

        /// <summary>
        /// Initializes the control.
        /// </summary>
        protected override void OnCreateControl()
        {
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

            base.OnCreateControl();

            mIsInitialized = true;
        }


        /// <summary>
        /// Disposes the control.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (graphicsDeviceService != null)
            {
                graphicsDeviceService.Release(disposing);
                graphicsDeviceService = null;
            }

            base.Dispose(disposing);
        }


        #endregion

        #region Events

        public event Action XnaDraw;
        public event Action XnaInitialize;
        public event Action XnaUpdate;

        #endregion

        #region Paint

        /// <summary>
        /// Forces the display to redraw.  This can be performed if a control has a low or 0 FramesPerSecond value.  Continually
        /// calling this does not have any negative consequences - it will simply update at maximum framerate.
        /// </summary>
        public void RefreshDisplay()
        {
            this.Invalidate();
        }

        int simultaneousPaints = 0;

        /// <summary>
        /// Redraws the control in response to a WinForms paint message.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
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
                        }
                        catch (Exception exception)
                        {
                            mRenderError.Message = exception.ToString();
                        }
                    }
                    
                    else
                    {
                        // If BeginDraw failed, show an error message using System.Drawing.
                        PaintUsingSystemDrawing(e.Graphics, mRenderError.ProcessedMessage);

                        DesiredFramesPerSecond = 1;
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
                Rectangle sourceRectangle = new Rectangle(0, 0, ClientSize.Width,
                                                                ClientSize.Height);

                GraphicsDevice.Present(sourceRectangle, null, this.Handle);
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
                    error.GraphicsDeviceNeedsReset = false;
                    error.Message = "Graphics device reset failed\n\n" + e;
                }
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

            if (XnaInitialize != null)
            {
                XnaInitialize();
            }
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
