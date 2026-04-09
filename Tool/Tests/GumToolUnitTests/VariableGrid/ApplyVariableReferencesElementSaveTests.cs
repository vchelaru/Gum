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

    #region ApplyAllVariableReferences

    [Fact]
    public void ApplyAllVariableReferences_AppliesInDependencyOrder()
    {
        // A.Width = 100 (no references)
        // B.Width = Components/A.Width (depends on A)
        // C.Width = Components/B.Width (depends on B)
        // After ApplyAll, C.Width should be 100 regardless of element order
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave compA = new ComponentSave { Name = "A" };
        StateSave stateA = new StateSave { Name = "Default", ParentContainer = compA };
        stateA.Variables.Add(new VariableSave { Name = "Width", Value = 100f, Type = "float", SetsValue = true });
        compA.States.Add(stateA);
        project.Components.Add(compA);

        ComponentSave compB = new ComponentSave { Name = "B" };
        StateSave stateB = new StateSave { Name = "Default", ParentContainer = compB };
        stateB.Variables.Add(new VariableSave { Name = "Width", Value = 0f, Type = "float", SetsValue = true });
        VariableListSave<string> refsB = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        refsB.Value.Add("Width = Components/A.Width");
        stateB.VariableLists.Add(refsB);
        compB.States.Add(stateB);
        project.Components.Add(compB);

        ComponentSave compC = new ComponentSave { Name = "C" };
        StateSave stateC = new StateSave { Name = "Default", ParentContainer = compC };
        stateC.Variables.Add(new VariableSave { Name = "Width", Value = 0f, Type = "float", SetsValue = true });
        VariableListSave<string> refsC = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        refsC.Value.Add("Width = Components/B.Width");
        stateC.VariableLists.Add(refsC);
        compC.States.Add(stateC);
        project.Components.Add(compC);

        project.ApplyAllVariableReferences();

        stateA.GetValue("Width").ShouldBe(100f);
        stateB.GetValue("Width").ShouldBe(100f);
        stateC.GetValue("Width").ShouldBe(100f);
    }

    [Fact]
    public void ApplyAllVariableReferences_CircularDependency_DoesNotThrow()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave compA = new ComponentSave { Name = "A" };
        StateSave stateA = new StateSave { Name = "Default", ParentContainer = compA };
        stateA.Variables.Add(new VariableSave { Name = "Width", Value = 50f, Type = "float", SetsValue = true });
        VariableListSave<string> refsA = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        refsA.Value.Add("Width = Components/B.Width");
        stateA.VariableLists.Add(refsA);
        compA.States.Add(stateA);
        project.Components.Add(compA);

        ComponentSave compB = new ComponentSave { Name = "B" };
        StateSave stateB = new StateSave { Name = "Default", ParentContainer = compB };
        stateB.Variables.Add(new VariableSave { Name = "Width", Value = 75f, Type = "float", SetsValue = true });
        VariableListSave<string> refsB = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        refsB.Value.Add("Width = Components/A.Width");
        stateB.VariableLists.Add(refsB);
        compB.States.Add(stateB);
        project.Components.Add(compB);

        Should.NotThrow(() => project.ApplyAllVariableReferences());
    }

    [Fact]
    public void ApplyAllVariableReferences_CategoryStateReferences_AreApplied()
    {
        // Mirrors the real-world pattern: ColoredRectangle has a ColorCategory
        // with states like "Primary" that reference Components/Styles.Primary.Red
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave styles = new ComponentSave { Name = "Styles" };
        StateSave stylesState = new StateSave { Name = "Default", ParentContainer = styles };
        stylesState.Variables.Add(new VariableSave { Name = "Primary.Red", Value = 255, Type = "int", SetsValue = true });
        stylesState.Variables.Add(new VariableSave { Name = "Primary.Green", Value = 0, Type = "int", SetsValue = true });
        stylesState.Variables.Add(new VariableSave { Name = "Primary.Blue", Value = 0, Type = "int", SetsValue = true });
        styles.States.Add(stylesState);
        project.Components.Add(styles);

        StandardElementSave coloredRect = new StandardElementSave { Name = "ColoredRectangle" };
        StateSave coloredRectDefault = new StateSave { Name = "Default", ParentContainer = coloredRect };
        coloredRect.States.Add(coloredRectDefault);

        StateSaveCategory colorCategory = new StateSaveCategory { Name = "ColorCategory" };
        StateSave primaryState = new StateSave { Name = "Primary", ParentContainer = coloredRect };
        primaryState.Variables.Add(new VariableSave { Name = "Red", Value = 0, Type = "int", SetsValue = true });
        primaryState.Variables.Add(new VariableSave { Name = "Green", Value = 0, Type = "int", SetsValue = true });
        primaryState.Variables.Add(new VariableSave { Name = "Blue", Value = 0, Type = "int", SetsValue = true });
        VariableListSave<string> refs = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        refs.Value.Add("Red = Components/Styles.Primary.Red");
        refs.Value.Add("Green = Components/Styles.Primary.Green");
        refs.Value.Add("Blue = Components/Styles.Primary.Blue");
        primaryState.VariableLists.Add(refs);
        colorCategory.States.Add(primaryState);
        coloredRect.Categories.Add(colorCategory);
        project.StandardElements.Add(coloredRect);

        project.ApplyAllVariableReferences();

        primaryState.GetValue("Red").ShouldBe(255);
        primaryState.GetValue("Green").ShouldBe(0);
        primaryState.GetValue("Blue").ShouldBe(0);
    }

    [Fact]
    public void ApplyAllVariableReferences_NoReferences_DoesNotThrow()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave comp = new ComponentSave { Name = "Simple" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = comp };
        state.Variables.Add(new VariableSave { Name = "Width", Value = 100f, Type = "float", SetsValue = true });
        comp.States.Add(state);
        project.Components.Add(comp);

        Should.NotThrow(() => project.ApplyAllVariableReferences());

        state.GetValue("Width").ShouldBe(100f);
    }

    #endregion
}
