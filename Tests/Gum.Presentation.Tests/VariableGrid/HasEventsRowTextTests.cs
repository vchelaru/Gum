using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests.VariableGrid;

/// <summary>
/// The Has Events row says it only affects cursor input, and that line stacks with the other
/// subtext lines (#8034).
/// </summary>
public class HasEventsRowTextTests : BaseTestClass
{
    private const string Sentence = "Only affects cursor input (mouse and touch)";

    private readonly AutoMocker _mocker = new();
    private readonly ElementSaveDisplayer _displayer;
    private readonly ScreenSave _screen;
    private readonly InstanceSave _instance;

    public HasEventsRowTextTests()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

        _screen = new ScreenSave { Name = "TestScreen" };
        _screen.States.Add(new StateSave { Name = "Default", ParentContainer = _screen });
        _instance = new InstanceSave { Name = "Box", BaseType = "Container", ParentContainer = _screen };
        _screen.Instances.Add(_instance);
        project.Screens.Add(_screen);

        ObjectFinder.Self.GumProjectSave = project;

        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(_screen.DefaultState);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(_screen);
        _mocker.Use(StandardElementsManager.Self);
        Gum.Reflection.TypeManager typeManager = new Gum.Reflection.TypeManager();
        typeManager.Initialize();
        _mocker.Use(typeManager);
        _mocker.GetMock<IPluginManager>()
            .Setup(x => x.GetAttributesFor(It.IsAny<VariableSave>()))
            .Returns(new List<Attribute>());
        _mocker.Use<IVariableSaveLogic>(_mocker.CreateInstance<VariableSaveLogic>());
        _mocker.GetMock<IProjectState>().Setup(x => x.GumProjectSave).Returns(project);

        _displayer = _mocker.CreateInstance<ElementSaveDisplayer>();
    }

    private VariableGridEntry GetRow() =>
        _displayer.GetCategories(_screen, _instance, _screen.DefaultState!, null)
            .SelectMany(category => category.Members)
            .Single(member => member.Name == "Box.HasEvents");

    [Fact]
    public void HasEventsRow_ShowsCursorOnlySubtextAndTooltip()
    {
        VariableGridEntry row = GetRow();

        (row.DetailText ?? "").ShouldContain(Sentence);
        row.ToolTipText.ShouldBe(Sentence);
    }

    [Fact]
    public void HasEventsRow_WhenExposed_KeepsSentenceAndExposedAs()
    {
        _screen.DefaultState!.Variables.Add(new VariableSave
        {
            Name = "Box.HasEvents", Type = "bool", SetsValue = false, ExposedAsName = "BoxEvents",
        });

        string detail = GetRow().DetailText ?? "";

        detail.ShouldContain(Sentence);
        detail.ShouldContain("Exposed as BoxEvents");
    }

    [Fact]
    public void HasEventsRow_WhenSetByReference_KeepsSentenceAndSetBy()
    {
        VariableListSave<string> references = new VariableListSave<string> { Type = "string", Name = "Box.VariableReferences" };
        references.Value.Add("HasEvents = Other.HasEvents");
        _screen.DefaultState!.VariableLists.Add(references);

        string detail = GetRow().DetailText ?? "";

        detail.ShouldContain(Sentence);
        detail.ShouldContain("Set by Other.HasEvents");
    }
}
