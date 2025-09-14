using Gum.Wireframe;
using GumRuntime;
using MonoGameGum.Forms;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Renderables;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using Xunit.Abstractions;
using Xunit.Sdk;


[assembly: Xunit.TestFramework("MonoGameGum.Tests.V2.TestAssemblyInitialize", "MonoGameGum.Tests.V2")]
// Gum uses some statics internally. Although parallel execution is nice,
// it can cause some tests to fail randomly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace MonoGameGum.Tests.V2;

public sealed class TestAssemblyInitialize : TestAssemblyInitializeBase
{
    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink)
    {
    }
}
