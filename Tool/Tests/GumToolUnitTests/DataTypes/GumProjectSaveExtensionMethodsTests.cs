using Gum.DataTypes;
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
}
