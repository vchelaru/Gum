using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// A label component whose child <c>Shadow</c> copies the component's own variable through a
/// same-component reference (<c>Text = Text</c>) shows the label's value, whichever way that value was
/// set (#6385 sweep).
/// </summary>
public partial class VariableReferenceCreationTests : BaseTestClass
{
    private const string DefaultText = "Component default";

    public VariableReferenceCreationTests()
    {
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static void UseExpressions(bool useExpressions)
    {
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }
    }

    private static GumProjectSave CreateProject()
    {
        GumProjectSave project = new GumProjectSave();
        foreach (string standard in new[] { "Container", "Text", "ColoredRectangle" })
        {
            StandardElementSave standardElement = new StandardElementSave { Name = standard };
            standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor(standard)!);
            standardElement.DefaultState!.ParentContainer = standardElement;
            project.StandardElements.Add(standardElement);
        }
        ObjectFinder.Self.GumProjectSave = project;
        StandardRuntimeRegistrations.Register();
        return project;
    }

    private static void AddVariable(StateSave state, string name, string type, object value)
    {
        state.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });
    }

    private static void AddRows(StateSave state, string instanceName, params string[] rows)
    {
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = instanceName + ".VariableReferences" };
        foreach (string row in rows)
        {
            list.Value.Add(row);
        }
        state.VariableLists.Add(list);
    }

    /// <summary>A Label component (BaseType Text) holding a Text instance named Shadow with the given rows.</summary>
    private static ComponentSave AddLabelComponent(GumProjectSave project, string name, params string[] shadowRows)
    {
        ComponentSave label = new ComponentSave { Name = name, BaseType = "Text" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = label };
        label.States.Add(state);
        label.Instances.Add(new InstanceSave { Name = "Shadow", BaseType = "Text", ParentContainer = label });
        AddVariable(state, "Text", "string", DefaultText);
        AddVariable(state, "Shadow.X", "float", 1f);
        AddRows(state, "Shadow", shadowRows);
        project.Components.Add(label);
        return label;
    }

    private static ScreenSave AddScreen(GumProjectSave project, string componentName, params string[] instanceNames)
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        foreach (string instanceName in instanceNames)
        {
            screen.Instances.Add(new InstanceSave { Name = instanceName, BaseType = componentName, ParentContainer = screen });
        }
        project.Screens.Add(screen);
        return screen;
    }

    private static TextRuntime ShadowOf(GraphicalUiElement screenRuntime, string labelName)
    {
        GraphicalUiElement label = screenRuntime.GetGraphicalUiElementByName(labelName)!;
        return (TextRuntime)label.GetGraphicalUiElementByName("Shadow")!;
    }

    // ---- The report's three cases, built with plain TextRuntime labels ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScreenInstanceOverrideInTheFile_ShadowShowsTheOverride(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        AddVariable(screen.DefaultState, "Label1.Text", "string", "Testing");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!).Text.ShouldBe("Testing");
        ShadowOf(runtime, "Label1").Text.ShouldBe("Testing");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CodeSetAfterCreation_ShadowFollows(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!).Text = "From code";

        ShadowOf(runtime, "Label1").Text.ShouldBe("From code");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FileOverrideThenSameValueFromCode_ShadowShowsTheValue(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        AddVariable(screen.DefaultState, "Label1.Text", "string", "Dreams");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!).Text = "Dreams";

        ShadowOf(runtime, "Label1").Text.ShouldBe("Dreams");
    }

    [Fact]
    public void ComponentWithNoOverride_ShadowShowsTheComponentDefault()
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ShadowOf(runtime, "Label1").Text.ShouldBe(DefaultText);
    }
}
