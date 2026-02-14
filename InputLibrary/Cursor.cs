using System;
using Microsoft.Xna.Framework.Input;
using System.Windows.Forms;
using WinCursor = System.Windows.Forms.Cursor;

namespace InputLibrary
{
    public class Cursor
    {
        #region Fields

        static Cursor mSelf;


        MouseState mMouseState = new MouseState();
        MouseState mLastFrameMouseState = new MouseState();
        Control mControl;

        public const float MaximumSecondsBetweenClickForDoubleClick = .25f;
        double mLastClickTime;

        bool mHasBeenSet = false;

        WinCursor mSetCursor = Cursors.Arrow;
        WinCursor mLastSet = Cursors.Arrow;

        #endregion

        #region Properties

        /// <summary>
        /// Returns if the cursor is positioned over the window.
        /// </summary>
        /// <remarks>
        /// At one point I had this also return if the window is focused
        /// but that caused a lot of unexpected consequences...also IsInWindow
        /// doesn't really suggest anything with focus, so I'm going to keep it
        /// as simply detecting location and not focus.
        /// </remarks>
        public bool IsInWindow
        {
            get
            {
                if (mControl == null)
                {
                    throw new NullReferenceException("The Cursor's Control is null.  You must call Initialize before using the Cursor");
                }
                System.Drawing.Point point = mControl.PointToClient(new System.Drawing.Point(mMouseState.X, mMouseState.Y));
                return point.X >= 0 && point.Y >= 0 && point.X < mControl.Width && point.Y < mControl.Height;
            }
        }

        /// <summary>
        /// Returns the pixel X on the window - this is in window space, not world space.
        /// </summary>
        public float X
        {
            get
            {
                if(mControl != null)
                {
                    System.Drawing.Point point = mControl.PointToClient(new System.Drawing.Point(mMouseState.X, mMouseState.Y));
                    return point.X;
                }
                else
                {
                    return mMouseState.X;
                }
                //return mMouseState.X;
            }
        }

        /// <summary>
        /// Returns the pixel Y on the window - this is in window space, not world space.
        /// </summary>
        public float Y
        {
            get
            {
                if (mControl != null)
                {

                    System.Drawing.Point point = mControl.PointToClient(new System.Drawing.Point(mMouseState.X, mMouseState.Y));
                    return point.Y;
                }
                else
                {
                    return mMouseState.Y;
                }
                //return mMouseState.Y;
            }
        }

        public bool MiddleDown
        {
            get
            {
                return mControl.Focused && mMouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            }
        }

        public bool PrimaryClick
        {
            get
            {
                return IsInWindow && mControl.Focused && this.mLastFrameMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
                    this.mMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released;
            }
        }

        public bool PrimaryDown
        {
            get
            {
                return IsInWindow && mControl.Focused && this.mMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            }
        }

        public bool PrimaryDownIgnoringIsInWindow
        {
            get
            {
                return this.mMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            }

        }

        public bool PrimaryPush
        {
            get
            {
                return IsInWindow && mControl.Focused && this.mLastFrameMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released &&
                    this.mMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            }
        }

        public bool PrimaryDoubleClick
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether the user has pushed the secondary button (right click) this frame.
        /// </summary>
        public bool SecondaryPush
        {
            get
            {
                return IsInWindow && this.mLastFrameMouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Released &&
                    this.mMouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            }
        }


        /// <summary>
        /// Returns the screen space (in pixels) change on the X axis since the last frame.
        /// </summary>
        public float XChange
        {
            get
            {
                return mMouseState.X - mLastFrameMouseState.X;
            }
        }

        /// <summary>
        /// Returns the screen space (in pixel) change on the Y axis since the last frame.
        /// </summary>
        public float YChange
        {
            get
            {
                return mMouseState.Y - mLastFrameMouseState.Y;
            }
        }

        public bool HasBeenSet
        {
            get
            {
                return mHasBeenSet;
            }
        }

        public static Cursor Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new Cursor();
                }
                return mSelf;
            }
        }

        #endregion

        public void Activity(double currentTime)
        {
            mLastFrameMouseState = mMouseState;
            PrimaryDoubleClick = false;

            mMouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();

            // We want to keep track of whether
            // the user pushed in the window or not
            // to prevent the user from pushing outside
            // of the window and dragging "inward" (which 
            // happens if the user is moving the resize bar).
            // To do this we need to track if the user pushed in
            // the window or not.  We can't use PrimaryPush because
            // that checks IsInWindow, so we will manually do the MouseState
            // checks here.
            // Update, maybe we don't need this now that the wireframe window
            // can receive focus.
            //if(this.mLastFrameMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released &&
            //        this.mMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            //{
            //    mPushedInWindow = IsInWindow;
            //}
            

            if (PrimaryClick)
            {
                var timeSinceLastClick =
                    currentTime - mLastClickTime;

                if (timeSinceLastClick < MaximumSecondsBetweenClickForDoubleClick)
                {
                    PrimaryDoubleClick = true;
                }
                mLastClickTime = currentTime;
            }
        }

        public void Initialize( Control control)
        {
            mControl = control;

            // This was supposed to make the scroll wheel work, but instead
            // it made everything else not work
            //Microsoft.Xna.Framework.Input.Mouse.WindowHandle = control.Handle;
        }

        /// <summary>
        /// Marks that the cursor hasn't yet been set this frame. Calling SetWinformsCursor will set the cursor and
        /// prevents future setting of the cursor this frame until StartCursorSettingFrameStart is called again.
        /// </summary>
        public void StartCursorSettingFrameStart()
        {
            mHasBeenSet = false;
        }

        public void SetWinformsCursor(WinCursor cursor)
        {
            if (!mHasBeenSet)
            {
                mSetCursor = cursor;
                mHasBeenSet = true;
            }
        }


        public void EndCursorSettingFrameStart()
        {
            if (mHasBeenSet)
            {
                mControl.Cursor = mSetCursor;
                WinCursor.Current = mSetCursor;
            }
            else if (mControl.Cursor != Cursors.Arrow)
            {
                mControl.Cursor = Cursors.Arrow;
                WinCursor.Current = Cursors.Arrow;
            }
        }
    }
}
