using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests.VariableGrid;

/// <summary>
/// Repro for issue #4942: a composite variable reference (<c>Color = Components/Styles.Primary.FillColor</c>)
/// is stored collapsed and only expanded into Red/Green/Blue at apply time, so the grid's
/// reference lookup - which matched the literal left side against row names - never matched the
/// channel rows. The swatch stayed editable with no explanation, and edits were silently
/// overwritten the next time references were applied.
/// </summary>
public class ElementSaveDisplayerCompositeReferenceTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();
    private readonly ElementSaveDisplayer _displayer;
    private readonly GumProjectSave _project;
    private readonly ScreenSave _screen;
    private readonly StateSave _screenDefaultState;

    public ElementSaveDisplayerCompositeReferenceTests()
    {
        _project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_project);

        _screen = new ScreenSave { Name = "TestScreen" };
        _screenDefaultState = new StateSave { Name = "Default", ParentContainer = _screen };
        _screen.States.Add(_screenDefaultState);
        _project.Screens.Add(_screen);

        ObjectFinder.Self.GumProjectSave = _project;

        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedStateSave)
            .Returns(_screenDefaultState);
        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedElement)
            .Returns(_screen);

        _mocker.Use(StandardElementsManager.Self);

        var typeManager = new Gum.Reflection.TypeManager();
        typeManager.Initialize();
        _mocker.Use(typeManager);

        _mocker.GetMock<IPluginManager>()
            .Setup(x => x.GetAttributesFor(It.IsAny<VariableSave>()))
            .Returns(new List<Attribute>());

        _mocker.Use<IVariableSaveLogic>(_mocker.CreateInstance<VariableSaveLogic>());

        _mocker.GetMock<IProjectState>()
            .Setup(x => x.GumProjectSave)
            .Returns(_project);

        _displayer = _mocker.CreateInstance<ElementSaveDisplayer>();
    }

    [Fact]
    public void GetCategories_ShouldMarkColorChannelsAsReference_WhenInstanceUsesCollapsedColorReference()
    {
        InstanceSave instance = new InstanceSave
        {
            Name = "Background",
            BaseType = "NineSlice",
            ParentContainer = _screen
        };
        _screen.Instances.Add(instance);

        VariableListSave<string> references = new VariableListSave<string>
        {
            Type = "string",
            Name = "Background.VariableReferences"
        };
        references.Value.Add("Color = Components/Styles.Primary.FillColor");
        _screenDefaultState.VariableLists.Add(references);

        var members = _displayer.GetCategories(_screen, instance, _screenDefaultState, null)
            .SelectMany(category => category.Members)
            .ToList();

        var red = members.FirstOrDefault(member => member.RootVariableName == "Red");
        red.ShouldNotBeNull();
        red.IsAssignedByReference.ShouldBeTrue();
        red.IsReadOnly.ShouldBeTrue();
        red.DetailText.ShouldBe("=Components/Styles.Primary.FillRed");

        var blue = members.FirstOrDefault(member => member.RootVariableName == "Blue");
        blue.ShouldNotBeNull();
        blue.IsAssignedByReference.ShouldBeTrue();
        blue.DetailText.ShouldBe("=Components/Styles.Primary.FillBlue");
    }
}
