namespace InputLibrary
{
    /// <summary>
    /// The pointer as an <see cref="IInputHostControl"/> reports it for one frame: its position in
    /// the host's client space (the same units as the host's <see cref="IInputHostControl.Width"/>
    /// and <see cref="IInputHostControl.Height"/>) and which buttons are held.
    /// </summary>
    public readonly struct HostPointerState
    {
        /// <summary>Creates a sample.</summary>
        public HostPointerState(float x, float y, bool isLeftDown, bool isRightDown, bool isMiddleDown)
        {
            X = x;
            Y = y;
            IsLeftDown = isLeftDown;
            IsRightDown = isRightDown;
            IsMiddleDown = isMiddleDown;
        }

        /// <summary>Horizontal position in the host's client space. May be outside the host.</summary>
        public float X { get; }

        /// <summary>Vertical position in the host's client space. May be outside the host.</summary>
        public float Y { get; }

        /// <summary>Whether the primary button is held.</summary>
        public bool IsLeftDown { get; }

        /// <summary>Whether the secondary button is held.</summary>
        public bool IsRightDown { get; }

        /// <summary>Whether the middle button is held.</summary>
        public bool IsMiddleDown { get; }
    }
}
