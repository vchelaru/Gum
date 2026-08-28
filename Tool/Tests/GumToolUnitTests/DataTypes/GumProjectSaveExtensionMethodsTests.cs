using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.DataTypes;

public class GumProjectSaveExtensionMethodsTests : BaseTestClass
{
    // Regression: GumProjectSaveExtensionMethods.Initialize's Screen loop forwards
    // tolerateMissingDefaultStates, and the StandardElements loop tolerates via its own try/catch,
    // but the Component loop's two componentSave.Initialize(...) calls never forward the flag - so
    // it always resolves instances with throwExceptionOnMissing: true regardless of what the caller
    // asked for. A component containing an instance of a type whose default state can't be resolved
    // (e.g. a plugin-contributed standard the current process hasn't wired a resolver for) then
    // crashes the entire load, even though ProjectManager.LoadProject explicitly requests
    // tolerateMissingDefaultStates: true "so we don't immediately crash the tool."
    [Fact]
    public void Initialize_WithTolerateMissingDefaultStates_DoesNotThrow_WhenComponentInstanceHasUnresolvableBaseType()
    {
        const string unresolvableType = "TotallyUnresolvableTestStandardType";

        StandardElementSave standard = new StandardElementSave { Name = unresolvableType };

        ComponentSave component = new ComponentSave { Name = "MyComponent", BaseType = "Container" };
        InstanceSave instance = new InstanceSave
        {
            Name = "MyInstance",
            BaseType = unresolvableType,
            ParentContainer = component
        };
        component.Instances.Add(instance);

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(standard);
        project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = project;

        Should.NotThrow(() => project.Initialize(tolerateMissingDefaultStates: true));
    }

    // A malformed .gumx (a reference or element with no Name, or a nil entry) must not take down the
    // whole project load. The sort comparers used to dereference Name directly, which surfaced as
    // InvalidOperationException("Failed to compare two elements in the array").
    [Fact]
    public void SortElementAndBehaviors_DoesNotThrow_AndSortsNamedItems_WhenNamesAreNull()
    {
        GumProjectSave project = new GumProjectSave();
        project.ScreenReferences.Add(new ElementReference { Name = "ZScreen" });
        project.ScreenReferences.Add(new ElementReference { Name = null });
        project.ScreenReferences.Add(new ElementReference { Name = "AScreen" });
        project.ComponentReferences.Add(new ElementReference { Name = null });
        project.StandardElementReferences.Add(new ElementReference { Name = null });
        project.BehaviorReferences.Add(new BehaviorReference { Name = null });

        project.Screens.Add(new ScreenSave { Name = "ZScreen" });
        project.Screens.Add(new ScreenSave { Name = null });
        project.Screens.Add(new ScreenSave { Name = "AScreen" });
        project.Components.Add(new ComponentSave { Name = null });
        project.StandardElements.Add(new StandardElementSave { Name = null });
        project.Behaviors.Add(new BehaviorSave { Name = null });

        Should.NotThrow(() => project.SortElementAndBehaviors());

        project.ScreenReferences[1].Name.ShouldBe("AScreen");
        project.ScreenReferences[2].Name.ShouldBe("ZScreen");
        project.Screens[1].Name.ShouldBe("AScreen");
        project.Screens[2].Name.ShouldBe("ZScreen");
    }

    [Fact]
    public void SortElementAndBehaviors_DoesNotThrow_WhenListsContainNullEntries()
    {
        GumProjectSave project = new GumProjectSave();
        project.ScreenReferences.Add(new ElementReference { Name = "ZScreen" });
        project.ScreenReferences.Add(null!);
        project.ScreenReferences.Add(new ElementReference { Name = "AScreen" });
        project.BehaviorReferences.Add(null!);
        project.Screens.Add(new ScreenSave { Name = "ZScreen" });
        project.Screens.Add(null!);
        project.Behaviors.Add(null!);

        Should.NotThrow(() => project.SortElementAndBehaviors());
    }
}
