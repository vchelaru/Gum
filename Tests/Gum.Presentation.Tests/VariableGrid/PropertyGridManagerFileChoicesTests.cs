using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Reflection;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Moq.AutoMock;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.VariableGrid;

public class PropertyGridManagerFileChoicesTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();
    private readonly string _projectDirectory;
    private readonly ComponentSave _component = new ComponentSave { Name = "Card", BaseType = "Container" };

    public PropertyGridManagerFileChoicesTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "GumFileChoicesGrid_" + Guid.NewGuid().ToString("N")) + "/";
        Directory.CreateDirectory(_projectDirectory);
        _component.States.Add(new StateSave { Name = "Default", ParentContainer = _component });
        GumProjectSave project = new GumProjectSave { FullFileName = _projectDirectory + "Project.gumx" };
        project.Components.Add(_component);
        ObjectFinder.Self.GumProjectSave = project;
        _mocker.GetMock<IProjectState>().SetupGet(state => state.GumProjectSave).Returns(project);
    }

    public override void Dispose()
    {
        base.Dispose();
        Directory.Delete(_projectDirectory, recursive: true);
    }

    [Fact]
    public void UpdateFileFilters_SourceFileRow_OffersTheReferencedFilesTheCommitWouldAccept()
    {
        AddInstance("Hero", "Sprite", "SourceFile", "hero.png");
        AddInstance("Notes", "Sprite", "SourceFile", "notes.txt");
        _mocker.GetMock<ISetVariableLogic>()
            .Setup(logic => logic.GetWhySourcefileIsInvalid(It.IsAny<string>(), _component, null, "SourceFile"))
            .Returns((string value, ElementSave _, InstanceSave? __, string ___) => value.EndsWith(".txt") ? "not supported" : null);
        StateReferencingInstanceMember row = CreateMember("SourceFile");
        PropertyGridManager sut = _mocker.CreateInstance<PropertyGridManager>();

        sut.UpdateFileFilters(Categorize(row), _component, null);

        ChoicesOf(row)("old.png").ShouldBe(new[] { "hero.png", "old.png" });
    }

    [Fact]
    public void UpdateFileFilters_CustomFontFileAndShaderRows_OfferTheFilesTheirPickersShow()
    {
        AddInstance("Label", "Text", "CustomFontFile", "label.fnt");
        _component.DefaultState.Variables.Add(new VariableSave { Name = "Label.UseCustomFont", Type = "bool", Value = true, SetsValue = true });
        AddInstance("Shaded", "Sprite", "SourceShaderFile", "blur.fx");
        AddInstance("Hero", "Sprite", "SourceFile", "hero.png");
        StateReferencingInstanceMember font = CreateMember("CustomFontFile");
        StateReferencingInstanceMember shader = CreateMember("SourceShaderFile");
        PropertyGridManager sut = _mocker.CreateInstance<PropertyGridManager>();

        sut.UpdateFileFilters(Categorize(font, shader), _component, null);

        ChoicesOf(font)(null).ShouldBe(new[] { "label.fnt" });
        ChoicesOf(shader)(null).ShouldBe(new[] { "blur.fx" });
    }

    [Fact]
    public void UpdateFileFilters_ARowRetargetedToAnotherInstance_KeepsItsFilterAndChoices()
    {
        // Selecting another instance of the same type retargets the shown rows onto the new rows'
        // entries instead of rebuilding them, so the settings must live on the entry.
        AddInstance("Label", "Text", "CustomFontFile", "label.fnt");
        StateReferencingInstanceMember shown = CreateMember("CustomFontFile");
        StateReferencingInstanceMember next = CreateMember("CustomFontFile");
        PropertyGridManager sut = _mocker.CreateInstance<PropertyGridManager>();
        sut.UpdateFileFilters(Categorize(shown), _component, null);
        sut.UpdateFileFilters(Categorize(next), _component, null);

        shown.Retarget(next.Entry);

        shown.PropertiesToSetOnDisplayer.ContainsKey("FileChoices").ShouldBeTrue();
        shown.PropertiesToSetOnDisplayer["Filter"].ShouldBe("Bitmap Font Generator Font|*.fnt");
    }

    [Fact]
    public void FontSourceToggle_FontFileRow_OffersFontFiles_AndSystemFontDropsTheChoices()
    {
        AddInstance("Hero", "Sprite", "SourceFile", "hero.png");
        _component.BaseType = "Text";
        _component.DefaultState.SetValue("Font", "Fonts/Some.ttf");
        _mocker.GetMock<ISetVariableLogic>()
            .Setup(logic => logic.PropertyValueChanged(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<InstanceSave?>(),
                It.IsAny<StateSave?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(ToolsUtilities.GeneralResponse.SuccessfulResponse);
        StateReferencingInstanceMember fontMember = CreateMember("Font");
        MemberCategory category = new MemberCategory("Font");
        category.Members.Add(fontMember);
        PropertyGridManager sut = _mocker.CreateInstance<PropertyGridManager>();

        sut.AdjustFontSourceToggle(new List<MemberCategory> { category }, _component.DefaultState, null);

        ChoicesOf(fontMember)("Fonts/Some.ttf").ShouldBe(new[] { "Fonts/Some.ttf" }, "a project file that is not a font is not offered");

        category.Members.First(member => member.Name == "Font Source").Value = "System Font";

        fontMember.PropertiesToSetOnDisplayer.ContainsKey("FileChoices").ShouldBeFalse();
    }

    private static Func<string?, IReadOnlyList<string>> ChoicesOf(InstanceMember member) =>
        (Func<string?, IReadOnlyList<string>>)member.PropertiesToSetOnDisplayer["FileChoices"];

    private static List<MemberCategory> Categorize(params StateReferencingInstanceMember[] members)
    {
        MemberCategory category = new MemberCategory("Test");
        foreach (StateReferencingInstanceMember member in members)
        {
            category.Members.Add(member);
        }
        return new List<MemberCategory> { category };
    }

    private void AddInstance(string name, string baseType, string variable, string file)
    {
        _component.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = _component });
        _component.DefaultState.Variables.Add(new VariableSave
        {
            Name = name + "." + variable, Type = "string", Value = file, IsFile = true, SetsValue = true,
        });
        string path = _projectDirectory + file;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "");
    }

    private StateReferencingInstanceMember CreateMember(string variableName) =>
        new StateReferencingInstanceMember(
            Array.Empty<Attribute>(), null, typeof(string),
            isReadOnly: false, isAssignedByReference: false, isVariable: true,
            _component.DefaultState, null, variableName, null, _component,
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
}
