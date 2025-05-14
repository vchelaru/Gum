using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.Logging
{
    internal class DebugLogger : IGumFormsSampleLogger
    {
        public void LogError(string message) => System.Diagnostics.Debug.WriteLine(message);
    }
}
