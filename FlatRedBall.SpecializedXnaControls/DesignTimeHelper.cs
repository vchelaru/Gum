using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace FlatRedBall.SpecializedXnaControls
{
    public static class DesignTimeHelper
    {
        public static bool IsInDesignMode
        {
            get
            {
                bool isInDesignMode = LicenseManager.UsageMode == LicenseUsageMode.Designtime;

                return isInDesignMode;
            }
        }
    }
}
