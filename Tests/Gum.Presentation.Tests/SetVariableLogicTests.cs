using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests;
public class SetVariableLogicTests : BaseTestClass
{
    private readonly AutoMocker mocker;
    private readonly FakeDialogService _fakeDialogService;
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

        _fakeDialogService = new FakeDialogService();
        mocker.Use<IDialogService>(_fakeDialogService);

        _setVariableLogic = mocker.CreateInstance<SetVariableLogic>();
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

    [Fact]
    public void ReactToPropertyValueChanged_ShouldClearRenderTargetTextureSource_WhenNoneSelected()
    {
        // Picking "<NONE>" in the render-target dropdown must clear the variable back to default
        // rather than persisting the sentinel string (which would read as a set value), mirroring
        // how Parent and DefaultChildContainer normalize "<NONE>".
        ComponentSave container = new ComponentSave();
        container.States.Add(new StateSave());
        container.DefaultState.ParentContainer = container;

        InstanceSave instance = new InstanceSave();
        instance.Name = "SpriteInstance";
        instance.BaseType = "Sprite";

        container.DefaultState.SetValue("SpriteInstance.RenderTargetTextureSource", "<NONE>");
        VariableSave variable = container.DefaultState.GetVariableSave("SpriteInstance.RenderTargetTextureSource");

        Mock<ISelectedState> selectedState = mocker.GetMock<ISelectedState>();
        selectedState
            .Setup(x => x.SelectedStateSave)
            .Returns(container.DefaultState);
        selectedState
            .Setup(x => x.SelectedVariableSave)
            .Returns(variable);

        _setVariableLogic.ReactToPropertyValueChanged(
            "RenderTargetTextureSource",
            null,
            container,
            instance,
            container.DefaultState,
            refresh: false,
            recordUndo: false,
            trySave: false);

        variable.Value.ShouldBeNull();
    }

    [Fact]
    public void ReactToPropertyValueChanged_ShouldForceRefreshVariables_WhenAssigningStateOnInstance()
    {
        // Repro: BaseComponent has a category "Category1"; an instance of it is placed
        // on a screen. Assigning the instance's "Category1State" should force a full
        // grid rebuild because IsDefault / subtext on sibling variable rows
        // depend on which categorized state is active.
        ComponentSave baseComponent = new ComponentSave { Name = "BaseComponent" };
        StateSave baseDefault = new StateSave { Name = "Default", ParentContainer = baseComponent };
        baseComponent.States.Add(baseDefault);
        StateSaveCategory category = new StateSaveCategory { Name = "Category1" };
        category.States.Add(new StateSave { Name = "A", ParentContainer = baseComponent });
        category.States.Add(new StateSave { Name = "B", ParentContainer = baseComponent });
        baseComponent.Categories.Add(category);

        ScreenSave screen = new ScreenSave { Name = "MyScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        InstanceSave instance = new InstanceSave { Name = "BaseComponentInstance", BaseType = "BaseComponent" };
        screen.Instances.Add(instance);

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(baseComponent);
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;

        screenDefault.SetValue("BaseComponentInstance.Category1State", "A");

        mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedElement).Returns(screen);
        mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedStateSave).Returns(screenDefault);

        _setVariableLogic.ReactToPropertyValueChanged(
            "Category1State",
            null,
            screen,
            instance,
            screenDefault,
            refresh: true,
            recordUndo: false,
            trySave: false);

