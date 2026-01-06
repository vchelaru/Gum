using MonoGameGum.TestsCommon;
using Xunit.Abstractions;


[assembly: Xunit.TestFramework("MonoGameGum.Tests.V2.TestAssemblyInitialize", "MonoGameGum.Tests.V2")]
// Gum uses some statics internally. Although parallel execution is nice,
// it can cause some tests to fail randomly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace MonoGameGum.Tests.V2;

public sealed class TestAssemblyInitialize : TestAssemblyInitializeBase
{
    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink, Gum.Forms.DefaultVisualsVersion.V2)
    {
    }
}
