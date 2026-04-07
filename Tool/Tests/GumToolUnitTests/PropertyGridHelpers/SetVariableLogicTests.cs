using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using ToolsUtilities;

namespace GumToolUnitTests.PropertyGridHelpers;
public class SetVariableLogicTests : BaseTestClass
{
    private readonly AutoMocker mocker;

    private readonly SetVariableLogic _setVariableLogic;
    public SetVariableLogicTests()
    {
        mocker = new();

        mocker.GetMock<IProjectState>()
            .Setup(x => x.GumProjectSave)
            .Returns(new GumProjectSave());
        mocker.GetMock<IProjectState>()
            .Setup(x => x.ProjectDirectory)
            .Returns("c:/TestProject/");

        _setVariableLogic = mocker.CreateInstance<SetVariableLogic>();
        StandardElementsManager.Self.Initialize();
    }

    [Fact]
    public void GetWhySourcefileIsInvalid_ShouldReturnLottieMessage_WhenExtensionIsLottie()
    {
        ComponentSave element = new ComponentSave();
        InstanceSave instance = new InstanceSave();

        string result = _setVariableLogic.GetWhySourcefileIsInvalid(
            "animation.lottie", element, instance, "SourceFile");

        result.ShouldBe("The .lottie format is not supported. Please use a .json Lottie file instead.");
    }

    [Fact]
    public void GetWhySourcefileIsInvalid_ShouldReturnNotSupported_WhenExtensionIsUnknown()
    {
        ComponentSave element = new ComponentSave();
        InstanceSave instance = new InstanceSave();

        string result = _setVariableLogic.GetWhySourcefileIsInvalid(
            "file.xyz", element, instance, "SourceFile");

        result.ShouldBe("The extension xyz is not supported");
    }

    [Fact]
    public void GetWhySourcefileIsInvalid_ShouldReturnNull_WhenExtensionIsJpg()
    {
        ComponentSave element = new ComponentSave();
        InstanceSave instance = new InstanceSave();

        string result = _setVariableLogic.GetWhySourcefileIsInvalid(
            "photo.jpg", element, instance, "SourceFile");

        result.ShouldBeNull();
    }

    [Fact]
    public void GetWhySourcefileIsInvalid_ShouldReturnNull_WhenExtensionIsPng()
    {
        ComponentSave element = new ComponentSave();
        InstanceSave instance = new InstanceSave();

        string result = _setVariableLogic.GetWhySourcefileIsInvalid(
            "image.png", element, instance, "SourceFile");

        result.ShouldBeNull();
    }

    [Fact]
    public void GetWhySourcefileIsInvalid_ShouldReturnNull_WhenPluginAcceptsExtension()
    {
        ComponentSave element = new ComponentSave();
        InstanceSave instance = new InstanceSave();

        mocker.GetMock<IPluginManager>()
            .Setup(x => x.GetIfExtensionIsValid("custom", element, instance, "SourceFile"))
            .Returns(true);

        string result = _setVariableLogic.GetWhySourcefileIsInvalid(
            "file.custom", element, instance, "SourceFile");

        result.ShouldBeNull();
    }

    [Fact]
    public void GetWhySourcefileIsInvalid_ShouldReturnNull_WhenValueIsUrl()
    {
        ComponentSave element = new ComponentSave();
        InstanceSave instance = new InstanceSave();

        string result = _setVariableLogic.GetWhySourcefileIsInvalid(
            "https://example.com/image.xyz", element, instance, "SourceFile");

        result.ShouldBeNull();
    }

    [Fact]
    public void ReactToPropertyValueChanged_ShouldRemoveVariable_WhenVariableDidNotExistBefore()
    {
        // Case 1: Variable did not exist at all before the property grid created it
        ComponentSave container = new ComponentSave();
        container.States.Add(new StateSave());
        container.DefaultState.ParentContainer = container;

        InstanceSave instance = new InstanceSave();
        instance.Name = "LottieInstance";
        instance.BaseType = "LottieAnimation";

        // Simulate what the property grid does: set the value on the state (creates new VariableSave)
        container.DefaultState.SetValue(
            "LottieInstance.SourceFile",
            "animation.lottie");

        Mock<ISelectedState> selectedState = mocker.GetMock<ISelectedState>();
        selectedState
            .Setup(x => x.SelectedStateSave)
            .Returns(container.DefaultState);

        GeneralResponse response = _setVariableLogic.ReactToPropertyValueChanged(
            "SourceFile",
            null,
            container,
            instance,
            container.DefaultState,
            refresh: false);

        response.Succeeded.ShouldBeFalse();

        // The variable should be removed entirely — it didn't exist before
        VariableSave variable = container.DefaultState.GetVariableSave("LottieInstance.SourceFile");
        variable.ShouldBeNull();
    }