        mocker.GetMock<IGuiCommands>()
            .Verify(x => x.RefreshVariables(true), Times.AtLeastOnce);
    }

    [Fact]
    public void ReactToPropertyValueChanged_ShouldNotRebuildTreeView_WhenCommitIsIntermediate()
    {
        // Dragging the StrokeWidth label scrubs the value via intermediate commits. A structural
        // rebuild (tree view + grid) per tick destroys the control being dragged and breaks the
        // drag. The rebuild must wait for the full commit on release.
        ScreenSave screen = new ScreenSave { Name = "MyScreen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);

        _setVariableLogic.ReactToPropertyValueChanged(
            "StrokeWidth",
            2f,
            screen,
            null,
            state,
            refresh: true,
            recordUndo: false,
            trySave: false,
            isFullCommit: false);

        mocker.GetMock<IGuiCommands>()
            .Verify(x => x.RefreshElementTreeView(It.IsAny<ElementSave>()), Times.Never);
    }

    [Fact]
    public void ReactToPropertyValueChanged_ShouldRebuildTreeView_WhenCommitIsFull()
    {
        ScreenSave screen = new ScreenSave { Name = "MyScreen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);

        _setVariableLogic.ReactToPropertyValueChanged(
            "StrokeWidth",
            2f,
            screen,
            null,
            state,
            refresh: true,
            recordUndo: false,
            trySave: false,
            isFullCommit: true);

        mocker.GetMock<IGuiCommands>()
            .Verify(x => x.RefreshElementTreeView(It.IsAny<ElementSave>()), Times.AtLeastOnce);
    }

    [Fact]
    public void IsStateVariable_ShouldReturnTrue_WhenCategoryIsInheritedFromBaseType()
    {
        // DerivedComponent inherits from BaseComponent which defines Category1.
        // Assigning Category1State on an instance of DerivedComponent should still
        // be detected as a state variable.
        ComponentSave baseComponent = new ComponentSave { Name = "BaseComponent" };
        baseComponent.States.Add(new StateSave { Name = "Default", ParentContainer = baseComponent });
        StateSaveCategory category = new StateSaveCategory { Name = "Category1" };
        category.States.Add(new StateSave { Name = "A", ParentContainer = baseComponent });
        baseComponent.Categories.Add(category);

        ComponentSave derivedComponent = new ComponentSave { Name = "DerivedComponent", BaseType = "BaseComponent" };
        derivedComponent.States.Add(new StateSave { Name = "Default", ParentContainer = derivedComponent });

        ScreenSave screen = new ScreenSave { Name = "MyScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        InstanceSave instance = new InstanceSave { Name = "DerivedComponentInstance", BaseType = "DerivedComponent" };
        screen.Instances.Add(instance);

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(baseComponent);
        project.Components.Add(derivedComponent);
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;

        bool result = SetVariableLogic.IsStateVariable("Category1State", screen, instance);

        result.ShouldBeTrue();
    }

    [Fact]
    public void ReactToPropertyValueChanged_ShouldNotShowCopyDialog_WhenBatchFileCopyDecisionIsSetToFalse()
    {
        // A SourceFile path that resolves outside the project folder should normally
        // trigger the "copy or reference?" dialog. With SetBatchFileCopyDecision(false),
        // the dialog must be suppressed and the response must succeed (reference-in-place).
        ComponentSave container = new ComponentSave();
        container.States.Add(new StateSave());
        container.DefaultState.ParentContainer = container;

        InstanceSave instance = new InstanceSave();
        instance.Name = "SpriteInstance";
        instance.BaseType = "Sprite";

        // "../../ExternalFolder/image.png" resolves to c:/ExternalFolder/image.png,
        // which is outside c:/TestProject/.
        container.DefaultState.SetValue("SpriteInstance.SourceFile", "../../ExternalFolder/image.png");

        mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedStateSave)
            .Returns(container.DefaultState);

        _setVariableLogic.SetBatchFileCopyDecision(shouldCopy: false);

        GeneralResponse response = _setVariableLogic.ReactToPropertyValueChanged(
            "SourceFile", null, container, instance, container.DefaultState, refresh: false,
            recordUndo: false, trySave: false);

        _setVariableLogic.SetBatchFileCopyDecision(shouldCopy: null);

        response.Succeeded.ShouldBeTrue();
        _fakeDialogService.ShowChoiceCallCount.ShouldBe(0);
    }

    private class FakeDialogService : IDialogService
    {
        public int ShowChoiceCallCount { get; private set; }
        public Action<ChoiceDialogViewModel>? ChoiceDialogStub { get; set; }

        public MessageDialogResult ShowMessage(string message, string? title = null, MessageDialogStyle? style = null)
            => MessageDialogResult.Canceled;

        public bool Show<T>(T dialogViewModel) where T : DialogViewModel => false;

        public bool Show<T>(Action<T>? initializer, out T viewModel) where T : DialogViewModel
        {
            if (typeof(T) == typeof(ChoiceDialogViewModel))
            {
                ChoiceDialogViewModel choice = new ChoiceDialogViewModel();
                initializer?.Invoke((T)(object)choice);
                ChoiceDialogStub?.Invoke(choice);
                ShowChoiceCallCount++;
                viewModel = (T)(object)choice;
                return false;
            }

            viewModel = null!;
            return false;
        }

        public string? GetUserString(string message, string? title = null, GetUserStringOptions? options = null) => null;

        public List<string>? OpenFile(OpenFileDialogOptions? options = null) => null;

        public string? SaveFile(SaveFileDialogOptions? options = null) => null;
    }

    [Fact]
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
