using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// An instance whose reference copies another instance's property (#6385) has the right value right
/// after load and follows the source when it changes, with and without Gum.Expressions.
/// </summary>
public class InstanceReferenceInitialAndLiveTests : BaseTestClass
{
    public InstanceReferenceInitialAndLiveTests()
    {
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
    }

    public override void Dispose()
    {
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Load_TextCopiedFromAnotherInstance_HasTheAuthoredText(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement card = Create("Text = Source.Text");

        TextRuntime shadow = (TextRuntime)card.GetGraphicalUiElementByName("Shadow")!;
        shadow.Text.ShouldBe("Hello");
    }

    [Fact]
    public void Load_TextWithExpression_HasTheAuthoredText()
    {
        UseExpressions(true);
        GraphicalUiElement card = Create("Text = Source.Text + \"!\"");

        TextRuntime shadow = (TextRuntime)card.GetGraphicalUiElementByName("Shadow")!;
        shadow.Text.ShouldBe("Hello!");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceTextChangedThroughTheTypedProperty_UpdatesTheCopy(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement card = Create("Text = Source.Text");
        TextRuntime source = (TextRuntime)card.GetGraphicalUiElementByName("Source")!;
        TextRuntime shadow = (TextRuntime)card.GetGraphicalUiElementByName("Shadow")!;

        source.Text = "Changed";

        shadow.Text.ShouldBe("Changed");
    }

    [Fact]
    public void SourceTextChangedThroughTheTypedProperty_ExpressionCopy_UpdatesTheCopy()
    {
        UseExpressions(true);
        GraphicalUiElement card = Create("Text = Source.Text + \"!\"");
        TextRuntime source = (TextRuntime)card.GetGraphicalUiElementByName("Source")!;
        TextRuntime shadow = (TextRuntime)card.GetGraphicalUiElementByName("Shadow")!;

        source.Text = "Changed";

        shadow.Text.ShouldBe("Changed!");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceTextChangedThroughSetProperty_UpdatesTheCopy(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement card = Create("Text = Source.Text");
        GraphicalUiElement source = card.GetGraphicalUiElementByName("Source")!;
        TextRuntime shadow = (TextRuntime)card.GetGraphicalUiElementByName("Shadow")!;

        source.SetProperty("Text", "Changed");

        shadow.Text.ShouldBe("Changed");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceTextSetToItsCurrentValue_LeavesTheCopyCorrect(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement card = Create("Text = Source.Text");
        TextRuntime source = (TextRuntime)card.GetGraphicalUiElementByName("Source")!;
        TextRuntime shadow = (TextRuntime)card.GetGraphicalUiElementByName("Shadow")!;

        source.Text = "Hello";

        shadow.Text.ShouldBe("Hello");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NotifyVariableChanged_OnTheSourceText_UpdatesTheCopy(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement card = Create("Text = Source.Text");
        TextRuntime shadow = (TextRuntime)card.GetGraphicalUiElementByName("Shadow")!;

        card.NotifyVariableChanged("Source.Text", "Notified");

        shadow.Text.ShouldBe("Notified");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RefreshReferences_AfterTheSourceTextChanged_UpdatesTheCopy(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement card = Create("Text = Source.Text");
        TextRuntime source = (TextRuntime)card.GetGraphicalUiElementByName("Source")!;
        TextRuntime shadow = (TextRuntime)card.GetGraphicalUiElementByName("Shadow")!;
        source.Text = "Changed";

        card.RefreshReferences("Source.Text");

        shadow.Text.ShouldBe("Changed");
    }

    // The other typed properties of a Text instance. Each is copied from Source to Shadow by a reference.
    [Theory]
    [InlineData("Red", false)]
    [InlineData("Red", true)]
    [InlineData("Alpha", false)]
    [InlineData("Alpha", true)]
    [InlineData("Rotation", false)]
    [InlineData("Rotation", true)]
    [InlineData("X", false)]
    [InlineData("X", true)]
    [InlineData("Width", false)]
    [InlineData("Width", true)]
    public void SourcePropertyChangedThroughTheTypedProperty_UpdatesTheCopy(string property, bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement card = Create($"{property} = Source.{property}");
        TextRuntime source = (TextRuntime)card.GetGraphicalUiElementByName("Source")!;
        TextRuntime shadow = (TextRuntime)card.GetGraphicalUiElementByName("Shadow")!;

        switch (property)
        {
            case "Red": source.Red = 99; break;
            case "Alpha": source.Alpha = 77; break;
            case "Rotation": source.Rotation = 12; break;
            case "X": source.X = 45; break;
            case "Width": source.Width = 150; break;
        }

        string copied = property switch
        {
            "Red" => shadow.Red.ToString(),
            "Alpha" => shadow.Alpha.ToString(),
            "Rotation" => shadow.Rotation.ToString(),
            "X" => shadow.X.ToString(),
            _ => shadow.Width.ToString()
        };
        string expected = property switch
        {
            "Red" => "99",
            "Alpha" => "77",
            "Rotation" => "12",
            "X" => "45",
            _ => "150"
        };
        copied.ShouldBe(expected);
    }

    private static GraphicalUiElement Create(string shadowRow)
    {
        ComponentSave card = new ComponentSave { Name = "Card", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = card };
        card.States.Add(state);
        foreach (string name in new[] { "Source", "Shadow" })
        {
            card.Instances.Add(new InstanceSave { Name = name, BaseType = "Text", ParentContainer = card });
        }
        state.Variables.Add(new VariableSave { Name = "Source.Text", Type = "string", Value = "Hello", SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Source.Red", Type = "int", Value = 10, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Source.FontSize", Type = "int", Value = 20, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Source.Alpha", Type = "int", Value = 255, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Source.Rotation", Type = "float", Value = 3f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Source.X", Type = "float", Value = 5f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Source.Width", Type = "float", Value = 100f, SetsValue = true });

        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "Shadow.VariableReferences" };
        list.Value.Add(shadowRow);
        state.VariableLists.Add(list);

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(card);
        foreach (string standard in new[] { "Container", "Text" })
        {
            StandardElementSave standardElement = new StandardElementSave { Name = standard };
            standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor(standard)!);
            standardElement.DefaultState!.ParentContainer = standardElement;
            project.StandardElements.Add(standardElement);
        }
        ObjectFinder.Self.GumProjectSave = project;
        ElementSaveExtensions.RegisterGueInstantiation("Text", () => new TextRuntime());
        return card.ToGraphicalUiElement();
    }
}
