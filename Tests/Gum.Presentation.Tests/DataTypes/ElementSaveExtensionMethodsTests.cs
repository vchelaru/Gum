using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests.DataTypes;

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

    [Fact]
    public void VariableSaveSorter_SortToMatch_SameListAsReference_ShouldPreserveOrder()
    {
        // Glue re-initializes a loaded element against its own default state, so the sort's
        // reference list is the very list being sorted. The order must not change.
        var variables = new List<VariableSave>();
        var expectedNames = new List<string>();
        for (int i = 0; i < 40; i++)
        {
            string name = "Variable" + ((i * 17) % 40);
            expectedNames.Add(name);
            variables.Add(new VariableSave { Name = name });
        }

        VariableSaveSorter.SortToMatch(variables, variables);

        variables.Select(v => v.Name).ToList().ShouldBe(expectedNames);
    }

    [Fact]
    public void VariableSaveSorter_Compare_ShouldMatchIndexOfFirstNameAndPlaceMissingFirst()
    {
        var reference = new List<VariableSave>
        {
            new VariableSave { Name = "B" },
            new VariableSave { Name = "A" },
            new VariableSave { Name = "B" },
            new VariableSave { Name = "C" },
        };
        var toSort = new List<VariableSave>
        {
            new VariableSave { Name = "C" },
            new VariableSave { Name = "Missing" },
            new VariableSave { Name = "A" },
            new VariableSave { Name = "B" },
        };
        VariableSaveSorter.SortToMatch(toSort, reference);

        // Duplicate "B" resolves to its first index (0); a name not in the reference is -1.
        toSort.Select(v => v.Name).ToList().ShouldBe(new[] { "Missing", "B", "A", "C" });
    }
}
