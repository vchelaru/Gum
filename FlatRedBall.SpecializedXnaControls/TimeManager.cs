namespace FlatRedBall.SpecializedXnaControls
{
    public class TimeManager
    {

        static TimeManager? mSelf;

        System.TimeProvider _timeProvider;
        long _startTimestamp;


        public double CurrentTime
        {
            get;
            private set;
        }

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


        public TimeManager() : this(System.TimeProvider.System)
        {
        }

        public TimeManager(System.TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
            _startTimestamp = timeProvider.GetTimestamp();
        }

        /// <summary>
        /// Switches the clock this reads. Time restarts at zero, so tests can drive the
        /// double-click window with a manual clock.
        /// </summary>
        public void SetTimeProvider(System.TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
            _startTimestamp = timeProvider.GetTimestamp();
            CurrentTime = 0;
        }


        public void Activity()
        {
            CurrentTime = _timeProvider.GetElapsedTime(_startTimestamp).TotalSeconds;
        }
    }
}