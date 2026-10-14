using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Localization;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Localization;

/// <summary>
/// A variable reference that reads <c>global::Localization.CurrentLanguage</c> follows a language
/// switch on a live visual, the way translated text does.
/// </summary>
public class CurrentLanguageReferenceTests : BaseTestClass
{
    private readonly LocalizationService _localizationService;

    public CurrentLanguageReferenceTests()
    {
        StandardElementsManager.Self.Initialize();
        GumExpressionService.Initialize();

        _localizationService = new LocalizationService();
        _localizationService.AddDatabase(
            new Dictionary<string, string[]> { { "Greeting", new[] { "Hello", "Hola", "Bonjour" } } },
            new List<string> { "English", "Spanish", "French" });
        _localizationService.CurrentLanguage = 0;
        CustomSetPropertyOnRenderable.LocalizationService = _localizationService;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Fact]
    public void CurrentLanguage_WhenChanged_ReevaluatesReferenceReadingIt()
    {
        ComponentSave element = CreateElement("X = global::Localization.CurrentLanguage == 2 ? 220 : 160");
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.AddToRoot();
        gue.X.ShouldBe(160f);

        _localizationService.CurrentLanguage = 2;

        gue.X.ShouldBe(220f);
    }

    [Fact]
    public void CurrentLanguage_WhenChangedBackAndForth_FollowsEachSwitch()
    {
        ComponentSave element = CreateElement("X = global::Localization.CurrentLanguage * 100");
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.AddToRoot();

        _localizationService.CurrentLanguage = 2;
        gue.X.ShouldBe(200f);

        _localizationService.CurrentLanguage = 1;
        gue.X.ShouldBe(100f);
    }

    [Fact]
    public void CurrentLanguage_WhenChanged_LeavesReferencesNotReadingItAlone()
    {
        ComponentSave element = CreateElement(
            "X = global::Localization.CurrentLanguage * 100",
            "Y = Other + 1");
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.AddToRoot();
        gue.Y = 500;

        _localizationService.CurrentLanguage = 1;

        // Y was set in code after load and its row does not read the language.
        gue.Y.ShouldBe(500f);
        gue.X.ShouldBe(100f);
    }

    [Fact]
    public void CurrentLanguage_WhenChanged_ReevaluatesReferenceOnAChildInstance()
    {
        ComponentSave element = CreateElement();
        InstanceSave child = new InstanceSave { Name = "Child", BaseType = "Container", ParentContainer = element };
        element.Instances.Add(child);
        element.DefaultState!.Variables.Add(new VariableSave { Name = "Child.X", Type = "float", Value = 0f, SetsValue = true });
        VariableListSave<string> childReferences = new VariableListSave<string> { Type = "string", Name = "Child.VariableReferences" };
        childReferences.Value.Add("X = global::Localization.CurrentLanguage == 1 ? 50 : 10");
        element.DefaultState!.VariableLists.Add(childReferences);
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.AddToRoot();
        GraphicalUiElement childGue = gue.GetGraphicalUiElementByName("Child")!;
        childGue.X.ShouldBe(10f);

        _localizationService.CurrentLanguage = 1;

        childGue.X.ShouldBe(50f);
    }

    private static ComponentSave CreateElement(params string[] rows)
    {
        ComponentSave element = new ComponentSave { Name = "Wave", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "Other", Type = "float", Value = 0f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 0f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Y", Type = "float", Value = 0f, SetsValue = true });

        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        list.Value.AddRange(rows);
        state.VariableLists.Add(list);

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(element);
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container")!);
        container.DefaultState!.ParentContainer = container;
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;
        return element;
    }
}
