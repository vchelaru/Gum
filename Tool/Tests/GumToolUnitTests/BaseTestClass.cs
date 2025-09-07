using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumToolUnitTests;
public class BaseTestClass : IDisposable
{
    public void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }
}
