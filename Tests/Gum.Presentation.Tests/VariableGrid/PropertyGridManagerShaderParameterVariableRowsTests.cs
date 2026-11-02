using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Logic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Reflection;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Moq.AutoMock;
using RenderingLibrary.Graphics;
using Shouldly;
using ToolsUtilities;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.VariableGrid;

/// <summary>
/// Scalar shader parameter rows are real saved variables (#7010 slice 4): editing writes
/// <c>Holder.Outline_Thickness</c> with the shader marker, an untouched row saves nothing, and a value
/// still in the legacy <c>ShaderParameters</c> text counts until the row is edited.
/// </summary>
public class PropertyGridManagerShaderParameterVariableRowsTests : BaseTestClass, IDisposable
{
    private const string VariableName = "Holder.Outline_Thickness";

    private readonly AutoMocker _mocker = new();
    private readonly ComponentSave _component;
    private readonly InstanceSave _holder;

    public PropertyGridManagerShaderParameterVariableRowsTests()
    {
        _component = new ComponentSave { Name = "ShaderComponent", BaseType = "Container" };
        StateSave defaultState = new StateSave { ParentContainer = _component };
        _component.States.Add(defaultState);
        _holder = new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = _component };
        _component.Instances.Add(_holder);
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(_component);
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(new StateSave { Name = "Default", ParentContainer = container });
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;

        _component.DefaultState.SetValue("Holder.SourceShaderFile", "Shaders/Outline.slang");

