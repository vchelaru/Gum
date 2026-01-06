using MonoGameGum.TestsCommon;
using Xunit.Abstractions;

[assembly: Xunit.TestFramework("MonoGameGum.Tests.V3.TestAssemblyInitialize", "MonoGameGum.Tests.V3")]
// Gum uses some statics internally. Although parallel execution is nice,
// it can cause some tests to fail randomly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace MonoGameGum.Tests.V3;

internal class TestAssemblyInitialize : TestAssemblyInitializeBase
{
    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink, Gum.Forms.DefaultVisualsVersion.V3)
    {
    }
}
