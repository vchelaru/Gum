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

        string mRenderError = null;

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


        /// <summary>
        /// Redraws the control in response to a WinForms paint message.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (string.IsNullOrEmpty(mRenderError))
            {
                string beginDrawError = BeginDraw();

                if (!string.IsNullOrEmpty(beginDrawError))
                {
                    mRenderError = beginDrawError;
                }
            }

            if(string.IsNullOrEmpty(mRenderError))
            {
                try
                {
                    if (XnaUpdate != null)
                    {
                        XnaUpdate();
                    }
                    // Draw the control using the GraphicsDevice.
                    Draw();
                    EndDraw();
                }
                catch (Exception exception)
                {
                    mRenderError = exception.ToString();
                }
            }
            else
            {
                // If BeginDraw failed, show an error message using System.Drawing.
                PaintUsingSystemDrawing(e.Graphics, mRenderError);
            }
        }


        /// <summary>
        /// Attempts to begin drawing the control. Returns an error message string
        /// if this was not possible, which can happen if the graphics device is
        /// lost, or if we are running inside the Form designer.
        /// </summary>
        string BeginDraw()
        {
            // If we have no graphics device, we must be running in the designer.
            if (graphicsDeviceService == null)
            {
                return Text + "\n\n" + GetType();
            }

            // Make sure the graphics device is big enough, and is not lost.
            string deviceResetError = HandleDeviceReset();

            if (!string.IsNullOrEmpty(deviceResetError))
            {
                return deviceResetError;
            }

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

            return null;
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
        string HandleDeviceReset()
        {
            bool deviceNeedsReset = false;

            switch (GraphicsDevice.GraphicsDeviceStatus)
            {
                case GraphicsDeviceStatus.Lost:
                    // If the graphics device is lost, we cannot use it at all.
                    //deviceNeedsReset = true;
                    graphicsDeviceService.Release(true);
                    
                    graphicsDeviceService = GraphicsDeviceService.AddRef(Handle,
                                                     ClientSize.Width,
                                                     ClientSize.Height);

                    break;
                case GraphicsDeviceStatus.NotReset:
                    // If device is in the not-reset state, we should try to reset it.
                    deviceNeedsReset = true;
                    break;

                default:
                    // If the device state is ok, check whether it is big enough.
                    PresentationParameters pp = GraphicsDevice.PresentationParameters;

                    deviceNeedsReset = (ClientSize.Width > pp.BackBufferWidth) ||
                                       (ClientSize.Height > pp.BackBufferHeight);
                    break;
            }

            // Do we need to reset the device?
            if (deviceNeedsReset)
            {
                try
                {
                    graphicsDeviceService.ResetDevice(ClientSize.Width,
                                                      ClientSize.Height);
                }
                catch (Exception e)
                {
                    return "Graphics device reset failed\n\n" + e;
                }
            }

            return null;
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

        #region Abstract Methods


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
                this.mRenderError = e.ToString();
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
