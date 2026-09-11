using System;
using Gum.Input;

namespace InputLibrary
{
    /// <summary>
    /// Polled pointer state for an editor canvas, sampled from the host once per frame through
    /// <see cref="IInputHostControl.GetPointerState"/>. Positions are in the host's client space;
    /// pushes and clicks only count while the pointer is over the host and the host has focus.
    /// </summary>
    public class Cursor : IGumCursorState
    {
        #region Fields

        static Cursor? mSelf;

        HostPointerState mPointerState;
        HostPointerState mLastFramePointerState;

        IInputHostControl? mControl;

        public const float MaximumSecondsBetweenClickForDoubleClick = .25f;
        // Negative infinity so the first click after startup can never read as a double click.
        double mLastClickTime = double.NegativeInfinity;

        bool mHasBeenSet = false;
        CursorKind mSetCursor = CursorKind.Arrow;

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
                return mPointerState.X >= 0 && mPointerState.Y >= 0 &&
                    mPointerState.X < mControl.Width && mPointerState.Y < mControl.Height;
            }
        }

        /// <summary>
        /// Returns the X on the window - this is in window space, not world space.
        /// </summary>
        public float X => mPointerState.X;

        /// <summary>
        /// Returns the Y on the window - this is in window space, not world space.
        /// </summary>
        public float Y => mPointerState.Y;

        public bool MiddleDown
        {
            get
            {
                return IsFocused && mPointerState.IsMiddleDown;
            }
        }

        public bool PrimaryClick
        {
            get
            {
                return IsInWindow && IsFocused && mLastFramePointerState.IsLeftDown && !mPointerState.IsLeftDown;
            }
        }

        public bool PrimaryDown
        {
            get
            {
                return IsInWindow && IsFocused && mPointerState.IsLeftDown;
            }
        }

        public bool PrimaryDownIgnoringIsInWindow
        {
            get
            {
                return mPointerState.IsLeftDown;
            }
        }

        public bool PrimaryPush
        {
            get
            {
                return IsInWindow && IsFocused && !mLastFramePointerState.IsLeftDown && mPointerState.IsLeftDown;
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
                return IsInWindow && IsFocused && !mLastFramePointerState.IsRightDown && mPointerState.IsRightDown;
            }
        }

        /// <summary>
        /// Returns the window space change on the X axis since the last frame.
        /// </summary>
        public float XChange
        {
            get
            {
                return mPointerState.X - mLastFramePointerState.X;
            }
        }

        /// <summary>
        /// Returns the window space change on the Y axis since the last frame.
        /// </summary>
        public float YChange
        {
            get
            {
                return mPointerState.Y - mLastFramePointerState.Y;
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

        private bool IsFocused => mControl != null && mControl.Focused;

        #endregion

        public void Activity(double currentTime)
        {
            mLastFramePointerState = mPointerState;
            PrimaryDoubleClick = false;

            mPointerState = mControl?.GetPointerState() ?? default;

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

        public void Initialize(IInputHostControl control)
        {
            mControl = control;
        }

        /// <summary>
        /// Marks that the cursor hasn't yet been set this frame. Calling <see cref="SetCursorKind"/>
        /// will set the cursor and prevents future setting of the cursor this frame until
        /// StartCursorSettingFrameStart is called again.
        /// </summary>
        public void StartCursorSettingFrameStart()
        {
            mHasBeenSet = false;
        }

        /// <summary>
        /// Requests the cursor icon for this frame. The first caller each frame wins, so the
        /// highest-priority editor surface should ask first.
        /// </summary>
        public void SetCursorKind(CursorKind kind)
        {
            if (!mHasBeenSet)
            {
                mSetCursor = kind;
                mHasBeenSet = true;
            }
        }

        /// <summary>
        /// <see cref="IGumCursorState.SetCursor"/> implementation, for headless callers (e.g.
        /// <c>SelectionManager</c>) that speak <see cref="GumCursorKind"/> rather than this
        /// assembly's <see cref="CursorKind"/>.
        /// </summary>
        public void SetCursor(GumCursorKind kind) => SetCursorKind(ToCursorKind(kind));

        private static CursorKind ToCursorKind(GumCursorKind kind) => kind switch
        {
            GumCursorKind.Cross => CursorKind.Cross,
            GumCursorKind.Hand => CursorKind.Hand,
            GumCursorKind.SizeAll => CursorKind.SizeAll,
            GumCursorKind.SizeNS => CursorKind.SizeNS,
            GumCursorKind.SizeWE => CursorKind.SizeWE,
            GumCursorKind.SizeNESW => CursorKind.SizeNESW,
            GumCursorKind.SizeNWSE => CursorKind.SizeNWSE,
            _ => CursorKind.Arrow
        };

        // Only the host control's own cursor is assigned - the editor canvases are framework
        // elements, so a process-wide cursor would have no effect on them.
        public void EndCursorSettingFrameStart()
        {
            if (mControl == null)
            {
                return;
            }
            CursorKind kindToShow = mHasBeenSet ? mSetCursor : CursorKind.Arrow;
            if (mControl.Cursor != kindToShow)
            {
                mControl.Cursor = kindToShow;
            }
        }
    }
}
