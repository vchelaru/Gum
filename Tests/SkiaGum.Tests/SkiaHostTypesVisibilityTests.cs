using Shouldly;

namespace SkiaGum.Tests;

public class SkiaHostTypesVisibilityTests
{
    [Theory]
    [InlineData("SkiaGum.Helpers.ISkiaHostTarget")]
    [InlineData("SkiaGum.Helpers.SkiaTargetHolder`1")]
    public void HostPlumbingTypes_AreNotPublic(string typeName)
    {
        Type type = typeof(Gum.GumServiceSkiaBase).Assembly.GetType(typeName).ShouldNotBeNull();

        type.IsPublic.ShouldBeFalse($"{typeName} exposes SKCanvas and is host plumbing, not game API (#7788)");
    }
}
