using MonoGameGum.TestsCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

[assembly: Xunit.TestFramework("MonoGameGum.Shapes.Tests.TestAssemblyInitialize", "MonoGameGum.Shapes.Tests")]
// Gum uses some statics internally. Although parallel execution is nice,
// it can cause some tests to fail randomly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MonoGameGum.Shapes.Tests;

internal class TestAssemblyInitialize : TestAssemblyInitializeBase
{
    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink, Gum.Forms.DefaultVisualsVersion.V2)
    {
    }
}
