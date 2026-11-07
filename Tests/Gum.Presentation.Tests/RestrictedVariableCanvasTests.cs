using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;

using static Gum.Presentation.Tests.StateSelectionCanvasHarness;

namespace Gum.Presentation.Tests;

/// <summary>
/// #7720: a variable the project's rules hide is not drawn from its stored value. Drives the real
/// <see cref="WireframeObjectManager.RefreshAll(bool, bool)"/> over elements built with the real
/// <c>ToGraphicalUiElement</c>; the stored value stays in the file.
/// </summary>
public class RestrictedVariableCanvasTests : BaseTestClass
{
    private readonly Func<string, ISystemManagers, IRenderable?>? _previousRenderableFunc;

    public RestrictedVariableCanvasTests()
    {
        _previousRenderableFunc = ElementSaveExtensions.CustomCreateGraphicalComponentFunc;
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = (_, _) => new RecordingRenderable();
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = _previousRenderableFunc;
        base.Dispose();
    }

    private static GumProjectSave BuildSpriteProject(bool withRules)
    {
        GumProjectSave project = BuildProject();
        project.StandardElements.Add(BuildStandard("Sprite"));
        if (withRules)
        {
            project.HideVariablesByDefault = true;
            foreach (string pattern in new[] { "Sprite.X", "Sprite.Y", "Sprite.SourceFile" })
            {
                project.VariableRules.Add(new VariableRule { Pattern = pattern, Action = VariableRuleAction.Show });
            }
        }
        return project;
    }

    private static InstanceSave AddRotatedSprite(ElementSave owner, string name, float rotation)
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = "Sprite", ParentContainer = owner };
        owner.Instances.Add(instance);
        SetValue(owner.DefaultState, name + ".X", 10f);
        SetValue(owner.DefaultState, name + ".Rotation", rotation);
        return instance;
    }

    private static void SetValue(StateSave state, string name, float value)
    {
        state.Variables.Add(new VariableSave { Name = name, Value = value, Type = "float", SetsValue = true });
    }

    private static ScreenSave BuildScreen()
    {
        ScreenSave screen = new ScreenSave { Name = "Level1" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        return screen;
    }

    private static CanvasSession StartSession(ElementSave element, bool withRules) =>
        new CanvasSession(element, element.DefaultState, withRules ? new RestrictedVariableResetter(new ProjectRestrictions()) : null);

    [Fact]
    public void RefreshAll_RuleHidesRotation_InstanceDrawsWithDefaultRotationAndKeepsShownVariables()
    {
        BuildSpriteProject(withRules: true);
        ScreenSave screen = BuildScreen();
        AddRotatedSprite(screen, "Player", 30f);

        CanvasSession session = StartSession(screen, withRules: true);

        GraphicalUiElement player = session.Root!.GetGraphicalUiElementByName("Player")!;
        player.Rotation.ShouldBe(0f);
        player.X.ShouldBe(10f);
        screen.DefaultState.GetValue("Player.Rotation").ShouldBe(30f);
    }

    [Fact]
    public void RefreshAll_NoRules_InstanceKeepsStoredRotation()
    {
        BuildSpriteProject(withRules: false);
        ScreenSave screen = BuildScreen();
        AddRotatedSprite(screen, "Player", 30f);

        CanvasSession session = StartSession(screen, withRules: true);

        session.Root!.GetGraphicalUiElementByName("Player")!.Rotation.ShouldBe(30f);
    }

    [Fact]
    public void RefreshAll_AfterEditingAnotherVariable_RotationStaysDefault()
    {
        BuildSpriteProject(withRules: true);
        ScreenSave screen = BuildScreen();
        AddRotatedSprite(screen, "Player", 30f);
        CanvasSession session = StartSession(screen, withRules: true);

        screen.DefaultState.SetValue("Player.X", 55f, screen.Instances[0]);
        session.Refresh();

        GraphicalUiElement player = session.Root!.GetGraphicalUiElementByName("Player")!;
        player.X.ShouldBe(55f);
        player.Rotation.ShouldBe(0f);
    }

    [Fact]
    public void RefreshAll_RuleHidesRotation_SpriteInsideComponentAndComponentInstanceDrawDefault()
    {
        BuildSpriteProject(withRules: true);
        ComponentSave thing = new ComponentSave { Name = "Thing", BaseType = "Container" };
        thing.States.Add(new StateSave { Name = "Default", ParentContainer = thing });
        AddRotatedSprite(thing, "Inner", 20f);
        ObjectFinder.Self.GumProjectSave!.Components.Add(thing);

        ScreenSave screen = BuildScreen();
        screen.Instances.Add(new InstanceSave { Name = "ThingInstance", BaseType = "Thing", ParentContainer = screen });
        SetValue(screen.DefaultState, "ThingInstance.Rotation", 40f);

        CanvasSession session = StartSession(screen, withRules: true);

        GraphicalUiElement thingInstance = session.Root!.GetGraphicalUiElementByName("ThingInstance")!;
        thingInstance.Rotation.ShouldBe(0f);
        thingInstance.GetGraphicalUiElementByName("Inner")!.Rotation.ShouldBe(0f);
    }

    [Fact]
    public void RefreshAll_RuleHidesRotation_SelectedElementItselfDrawsDefault()
    {
        BuildSpriteProject(withRules: true);
        ComponentSave thing = new ComponentSave { Name = "Thing", BaseType = "Container" };
        thing.States.Add(new StateSave { Name = "Default", ParentContainer = thing });
        SetValue(thing.DefaultState, "Rotation", 25f);
        ObjectFinder.Self.GumProjectSave!.Components.Add(thing);

        CanvasSession session = StartSession(thing, withRules: true);

        session.Root!.Rotation.ShouldBe(0f);
    }
}
