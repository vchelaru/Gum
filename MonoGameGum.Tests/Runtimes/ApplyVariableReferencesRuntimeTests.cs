using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class ApplyVariableReferencesRuntimeTests : BaseTestClass
{
    public ApplyVariableReferencesRuntimeTests()
    {
        // Ensure no expression evaluator is wired up by default for fallback tests
        ElementSaveExtensions.CustomEvaluateExpression = null;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    #region Helpers

    private static StateSave BuildStateWithVariableReference(
        string referenceString,
        string? sourceObject = null,
        params (string name, object value, string type)[] variables)
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

        string listName = sourceObject != null
            ? $"{sourceObject}.VariableReferences"
            : "VariableReferences";

        VariableListSave<string> varRefList = new VariableListSave<string>
        {
            Type = "string",
            Name = listName
        };
        varRefList.Value.Add(referenceString);
        state.VariableLists.Add(varRefList);

        return state;
    }

    #endregion

    #region CommentSkipping

    [Fact]
    public void ApplyVariableReferences_CommentedOutLine_DoesNotApplyToRuntime()
    {
        ContainerRuntime parent = new ContainerRuntime();
        parent.X = 0;

        StateSave state = BuildStateWithVariableReference(
            "// X = SourceInstance.X",
            null,
            ("X", 0f, "float"),
            ("SourceInstance.X", 100f, "float"));

        parent.ApplyVariableReferences(state);

        parent.X.ShouldBe(0);
    }

    #endregion

    #region FallbackPath

    [Fact]
    public void ApplyVariableReferences_WithoutCustomEvaluator_FallsBackToRecursiveVariableFinder()
    {
        // CustomEvaluateExpression is null (set in constructor), so this uses the fallback path
        ContainerRuntime parent = new ContainerRuntime();
        parent.X = 0;

        StateSave state = BuildStateWithVariableReference(
            "X = SourceInstance.X",
            null,
            ("X", 0f, "float"),
            ("SourceInstance.X", 75f, "float"));

        parent.ApplyVariableReferences(state);

        parent.X.ShouldBe(75f);
    }

    #endregion

    #region InstanceScoped

    [Fact]
    public void ApplyVariableReferences_InstanceScopedReference_AppliesToChildInstance()
    {
        ContainerRuntime parent = new ContainerRuntime();
        ContainerRuntime child = new ContainerRuntime();
        child.Name = "MyChild";
        child.Tag = new InstanceSave { Name = "MyChild" };
        child.X = 0;
        child.Parent = parent;

        StateSave state = BuildStateWithVariableReference(
            "X = SourceInstance.X",
            sourceObject: "MyChild",
            ("SourceInstance.X", 55f, "float"));

        parent.ApplyVariableReferences(state);

        child.X.ShouldBe(55f);
    }

    #endregion

    #region MalformedInput

    [Fact]
    public void ApplyVariableReferences_EmptyReferenceString_DoesNotThrow()
    {
        ContainerRuntime parent = new ContainerRuntime();

        StateSave state = BuildStateWithVariableReference("");

        Should.NotThrow(() => parent.ApplyVariableReferences(state));
    }

    [Fact]
    public void ApplyVariableReferences_MalformedLine_DoesNotThrow()
    {
        ContainerRuntime parent = new ContainerRuntime();
        parent.X = 5;

        StateSave state = BuildStateWithVariableReference("not a valid reference");

        Should.NotThrow(() => parent.ApplyVariableReferences(state));
        parent.X.ShouldBe(5f);
    }

    #endregion

    #region SimpleAssignment

    [Fact]
    public void ApplyVariableReferences_SimpleAssignment_SetsPropertyOnRuntime()
    {
        ContainerRuntime parent = new ContainerRuntime();
        parent.X = 0;

        StateSave state = BuildStateWithVariableReference(
            "X = SourceInstance.X",
            null,
            ("X", 0f, "float"),
            ("SourceInstance.X", 42f, "float"));

        parent.ApplyVariableReferences(state);

        parent.X.ShouldBe(42f);
    }

    [Fact]
    public void ApplyVariableReferences_MultipleReferences_AppliesAll()
    {
        ContainerRuntime parent = new ContainerRuntime();
        parent.X = 0;
        parent.Y = 0;

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);

        state.Variables.Add(new VariableSave { Name = "X", Value = 0f, Type = "float", SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Y", Value = 0f, Type = "float", SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Source.X", Value = 10f, Type = "float", SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Source.Y", Value = 20f, Type = "float", SetsValue = true });

        VariableListSave<string> varRefList = new VariableListSave<string>
        {
            Type = "string",
            Name = "VariableReferences"
        };
        varRefList.Value.Add("X = Source.X");
        varRefList.Value.Add("Y = Source.Y");
        state.VariableLists.Add(varRefList);

        parent.ApplyVariableReferences(state);

        parent.X.ShouldBe(10f);
        parent.Y.ShouldBe(20f);
    }

    #endregion

    #region VisibleProperty

    [Fact]
    public void ApplyVariableReferences_BooleanProperty_SetsCorrectly()
    {
        ContainerRuntime parent = new ContainerRuntime();
        parent.Visible = true;

        StateSave state = BuildStateWithVariableReference(
            "Visible = Source.Visible",
            null,
            ("Visible", true, "bool"),
            ("Source.Visible", false, "bool"));

        parent.ApplyVariableReferences(state);

        parent.Visible.ShouldBeFalse();
    }

    #endregion
}
