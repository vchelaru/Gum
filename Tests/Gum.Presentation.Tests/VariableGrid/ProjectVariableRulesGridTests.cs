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
/// A project's variable rules hide standard variables in the Variables tab for both an element's own
/// variables and an instance's (#7720). Structural variables stay visible and stored values are kept.
/// </summary>
public class ProjectVariableRulesGridTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();
    private readonly ElementSaveDisplayer _displayer;
    private readonly GumProjectSave _project;
    private readonly EngineProfile _profile = new EngineProfile();
    private readonly ScreenSave _screen;
    private readonly StateSave _screenDefaultState;

    public ProjectVariableRulesGridTests()
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
        _mocker.Use<IProjectRestrictions>(TestProjectRestrictions.Create(_profile));

        _mocker.GetMock<IProjectState>()
            .Setup(x => x.GumProjectSave)
            .Returns(_project);

        _displayer = _mocker.CreateInstance<ElementSaveDisplayer>();
    }

    private void RestrictToSpritePositionAndTexture()
    {
        _profile.HideVariablesByDefault = true;
        _profile.VariableRules.Add(new VariableRule { Pattern = "Sprite.X", Action = VariableRuleAction.Show });
        _profile.VariableRules.Add(new VariableRule { Pattern = "Sprite.Y", Action = VariableRuleAction.Show });
        _profile.VariableRules.Add(new VariableRule { Pattern = "Sprite.SourceFile", Action = VariableRuleAction.Show });
    }

    private List<string> GetInstanceRootNames(string baseType, out InstanceSave instance)
    {
        instance = new InstanceSave { Name = "Instance", BaseType = baseType, ParentContainer = _screen };
        _screen.Instances.Add(instance);

        return _displayer.GetCategories(_screen, instance, _screenDefaultState, null)
            .SelectMany(category => category.Members)
            .Select(member => member.RootVariableName)
            .ToList();
    }

    [Fact]
    public void GetCategories_ShouldShowOnlyAllowedVariables_OnSpriteInstance()
    {
        RestrictToSpritePositionAndTexture();

        List<string> names = GetInstanceRootNames("Sprite", out _);

        names.ShouldContain("X");
        names.ShouldContain("Y");
        names.ShouldContain("SourceFile");
        names.ShouldNotContain("Width");
        names.ShouldNotContain("Rotation");
        names.ShouldNotContain("Alpha");
    }

    [Fact]
    public void GetCategories_ShouldHideParentAndVariableReferences_WhenDefaultIsHide()
    {
        RestrictToSpritePositionAndTexture();

        List<string> names = GetInstanceRootNames("Sprite", out _);

        names.ShouldNotContain("Parent");
        names.ShouldNotContain("VariableReferences");
    }

    [Fact]
    public void GetCategories_ShouldKeepParent_WhenRulesShowIt()
    {
        RestrictToSpritePositionAndTexture();
        _profile.VariableRules.Add(new VariableRule { Pattern = "Parent", Action = VariableRuleAction.Show });

        List<string> names = GetInstanceRootNames("Sprite", out _);

        names.ShouldContain("Parent");
    }

    [Fact]
    public void GetCategories_ShouldShowOnlyAllowedVariables_OnElementBasedOnSprite()
    {
        RestrictToSpritePositionAndTexture();
        ComponentSave component = new ComponentSave { Name = "Wing", BaseType = "Sprite" };
        StateSave componentState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(componentState);
        _project.Components.Add(component);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(componentState);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(component);

        List<string> names = _displayer.GetCategories(component, null, componentState, null)
            .SelectMany(category => category.Members)
            .Select(member => member.RootVariableName)
            .ToList();

        names.ShouldContain("X");
        names.ShouldNotContain("Width");
        names.ShouldNotContain("Rotation");
    }

    [Fact]
    public void GetCategories_ShouldKeepStoredValueOfHiddenVariable()
    {
        RestrictToSpritePositionAndTexture();
        InstanceSave instance = new InstanceSave { Name = "Instance", BaseType = "Sprite", ParentContainer = _screen };
        _screen.Instances.Add(instance);
        _screenDefaultState.SetValue("Instance.Rotation", 45f, "float");

        List<string> names = _displayer.GetCategories(_screen, instance, _screenDefaultState, null)
            .SelectMany(category => category.Members)
            .Select(member => member.RootVariableName)
            .ToList();

        names.ShouldNotContain("Rotation");
        _screenDefaultState.GetValue("Instance.Rotation").ShouldBe(45f);
    }

    [Fact]
    public void GetCategories_ShouldShowEverything_WhenProjectHasNoRules()
    {
        List<string> names = GetInstanceRootNames("Sprite", out _);

        names.ShouldContain("Width");
        names.ShouldContain("Rotation");
    }
}