        InvisibleRenderable renderable = new InvisibleRenderable { RenderTargetEffect = new object() };
        _mocker.GetMock<IWireframeObjectManager>()
            .Setup(manager => manager.GetRepresentation(_holder, It.IsAny<List<ElementWithState>?>()))
            .Returns(new GraphicalUiElement(renderable));
        _mocker.GetMock<ISetVariableLogic>().SetReturnsDefault(GeneralResponse.SuccessfulResponse);
        _mocker.GetMock<ISelectedState>().Setup(state => state.SelectedStateSave).Returns(_component.DefaultState);
        _mocker.GetMock<ISelectedState>().Setup(state => state.SelectedElement).Returns(_component);
        _mocker.GetMock<ISelectedState>().Setup(state => state.SelectedInstance).Returns(_holder);
    }

    public new void Dispose() => ShaderParameterReader.Reader = null;

    private static void GivenTheEffectDeclares(params ShaderParameterInfo[] parameters) =>
        ShaderParameterReader.Reader = _ => parameters.ToList();

    private static ShaderParameterInfo Float(string name, float defaultValue) =>
        new ShaderParameterInfo(name, ShaderParameterKind.Float, 1, ShaderParameterValue.FromFloat(defaultValue));

    private StateReferencingInstanceMember CreateTextMember() =>
        new StateReferencingInstanceMember(
            Array.Empty<Attribute>(), null, typeof(string),
            isReadOnly: false, isAssignedByReference: false, isVariable: true,
            _component.DefaultState, null, "Holder.ShaderParameters", _holder, _component,
            _mocker.Get<ISelectedState>(),
            _mocker.Get<IUndoManager>(),
            _mocker.Get<IGuiCommands>(),
            _mocker.Get<IFileCommands>(),
            _mocker.Get<ISetVariableLogic>(),
            _mocker.Get<IWireframeObjectManager>(),
            _mocker.Get<IPluginManager>(),
            _mocker.Get<IHotkeyManager>(),
            _mocker.Get<IDeleteVariableService>(),
            _mocker.Get<IExposeVariableService>(),
            _mocker.Get<IEditVariableService>(),
            _mocker.Get<ITypeManager>(),
            _mocker.Get<IClipboardService>());

    private MemberCategory BuildRows()
    {
        MemberCategory category = new MemberCategory("Rendering");
        category.Members.Add(CreateTextMember());
        PropertyGridManager sut = _mocker.CreateInstance<PropertyGridManager>();
        sut.AdjustShaderParameters(new List<MemberCategory> { category }, _holder);
        return category;
    }

    private static InstanceMember Row(MemberCategory category, string name) =>
        category.Members.Single(member => member.Name == name);

    [Fact]
    public void BuildingTheRows_SavesNothing()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));

        MemberCategory category = BuildRows();

        _component.DefaultState.Variables.Any(variable => variable.Name == VariableName).ShouldBeFalse();
        Row(category, "Thickness").Value.ShouldBe(2f);
        Row(category, "Thickness").IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void EditingARow_SavesAMarkedVariableNamedAfterTheShaderFile_AndNotTheLegacyText()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        MemberCategory category = BuildRows();

        Row(category, "Thickness").Value = 3f;

        VariableSave saved = _component.DefaultState.GetVariableSave(VariableName).ShouldNotBeNull("the edit should have saved the variable");
        saved.Value.ShouldBe(3f);
        saved.Type.ShouldBe("float");
        saved.ShaderParameter.ShouldBe("Thickness");
        _component.DefaultState.GetVariableSave("Holder.ShaderParameters")?.Value.ShouldBeNull();
    }

    [Fact]
    public void EditedVariable_IsFoundByTheRecursiveFinder_AndRowIsNoLongerDefault()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        MemberCategory category = BuildRows();

        Row(category, "Thickness").Value = 3f;

        ObjectFinder.Self.GetRootVariable(VariableName, _component).ShouldNotBeNull();
        _component.DefaultState.GetValueRecursive(VariableName).ShouldBe(3f);
        Row(category, "Thickness").IsDefault.ShouldBeFalse();
        Row(category, "Thickness").Value.ShouldBe(3f);
    }

    [Fact]
    public void MakeDefault_RemovesTheVariable_SoNothingIsSaved()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        MemberCategory category = BuildRows();
        Row(category, "Thickness").Value = 3f;

        Row(category, "Thickness").IsDefault = true;

        _component.DefaultState.Variables.Any(variable => variable.Name == VariableName).ShouldBeFalse();
        Row(category, "Thickness").Value.ShouldBe(2f);
    }

    [Fact]
    public void ALegacyTextValue_IsShown_AndCountsAsSet()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        _component.DefaultState.SetValue("Holder.ShaderParameters", "Thickness=4.0");

        MemberCategory category = BuildRows();

        Row(category, "Thickness").Value.ShouldBe(4f);
        Row(category, "Thickness").IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void EditingARowWithALegacyValue_MovesOnlyThatParameterOutOfTheText()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f), Float("Softness", 1f));
        _component.DefaultState.SetValue("Holder.ShaderParameters", "Thickness=4.0;Softness=0.5");
        MemberCategory category = BuildRows();

        Row(category, "Thickness").Value = 5f;

        _component.DefaultState.GetVariableSave(VariableName).ShouldNotBeNull("the edit should have saved the variable").Value.ShouldBe(5f);
        string text = (string)_component.DefaultState.GetVariableSave("Holder.ShaderParameters")!.Value!;
        text.ShouldNotContain("Thickness");
        text.ShouldContain("Softness");
    }

    [Fact]
    public void MakeDefaultOnALegacyValue_RemovesItFromTheText()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        _component.DefaultState.SetValue("Holder.ShaderParameters", "Thickness=4.0");
        MemberCategory category = BuildRows();

        Row(category, "Thickness").IsDefault = true;

        ((string?)_component.DefaultState.GetVariableSave("Holder.ShaderParameters")!.Value).ShouldNotContain("Thickness");
        Row(category, "Thickness").Value.ShouldBe(2f);
    }

    [Fact]
    public void IntAndBoolRows_SaveTypedVariables()
    {
        GivenTheEffectDeclares(
            new ShaderParameterInfo("Count", ShaderParameterKind.Int, 1, ShaderParameterValue.FromInt(1)),
            new ShaderParameterInfo("Wrapped", ShaderParameterKind.Bool, 1, ShaderParameterValue.FromBool(false)));
        MemberCategory category = BuildRows();

        Row(category, "Count").Value = 3;
        Row(category, "Wrapped").Value = true;

        VariableSave count = _component.DefaultState.GetVariableSave("Holder.Outline_Count").ShouldNotBeNull("the edit should have saved the variable");
        count.Type.ShouldBe("int");
        count.Value.ShouldBe(3);
        VariableSave wrapped = _component.DefaultState.GetVariableSave("Holder.Outline_Wrapped").ShouldNotBeNull("the edit should have saved the variable");
        wrapped.Type.ShouldBe("bool");
        wrapped.Value.ShouldBe(true);
        wrapped.ShaderParameter.ShouldBe("Wrapped");
    }

    [Fact]
    public void WithoutAShaderFile_ScalarRowsStayOnTheLegacyText()
    {
        _component.DefaultState.Variables.RemoveAll(variable => variable.Name == "Holder.SourceShaderFile");
        GivenTheEffectDeclares(Float("Thickness", 2f));
        MemberCategory category = BuildRows();

        Row(category, "Thickness").Value = 3f;

        _component.DefaultState.GetValueRecursive("Holder.ShaderParameters").ShouldBe("Thickness=3.0");
    }

    [Fact]
    public void AParameterNameTheNamingSchemeCannotRepresent_StaysOnTheLegacyText()
    {
        GivenTheEffectDeclares(Float("Bad__Name", 2f));
        MemberCategory category = BuildRows();

        Row(category, "Bad__Name").Value = 3f;

        _component.DefaultState.GetValueRecursive("Holder.ShaderParameters").ShouldBe("Bad__Name=3.0");
    }

    [Fact]
    public void TheRow_OffersExpose()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        MemberCategory category = BuildRows();

        Row(category, "Thickness").ContextMenuEvents.Keys.ShouldContain("Expose Variable");
    }

    private UndoManager UseRealUndoManager()
    {
        Mock<IReferenceFinderProjectProvider> projectProvider = new();
        projectProvider.Setup(x => x.GumProjectSave).Returns(ObjectFinder.Self.GumProjectSave!);
        _mocker.GetMock<ISelectedState>().Setup(state => state.SelectedComponent).Returns(_component);
        UndoManager undoManager = new UndoManager(_mocker.Get<ISelectedState>(), new Mock<IUndoRenameLogic>().Object,
            new Mock<IGuiCommands>().Object, new Mock<IFileCommands>().Object, new Mock<IMessenger>().Object,
            new Mock<IUndoPluginNotifier>().Object, new Mock<IAnimationUndoProvider>().Object, projectProvider.Object,
            new Lazy<IVariableReferenceLogic>(() => new Mock<IVariableReferenceLogic>().Object));
        _mocker.Use<IUndoManager>(undoManager);
        return undoManager;
    }

    [Fact]
    public void UndoAndRedo_OfARowEdit_RemoveAndRestoreTheMarkedVariable()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        UndoManager undoManager = UseRealUndoManager();
        MemberCategory category = BuildRows();

        undoManager.RecordState();
        Row(category, "Thickness").Value = 3f;
        undoManager.RecordUndo();
        undoManager.PerformUndo();

        _component.DefaultState.Variables.Any(variable => variable.Name == VariableName).ShouldBeFalse();

        undoManager.PerformRedo();

        VariableSave restored = _component.DefaultState.GetVariableSave(VariableName).ShouldNotBeNull("redo should bring the variable back");
        restored.Value.ShouldBe(3f);
        restored.ShaderParameter.ShouldBe("Thickness");
        restored.Type.ShouldBe("float");
    }

    [Fact]
    public void UndoOfASecondEdit_RestoresThePreviousValue()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        UndoManager undoManager = UseRealUndoManager();
        MemberCategory category = BuildRows();
        undoManager.RecordState();
        Row(category, "Thickness").Value = 3f;
        undoManager.RecordUndo();

        undoManager.RecordState();
        Row(category, "Thickness").Value = 6f;
        undoManager.RecordUndo();
        undoManager.PerformUndo();

        _component.DefaultState.GetVariableSave(VariableName).ShouldNotBeNull().Value.ShouldBe(3f);
    }

    [Fact]
    public void EditingOneInstance_LeavesAnotherInstanceOfTheSameShaderUnset()
    {
        InstanceSave other = new InstanceSave { Name = "Other", BaseType = "Container", ParentContainer = _component };
        _component.Instances.Add(other);
        _component.DefaultState.SetValue("Other.SourceShaderFile", "Shaders/Outline.slang");
        GivenTheEffectDeclares(Float("Thickness", 2f));
        MemberCategory category = BuildRows();

        Row(category, "Thickness").Value = 3f;

        _component.DefaultState.GetVariableSave(VariableName).ShouldNotBeNull();
        _component.DefaultState.Variables.Any(variable => variable.Name == "Other.Outline_Thickness").ShouldBeFalse();
    }

    #region Expose (slice 6)

    private const string ExposedName = "OutlineThickness";

    private void UseRealExposeService(UndoManager undoManager, string? answer = ExposedName)
    {
        Mock<IDialogService> dialogService = new();
        dialogService
            .Setup(d => d.GetUserString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GetUserStringOptions>()))
            .Returns(answer);
        Mock<IFileCommands> fileCommands = new();
        Mock<IPluginManager> pluginManager = new();
        Mock<IRenameLogic> renameLogic = new();
        renameLogic
            .Setup(x => x.GetChangesForRenamedVariable(It.IsAny<ElementSave>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new VariableChangeResponse());
        ExposeVariableService service = new ExposeVariableService(
            undoManager, _mocker.Get<IGuiCommands>(), fileCommands.Object, renameLogic.Object,
            _mocker.Get<ISelectedState>(), new Mock<INameVerifier>().Object, dialogService.Object,
            new Mock<IVariableSaveLogic>().Object, pluginManager.Object,
            new InstanceOverrideRemover(undoManager, fileCommands.Object, pluginManager.Object));
        _mocker.Use<IExposeVariableService>(service);
    }

    private static void Expose(InstanceMember row) => row.ContextMenuEvents["Expose Variable"].Invoke(row, EventArgs.Empty);

    [Fact]
    public void Expose_OnAnUntouchedRow_SavesAMarkedPlaceholderUnderTheChosenName()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        UseRealExposeService(UseRealUndoManager());
        MemberCategory category = BuildRows();

        Expose(Row(category, "Thickness"));

        VariableSave saved = _component.DefaultState.GetVariableSave(VariableName).ShouldNotBeNull("exposing names the parameter, so it is saved");
        saved.ExposedAsName.ShouldBe(ExposedName);
        saved.SetsValue.ShouldBeFalse();
        saved.Value.ShouldBeNull();
        saved.Type.ShouldBe("float");
        saved.ShaderParameter.ShouldBe("Thickness");
    }

    [Fact]
    public void Expose_LeavesTheRowShowingTheCompiledDefault()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        UseRealExposeService(UseRealUndoManager());
        MemberCategory category = BuildRows();

        Expose(Row(category, "Thickness"));
        MemberCategory rebuilt = BuildRows();

        Row(rebuilt, "Thickness").Value.ShouldBe(2f);
        Row(rebuilt, "Thickness").IsDefault.ShouldBeTrue();
        Row(rebuilt, "Thickness").ContextMenuEvents.Keys.ShouldNotContain("Expose Variable");
    }

    [Fact]
    public void Expose_WhenTheNamePromptIsCancelled_SavesNothing()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        UseRealExposeService(UseRealUndoManager(), answer: null);
        MemberCategory category = BuildRows();

        Expose(Row(category, "Thickness"));

        _component.DefaultState.Variables.Any(variable => variable.Name == VariableName).ShouldBeFalse();
    }

    [Fact]
    public void Expose_OnARowWithALegacyValue_KeepsTheValueAsTheExposedVariablesValue()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        _component.DefaultState.SetValue("Holder.ShaderParameters", "Thickness=4.0");
        UseRealExposeService(UseRealUndoManager());
        MemberCategory category = BuildRows();

        Expose(Row(category, "Thickness"));

        VariableSave saved = _component.DefaultState.GetVariableSave(VariableName).ShouldNotBeNull();
        saved.ExposedAsName.ShouldBe(ExposedName);
        saved.Value.ShouldBe(4f);
        ((string?)_component.DefaultState.GetVariableSave("Holder.ShaderParameters")!.Value).ShouldNotContain("Thickness");
    }

    [Fact]
    public void Expose_WhenTheServiceCannotExpose_RemovesThePlaceholderAndLeavesTheLegacyValue()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        _component.DefaultState.SetValue("Holder.ShaderParameters", "Thickness=4.0");
        UseRealExposeService(UseRealUndoManager());
        // With no selected element the service shows a message and exposes nothing.
        _mocker.GetMock<ISelectedState>().Setup(state => state.SelectedElement).Returns((ElementSave?)null);
        MemberCategory category = BuildRows();

        Expose(Row(category, "Thickness"));

        _component.DefaultState.Variables.Any(variable => variable.Name == VariableName).ShouldBeFalse();
        ((string?)_component.DefaultState.GetVariableSave("Holder.ShaderParameters")!.Value).ShouldContain("Thickness");
    }

    [Fact]
    public void Expose_OnARowAlreadyEdited_KeepsItsValue()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        UseRealExposeService(UseRealUndoManager());
        MemberCategory category = BuildRows();
        Row(category, "Thickness").Value = 3f;

        Expose(Row(category, "Thickness"));

        VariableSave saved = _component.DefaultState.GetVariableSave(VariableName).ShouldNotBeNull();
        saved.ExposedAsName.ShouldBe(ExposedName);
        saved.Value.ShouldBe(3f);
    }

    [Fact]
    public void Expose_UndoAndRedo_RemoveAndRestoreTheExposure()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        UndoManager undoManager = UseRealUndoManager();
        UseRealExposeService(undoManager);
        MemberCategory category = BuildRows();

        undoManager.RecordState();
        Expose(Row(category, "Thickness"));
        undoManager.PerformUndo();

        _component.DefaultState.Variables.Any(variable => variable.Name == VariableName).ShouldBeFalse();

        undoManager.PerformRedo();

        VariableSave restored = _component.DefaultState.GetVariableSave(VariableName).ShouldNotBeNull("redo should bring the exposure back");
        restored.ExposedAsName.ShouldBe(ExposedName);
        restored.ShaderParameter.ShouldBe("Thickness");
        restored.SetsValue.ShouldBeFalse();
    }

    private static void Unexpose(InstanceMember row) =>
        row.ContextMenuEvents.Single(item => item.Key.StartsWith("Un-expose Variable")).Value.Invoke(row, EventArgs.Empty);

    [Fact]
    public void Unexpose_OfAnUntouchedParameter_PrunesThePlaceholder()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        UseRealExposeService(UseRealUndoManager());
        Expose(Row(BuildRows(), "Thickness"));

        Unexpose(Row(BuildRows(), "Thickness"));

        _component.DefaultState.Variables.Any(variable => variable.Name == VariableName).ShouldBeFalse("nothing names the parameter any more");
    }

    [Fact]
    public void Unexpose_OfAParameterWithAValue_KeepsTheVariable()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        UseRealExposeService(UseRealUndoManager());
        MemberCategory category = BuildRows();
        Row(category, "Thickness").Value = 3f;
        Expose(Row(category, "Thickness"));

        Unexpose(Row(BuildRows(), "Thickness"));

        VariableSave kept = _component.DefaultState.GetVariableSave(VariableName).ShouldNotBeNull();
        kept.ExposedAsName.ShouldBeNull();
        kept.Value.ShouldBe(3f);
    }

    [Fact]
    public void Unexpose_OfAParameterAnotherStateSets_KeepsTheVariable()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        UseRealExposeService(UseRealUndoManager());
        Expose(Row(BuildRows(), "Thickness"));
        StateSaveCategory stateCategory = new StateSaveCategory { Name = "Look" };
        StateSave highlighted = new StateSave { Name = "Highlighted", ParentContainer = _component };
        highlighted.SetValue(VariableName, 7f, _holder, "float");
        stateCategory.States.Add(highlighted);
        _component.Categories.Add(stateCategory);

        Unexpose(Row(BuildRows(), "Thickness"));

        _component.DefaultState.GetVariableSave(VariableName).ShouldNotBeNull();
    }

    [Fact]
    public void Unexpose_UndoAndRedo_RestoreAndRemoveThePlaceholder()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        UndoManager undoManager = UseRealUndoManager();
        UseRealExposeService(undoManager);
        Expose(Row(BuildRows(), "Thickness"));
        MemberCategory category = BuildRows();

        undoManager.RecordState();
        Unexpose(Row(category, "Thickness"));
        _component.DefaultState.Variables.Any(variable => variable.Name == VariableName).ShouldBeFalse();
        undoManager.PerformUndo();

        VariableSave restored = _component.DefaultState.GetVariableSave(VariableName).ShouldNotBeNull("undo should bring the exposed placeholder back");
        restored.ExposedAsName.ShouldBe(ExposedName);

        undoManager.PerformRedo();

        _component.DefaultState.Variables.Any(variable => variable.Name == VariableName).ShouldBeFalse();
    }

    [Fact]
    public void ExposedParameter_IsUsableByAScreenInstance_AndSurvivesSaveAndLoad()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f));
        UseRealExposeService(UseRealUndoManager());
        MemberCategory category = BuildRows();
        Expose(Row(category, "Thickness"));

        ComponentSave reloaded = RoundTrip(_component);

        VariableSave exposed = reloaded.DefaultState.GetVariableSave(VariableName).ShouldNotBeNull();
        exposed.ExposedAsName.ShouldBe(ExposedName);
        exposed.SetsValue.ShouldBeFalse();
        exposed.ShaderParameter.ShouldBe("Thickness");
        exposed.Type.ShouldBe("float");

        ScreenSave screen = new ScreenSave { Name = "MyScreen" };
        screen.States.Add(new StateSave { ParentContainer = screen });
        InstanceSave user = new InstanceSave { Name = "Comp1", BaseType = "ShaderComponent", ParentContainer = screen };
        screen.Instances.Add(user);
        ObjectFinder.Self.GumProjectSave!.Components.Clear();
        ObjectFinder.Self.GumProjectSave.Components.Add(reloaded);
        ObjectFinder.Self.GumProjectSave.Screens.Add(screen);

        VariableSave? root = ObjectFinder.Self.GetRootVariable("Comp1." + ExposedName, screen);
        root.ShouldNotBeNull("the screen should be able to name the exposed shader parameter");
        screen.DefaultState.SetValue("Comp1." + ExposedName, 5f, user);
        screen.DefaultState.GetValueRecursive("Comp1." + ExposedName).ShouldBe(5f);
    }

    private static ComponentSave RoundTrip(ComponentSave component)
    {
        string path = Path.Combine(Path.GetTempPath(), "gum-7010-s6-" + Guid.NewGuid().ToString("N") + ".gucx");
        try
        {
            component.Save(path);
            return FileManager.XmlDeserialize<ComponentSave>(path);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    #endregion

    #region Vectors (slice 5)

    private const string TintX = "Holder.Outline_Tint__X";
    private const string TintY = "Holder.Outline_Tint__Y";
    private const string TintZ = "Holder.Outline_Tint__Z";
    private const string TintW = "Holder.Outline_Tint__W";

    private static ShaderParameterInfo Tint2() =>
        new ShaderParameterInfo("Tint", ShaderParameterKind.Float, 2, ShaderParameterValue.FromVector2(0.5f, 0.25f));

    private static ShaderParameterInfo Tint4() =>
        new ShaderParameterInfo("Tint", ShaderParameterKind.Float, 4, ShaderParameterValue.FromVector4(0.1f, 0.2f, 0.3f, 0.4f));

    private bool IsSaved(string name) => _component.DefaultState.Variables.Any(variable => variable.Name == name);

    [Fact]
    public void VectorRows_KeepTheirLabels_AndBuildingThemSavesNothing()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f), Tint2());

        MemberCategory category = BuildRows();

        category.Members.Select(member => member.Name).ToArray().ShouldBe(new[] { "Thickness", "Tint X", "Tint Y" });
        Row(category, "Tint X").Value.ShouldBe(0.5f);
        Row(category, "Tint Y").Value.ShouldBe(0.25f);
        Row(category, "Tint X").IsDefault.ShouldBeTrue();
        _component.DefaultState.Variables.Any(variable => variable.Name.Contains("Tint")).ShouldBeFalse();
    }

    [Fact]
    public void EditingOneComponent_SavesEveryComponentAsAMarkedVariable()
    {
        GivenTheEffectDeclares(Tint2());
        MemberCategory category = BuildRows();

        Row(category, "Tint X").Value = 0.75f;

        VariableSave x = _component.DefaultState.GetVariableSave(TintX).ShouldNotBeNull("the edited component should be saved");
        x.Value.ShouldBe(0.75f);
        x.Type.ShouldBe("float");
        x.ShaderParameter.ShouldBe("Tint#0/2");
        VariableSave y = _component.DefaultState.GetVariableSave(TintY).ShouldNotBeNull("the sibling should be saved with its current value");
        y.Value.ShouldBe(0.25f);
        y.Type.ShouldBe("float");
        y.ShaderParameter.ShouldBe("Tint#1/2");
        _component.DefaultState.GetVariableSave("Holder.ShaderParameters")?.Value.ShouldBeNull();
    }

    [Fact]
    public void EditingOneComponentOfAFloat4_SavesAllFour()
    {
        GivenTheEffectDeclares(Tint4());
        MemberCategory category = BuildRows();

        Row(category, "Tint Z").Value = 0.9f;

        _component.DefaultState.GetVariableSave(TintX).ShouldNotBeNull().Value.ShouldBe(0.1f);
        _component.DefaultState.GetVariableSave(TintY).ShouldNotBeNull().Value.ShouldBe(0.2f);
        _component.DefaultState.GetVariableSave(TintZ).ShouldNotBeNull().Value.ShouldBe(0.9f);
        VariableSave w = _component.DefaultState.GetVariableSave(TintW).ShouldNotBeNull();
        w.Value.ShouldBe(0.4f);
        w.ShaderParameter.ShouldBe("Tint#3/4");
    }

    [Fact]
    public void SavedComponents_AreFoundByTheFinder_AndEveryRowShowsAsSet()
    {
        GivenTheEffectDeclares(Tint2());
        MemberCategory category = BuildRows();

        Row(category, "Tint X").Value = 0.75f;

        ObjectFinder.Self.GetRootVariable(TintX, _component).ShouldNotBeNull();
        ObjectFinder.Self.GetRootVariable(TintY, _component).ShouldNotBeNull();
        Row(category, "Tint X").IsDefault.ShouldBeFalse();
        Row(category, "Tint Y").IsDefault.ShouldBeFalse();
        Row(category, "Tint Y").Value.ShouldBe(0.25f);
    }

    [Fact]
    public void EditingASiblingLater_DoesNotChangeTheOtherComponents()
    {
        GivenTheEffectDeclares(Tint2());
        MemberCategory category = BuildRows();
        Row(category, "Tint X").Value = 0.75f;

        Row(category, "Tint Y").Value = 0.1f;

        _component.DefaultState.GetVariableSave(TintX).ShouldNotBeNull().Value.ShouldBe(0.75f);
        _component.DefaultState.GetVariableSave(TintY).ShouldNotBeNull().Value.ShouldBe(0.1f);
    }

    [Fact]
    public void EditingAComponent_TakesSiblingValuesFromTheLegacyText_AndMovesTheParameterOut()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f), Tint2());
        _component.DefaultState.SetValue("Holder.ShaderParameters", "Tint=(0.5, 0.9);Thickness=4.0");
        MemberCategory category = BuildRows();
        Row(category, "Tint Y").Value.ShouldBe(0.9f);

        Row(category, "Tint X").Value = 0.1f;

        _component.DefaultState.GetVariableSave(TintX).ShouldNotBeNull().Value.ShouldBe(0.1f);
        _component.DefaultState.GetVariableSave(TintY).ShouldNotBeNull().Value.ShouldBe(0.9f);
        string text = (string)_component.DefaultState.GetVariableSave("Holder.ShaderParameters")!.Value!;
        text.ShouldNotContain("Tint");
        text.ShouldContain("Thickness");
    }

    [Fact]
    public void ALegacyVectorValue_IsShownAndCountsAsSet()
    {
        GivenTheEffectDeclares(Tint2());
        _component.DefaultState.SetValue("Holder.ShaderParameters", "Tint=(0.5, 0.9)");

        MemberCategory category = BuildRows();

        Row(category, "Tint Y").Value.ShouldBe(0.9f);
        Row(category, "Tint X").IsDefault.ShouldBeFalse();
        Row(category, "Tint Y").IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void MakeDefaultOnOneComponent_ResetsTheWholeParameter()
    {
        GivenTheEffectDeclares(Float("Thickness", 2f), Tint2());
        MemberCategory category = BuildRows();
        Row(category, "Thickness").Value = 3f;
        Row(category, "Tint X").Value = 0.75f;

        Row(category, "Tint Y").IsDefault = true;

        IsSaved(TintX).ShouldBeFalse();
        IsSaved(TintY).ShouldBeFalse();
        IsSaved(VariableName).ShouldBeTrue("other parameters are untouched");
        Row(category, "Tint X").Value.ShouldBe(0.5f);
    }

    [Fact]
    public void MakeDefaultOnAComponentWithALegacyValue_RemovesTheParameterFromTheText()
    {
        GivenTheEffectDeclares(Tint2());
        _component.DefaultState.SetValue("Holder.ShaderParameters", "Tint=(0.5, 0.9)");
        MemberCategory category = BuildRows();

        Row(category, "Tint X").IsDefault = true;

        ((string?)_component.DefaultState.GetVariableSave("Holder.ShaderParameters")!.Value).ShouldNotContain("Tint");
        Row(category, "Tint Y").Value.ShouldBe(0.25f);
    }

    [Fact]
    public void UndoAndRedo_OfAComponentEdit_RemoveAndRestoreAllComponents()
    {
        GivenTheEffectDeclares(Tint2());
        UndoManager undoManager = UseRealUndoManager();
        MemberCategory category = BuildRows();

        undoManager.RecordState();
        Row(category, "Tint X").Value = 0.75f;
        undoManager.RecordUndo();
        undoManager.PerformUndo();

        IsSaved(TintX).ShouldBeFalse();
        IsSaved(TintY).ShouldBeFalse();

        undoManager.PerformRedo();

        _component.DefaultState.GetVariableSave(TintX).ShouldNotBeNull().Value.ShouldBe(0.75f);
        _component.DefaultState.GetVariableSave(TintY).ShouldNotBeNull().Value.ShouldBe(0.25f);
    }

    [Fact]
    public void ExposeOnOneComponent_ExposesIt_AndSavesTheWholeParameterWithValues()
    {
        GivenTheEffectDeclares(Tint2());
        UseRealExposeService(UseRealUndoManager(), answer: "TintX");
        MemberCategory category = BuildRows();

        Expose(Row(category, "Tint X"));

        VariableSave x = _component.DefaultState.GetVariableSave(TintX).ShouldNotBeNull("exposing names the component");
        x.ExposedAsName.ShouldBe("TintX");
        x.ShaderParameter.ShouldBe("Tint#0/2");
        x.Type.ShouldBe("float");
        x.Value.ShouldBe(0.5f, "the exposed component carries its value so the vector is complete");
        VariableSave y = _component.DefaultState.GetVariableSave(TintY).ShouldNotBeNull("exposing pins the whole parameter");
        y.Value.ShouldBe(0.25f);
        y.ExposedAsName.ShouldBeNull();
    }

    [Fact]
    public void ExposeOnOneComponent_ThenAnOuterElementSetsIt_TheVectorStaysComplete()
    {
        GivenTheEffectDeclares(Tint2());
        UseRealExposeService(UseRealUndoManager(), answer: "TintX");
        Expose(Row(BuildRows(), "Tint X"));
        ComponentSave reloaded = RoundTrip(_component);
        ScreenSave screen = new ScreenSave { Name = "MyScreen" };
        screen.States.Add(new StateSave { ParentContainer = screen });
        InstanceSave user = new InstanceSave { Name = "Comp1", BaseType = "ShaderComponent", ParentContainer = screen };
        screen.Instances.Add(user);
        ObjectFinder.Self.GumProjectSave!.Components.Clear();
        ObjectFinder.Self.GumProjectSave.Components.Add(reloaded);
        ObjectFinder.Self.GumProjectSave.Screens.Add(screen);

        screen.DefaultState.SetValue("Comp1.TintX", 0.9f, user);

        screen.DefaultState.GetValueRecursive("Comp1.TintX").ShouldBe(0.9f);
        reloaded.DefaultState.GetVariableSave(TintX).ShouldNotBeNull().Value.ShouldBe(0.5f);
        reloaded.DefaultState.GetVariableSave(TintY).ShouldNotBeNull().Value.ShouldBe(0.25f);
    }

    [Fact]
    public void ExposeOnOneComponent_WhenThePromptIsCancelled_SavesNothing()
    {
        GivenTheEffectDeclares(Tint2());
        UseRealExposeService(UseRealUndoManager(), answer: null);
        MemberCategory category = BuildRows();

        Expose(Row(category, "Tint Y"));

        IsSaved(TintX).ShouldBeFalse();
        IsSaved(TintY).ShouldBeFalse();
    }

    [Fact]
    public void ExposeOnOneComponent_WithALegacyValue_KeepsItAsTheComponentsValues()
    {
        GivenTheEffectDeclares(Tint2());
        _component.DefaultState.SetValue("Holder.ShaderParameters", "Tint=(0.6, 0.9)");
        UseRealExposeService(UseRealUndoManager(), answer: "TintX");
        MemberCategory category = BuildRows();

        Expose(Row(category, "Tint X"));

        _component.DefaultState.GetVariableSave(TintX).ShouldNotBeNull().Value.ShouldBe(0.6f);
        _component.DefaultState.GetVariableSave(TintY).ShouldNotBeNull().Value.ShouldBe(0.9f);
        ((string?)_component.DefaultState.GetVariableSave("Holder.ShaderParameters")!.Value).ShouldNotContain("Tint");
    }

    [Fact]
    public void ExposeOnOneComponent_UndoAndRedo_RemoveAndRestoreTheWholeParameter()
    {
        GivenTheEffectDeclares(Tint2());
        UndoManager undoManager = UseRealUndoManager();
        UseRealExposeService(undoManager, answer: "TintX");
        MemberCategory category = BuildRows();

        undoManager.RecordState();
        Expose(Row(category, "Tint X"));
        undoManager.PerformUndo();

        IsSaved(TintX).ShouldBeFalse();
        IsSaved(TintY).ShouldBeFalse();

        undoManager.PerformRedo();

        _component.DefaultState.GetVariableSave(TintX).ShouldNotBeNull().ExposedAsName.ShouldBe("TintX");
        _component.DefaultState.GetVariableSave(TintY).ShouldNotBeNull();
    }

    [Fact]
    public void MakeDefaultOnAnExposedComponent_KeepsTheExposureAndClearsTheValues()
    {
        GivenTheEffectDeclares(Tint2());
        UseRealExposeService(UseRealUndoManager(), answer: "TintX");
        Expose(Row(BuildRows(), "Tint X"));
        MemberCategory category = BuildRows();

        Row(category, "Tint Y").IsDefault = true;

        VariableSave x = _component.DefaultState.GetVariableSave(TintX).ShouldNotBeNull("an exposure is never removed by Make Default");
        x.ExposedAsName.ShouldBe("TintX");
        x.SetsValue.ShouldBeFalse();
        IsSaved(TintY).ShouldBeFalse();
    }

    #endregion
}
