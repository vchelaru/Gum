using Gum.Wireframe;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("SokolGum.Tests.TestAssemblyInitialize", "SokolGum.Tests")]

// Disables xunit's default parallel-test collection so tests that mutate
// process-wide statics (CustomSetPropertyOnRenderable reflection delegate
// on GraphicalUiElement, etc.) don't race each other. Matches the pattern
// in RaylibGum.Tests.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SokolGum.Tests;

public class TestAssemblyInitialize : XunitTestFramework
{
    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink)
    {
        // Wire the reflection fallback so .gumx-style property assignment
        // tests can exercise the full Gum dispatch path (SetProperty →
        // SetPropertyOnRenderable delegate → SetPropertyWithEnumConversion).
        // Production code wires this in SystemManagers.Initialize; tests
        // run without a SystemManagers, so do it here.
        GraphicalUiElement.SetPropertyOnRenderable =
            (r, e, name, value) =>
                CustomSetPropertyOnRenderable.SetPropertyOnRenderable(r, e, name, value!);
    }
}
