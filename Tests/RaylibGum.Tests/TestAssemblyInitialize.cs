using Gum.Wireframe;
using RaylibGum.Renderables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;


[assembly: Xunit.TestFramework("RaylibGum.Tests.TestAssemblyInitialize", "RaylibGum.Tests")]

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace RaylibGum.Tests;
public class TestAssemblyInitialize : XunitTestFramework
{
    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink)
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;


    }
}
