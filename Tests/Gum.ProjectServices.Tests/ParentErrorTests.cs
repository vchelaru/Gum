using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Moq;
using Shouldly;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>Pins GUM0034 (missing parent) and GUM0048 (stale parent variable) for <c>X.Parent</c> variables (#8393).</summary>
public class ParentErrorTests : BaseTestClass
{
    private readonly HeadlessErrorChecker _sut;

    public ParentErrorTests()
    {
        _sut = new HeadlessErrorChecker(new Mock<ITypeResolver>().Object);
    }

    private ComponentSave CreateComponent(string[] instanceNames, params (string State, string Variable, string Value)[] parentVariables)
    {
        ComponentSave component = new ComponentSave { Name = "Holder" };
        foreach (string name in instanceNames)
        {
            component.Instances.Add(new InstanceSave { Name = name, BaseType = "Sprite" });
        }

        foreach (IGrouping<string, (string State, string Variable, string Value)> group in parentVariables.GroupBy(item => item.State))
        {
            StateSave state = new StateSave { Name = group.Key, ParentContainer = component };
            foreach ((string State, string Variable, string Value) item in group)
            {
                state.Variables.Add(new VariableSave { Name = item.Variable, Value = item.Value, Type = "string" });
            }
            component.States.Add(state);
        }

        Project.Components.Add(component);
        return component;
    }

    [Fact]
    public void GetErrorsFor_SourceInstanceMissing_ReportsStaleVariable()
    {
        ComponentSave component = CreateComponent(new[] { "Other" }, ("Default", "TopRow.Parent", "Ghost"));

        ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0048");
        StaleVariableDetails details = error.Details.ShouldBeOfType<StaleVariableDetails>();
        details.InstanceName.ShouldBe("TopRow");
        details.VariableName.ShouldBe("TopRow.Parent");
        details.StateName.ShouldBe("Default");
        error.Message.ShouldContain("stale variable TopRow.Parent");
        error.Message.ShouldContain("no instance TopRow exists");
    }

    [Fact]
    public void GetErrorsFor_SourceInstanceMissingAndParentMissing_ReportsOnlyStaleVariable()
    {
        ComponentSave component = CreateComponent(new string[0], ("Default", "TopRow.Parent", "Ghost"));

        ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0048");
    }

    [Fact]
    public void GetErrorsFor_ParentMissing_NamesTheMissingInstance()
    {
        ComponentSave component = CreateComponent(new[] { "Child" }, ("Default", "Child.Parent", "Ghost"));

        ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0034");
        error.Message.ShouldContain("Ghost");
        MissingParentDetails details = error.Details.ShouldBeOfType<MissingParentDetails>();
        details.InstanceName.ShouldBe("Child");
        details.ParentValue.ShouldBe("Ghost");
    }

    [Fact]
    public void GetErrorsFor_DottedParentMissing_MessageNamesOnlyTheInstance()
    {
        ComponentSave component = CreateComponent(new[] { "Child" }, ("Default", "Child.Parent", "Ghost.Inner"));

        ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0034");
        error.Message.ShouldContain("Ghost ");
        error.Message.ShouldNotContain("Inner");
        error.Details.ShouldBeOfType<MissingParentDetails>().ParentValue.ShouldBe("Ghost.Inner");
    }

    [Fact]
    public void GetErrorsFor_ValidDottedParent_HasNoError()
    {
        ComponentSave component = CreateComponent(new[] { "Child", "Menu" }, ("Default", "Child.Parent", "Menu.Inner"));

        _sut.GetErrorsFor(component, Project).ShouldBeEmpty();
    }

    [Fact]
    public void GetErrorsFor_StaleVariableWithValidParent_HasNoError()
    {
        ComponentSave component = CreateComponent(new[] { "Menu" }, ("Default", "TopRow.Parent", "Menu"));

        _sut.GetErrorsFor(component, Project).ShouldBeEmpty();
    }

    [Fact]
    public void GetErrorsFor_StaleVariableInTwoStates_ReportsEachState()
    {
        ComponentSave component = CreateComponent(new string[0],
            ("Default", "TopRow.Parent", "Ghost"),
            ("Other", "TopRow.Parent", "Ghost"));

        List<ErrorResult> errors = _sut.GetErrorsFor(component, Project).ToList();

        errors.Count.ShouldBe(2);
        errors.Select(item => ((StaleVariableDetails)item.Details!).StateName).ShouldBe(new[] { "Default", "Other" }, ignoreOrder: true);
    }
}
