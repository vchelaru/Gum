using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using GumRuntime;
using Shouldly;

namespace GumToolUnitTests.VariableGrid;

public class ApplyVariableReferencesElementSaveTests : BaseTestClass
{
    public ApplyVariableReferencesElementSaveTests()
    {
        GumExpressionService.Initialize();
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.VariableChangedThroughReference = null;
        base.Dispose();
    }

    #region Helpers

    private static ScreenSave BuildScreenWithReference(string referenceString, params (string name, object value, string type)[] variables)
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);

        foreach ((string name, object value, string type) variable in variables)
        {
            state.Variables.Add(new VariableSave
            {
                Name = variable.name,
                Value = variable.value,
                Type = variable.type,
                SetsValue = true
            });
        }

        VariableListSave<string> varRefList = new VariableListSave<string>
        {
            Type = "string",
            Name = "VariableReferences"
        };
        varRefList.Value.Add(referenceString);
        state.VariableLists.Add(varRefList);

        return screen;
    }

    #endregion

    #region CommentSkipping

    [Fact]
    public void ApplyVariableReferences_CommentedOutLine_DoesNotApply()
    {
        ScreenSave screen = BuildScreenWithReference(
            "// X = OtherInstance.X",
            ("X", 0f, "float"),
            ("OtherInstance.X", 100f, "float"));

        screen.ApplyVariableReferences(screen.DefaultState);

        screen.DefaultState.GetValue("X").ShouldBe(0f);
    }

    #endregion

    #region CrossElementReferences

    [Fact]
    public void ApplyVariableReferences_CrossElementComponentReference_ResolvesValue()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave button = new ComponentSave { Name = "Button" };
        StateSave buttonState = new StateSave { Name = "Default", ParentContainer = button };
        buttonState.Variables.Add(new VariableSave
        {
            Name = "Width",
            Value = 300f,
            Type = "float",
            SetsValue = true
        });
        button.States.Add(buttonState);
        project.Components.Add(button);

        ScreenSave screen = BuildScreenWithReference(
            "Width = Components/Button.Width",
            ("Width", 0f, "float"));
        project.Screens.Add(screen);

        screen.ApplyVariableReferences(screen.DefaultState);

        screen.DefaultState.GetValue("Width").ShouldBe(300f);
    }

    #endregion

    #region ExpressionEvaluation

    [Fact]
    public void ApplyVariableReferences_ArithmeticExpression_EvaluatesCorrectly()
    {
        ScreenSave screen = BuildScreenWithReference(
            "Width = OtherInstance.Width + 20",
            ("Width", 0f, "float"),
            ("OtherInstance.Width", 100f, "float"));

        screen.ApplyVariableReferences(screen.DefaultState);

        screen.DefaultState.GetValue("Width").ShouldBe(120f);
    }

    #endregion

    #region InstanceScoped

    [Fact]
    public void ApplyVariableReferences_InstanceScopedReference_AppliesCorrectly()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        // Add Container standard element so ObjectFinder can resolve instance base types
        StandardElementSave containerElement = new StandardElementSave { Name = "Container" };
        StateSave containerState = new StateSave { Name = "Default", ParentContainer = containerElement };
        containerState.Variables.Add(new VariableSave { Name = "Width", Value = 0f, Type = "float", SetsValue = true });
        containerElement.States.Add(containerState);
        project.StandardElements.Add(containerElement);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);
        project.Screens.Add(screen);

        InstanceSave sourceInstance = new InstanceSave
        {
            Name = "SourceInstance",
            BaseType = "Container",
            ParentContainer = screen
        };
        screen.Instances.Add(sourceInstance);

        InstanceSave myInstance = new InstanceSave
        {
            Name = "MyInstance",
            BaseType = "Container",
            ParentContainer = screen
        };
        screen.Instances.Add(myInstance);

        state.Variables.Add(new VariableSave { Name = "MyInstance.Width", Value = 0f, Type = "float", SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "SourceInstance.Width", Value = 250f, Type = "float", SetsValue = true });

        VariableListSave<string> varRefList = new VariableListSave<string>
        {
            Type = "string",
            Name = "MyInstance.VariableReferences"
        };
        varRefList.Value.Add("Width = SourceInstance.Width");
        state.VariableLists.Add(varRefList);

        screen.ApplyVariableReferences(state);

        state.GetValue("MyInstance.Width").ShouldBe(250f);
    }

    #endregion

    #region InvalidLines

    [Fact]
    public void ApplyVariableReferences_MalformedLine_DoesNotThrow()
    {
        ScreenSave screen = BuildScreenWithReference(
            "not a valid reference",
            ("Width", 0f, "float"));

        Should.NotThrow(() => screen.ApplyVariableReferences(screen.DefaultState));
        screen.DefaultState.GetValue("Width").ShouldBe(0f);
    }

    #endregion

    #region SimpleAssignment

    [Fact]
    public void ApplyVariableReferences_SimpleAssignment_CopiesValue()
    {
        ScreenSave screen = BuildScreenWithReference(
            "X = OtherInstance.X",
            ("X", 0f, "float"),
            ("OtherInstance.X", 42f, "float"));

        screen.ApplyVariableReferences(screen.DefaultState);

        screen.DefaultState.GetValue("X").ShouldBe(42f);
    }

    #endregion

    #region VariableChangedThroughReference

    [Fact]
    public void ApplyVariableReferences_ValueChanged_FiresVariableChangedDelegate()
    {
        ScreenSave screen = BuildScreenWithReference(
            "X = OtherInstance.X",
            ("X", 0f, "float"),
            ("OtherInstance.X", 99f, "float"));

        bool delegateFired = false;
        string changedMember = null;
        ElementSaveExtensions.VariableChangedThroughReference = (element, instance, member, oldValue) =>
        {
            delegateFired = true;
            changedMember = member;
        };

        screen.ApplyVariableReferences(screen.DefaultState);

        delegateFired.ShouldBeTrue();
        changedMember.ShouldBe("X");
    }

    [Fact]
    public void ApplyVariableReferences_ValueUnchanged_DoesNotFireDelegate()
    {
        ScreenSave screen = BuildScreenWithReference(
            "X = OtherInstance.X",
            ("X", 42f, "float"),
            ("OtherInstance.X", 42f, "float"));

        bool delegateFired = false;
        ElementSaveExtensions.VariableChangedThroughReference = (_, _, _, _) =>
        {
            delegateFired = true;
        };

        screen.ApplyVariableReferences(screen.DefaultState);

        delegateFired.ShouldBeFalse();
    }

    #endregion
}
