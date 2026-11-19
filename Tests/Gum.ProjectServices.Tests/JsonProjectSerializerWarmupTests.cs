using System.Linq;
using Gum.ProjectServices;
using Shouldly;
using Xunit;

namespace Gum.ProjectServices.Tests;

public class JsonProjectSerializerWarmupTests
{
    [Fact]
    public void WarmUp_ConfiguresTheNestedTypesAProjectLoadReads()
    {
        JsonProjectSerializerWarmup warmup = new JsonProjectSerializerWarmup();

        string[] names = warmup.WarmUp().Select(type => type.Name).ToArray();

        names.ShouldContain("GumProjectSaveJson");
        names.ShouldContain("ElementSaveJson");
        names.ShouldContain("BehaviorSaveJson");
        // Reached only through properties and collections of the roots.
        names.ShouldContain("StateSaveJson");
        names.ShouldContain("InstanceSaveJson");
    }
}
