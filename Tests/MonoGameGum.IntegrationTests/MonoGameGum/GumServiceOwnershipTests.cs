using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// With two or more live services, adding an element built under one service to another service's tree
/// throws, and resolving the current service with no default assigned throws. With one service neither
/// ever does.
/// </summary>
public class GumServiceOwnershipTests : BaseTestClass
{
    private readonly string _root;

    public GumServiceOwnershipTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumOwnership_" + Guid.NewGuid().ToString("N"));
    }

    public override void Dispose()
    {
        base.Dispose();
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private GumProjectFolder NewFolder(string name)
    {
        GumProjectFolder folder = new GumProjectFolder(Path.Combine(_root, name));
        folder.Add(new GumElement("MainScreen", isScreen: true));
        return folder;
    }

    private string WriteProject(string name) => NewFolder(name).Write();

    private static CollisionGame CreateGame()
    {
        CollisionGame game = new CollisionGame();
        game.RunOneFrame();
        return game;
    }

    #region The wrong-service add throws

    [Fact]
    public void AddingAControlBuiltUnderAnotherService_ShouldThrow_NamingBothServices()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));
        IGumService.Default = game.ServiceB;
        Button builtUnderB = new Button();
        string builtUnder = game.ServiceB.Context!.Describe();
        string addedUnder = game.ServiceA.Context!.Describe();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => game.ServiceA.Root.Children.Add(builtUnderB.Visual));

        exception.Message.ShouldContain(builtUnder);
        exception.Message.ShouldContain(addedUnder);
        exception.Message.ShouldContain("IGumService.Default = " + addedUnder);
    }

    [Fact]
    public void AddingToTheServiceThatBuiltIt_ShouldNotThrow()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));
        IGumService.Default = game.ServiceB;
        Button builtUnderB = new Button();

        Should.NotThrow(() => game.ServiceB.Root.Children.Add(builtUnderB.Visual));
    }

    [Fact]
    public void AddingAControlBuiltAfterAssigningTheDefault_ShouldNotThrow()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));

        IGumService.Default = game.ServiceA;
        Button forA = new Button();
        IGumService.Default = game.ServiceB;
        Button forB = new Button();

        Should.NotThrow(() =>
        {
            game.ServiceA.Root.Children.Add(forA.Visual);
            game.ServiceB.Root.Children.Add(forB.Visual);
        });
    }

    [Fact]
    public void AddingADetachedElementBuiltForAnotherService_ShouldThrow()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        GumProjectSave projectB = game.InitializeB(WriteProject("B"));
        ScreenSave screen = projectB.Screens.First(item => item.Name == "MainScreen");
        GraphicalUiElement detachedForB = screen.ToGraphicalUiElement(game.ServiceB.SystemManagers, addToManagers: false);

        Should.Throw<InvalidOperationException>(() => game.ServiceA.Root.Children.Add(detachedForB));
    }

    [Fact]
    public void ParentAssignment_ShouldThrowForTheWrongService_AsWellAsChildrenAdd()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));
        IGumService.Default = game.ServiceB;
        Button builtUnderB = new Button();

        Should.Throw<InvalidOperationException>(() => builtUnderB.Visual.Parent = game.ServiceA.Root);
    }

    #endregion

    #region One service never throws

    [Fact]
    public void WithOneService_AddingAnything_ShouldNeverThrow()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        IGumService.Default = game.ServiceA;
        Button button = new Button();

        Should.NotThrow(() => game.ServiceA.Root.Children.Add(button.Visual));
    }

    [Fact]
    public void WithOneServiceLeft_AnElementBuiltEarlierUnderTheOtherService_ShouldNotThrow()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));
        IGumService.Default = game.ServiceB;
        Button builtUnderB = new Button();
        game.ServiceB.Uninitialize();
        IGumService.Default = game.ServiceA;

        // B is gone, so the check no longer applies and the element has no live owner to conflict with.
        Should.NotThrow(() => game.ServiceA.Root.Children.Add(builtUnderB.Visual));
    }

    #endregion

    #region No default with two or more services

    [Fact]
    public void ResolvingTheCurrentService_WithTwoServicesAndNoDefault_ShouldThrowAClearError()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));
        IGumService.Default = null;

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => ObjectFinder.Self);

        exception.Message.ShouldContain("IGumService.Default");
    }

    [Fact]
    public void BuildingAControl_WithTwoServicesAndNoDefault_ShouldThrow()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));
        IGumService.Default = null;

        Should.Throw<InvalidOperationException>(() => new Button());
    }

    [Fact]
    public void ResolvingTheCurrentService_WithOneServiceAndNoDefault_ShouldUseThatService()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        IGumService.Default = null;

        Should.NotThrow(() => ObjectFinder.Self);
        Gum.GumContext.Current.ShouldBeSameAs(game.ServiceA.Context);
    }

    [Fact]
    public void ResolvingTheCurrentService_WithTwoServicesAndADefault_ShouldNotThrow()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));
        IGumService.Default = game.ServiceA;

        Should.NotThrow(() => ObjectFinder.Self);
    }

    #endregion
}
