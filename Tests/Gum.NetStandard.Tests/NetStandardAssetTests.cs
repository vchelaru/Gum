using System.Reflection;
using System.Runtime.Versioning;
using Gum.Wireframe;
using Shouldly;

namespace Gum.NetStandard.Tests;

public class NetStandardAssetTests
{
    [Fact]
    public void LoadedAssemblies_AreTheNetStandard21Builds()
    {
        Assembly[] assemblies =
        {
            typeof(GraphicalUiElement).Assembly,
            typeof(GumServiceSkiaBase).Assembly,
        };

        foreach (Assembly assembly in assemblies)
        {
            assembly.GetCustomAttribute<TargetFrameworkAttribute>()!.FrameworkName
                .ShouldBe(".NETStandard,Version=v2.1", assembly.GetName().Name);
        }
    }
}
