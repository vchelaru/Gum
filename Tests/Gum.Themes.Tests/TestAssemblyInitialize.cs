using MonoGameGum.TestsCommon;
using Xunit.Abstractions;

[assembly: Xunit.TestFramework("Gum.Themes.Tests.TestAssemblyInitialize", "Gum.Themes.Tests")]
// Theme Apply()/ConfigureStyling() mutate shared statics (Styling.ActiveStyle,
// XyzStyling.ActiveStyle), so parallel test execution would cause random failures.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Gum.Themes.Tests;

internal class TestAssemblyInitialize : TestAssemblyInitializeBase
{
    public TestAssemblyInitialize(IMessageSink messageSink) :
        base(messageSink, Gum.Forms.DefaultVisualsVersion.V3)
    {
    }
}
