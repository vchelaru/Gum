using System;

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

        TimeProvider _timeProvider;
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


        public void Activity()
        {
            var lastTime = CurrentTime; 
            CurrentTime = _timeProvider.GetElapsedTime(_startTimestamp).TotalSeconds;

            SecondDifference = (float)(CurrentTime - lastTime);
        }

    }
}
