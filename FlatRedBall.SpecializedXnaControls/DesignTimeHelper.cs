using System.ComponentModel;

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