    [Fact]
    public void ReactToPropertyValueChanged_ShouldRestoreSetsValueFalse_WhenVariableWasExposedBefore()
    {
        // Case 2: Variable existed with SetsValue=false (exposed variable)
        ComponentSave container = new ComponentSave();
        container.States.Add(new StateSave());
        container.DefaultState.ParentContainer = container;

        InstanceSave instance = new InstanceSave();
        instance.Name = "LottieInstance";
        instance.BaseType = "LottieAnimation";

        // Pre-create the variable with SetsValue=false and ExposedAsName (simulates an exposed variable)
        VariableSave existingVariable = new VariableSave();
        existingVariable.Name = "LottieInstance.SourceFile";
        existingVariable.Type = "string";
        existingVariable.SetsValue = false;
        existingVariable.IsFile = true;
        existingVariable.ExposedAsName = "LottieSourceFile";
        container.DefaultState.Variables.Add(existingVariable);

        // Simulate the property grid setting a new value (modifies existing VariableSave)
        container.DefaultState.SetValue(
            "LottieInstance.SourceFile",
            "animation.lottie");

        Mock<ISelectedState> selectedState = mocker.GetMock<ISelectedState>();
        selectedState
            .Setup(x => x.SelectedStateSave)
            .Returns(container.DefaultState);

        GeneralResponse response = _setVariableLogic.ReactToPropertyValueChanged(
            "SourceFile",
            null,
            container,
            instance,
            container.DefaultState,
            refresh: false);

        response.Succeeded.ShouldBeFalse();

        // The variable should remain but with SetsValue=false restored
        VariableSave variable = container.DefaultState.GetVariableSave("LottieInstance.SourceFile");
        variable.ShouldNotBeNull();
        variable.SetsValue.ShouldBeFalse();
    }

    [Fact]
    public void ReactToPropertyValueChanged_ShouldRestoreOldValue_WhenVariableHadPreviousValue()
    {
        // Case 3: Variable existed with SetsValue=true and a valid previous value
        ComponentSave container = new ComponentSave();
        container.States.Add(new StateSave());
        container.DefaultState.ParentContainer = container;

        InstanceSave instance = new InstanceSave();
        instance.Name = "LottieInstance";
        instance.BaseType = "LottieAnimation";

        // Set an initial valid value
        container.DefaultState.SetValue(
            "LottieInstance.SourceFile",
            "animation.json");

        // Simulate the property grid changing to an invalid value
        container.DefaultState.SetValue(
            "LottieInstance.SourceFile",
            "animation.lottie");

        Mock<ISelectedState> selectedState = mocker.GetMock<ISelectedState>();
        selectedState
            .Setup(x => x.SelectedStateSave)
            .Returns(container.DefaultState);

        GeneralResponse response = _setVariableLogic.ReactToPropertyValueChanged(
            "SourceFile",
            "animation.json",
            container,
            instance,
            container.DefaultState,
            refresh: false);

        response.Succeeded.ShouldBeFalse();

        // The variable should remain with the old valid value restored
        VariableSave variable = container.DefaultState.GetVariableSave("LottieInstance.SourceFile");
        variable.ShouldNotBeNull();
        variable.SetsValue.ShouldBeTrue();
        (variable.Value as string).ShouldBe("animation.json");
    }

    [Fact]
    public void ReactToPropertyValueChanged_ShouldNotRecordUndo_WhenSourceFileExtensionIsInvalid()
    {
        ComponentSave container = new ComponentSave();
        container.States.Add(new StateSave());
        container.DefaultState.ParentContainer = container;

        InstanceSave instance = new InstanceSave();
        instance.Name = "LottieInstance";
        instance.BaseType = "LottieAnimation";

        container.DefaultState.SetValue(
            "LottieInstance.SourceFile",
            "animation.lottie");

        Mock<ISelectedState> selectedState = mocker.GetMock<ISelectedState>();
        selectedState
            .Setup(x => x.SelectedStateSave)
            .Returns(container.DefaultState);

        GeneralResponse response = _setVariableLogic.ReactToPropertyValueChanged(
            "SourceFile",
            "",
            container,
            instance,
            container.DefaultState,
            refresh: false);

        response.Succeeded.ShouldBeFalse();
        mocker.GetMock<Gum.Undo.IUndoManager>()
            .Verify(x => x.RecordUndo(), Times.Never());
    }

    [Fact]
    public void ReactToPropertyValueChanged_ShouldRecordUndo_WhenSourceFileExtensionIsValid()
    {
        ComponentSave container = new ComponentSave();
        container.States.Add(new StateSave());
        container.DefaultState.ParentContainer = container;

        InstanceSave instance = new InstanceSave();
        instance.Name = "SpriteInstance";
        instance.BaseType = "Sprite";

        container.DefaultState.SetValue(
            "SpriteInstance.SourceFile",
            "image.png");

        Mock<ISelectedState> selectedState = mocker.GetMock<ISelectedState>();
        selectedState
            .Setup(x => x.SelectedStateSave)
            .Returns(container.DefaultState);

        GeneralResponse response = _setVariableLogic.ReactToPropertyValueChanged(
            "SourceFile",
            "",
            container,
            instance,
            container.DefaultState,
            refresh: false);

        response.Succeeded.ShouldBeTrue();
        mocker.GetMock<Gum.Undo.IUndoManager>()
            .Verify(x => x.RecordUndo(), Times.Once());
    }

    [Fact(Skip = "ProjectManager.Self needs to be removed")]
    public void ReactToPropertyValueChanged_ShouldAddTextureAddressValues_WhenSettingTextureAddresOnNineSlice()
    {
        ComponentSave container = new ComponentSave();
        container.States.Add(new Gum.DataTypes.Variables.StateSave());
        container.DefaultState.ParentContainer = container;
        container.DefaultState.SetValue(
            "NineSliceInstance.TextureAddress",
            TextureAddress.Custom);
        container.DefaultState.SetValue(
            "NineSliceInstance.SourceFile",
            "c:/MyFile.png");

        InstanceSave instance = new InstanceSave();
        instance.Name = "NineSliceInstance";

        Mock<ISelectedState> selectedState = mocker.GetMock<ISelectedState>();
        selectedState
            .Setup(x => x.SelectedInstance)
            .Returns(instance);

        _setVariableLogic.ReactToPropertyValueChanged(
            "TextureAddress",
            TextureAddress.EntireTexture,
            container,
            instance,
            container.DefaultState,
            refresh: true);
    }
}
