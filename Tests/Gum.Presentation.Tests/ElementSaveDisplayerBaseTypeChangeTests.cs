using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Repro for issue #4196: changing an instance's BaseType from NineSlice to Sprite should not
/// drop the SourceFile value, exercised through the actual grid-row factory
/// (<see cref="ElementSaveDisplayer.GetCategories"/>) rather than just the underlying state data.
/// </summary>
public class ElementSaveDisplayerBaseTypeChangeTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();
    private readonly ElementSaveDisplayer _displayer;
    private readonly GumProjectSave _project;
    private readonly ScreenSave _screen;
    private readonly StateSave _screenDefaultState;

    public ElementSaveDisplayerBaseTypeChangeTests()
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

        // Use the real inclusion-decision logic (rather than the default auto-mock, which would
        // report every variable as inactive) so this test exercises the actual
        // GetIfVariableIsActive/GetShouldIncludeBasedOnBaseType path the bug report points at.
        _mocker.Use<IVariableSaveLogic>(_mocker.CreateInstance<VariableSaveLogic>());

        _mocker.GetMock<IProjectState>()
            .Setup(x => x.GumProjectSave)
            .Returns(_project);

        _displayer = _mocker.CreateInstance<ElementSaveDisplayer>();
    }

    [Fact]
    public void GetCategories_ShouldPreserveSourceFileValue_WhenInstanceBaseTypeChangesFromNineSliceToSprite()
    {
        InstanceSave instance = new InstanceSave { Name = "MyInstance", BaseType = "NineSlice", ParentContainer = _screen };
        _screen.Instances.Add(instance);

        _screenDefaultState.SetValue("MyInstance.SourceFile", "image.png");

        var categoriesBefore = _displayer.GetCategories(_screen, instance, _screenDefaultState, null);
        var sourceFileBefore = categoriesBefore.SelectMany(c => c.Members).FirstOrDefault(m => m.Name == "MyInstance.SourceFile");
        sourceFileBefore.ShouldNotBeNull("SourceFile should be a visible row while the instance is a NineSlice");
        sourceFileBefore.GetValue(instance).ShouldBe("image.png");

        instance.BaseType = "Sprite";

        var categoriesAfter = _displayer.GetCategories(_screen, instance, _screenDefaultState, null);
        var sourceFileAfter = categoriesAfter.SelectMany(c => c.Members).FirstOrDefault(m => m.Name == "MyInstance.SourceFile");
        sourceFileAfter.ShouldNotBeNull("SourceFile should still be a visible row once the instance is a Sprite");
        sourceFileAfter.GetValue(instance).ShouldBe("image.png");
    }
}
