using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// References are applied again when an element is created, so a row reading an element that
/// gumcli pack --shake trimmed is reported instead of being skipped silently (#6817).
/// </summary>
public class TrimmedReferenceSourceTests : BaseTestClass
{
    private readonly List<string> _errors = new();

    public TrimmedReferenceSourceTests()
    {
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Record;
    }

    private void Record(string message)
    {
        if (message.StartsWith("Variable reference"))
        {
            _errors.Add(message);
        }
    }

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError -= Record;
        base.Dispose();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_RowReadingAnElement_ReportsOnlyWhenPackTrimmedIt(bool trimmed)
    {
        ComponentSave panel = new ComponentSave { Name = "Panel", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = panel };
        state.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 77f, SetsValue = true });
        state.VariableLists.Add(new VariableListSave<string>
        {
            Name = "VariableReferences",
            Type = "string",
            Value = new List<string> { "Width = Components/Styles.CardWidth" },
        });
        panel.States.Add(state);
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(panel);
        if (trimmed)
        {
            project.TrimmedElements.Add("Styles");
        }
        ObjectFinder.Self.GumProjectSave = project;

        GraphicalUiElement visual = panel.ToGraphicalUiElement();

        visual.Width.ShouldBe(77f);
        if (trimmed)
        {
            _errors.ShouldHaveSingleItem().ShouldBe(
                "Variable reference 'Width = Components/Styles.CardWidth' was not applied in Panel (Container): " +
                "it reads Styles, which gumcli pack --shake trimmed; pack with --keep Styles. Width keeps its current value.");
        }
        else
        {
            _errors.ShouldBeEmpty();
        }
    }
}
