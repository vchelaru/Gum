using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.Wireframe
{
    /// <summary>
    /// A singleton intended to simplify timing.  Activity on TimeManager should
    /// get called once per frame so that an entire frame can operate on the same
    /// CurrentTime.
    /// </summary>
    public class TimeManager
    {

        static TimeManager mSelf;

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
            InitializeStopwatch();
        }


        public void Activity()
        {
            CurrentTime = mStopWatch.Elapsed.TotalSeconds;

        }



        void InitializeStopwatch()
        {
            mStopWatch = new System.Diagnostics.Stopwatch();
            mStopWatch.Start();
        }
    }
}
