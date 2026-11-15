using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Reflection;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Gum.Presentation.Tests.VariableGrid;

/// <summary>
/// The Variables tab hides every standard variable whose <c>MinimumGumxVersion</c> is newer than the
/// project's version, for every standard element type (#7520), not only the ones named in the
/// hardcoded <c>ShapeVariableVersionGate</c> list.
/// </summary>
public class StandardVariableVersionGridTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();
    private readonly ElementSaveDisplayer _displayer;
    private readonly GumProjectSave _project;
    private readonly ScreenSave _screen;
    private readonly StateSave _screenDefaultState;

    public StandardVariableVersionGridTests()
    {
        _project = new GumProjectSave { Version = GumProjectSave.NativeVersion };
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

        TypeManager typeManager = new TypeManager();
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

    private List<string> GetRootNames(string baseType)
    {
        _screen.Instances.Clear();
        InstanceSave instance = new InstanceSave
        {
            Name = "Instance",
            BaseType = baseType,
            ParentContainer = _screen
        };
        _screen.Instances.Add(instance);

        return _displayer.GetCategories(_screen, instance, _screenDefaultState, null)
            .SelectMany(category => category.Members)
            .Select(member => member.RootVariableName)
            .ToList();
    }

    [Fact]
    public void GetCategories_ShouldHideRaiseChildrenEventsOutsideOfBounds_OnV7Container()
    {
        _project.Version = (int)GumProjectSave.GumxVersions.ShaderParametersExpansion;

        GetRootNames("Container").ShouldNotContain("RaiseChildrenEventsOutsideOfBounds");
    }

    [Theory]
    [InlineData((int)GumProjectSave.GumxVersions.RaiseChildrenEventsExpansion)]
    [InlineData(GumProjectSave.NativeVersion)]
    public void GetCategories_ShouldShowRaiseChildrenEventsOutsideOfBounds_OnV8OrNewerContainer(int version)
    {
        _project.Version = version;

        GetRootNames("Container").ShouldContain("RaiseChildrenEventsOutsideOfBounds");
    }

    [Fact]
    public void GetCategories_ShouldHideShaderParameters_OnV6Container()
    {
        _project.Version = (int)GumProjectSave.GumxVersions.FontCachePathHash;

        GetRootNames("Container").ShouldNotContain("ShaderParameters");
    }

    [Fact]
    public void GetCategories_ShouldHideEveryTaggedVariable_OnProjectOlderThanItsTag()
    {
        List<string> shownTooEarly = new List<string>();

        foreach (KeyValuePair<string, StateSave> pair in StandardElementsManager.Self.DefaultStates!)
        {
            // Stroke and Blend on the shapes stay visible on older projects by design (pinned by the
            // gate's existing behavior), so they are not part of this rule.
            IEnumerable<VariableSave> taggedVariables = pair.Value.Variables
                .Where(v => v.MinimumGumxVersion > 0)
                .Where(v => !v.Name.StartsWith("Stroke") && v.Name != "Blend");

            foreach (VariableSave tagged in taggedVariables)
            {
                _project.Version = tagged.MinimumGumxVersion - 1;

                if (GetRootNames(pair.Key).Contains(tagged.GetRootName()))
                {
                    shownTooEarly.Add($"{pair.Key}.{tagged.Name} (v{tagged.MinimumGumxVersion}) shown on v{_project.Version}");
                }
            }
        }

        shownTooEarly.ShouldBeEmpty();
    }
}
