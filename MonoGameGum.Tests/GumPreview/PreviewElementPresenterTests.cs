using System.Linq;
using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Wireframe;
using GumPreview;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.GumPreview;

/// <summary>
/// What Preview shows for a project that is broken on disk (a text editor edit, a missing file). Showing
/// runs on the game thread from the first load and from every selection change, and only the reload
/// path catches, so a build that throws crashes Preview.
/// </summary>
public class PreviewElementPresenterTests : BaseTestClass
{
    private GumProjectSave _project = null!;

    public PreviewElementPresenterTests()
    {
        _project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_project);
        ObjectFinder.Self.GumProjectSave = _project;
    }

    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        GumService.Default.Root.Children.Clear();
        base.Dispose();
    }

    private ComponentSave AddComponent(string name, string baseType = "Container")
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        _project.Components.Add(component);
        return component;
    }

    private ScreenSave AddScreen(string name)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        _project.Screens.Add(screen);
        return screen;
    }

    private static void AddInstance(ElementSave parent, string name, string baseType)
    {
        parent.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = parent });
    }

    [Fact]
    public void Show_ScreenWithAnInstanceOfAMissingComponent_DoesNotThrowAndShowsTheScreen()
    {
        ScreenSave screen = AddScreen("MainScreen");
        AddInstance(screen, "Gone", "DeletedComponent");

        bool shown = false;
        Should.NotThrow(() => shown = PreviewElementPresenter.Show(new PreviewSelectionMessage("MainScreen")));

        shown.ShouldBeTrue();
        GumService.Default.Root.Children.Count.ShouldBe(1);
    }

    [Fact]
    public void Show_ComponentWhoseBaseTypeIsMissing_DoesNotThrow()
    {
        AddComponent("Orphan", "DeletedBase");

        Should.NotThrow(() => PreviewElementPresenter.Show(new PreviewSelectionMessage("Orphan")));
    }

    [Fact]
    public void Show_ComponentInANestedFolder_ShowsIt()
    {
        AddComponent("Controls/Buttons/Primary");

        PreviewElementPresenter.Show(new PreviewSelectionMessage("Controls/Buttons/Primary")).ShouldBeTrue();

        GumService.Default.Root.Children.Single().ElementSave!.Name.ShouldBe("Controls/Buttons/Primary");
    }

    [Fact]
    public void Show_TheSameElementTwice_LeavesOneTree()
    {
        AddScreen("MainScreen");
        PreviewSelectionMessage selection = new PreviewSelectionMessage("MainScreen");

        PreviewElementPresenter.Show(selection);
        PreviewElementPresenter.Show(selection);

        GumService.Default.Root.Children.Count.ShouldBe(1);
    }

    [Fact]
    public void Show_AnElementThatDoesNotExist_ReturnsFalseAndClearsThePreviousTree()
    {
        AddScreen("MainScreen");
        PreviewElementPresenter.Show(new PreviewSelectionMessage("MainScreen")).ShouldBeTrue();

        PreviewElementPresenter.Show(new PreviewSelectionMessage("NoSuchScreen")).ShouldBeFalse();

        GumService.Default.Root.Children.Count.ShouldBe(0);
    }

    [Fact]
    public void Show_ASelectedStateThatNoLongerExists_ShowsTheDefaultStateWithoutThrowing()
    {
        AddScreen("MainScreen");
        PreviewSelectionMessage selection = new PreviewSelectionMessage("MainScreen") { CategoryName = "GoneCategory", StateName = "GoneState" };

        bool shown = false;
        Should.NotThrow(() => shown = PreviewElementPresenter.Show(selection));

        shown.ShouldBeTrue();
    }

    [Fact]
    public void Show_ASpriteWhoseTextureFileIsMissing_DoesNotThrow()
    {
        ComponentSave component = AddComponent("WithSprite");
        AddInstance(component, "Pic", "Sprite");
        component.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Pic.SourceFile",
            Type = "string",
            Value = "does/not/exist.png",
            SetsValue = true,
        });

        Should.NotThrow(() => PreviewElementPresenter.Show(new PreviewSelectionMessage("WithSprite")));
    }
}
