using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using GumRuntime;
using Shouldly;

namespace GumToolUnitTests.VariableGrid;

public class DetectUnpropagatedReferencesTests : BaseTestClass
{
    public DetectUnpropagatedReferencesTests()
    {
        GumExpressionService.Initialize();
    }

    [Fact]
    public void GetStatesWithUnpropagatedReferences_StateHasReferenceButMissingScalar_ReportsState()
    {
        // AI-authored repro: a state has a VariableReferences row assigning X,
        // but no corresponding scalar X was materialized into state.Variables.
        // ApplyVariableReferences was never run against this state.
        ScreenSave screen = new ScreenSave { Name = "ScreenWithBrokenState" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);

        VariableListSave<string> refs = new VariableListSave<string>
        {
            Name = "VariableReferences",
            Type = "string"
        };
        refs.Value.Add("X = 100");
        state.VariableLists.Add(refs);

        var result = screen.GetStatesWithUnpropagatedReferences();

        result.ShouldContain(state,
            "because the state has a VariableReferences row assigning X but no scalar X exists in state.Variables.");
    }

    [Fact]
    public void GetStatesWithUnpropagatedReferences_TunneledReferenceMissingScalar_ReportsState()
    {
        // Row name is "Instance.VariableReferences"; the LHS "X" must be qualified
        // as "Instance.X" when checking state.Variables — same qualification
        // ApplyVariableReferences performs.
        ScreenSave screen = new ScreenSave { Name = "ScreenWithTunneledMissing" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);

        InstanceSave instance = new InstanceSave { Name = "TextInstance", BaseType = "Text", ParentContainer = screen };
        screen.Instances.Add(instance);

        VariableListSave<string> refs = new VariableListSave<string>
        {
            Name = "TextInstance.VariableReferences",
            Type = "string"
        };
        refs.Value.Add("X = 100");
        state.VariableLists.Add(refs);

        var result = screen.GetStatesWithUnpropagatedReferences();

        result.ShouldContain(state);
    }

    [Fact]
    public void GetStatesWithUnpropagatedReferences_TunneledReferenceWithMaterializedScalar_DoesNotReport()
    {
        // Regression guard for the tunneled qualification: a row of
        // "Instance.VariableReferences" with line "X = 100" is correctly
        // propagated when state.Variables contains "Instance.X".
        ScreenSave screen = new ScreenSave { Name = "ScreenWithTunneledGood" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);

        InstanceSave instance = new InstanceSave { Name = "TextInstance", BaseType = "Text", ParentContainer = screen };
        screen.Instances.Add(instance);

        state.Variables.Add(new VariableSave
        {
            Name = "TextInstance.X",
            Type = "float",
            Value = 100f,
            SetsValue = true
        });

        VariableListSave<string> refs = new VariableListSave<string>
        {
            Name = "TextInstance.VariableReferences",
            Type = "string"
        };
        refs.Value.Add("X = 100");
        state.VariableLists.Add(refs);

        var result = screen.GetStatesWithUnpropagatedReferences();

        result.ShouldNotContain(state);
    }

    [Fact]
    public void GetStatesWithUnpropagatedReferences_CategorizedStateMissingScalar_ReportsState()
    {
        // The walk must include categorized states, not just DefaultState.
        ComponentSave component = new ComponentSave { Name = "ComponentWithCategorized" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);

        StateSaveCategory category = new StateSaveCategory { Name = "Category1" };
        StateSave state1 = new StateSave { Name = "State1", ParentContainer = component };
        VariableListSave<string> refs = new VariableListSave<string>
        {
            Name = "VariableReferences",
            Type = "string"
        };
        refs.Value.Add("X = 100");
        state1.VariableLists.Add(refs);
        category.States.Add(state1);
        component.Categories.Add(category);

        var result = component.GetStatesWithUnpropagatedReferences();

        result.ShouldContain(state1);
    }

    [Fact]
    public void GetStatesWithUnpropagatedReferences_CommentedLine_DoesNotReport()
    {
        // A `// ...` line is intentionally inert — ApplyVariableReferences skips
        // it, so the detector must not treat it as a missing materialization.
        ScreenSave screen = new ScreenSave { Name = "ScreenWithCommentedRef" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);

        VariableListSave<string> refs = new VariableListSave<string>
        {
            Name = "VariableReferences",
            Type = "string"
        };
        refs.Value.Add("// X = 100");
        state.VariableLists.Add(refs);

        var result = screen.GetStatesWithUnpropagatedReferences();

        result.ShouldNotContain(state);
    }

