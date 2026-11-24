using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

/// <summary>
/// Create Component on Header in the Card component, the way the tool and gumcli run it: Header (Container) holds
/// Title (Text), which holds Badge (Sprite). Card exposes variables of the moved instances, and a Game screen
/// sets them through a CardInstance.
/// </summary>
public class ComponentExtractionExposedVariableTests : BaseTestClass
{
    private readonly ComponentSave _card;
    private readonly StateSave _cardDefault;
    private readonly InstanceSave _header;
    private readonly ScreenSave _game;
    private readonly StateSave _gameDefault;

    public ComponentExtractionExposedVariableTests()
    {
        _card = EditingTestProject.AddComponent(Project, "Card");
        _cardDefault = _card.GetDefaultStateOrThrow();
        _header = EditingTestProject.AddInstance(_card, "Header", "Container");
        EditingTestProject.AddInstance(_card, "Title", "Text");
        EditingTestProject.AddInstance(_card, "Badge", "Sprite");
        EditingTestProject.AddInstance(_card, "Footer", "Text");
        _cardDefault.SetValue("Title.Parent", "Header", "string");
        _cardDefault.SetValue("Badge.Parent", "Title", "string");
        _cardDefault.SetValue("Header.X", 10f, "float");
        _game = EditingTestProject.AddScreen(Project, "Game");
        _gameDefault = _game.GetDefaultStateOrThrow();
        EditingTestProject.AddInstance(_game, "CardInstance", "Card");
    }

