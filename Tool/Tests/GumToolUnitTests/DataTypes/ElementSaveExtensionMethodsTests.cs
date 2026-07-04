using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.DataTypes;

public class ElementSaveExtensionMethodsTests : BaseTestClass
{
    public override void Dispose()
    {
        // CustomFixEnumerations is a static hook (normally wired up once via reflection in
        // Program.cs) - reset it so this test's stub doesn't leak into other tests.
        VariableSaveExtensionMethods.CustomFixEnumerations = null;
        base.Dispose();
    }

    [Fact]
    public void Initialize_VariableOnlyCoercedByFixEnumerations_ShouldNotMarkElementAsModified()
    {
        // FixEnumerations only coerces a variable's in-memory representation (int -> enum) so
        // reflection/property-grid code sees the right CLR type; it never changes the persisted
        // value. Simulate the tool's real coercion hook reporting "this value changed" to prove
        // that alone does not mark the element (and therefore the project) as modified - doing so
        // would force a re-save on every load of any project with enum variables, for no real
        // content change.
        VariableSaveExtensionMethods.CustomFixEnumerations = _ => true;

        var component = new ComponentSave { Name = "EnumCoercionComponent", BaseType = "Container" };
        var defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Name = "SomeEnumVariable",
            Type = "SomeEnumType",
            Value = 0,
            SetsValue = true
        });
        component.States.Add(defaultState);

        var wasModified = component.Initialize(defaultState: null);

        wasModified.ShouldBeFalse(
            "because FixEnumerations coercing a variable's in-memory value must not, by itself, " +
            "cause the element to be considered modified.");
    }
}