    [Fact]
    public void GetStatesWithUnpropagatedReferences_MultiLineOneMissing_ReportsState()
    {
        // A state with multiple reference lines is inconsistent if *any* line is
        // missing its materialized scalar.
        ScreenSave screen = new ScreenSave { Name = "ScreenWithMixedLines" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);

        state.Variables.Add(new VariableSave
        {
            Name = "X",
            Type = "float",
            Value = 100f,
            SetsValue = true
        });

        VariableListSave<string> refs = new VariableListSave<string>
        {
            Name = "VariableReferences",
            Type = "string"
        };
        refs.Value.Add("X = 100");
        refs.Value.Add("Y = 200");
        state.VariableLists.Add(refs);

        var result = screen.GetStatesWithUnpropagatedReferences();

        result.ShouldContain(state,
            "because Y has a reference assignment but no materialized scalar in state.Variables.");
    }

    [Fact]
    public void GetStatesWithUnpropagatedReferences_TunneledCollapsedColorReferenceWithMaterializedScalars_DoesNotReport()
    {
        // "Color = Source.Color" on an instance's VariableReferences row must expand to
        // RectangleInstance.Red/Green/Blue for the materialized-scalar check - the instance's
        // type (ColorType) is what tells the detector the composite's channels are real.
        // Without the fix, the detector looks for a literal "RectangleInstance.Color" scalar,
        // which never exists, and always false-positives as unpropagated.
        ComponentSave colorType = new ComponentSave { Name = "ColorType" };
        StateSave colorTypeDefault = new StateSave { Name = "Default", ParentContainer = colorType };
        colorTypeDefault.Variables.Add(new VariableSave { Name = "Red", Type = "int", Value = 0, SetsValue = true });
        colorTypeDefault.Variables.Add(new VariableSave { Name = "Green", Type = "int", Value = 0, SetsValue = true });
        colorTypeDefault.Variables.Add(new VariableSave { Name = "Blue", Type = "int", Value = 0, SetsValue = true });
        colorType.States.Add(colorTypeDefault);

        ScreenSave screen = new ScreenSave { Name = "ScreenWithTunneledCollapsedColorGood" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);

        InstanceSave instance = new InstanceSave { Name = "RectangleInstance", BaseType = "ColorType", ParentContainer = screen };
        screen.Instances.Add(instance);

        state.Variables.Add(new VariableSave { Name = "RectangleInstance.Red", Type = "int", Value = 10, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "RectangleInstance.Green", Type = "int", Value = 20, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "RectangleInstance.Blue", Type = "int", Value = 30, SetsValue = true });

        VariableListSave<string> refs = new VariableListSave<string>
        {
            Name = "RectangleInstance.VariableReferences",
            Type = "string"
        };
        refs.Value.Add("Color = Source.Color");
        state.VariableLists.Add(refs);

        GumProjectSave project = new GumProjectSave();
        project.Screens.Add(screen);
        project.Components.Add(colorType);
        ObjectFinder.Self.GumProjectSave = project;

        var result = screen.GetStatesWithUnpropagatedReferences();

        result.ShouldNotContain(state);
    }

    [Fact]
    public void GetStatesWithUnpropagatedReferences_StateHasReferenceAndMaterializedScalar_DoesNotReport()
    {
        // Regression guard: a properly authored state (reference row + materialized
        // scalar already present) should NOT be reported. This is the shape
        // ApplyVariableReferences leaves behind, so the detector must not flag it.
        ScreenSave screen = new ScreenSave { Name = "ScreenWithGoodState" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);

        state.Variables.Add(new VariableSave
        {
            Name = "X",
            Type = "float",
            Value = 100f,
            SetsValue = true
        });

        VariableListSave<string> refs = new VariableListSave<string>
        {
            Name = "VariableReferences",
            Type = "string"
        };
        refs.Value.Add("X = 100");
        state.VariableLists.Add(refs);

        var result = screen.GetStatesWithUnpropagatedReferences();

        result.ShouldNotContain(state,
            "because the scalar X is materialized in state.Variables, matching what ApplyVariableReferences would leave behind.");
    }
}
