using System;
#if !NET8_0_OR_GREATER
using System.Diagnostics;
#endif

namespace Gum.Wireframe
{
    /// <summary>
    /// A singleton intended to simplify timing.  Activity on TimeManager should
    /// get called once per frame so that an entire frame can operate on the same
    /// CurrentTime.
    /// </summary>
    public class TimeManager
    {
        #region Fields/Properties

        static TimeManager? mSelf;

#if NET8_0_OR_GREATER
        TimeProvider _timeProvider;
#endif
        long _startTimestamp;

        public double CurrentTime
        {
            get;
            private set;
        }

        public float SecondDifference { get; private set; }

        public static TimeManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new TimeManager();
                }
                return mSelf;
            }
        }

        #endregion

#if NET8_0_OR_GREATER
        public TimeManager() : this(TimeProvider.System)
        {
        }

        public TimeManager(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
            _startTimestamp = timeProvider.GetTimestamp();
        }

        /// <summary>
        /// Switches the clock this reads. Time restarts at zero, so tests can drive the
        /// double-click window and similar timing with a manual clock.
        /// </summary>
        public void SetTimeProvider(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
            _startTimestamp = timeProvider.GetTimestamp();
            CurrentTime = 0;
            SecondDifference = 0;
        }
#else
        // TimeProvider is .NET 8+; older targets (FRB's net6 build) read Stopwatch directly.
        public TimeManager()
        {
            _startTimestamp = Stopwatch.GetTimestamp();
        }
#endif

        public void Activity()
        {
            var lastTime = CurrentTime; 
#if NET8_0_OR_GREATER
            CurrentTime = _timeProvider.GetElapsedTime(_startTimestamp).TotalSeconds;
#else
            CurrentTime = (Stopwatch.GetTimestamp() - _startTimestamp) / (double)Stopwatch.Frequency;
#endif

            SecondDifference = (float)(CurrentTime - lastTime);
        }

    }
}
