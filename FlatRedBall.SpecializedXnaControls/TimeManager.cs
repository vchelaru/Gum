namespace FlatRedBall.SpecializedXnaControls
{
    public class TimeManager
    {

        static TimeManager? mSelf;

        System.Diagnostics.Stopwatch mStopWatch;


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


        public TimeManager()
        {
            mStopWatch = new System.Diagnostics.Stopwatch();
            mStopWatch.Start();
        }


        public void Activity()
        {
            CurrentTime = mStopWatch.Elapsed.TotalSeconds;
        }
    }
}