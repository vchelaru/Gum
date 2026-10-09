using MonoGameGum.TestsCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;


[assembly: Xunit.TestFramework("MonoGameGum.Tests.TestAssemblyInitialize", "MonoGameGum.Tests")]
// Gum uses some statics internally. Although parallel execution is nice,
// it can cause some tests to fail randomly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
// Default order unless GUM_TEST_SHUFFLE_SEED is set (weekly shuffled-order workflow, #5820, #5840).
[assembly: TestCollectionOrderer("GumTestSupport.ShuffledTestCollectionOrderer", "MonoGameGum.Tests")]
[assembly: TestCaseOrderer("GumTestSupport.ShuffledTestCaseOrderer", "MonoGameGum.Tests")]
namespace MonoGameGum.Tests;
public class TestAssemblyInitialize : TestAssemblyInitializeBase
{
    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink, Gum.Forms.DefaultVisualsVersion.V3)
    {
    }
}
