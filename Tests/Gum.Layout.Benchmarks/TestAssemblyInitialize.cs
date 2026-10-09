using MonoGameGum.TestsCommon;
using Xunit.Abstractions;

[assembly: Xunit.TestFramework("Gum.Layout.Benchmarks.TestAssemblyInitialize", "Gum.Layout.Benchmarks")]
// Gum uses some statics internally, and timings are only meaningful when nothing else is running.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Gum.Layout.Benchmarks;

internal class TestAssemblyInitialize : TestAssemblyInitializeBase
{
    public TestAssemblyInitialize(IMessageSink messageSink) :
        base(messageSink, Gum.Forms.DefaultVisualsVersion.V3)
    {
    }
}
