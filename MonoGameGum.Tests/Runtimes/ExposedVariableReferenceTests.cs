using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// An exposed variable (<c>IsMyButtonVisible</c>) and the instance variable it exposes
/// (<c>MyButton.Visible</c>) are one value, so a write through either name re-evaluates rows reading
/// either name (#8066).
/// </summary>
public class ExposedVariableReferenceTests : BaseTestClass
{
    public enum Writer
    {
        ExposedSetProperty,
        ExplicitSetProperty,
        TypedOnInstance,
        TypedOnComponent
    }

    public enum Reader
    {
        ExposedName,
        ExplicitName
    }

    public ExposedVariableReferenceTests()
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

    private static GraphicalUiElement BuildCard(Reader reader, bool useExpressions, string exposedVariable, string explicitVariable,
        string instanceType, string variableType, object initialValue, string otherVariable, string otherType)
    {
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }

        GumProjectSave project = new GumProjectSave();
        foreach (string standard in new[] { "Container", "Text" })
        {
            StandardElementSave standardElement = new StandardElementSave { Name = standard };
            standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor(standard)!);
            standardElement.DefaultState!.ParentContainer = standardElement;
            project.StandardElements.Add(standardElement);
        }
        ObjectFinder.Self.GumProjectSave = project;
        StandardRuntimeRegistrations.Register();

        ComponentSave card = new ComponentSave { Name = "Card", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = card };
        card.States.Add(state);
        card.Instances.Add(new InstanceSave { Name = "MyButton", BaseType = instanceType, ParentContainer = card });
        card.Instances.Add(new InstanceSave { Name = "Other", BaseType = instanceType, ParentContainer = card });
        state.Variables.Add(new VariableSave
        {
            Name = "MyButton." + explicitVariable,
            Type = variableType,
            Value = initialValue,
            SetsValue = true,
            ExposedAsName = exposedVariable
        });

        string read = reader == Reader.ExposedName ? exposedVariable : "MyButton." + explicitVariable;
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "Other.VariableReferences" };
        list.Value.Add(otherVariable + " = " + read);
        state.VariableLists.Add(list);
        project.Components.Add(card);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Card1", BaseType = "Card", ParentContainer = screen });
        project.Screens.Add(screen);

        return screen.ToGraphicalUiElement().GetGraphicalUiElementByName("Card1")!;
    }

    [Theory]
    [InlineData(Reader.ExposedName, Writer.ExposedSetProperty, false)]
    [InlineData(Reader.ExposedName, Writer.ExplicitSetProperty, false)]
    [InlineData(Reader.ExposedName, Writer.TypedOnInstance, false)]
    [InlineData(Reader.ExplicitName, Writer.ExposedSetProperty, false)]
    [InlineData(Reader.ExplicitName, Writer.ExplicitSetProperty, false)]
    [InlineData(Reader.ExplicitName, Writer.TypedOnInstance, false)]
    [InlineData(Reader.ExposedName, Writer.ExposedSetProperty, true)]
    [InlineData(Reader.ExposedName, Writer.ExplicitSetProperty, true)]
    [InlineData(Reader.ExposedName, Writer.TypedOnInstance, true)]
    [InlineData(Reader.ExplicitName, Writer.ExposedSetProperty, true)]
    [InlineData(Reader.ExplicitName, Writer.ExplicitSetProperty, true)]
    [InlineData(Reader.ExplicitName, Writer.TypedOnInstance, true)]
    public void Visible_WrittenThroughEitherName_RowsReadingEitherNameFollow(Reader reader, Writer writer, bool useExpressions)
    {
        GraphicalUiElement card = BuildCard(reader, useExpressions, "IsMyButtonVisible", "Visible", "Container", "bool", false,
            "Visible", "bool");
        GraphicalUiElement other = card.GetGraphicalUiElementByName("Other")!;
        other.Visible.ShouldBeFalse();

        switch (writer)
        {
            case Writer.ExposedSetProperty:
                card.SetProperty("IsMyButtonVisible", true);
                break;
            case Writer.ExplicitSetProperty:
                card.SetProperty("MyButton.Visible", true);
                break;
            case Writer.TypedOnInstance:
                card.GetGraphicalUiElementByName("MyButton")!.Visible = true;
                break;
        }
        other.Visible.ShouldBeTrue();

        switch (writer)
        {
            case Writer.ExposedSetProperty:
                card.SetProperty("IsMyButtonVisible", false);
                break;
            case Writer.ExplicitSetProperty:
                card.SetProperty("MyButton.Visible", false);
                break;
            case Writer.TypedOnInstance:
                card.GetGraphicalUiElementByName("MyButton")!.Visible = false;
                break;
        }
        other.Visible.ShouldBeFalse();
    }

    // Text goes through NotifyPropertyChanged on the instance rather than the typed-setter list.
    [Theory]
    [InlineData(Reader.ExposedName, Writer.ExposedSetProperty, false)]
    [InlineData(Reader.ExposedName, Writer.ExplicitSetProperty, false)]
    [InlineData(Reader.ExposedName, Writer.TypedOnInstance, false)]
    [InlineData(Reader.ExplicitName, Writer.ExposedSetProperty, false)]
    [InlineData(Reader.ExplicitName, Writer.ExplicitSetProperty, false)]
    [InlineData(Reader.ExplicitName, Writer.TypedOnInstance, false)]
    [InlineData(Reader.ExposedName, Writer.ExposedSetProperty, true)]
    [InlineData(Reader.ExposedName, Writer.ExplicitSetProperty, true)]
    [InlineData(Reader.ExposedName, Writer.TypedOnInstance, true)]
    [InlineData(Reader.ExplicitName, Writer.ExposedSetProperty, true)]
    [InlineData(Reader.ExplicitName, Writer.ExplicitSetProperty, true)]
    [InlineData(Reader.ExplicitName, Writer.TypedOnInstance, true)]
    public void Text_WrittenThroughEitherName_RowsReadingEitherNameFollow(Reader reader, Writer writer, bool useExpressions)
    {
        GraphicalUiElement card = BuildCard(reader, useExpressions, "Caption", "Text", "Text", "string", "start",
            "Text", "string");
        TextRuntime other = (TextRuntime)card.GetGraphicalUiElementByName("Other")!;
        other.Text.ShouldBe("start");

        switch (writer)
        {
            case Writer.ExposedSetProperty:
                card.SetProperty("Caption", "later");
                break;
            case Writer.ExplicitSetProperty:
                card.SetProperty("MyButton.Text", "later");
                break;
            case Writer.TypedOnInstance:
                ((TextRuntime)card.GetGraphicalUiElementByName("MyButton")!).Text = "later";
                break;
        }

        other.Text.ShouldBe("later");
    }

    [Fact]
    public void Width_WrittenThroughTheExposedName_RowReadingTheExplicitNameFollows()
    {
        GraphicalUiElement card = BuildCard(Reader.ExplicitName, useExpressions: false, "ButtonWidth", "Width", "Container", "float", 10f,
            "Width", "float");
        GraphicalUiElement other = card.GetGraphicalUiElementByName("Other")!;

        card.SetProperty("ButtonWidth", 75f);

        other.Width.ShouldBe(75f);
    }
}
