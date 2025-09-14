using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;


[assembly: Xunit.TestFramework("GumToolUnitTests.TestAssemblyInitialize", "GumToolUnitTests")]
// Gum uses some statics internally. Although parallel execution is nice,
// it can cause some tests to fail randomly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace GumToolUnitTests;

public class TestAssemblyInitialize : XunitTestFramework
{
    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink)
    {
    }
}
