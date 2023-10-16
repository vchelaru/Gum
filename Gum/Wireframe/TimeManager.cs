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

        static TimeManager mSelf;

        System.Diagnostics.Stopwatch mStopWatch;

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

        public TimeManager()
        {
            InitializeStopwatch();
        }


        public void Activity()
        {
            var lastTime = CurrentTime; 
            CurrentTime = mStopWatch.Elapsed.TotalSeconds;

            SecondDifference = (float)(CurrentTime - lastTime);
        }


        void InitializeStopwatch()
        {
            mStopWatch = new System.Diagnostics.Stopwatch();
            mStopWatch.Start();
        }
    }
}