    private void Expose(StateSave state, string name, string type, object value, string exposedAs) =>
        state.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true, ExposedAsName = exposedAs });

    private ComponentSave Extract(bool replace)
    {
        ComponentExtractionLogic logic = new ComponentExtractionLogic(new ElementCreationLogic());
        ComponentExtraction extraction = logic.CreateComponent(_header, "HeaderComponent");
        Project.Components.Add(extraction.Component);
        if (replace)
        {
            ReferenceFinder referenceFinder = EditingTestProject.CreateReferenceFinder();
            MovedInstanceReferenceDropLogic drop = new MovedInstanceReferenceDropLogic(referenceFinder);
            drop.Drop(drop.Find(_card, _header, extraction.MovedInstances, animations: null));
            logic.ReplaceWithComponentInstance(extraction, instance =>
                new InstanceRemovalLogic(referenceFinder, new VariableOverrideRemover()).RemoveInstance(instance, _card));
        }
        return extraction.Component;
    }

    [Fact]
    public void CreateComponent_WithReplace_KeepsAMovedChildsExposedVariableExposed_InBothElements()
    {
        Expose(_cardDefault, "Title.Text", "string", "Hi", "TitleText");
        _gameDefault.SetValue("CardInstance.TitleText", "Yo", "string");

        ComponentSave component = Extract(replace: true);

        VariableSave inComponent = component.GetDefaultStateOrThrow().GetVariableSave("Title.Text").ShouldNotBeNull();
        inComponent.ExposedAsName.ShouldBe("TitleText");
        inComponent.Value.ShouldBe("Hi");
        VariableSave inCard = _cardDefault.GetVariableSave("Header.TitleText").ShouldNotBeNull();
        inCard.ExposedAsName.ShouldBe("TitleText");
        _gameDefault.GetValue("CardInstance.TitleText").ShouldBe("Yo");
    }

    [Fact]
    public void CreateComponent_WithoutReplace_ExposesTheVariableInTheComponent_AndLeavesTheSourceExposure()
    {
        Expose(_cardDefault, "Title.Text", "string", "Hi", "TitleText");
        _gameDefault.SetValue("CardInstance.TitleText", "Yo", "string");

        ComponentSave component = Extract(replace: false);

        component.GetDefaultStateOrThrow().GetVariableSave("Title.Text").ShouldNotBeNull().ExposedAsName.ShouldBe("TitleText");
        _cardDefault.GetVariableSave("Title.Text").ShouldNotBeNull().ExposedAsName.ShouldBe("TitleText");
        _cardDefault.GetVariableSave("Header.TitleText").ShouldBeNull();
        _card.Instances.Select(item => item.Name).ShouldBe(new[] { "Header", "Title", "Badge", "Footer" });
        _gameDefault.GetValue("CardInstance.TitleText").ShouldBe("Yo");
    }

    [Fact]
    public void CreateComponent_WithReplace_KeepsAGrandchildsExposedVariableExposed()
    {
        Expose(_cardDefault, "Badge.Visible", "bool", false, "BadgeVisible");
        _gameDefault.SetValue("CardInstance.BadgeVisible", true, "bool");

        ComponentSave component = Extract(replace: true);

        component.GetDefaultStateOrThrow().GetVariableSave("Badge.Visible").ShouldNotBeNull().ExposedAsName.ShouldBe("BadgeVisible");
        VariableSave inCard = _cardDefault.GetVariableSave("Header.BadgeVisible").ShouldNotBeNull();
        inCard.ExposedAsName.ShouldBe("BadgeVisible");
        inCard.Value.ShouldBe(false);
        _gameDefault.GetValue("CardInstance.BadgeVisible").ShouldBe(true);
    }

    [Fact]
    public void CreateComponent_WithReplace_KeepsSeveralExposuresAtDifferentDepths()
    {
        Expose(_cardDefault, "Title.Text", "string", "Hi", "TitleText");
        Expose(_cardDefault, "Badge.Visible", "bool", false, "BadgeVisible");
        Expose(_cardDefault, "Footer.Text", "string", "Bye", "FooterText");

        ComponentSave component = Extract(replace: true);

        component.GetDefaultStateOrThrow().Variables.Where(item => !string.IsNullOrEmpty(item.ExposedAsName))
            .Select(item => item.ExposedAsName).ShouldBe(new[] { "TitleText", "BadgeVisible" }, ignoreOrder: true);
        _cardDefault.Variables.Where(item => !string.IsNullOrEmpty(item.ExposedAsName))
            .Select(item => $"{item.Name}:{item.ExposedAsName}")
            .ShouldBe(new[] { "Footer.Text:FooterText", "Header.TitleText:TitleText", "Header.BadgeVisible:BadgeVisible" },
                ignoreOrder: true);
    }

    [Fact]
    public void CreateComponent_WithReplace_KeepsThePromotedInstancesOwnExposure()
    {
        Expose(_cardDefault, "Header.Width", "float", 200f, "HeaderWidth");
        _gameDefault.SetValue("CardInstance.HeaderWidth", 300f, "float");

        ComponentSave component = Extract(replace: true);

        VariableSave root = component.GetDefaultStateOrThrow().GetVariableSave("Width").ShouldNotBeNull();
        root.Value.ShouldBe(200f);
        root.ExposedAsName.ShouldBeNull();
        VariableSave inCard = _cardDefault.GetVariableSave("Header.Width").ShouldNotBeNull();
        inCard.ExposedAsName.ShouldBe("HeaderWidth");
        _gameDefault.GetValue("CardInstance.HeaderWidth").ShouldBe(300f);
    }

    [Fact]
    public void CreateComponent_WithReplace_KeepsAPromotedPositionExposure()
    {
        Expose(_cardDefault, "Header.Y", "float", 5f, "HeaderY");
        _gameDefault.SetValue("CardInstance.HeaderY", 7f, "float");

        Extract(replace: true);

        VariableSave inCard = _cardDefault.GetVariableSave("Header.Y").ShouldNotBeNull();
        inCard.ExposedAsName.ShouldBe("HeaderY");
        inCard.Value.ShouldBe(5f);
        _cardDefault.GetValue("Header.X").ShouldBe(10f);
        _gameDefault.GetValue("CardInstance.HeaderY").ShouldBe(7f);
    }

    [Fact]
    public void CreateComponent_WithReplace_MovesAnotherStatesValueOfAnExposedVariableToTheExposedName()
    {
        Expose(_cardDefault, "Title.Text", "string", "Hi", "TitleText");
        StateSave big = EditingTestProject.AddCategory(_card, "Size", "Big").States[0];
        big.SetValue("Title.Text", "Big", "string");
        big.SetValue("Title.FontSize", 40, "int");

        Extract(replace: true);

        big.GetValue("Header.TitleText").ShouldBe("Big");
        big.GetVariableSave("Header.TitleText").ShouldNotBeNull().ExposedAsName.ShouldBeNull();
        big.GetVariableSave("Title.Text").ShouldBeNull();
        // Not exposed, so nothing outside the component can set it; the move drops it as before.
        big.GetVariableSave("Title.FontSize").ShouldBeNull();
    }

    [Fact]
    public void CreateComponent_WithReplace_KeepsWhatOtherElementsStatesSetThroughTheExposedName()
    {
        Expose(_cardDefault, "Title.Text", "string", "Hi", "TitleText");
        StateSave gameBig = EditingTestProject.AddCategory(_game, "Mode", "Big").States[0];
        gameBig.SetValue("CardInstance.TitleText", "Huge", "string");

        Extract(replace: true);

        gameBig.GetValue("CardInstance.TitleText").ShouldBe("Huge");
    }

    [Fact]
    public void CreateComponent_WithReplace_KeepsReferencesInOtherElementsThatUseTheExposedName()
    {
        Expose(_cardDefault, "Title.Text", "string", "Hi", "TitleText");
        EditingTestProject.AddInstance(_game, "Label", "Text");
        EditingTestProject.AddInstance(_game, "Source", "Text");
        VariableListSave<string> reading = new VariableListSave<string> { Name = "Label.VariableReferences", Type = "string" };
        reading.ValueAsIList.Add("Text = CardInstance.TitleText");
        VariableListSave<string> writing = new VariableListSave<string> { Name = "CardInstance.VariableReferences", Type = "string" };
        writing.ValueAsIList.Add("TitleText = Source.Text");
        _gameDefault.VariableLists.Add(reading);
        _gameDefault.VariableLists.Add(writing);

        Extract(replace: true);

        _gameDefault.VariableLists.Select(item => $"{item.Name}:{item.ValueAsIList[0]}").ShouldBe(new[]
        {
            "Label.VariableReferences:Text = CardInstance.TitleText",
            "CardInstance.VariableReferences:TitleText = Source.Text",
        });
    }

    [Fact]
    public void CreateComponent_WithReplace_FromAScreen_KeepsTheExposureInTheComponent()
    {
        ScreenSave screen = EditingTestProject.AddScreen(Project, "Menu");
        StateSave screenDefault = screen.GetDefaultStateOrThrow();
        InstanceSave panel = EditingTestProject.AddInstance(screen, "Panel", "Container");
        EditingTestProject.AddInstance(screen, "Caption", "Text");
        screenDefault.SetValue("Caption.Parent", "Panel", "string");
        Expose(screenDefault, "Caption.Text", "string", "Hi", "CaptionText");
        ComponentExtractionLogic logic = new ComponentExtractionLogic(new ElementCreationLogic());
        ComponentExtraction extraction = logic.CreateComponent(panel, "PanelComponent");
        Project.Components.Add(extraction.Component);
        ReferenceFinder referenceFinder = EditingTestProject.CreateReferenceFinder();

        logic.ReplaceWithComponentInstance(extraction, instance =>
            new InstanceRemovalLogic(referenceFinder, new VariableOverrideRemover()).RemoveInstance(instance, screen));

        extraction.Component.GetDefaultStateOrThrow().GetVariableSave("Caption.Text").ShouldNotBeNull()
            .ExposedAsName.ShouldBe("CaptionText");
        screenDefault.GetVariableSave("Panel.CaptionText").ShouldNotBeNull().ExposedAsName.ShouldBe("CaptionText");
    }

    [Fact]
    public void GetWhyCannotCreateComponent_WhenTheInstancesTypeAlreadyExposesTheName_NamesTheNameAndTheVariable()
    {
        ComponentSave panel = EditingTestProject.AddComponent(Project, "Panel");
        EditingTestProject.AddInstance(panel, "Inner", "Text");
        Expose(panel.GetDefaultStateOrThrow(), "Inner.Text", "string", "In", "TitleText");
        _header.BaseType = "Panel";
        Expose(_cardDefault, "Title.Text", "string", "Hi", "TitleText");
        ElementNameValidator validator = new ElementNameValidator(topLevelNameSource: null);
        ComponentExtractionLogic logic = new ComponentExtractionLogic(new ElementCreationLogic());

        string? whyNot = logic.GetWhyCannotCreateComponent(_header, (name, element) =>
            validator.IsVariableNameValid(name, element, variable: null, out string? why) ? null : why);

        whyNot.ShouldNotBeNull();
        whyNot.ShouldContain("Title.Text");
        whyNot.ShouldContain("TitleText");
    }

    [Fact]
    public void GetWhyCannotCreateComponent_WhenTheExposedNamesAreFree_ReturnsNull()
    {
        Expose(_cardDefault, "Title.Text", "string", "Hi", "TitleText");
        Expose(_cardDefault, "Header.Width", "float", 200f, "HeaderWidth");
        Expose(_cardDefault, "Footer.Text", "string", "Bye", "Width");
        ElementNameValidator validator = new ElementNameValidator(topLevelNameSource: null);
        ComponentExtractionLogic logic = new ComponentExtractionLogic(new ElementCreationLogic());

        string? whyNot = logic.GetWhyCannotCreateComponent(_header, (name, element) =>
            validator.IsVariableNameValid(name, element, variable: null, out string? why) ? null : why);

        // Header.Width stays exposed on the replacement and Footer is outside the subtree.
        whyNot.ShouldBeNull();
    }
}
