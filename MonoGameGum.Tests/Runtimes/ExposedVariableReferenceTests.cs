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
        TypedOnInstance
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

    public enum NestedWriter
    {
        OuterExposedName,
        OuterDottedInnerExposedName,
        InnerExposedName,
        InnerDottedInstanceName,
        TypedOnInstance
    }

    // Outer exposes Card1.IsMyButtonVisible as Deep, and Card exposes MyButton.Visible as IsMyButtonVisible.
    private static GraphicalUiElement BuildOuter(bool readDeepName, bool useExpressions)
    {
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }

        GumProjectSave project = new GumProjectSave();
        StandardElementSave standardElement = new StandardElementSave { Name = "Container" };
        standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container")!);
        standardElement.DefaultState!.ParentContainer = standardElement;
        project.StandardElements.Add(standardElement);
        ObjectFinder.Self.GumProjectSave = project;
        StandardRuntimeRegistrations.Register();

        ComponentSave card = new ComponentSave { Name = "Card", BaseType = "Container" };
        StateSave cardState = new StateSave { Name = "Default", ParentContainer = card };
        card.States.Add(cardState);
        card.Instances.Add(new InstanceSave { Name = "MyButton", BaseType = "Container", ParentContainer = card });
        cardState.Variables.Add(new VariableSave
        {
            Name = "MyButton.Visible", Type = "bool", Value = false, SetsValue = true, ExposedAsName = "IsMyButtonVisible"
        });
        project.Components.Add(card);

        ComponentSave outer = new ComponentSave { Name = "Outer", BaseType = "Container" };
        StateSave outerState = new StateSave { Name = "Default", ParentContainer = outer };
        outer.States.Add(outerState);
        outer.Instances.Add(new InstanceSave { Name = "Card1", BaseType = "Card", ParentContainer = outer });
        outer.Instances.Add(new InstanceSave { Name = "Other", BaseType = "Container", ParentContainer = outer });
        outerState.Variables.Add(new VariableSave
        {
            Name = "Card1.IsMyButtonVisible", Type = "bool", Value = false, SetsValue = true, ExposedAsName = "Deep"
        });
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "Other.VariableReferences" };
        list.Value.Add("Visible = " + (readDeepName ? "Deep" : "Card1.IsMyButtonVisible"));
        outerState.VariableLists.Add(list);
        project.Components.Add(outer);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Outer1", BaseType = "Outer", ParentContainer = screen });
        project.Screens.Add(screen);

        return screen.ToGraphicalUiElement().GetGraphicalUiElementByName("Outer1")!;
    }

    [Theory]
    [InlineData(true, NestedWriter.OuterExposedName, false)]
    [InlineData(true, NestedWriter.OuterDottedInnerExposedName, false)]
    [InlineData(true, NestedWriter.InnerExposedName, false)]
    [InlineData(true, NestedWriter.InnerDottedInstanceName, false)]
    [InlineData(true, NestedWriter.TypedOnInstance, false)]
    [InlineData(false, NestedWriter.OuterExposedName, false)]
    [InlineData(false, NestedWriter.OuterDottedInnerExposedName, false)]
    [InlineData(false, NestedWriter.InnerExposedName, false)]
    [InlineData(false, NestedWriter.InnerDottedInstanceName, false)]
    [InlineData(false, NestedWriter.TypedOnInstance, false)]
    [InlineData(true, NestedWriter.OuterExposedName, true)]
    [InlineData(true, NestedWriter.OuterDottedInnerExposedName, true)]
    [InlineData(true, NestedWriter.InnerExposedName, true)]
    [InlineData(true, NestedWriter.InnerDottedInstanceName, true)]
    [InlineData(true, NestedWriter.TypedOnInstance, true)]
    [InlineData(false, NestedWriter.OuterExposedName, true)]
    [InlineData(false, NestedWriter.OuterDottedInnerExposedName, true)]
    [InlineData(false, NestedWriter.InnerExposedName, true)]
    [InlineData(false, NestedWriter.InnerDottedInstanceName, true)]
    [InlineData(false, NestedWriter.TypedOnInstance, true)]
    public void Visible_ExposedOfExposed_RowsReadingEitherNameFollowAnyWriter(bool readDeepName, NestedWriter writer, bool useExpressions)
    {
        GraphicalUiElement outer = BuildOuter(readDeepName, useExpressions);
        GraphicalUiElement card = outer.GetGraphicalUiElementByName("Card1")!;
        GraphicalUiElement other = outer.GetGraphicalUiElementByName("Other")!;
        other.Visible.ShouldBeFalse();

        foreach (bool value in new[] { true, false })
        {
            switch (writer)
            {
                case NestedWriter.OuterExposedName:
                    outer.SetProperty("Deep", value);
                    break;
                case NestedWriter.OuterDottedInnerExposedName:
                    outer.SetProperty("Card1.IsMyButtonVisible", value);
                    break;
                case NestedWriter.InnerExposedName:
                    card.SetProperty("IsMyButtonVisible", value);
                    break;
                case NestedWriter.InnerDottedInstanceName:
                    card.SetProperty("MyButton.Visible", value);
                    break;
                case NestedWriter.TypedOnInstance:
                    card.GetGraphicalUiElementByName("MyButton")!.Visible = value;
                    break;
            }
            other.Visible.ShouldBe(value);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Visible_WrittenAwayAndBackInsideOneApplyState_RowReadingTheExposedNameEndsWithTheLastValue(bool useExpressions)
    {
        GraphicalUiElement card = BuildCard(Reader.ExposedName, useExpressions, "IsMyButtonVisible", "Visible", "Container", "bool", false,
            "Visible", "bool");
        GraphicalUiElement other = card.GetGraphicalUiElementByName("Other")!;
        GraphicalUiElement button = card.GetGraphicalUiElementByName("MyButton")!;
        button.Visible = true;
        other.Visible.ShouldBeTrue();

        card.ApplyState(new StateSave
        {
            Name = "Batch",
            Apply = () =>
            {
                button.Visible = false;
                button.Visible = true;
            }
        });

        other.Visible.ShouldBeTrue();
    }
}
