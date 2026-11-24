using Gum.Commands;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Logic;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.ProjectServices.Editing;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for CreateComponentDialogViewModel, relocated out of
/// Gum.csproj into the headless Gum.Presentation assembly (ADR-0005, #3754) as a clean leaf VM
/// whose three injected interfaces (INameVerifier, ICopyPasteLogic, IGuiCommands) are all already
/// headless.
/// </summary>
public class CreateComponentDialogViewModelTests : BaseTestClass
{
    private readonly CreateComponentDialogViewModel _sut;
    private readonly Mock<INameVerifier> _nameVerifier;
    private readonly Mock<ICopyPasteLogic> _copyPasteLogic;
    private readonly Mock<IGuiCommands> _guiCommands;

    public CreateComponentDialogViewModelTests()
    {
        _nameVerifier = new Mock<INameVerifier>();
        _copyPasteLogic = new Mock<ICopyPasteLogic>();
        _guiCommands = new Mock<IGuiCommands>();

        // Make the name validation pass by default so OnAffirmative is not blocked.
        ObjectFinder.Self.GumProjectSave = new GumProjectSave { FullFileName = "C:/project/Test.gumx" };
        string? whyNotValid = null;
        _nameVerifier
            .Setup(x => x.IsElementNameValid(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<ElementSave?>(), out whyNotValid))
            .Returns(true);

        _sut = new CreateComponentDialogViewModel(_nameVerifier.Object, _copyPasteLogic.Object, _guiCommands.Object,
            new ComponentExtractionLogic(new ElementCreationLogic()));
    }

    [Fact]
    public void OnAffirmative_CallsCreateComponentFromInstance_WithReplaceFalse_WhenCheckboxUnchecked()
    {
        ComponentSave element = new ComponentSave();
        InstanceSave instance = new InstanceSave { Name = "MyButton", ParentContainer = element };
        _sut.Instance = instance;
        _sut.IsCheckboxChecked = false;

        _sut.OnAffirmative();

        _copyPasteLogic.Verify(x => x.CreateComponentFromInstance(instance, "MyButtonComponent", false), Times.Once);
    }

    [Fact]
    public void OnAffirmative_CallsCreateComponentFromInstance_WithReplaceTrue_WhenCheckboxChecked()
    {
        ComponentSave element = new ComponentSave();
        InstanceSave instance = new InstanceSave { Name = "MyButton", ParentContainer = element };
        _sut.Instance = instance;
        _sut.IsCheckboxChecked = true;

        _sut.OnAffirmative();

        _copyPasteLogic.Verify(x => x.CreateComponentFromInstance(instance, "MyButtonComponent", true), Times.Once);
    }

    [Fact]
    public void OnAffirmative_DoesNothing_WhenInstanceIsNull()
    {
        _sut.OnAffirmative();

        _copyPasteLogic.Verify(
            x => x.CreateComponentFromInstance(It.IsAny<InstanceSave>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public void SettingInstance_PopulatesCheckboxTextWithInstanceName()
    {
        ComponentSave element = new ComponentSave();
        InstanceSave instance = new InstanceSave { Name = "MyButton", ParentContainer = element };

        _sut.Instance = instance;

        _sut.CheckboxText.ShouldContain("MyButton");
    }

    [Fact]
    public void SettingInstance_SuggestsDefaultComponentName()
    {
        ComponentSave element = new ComponentSave();
        InstanceSave instance = new InstanceSave { Name = "MyButton", ParentContainer = element };

        _sut.Instance = instance;

        _sut.Value.ShouldBe("MyButtonComponent");
    }

    [Fact]
    public void Validate_RefusesAnInstanceWhoseTypeAlreadyExposesAnExposedName()
    {
        GumProjectSave project = new GumProjectSave { FullFileName = "C:/project/Test.gumx" };
        ObjectFinder.Self.GumProjectSave = project;
        ComponentSave panel = new ComponentSave { Name = "Panel", BaseType = "Container" };
        StateSave panelDefault = new StateSave { Name = "Default", ParentContainer = panel };
        panel.States.Add(panelDefault);
        panel.Instances.Add(new InstanceSave { Name = "Inner", BaseType = "Text", ParentContainer = panel });
        panelDefault.Variables.Add(new VariableSave { Name = "Inner.Text", Type = "string", Value = "In", SetsValue = true, ExposedAsName = "TitleText" });
        ComponentSave card = new ComponentSave { Name = "Card", BaseType = "Container" };
        StateSave cardDefault = new StateSave { Name = "Default", ParentContainer = card };
        card.States.Add(cardDefault);
        InstanceSave header = new InstanceSave { Name = "Header", BaseType = "Panel", ParentContainer = card };
        card.Instances.Add(header);
        card.Instances.Add(new InstanceSave { Name = "Title", BaseType = "Text", ParentContainer = card });
        cardDefault.SetValue("Title.Parent", "Header", "string");
        cardDefault.Variables.Add(new VariableSave { Name = "Title.Text", Type = "string", Value = "Hi", SetsValue = true, ExposedAsName = "TitleText" });
        project.Components.Add(panel);
        project.Components.Add(card);
        CreateComponentDialogViewModel sut = new CreateComponentDialogViewModel(new NameVerifier(new Mock<IPluginManager>().Object),
            _copyPasteLogic.Object, _guiCommands.Object, new ComponentExtractionLogic(new ElementCreationLogic()));

        sut.Instance = header;
        sut.OnAffirmative();

        sut.Error.ShouldNotBeNull();
        sut.Error.ShouldContain("Title.Text");
        sut.Error.ShouldContain("TitleText");
        _copyPasteLogic.Verify(x => x.CreateComponentFromInstance(It.IsAny<InstanceSave>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }
}
