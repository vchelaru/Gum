using RenderingLibrary;
using System;
using System.Drawing;
using System.Windows.Forms;
using Gum.Controls;

namespace FlatRedBall.SpecializedXnaControls
{
    public class ScrollBarControlLogic
    {
        #region Fields

        ThemedScrollBar mVerticalScrollBar;
        ThemedScrollBar mHorizontalScrollBar;

        int minimumX = 0;
        int minimumY = 0;

        int displayedAreaWidth = 2048;
        int displayedAreaHeight = 2048;

        float zoomPercentage = 100;

        Panel mPanel;
        Control xnaControl;

        #endregion

        #region Properties

        public float ZoomPercentage
        {
            get
            {
                return zoomPercentage;
            }
            set
            {
                zoomPercentage = value;
                UpdateScrollBars();
            }
        }



        SystemManagers managers;
        public SystemManagers Managers
        {
            get => managers;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Managers value should not be null");
                }
                managers = value;
            }
        }

        #endregion

        public ScrollBarControlLogic(Panel panel, Control xnaControl)
        {
            mPanel = panel;
            this.xnaControl = xnaControl;

            mVerticalScrollBar = new() { Orientation = ScrollOrientationEx.Vertical };
            mVerticalScrollBar.Dock = DockStyle.Right;
            mVerticalScrollBar.ValueChanged += HandleVerticalScroll;
            panel.Controls.Add(mVerticalScrollBar);

            mHorizontalScrollBar = new() { Orientation = ScrollOrientationEx.Horizontal };
            mHorizontalScrollBar.Dock = DockStyle.Bottom;
            mHorizontalScrollBar.ValueChanged += HandleHorizontalScroll;

            panel.Controls.Add(mHorizontalScrollBar);

            SetDisplayedArea(2048, 2048);

            xnaControl.Resize += HandlePanelResize;

            (mVerticalScrollBar as Control).BackColorChanged += (_, _) =>
            {
                //(mVerticalScrollBar as Control).BackColor = Color.Red;
            };
            //(mHorizontalScrollBar as Control).BackColor = Color.Red;
        }

        void HandlePanelResize(object sender, EventArgs e)
        {
            UpdateScrollBars();
        }

        private void HandleVerticalScroll(object sender, EventArgs e)
        {
            Managers.Renderer.Camera.Y = mVerticalScrollBar.Value;
        }

        private void HandleHorizontalScroll(object sender, EventArgs e)
        {
            Managers.Renderer.Camera.X = mHorizontalScrollBar.Value;

        }

        public void UpdateScrollBarsToCameraPosition()
        {
            mVerticalScrollBar.Value =
                Math.Min(Math.Max(mVerticalScrollBar.Minimum, (int)Managers.Renderer.Camera.Y), mVerticalScrollBar.Maximum);

            mHorizontalScrollBar.Value =
                Math.Min(Math.Max(mHorizontalScrollBar.Minimum, (int)Managers.Renderer.Camera.X), mHorizontalScrollBar.Maximum);
        }

        public void SetDisplayedArea(int? width = null, int? height = null)
        {
            if (width != null)
            {
                displayedAreaWidth = width.Value;
                minimumX = -width.Value / 2;
            }

            if (height != null)
            {
                displayedAreaHeight = height.Value;
                minimumY = -height.Value / 2;
            }


            UpdateScrollBars();


        }

        public void UpdateScrollBars()
        {
            if (Managers != null && Managers.Renderer != null)
            {
                // This clamps the scroll bar, but we don't want to adjust the position of the camera when this is called
                // because the user may manually move the camera beyond the bounds:
                var x = Managers.Renderer.Camera.X;
                var horizontalValue = System.Math.Max(x, mHorizontalScrollBar.Minimum);
                horizontalValue = System.Math.Min(horizontalValue, mHorizontalScrollBar.Maximum);
                mHorizontalScrollBar.Value = (int)horizontalValue;

                var y = Managers.Renderer.Camera.Y;
                var verticalValue = System.Math.Max(y, mVerticalScrollBar.Minimum);
                verticalValue = System.Math.Min(verticalValue, mVerticalScrollBar.Maximum);
                mVerticalScrollBar.Value = (int)verticalValue;

                // now preserve the values:
                Managers.Renderer.Camera.X = x;
                Managers.Renderer.Camera.Y = y;

                var camera = Managers.Renderer.Camera;

                var effectiveAreaHeight = -minimumY + displayedAreaHeight;

                var visibleAreaHeight = xnaControl.Height / camera.Zoom;
                mVerticalScrollBar.Minimum = minimumY;
                mVerticalScrollBar.Maximum = minimumY + (int)(effectiveAreaHeight + visibleAreaHeight);
                mVerticalScrollBar.LargeChange = (int)visibleAreaHeight;

                var visibleAreaWidth = xnaControl.Width / camera.Zoom;

                var effectiveAreaWidth = -minimumX + displayedAreaWidth;

                mHorizontalScrollBar.Minimum = minimumX; // The minimum value for the scroll bar, which should be 0, since that's the furthest left the scrollbar can go

                // The total amount that the scrollbar can cover. This is the width of the area plus the screen width since we can scroll until the edges 
                // are at the middle, meaning we can see half a screen width on either side 
                mHorizontalScrollBar.Maximum = minimumX + (int)(effectiveAreaWidth + visibleAreaWidth);
                mHorizontalScrollBar.LargeChange = (int)visibleAreaWidth; // the amount of visible area. It's called LargeChange but it really means how much the scrollbar can see 
            }
        }
    }
}
