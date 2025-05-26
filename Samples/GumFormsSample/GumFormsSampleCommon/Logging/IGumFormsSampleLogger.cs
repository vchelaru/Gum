using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.Logging
{
    internal interface IGumFormsSampleLogger
    {
        void LogError(string message);
    }
}
