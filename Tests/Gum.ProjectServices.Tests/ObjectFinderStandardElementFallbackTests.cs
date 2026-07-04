using Gum.DataTypes;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="ObjectFinder.RegisterFallbackStandardElements"/> /
/// <see cref="ObjectFinder.ClearFallbackStandardElements"/> and the fallback lookup they enable on
/// <see cref="ObjectFinder.GetStandardElement(string)"/>. Covers issue #3505: in a code-only game
/// (no <see cref="GumProjectSave"/> loaded), Standard-Element-owned category/state assignments
/// (e.g. a NineSlice's ColorCategoryState) silently no-op because GetStandardElement returns null.
/// </summary>
public class ObjectFinderStandardElementFallbackTests : BaseTestClass
{
    [Fact]
    public void GetStandardElement_WithNoProjectAndNoFallback_ReturnsNull()
    {
        ObjectFinder.Self.GumProjectSave = null;

        StandardElementSave? result = ObjectFinder.Self.GetStandardElement("NineSlice");

        result.ShouldBeNull();
    }

    [Fact]
    public void GetStandardElement_WithNoProjectAndRegisteredFallback_ReturnsFallback()
    {
        ObjectFinder.Self.GumProjectSave = null;
        StandardElementSave fallback = new StandardElementSave { Name = "NineSlice" };

        ObjectFinder.Self.RegisterFallbackStandardElements(new[] { fallback });

        StandardElementSave? result = ObjectFinder.Self.GetStandardElement("NineSlice");

        result.ShouldBeSameAs(fallback);
    }

    [Fact]
    public void GetStandardElement_WithLoadedProject_PrefersProjectOverFallback()
    {
        StandardElementSave projectStandard = Project.StandardElements.Find(item => item.Name == "NineSlice")!;
        ObjectFinder.Self.GumProjectSave = Project;

        StandardElementSave fallback = new StandardElementSave { Name = "NineSlice" };
        ObjectFinder.Self.RegisterFallbackStandardElements(new[] { fallback });

        StandardElementSave? result = ObjectFinder.Self.GetStandardElement("NineSlice");

        result.ShouldBeSameAs(projectStandard);
    }

    [Fact]
    public void RegisterFallbackStandardElements_CalledTwiceForSameName_OverwritesPreviousRegistration()
    {
        ObjectFinder.Self.GumProjectSave = null;
        StandardElementSave first = new StandardElementSave { Name = "NineSlice" };
        StandardElementSave second = new StandardElementSave { Name = "NineSlice" };

        ObjectFinder.Self.RegisterFallbackStandardElements(new[] { first });
        ObjectFinder.Self.RegisterFallbackStandardElements(new[] { second });

        StandardElementSave? result = ObjectFinder.Self.GetStandardElement("NineSlice");

        result.ShouldBeSameAs(second);
    }
}
