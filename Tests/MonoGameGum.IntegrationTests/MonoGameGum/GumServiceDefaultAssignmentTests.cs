using Gum.DataTypes;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// IGumService.Default is the one current-service pointer. Update and Draw make their service the default
/// for the length of the call and put the previous default back afterward.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class GumServiceDefaultAssignmentTests : BaseTestClass
{
    private readonly string _root;

    public GumServiceDefaultAssignmentTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumDefaultAssignment_" + Guid.NewGuid().ToString("N"));
    }

    public override void Dispose()
    {
        base.Dispose();
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private string WriteProject(string name)
    {
        GumProjectFolder folder = new GumProjectFolder(Path.Combine(_root, name));
        folder.Add(new GumElement("MainScreen", isScreen: true));
        return folder.Write();
    }

    private CollisionGame CreateTwoServices()
    {
        CollisionGame game = new CollisionGame();
        game.RunOneFrame();
        // No default yet, so A's Initialize makes A the default and B's leaves it alone.
        IGumService.Default = null;
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));
        return game;
    }

    private static readonly GameTime FrameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60.0));

    [Fact]
    public void Update_ShouldMakeItsServiceTheDefault_ForHandlersFiredInsideIt()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService? seen = null;
        game.ServiceB.DeferredQueue.Enqueue(() => seen = IGumService.Default);
        IGumService.Default = game.ServiceA;

        game.ServiceB.Update(FrameTime);

        seen.ShouldBeSameAs(game.ServiceB);
    }

    [Fact]
    public void Update_ShouldLeaveTheDefaultAsItWasBeforeTheCall()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = game.ServiceA;

        game.ServiceB.Update(FrameTime);

        IGumService.Default.ShouldBeSameAs(game.ServiceA);
    }

    [Fact]
    public void Update_ShouldLeaveANullDefaultNull()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = null;

        game.ServiceB.Update(FrameTime);

        IGumService.Default.ShouldBeNull();
    }

    [Fact]
    public void Update_ShouldUndoADefaultAssignedByAHandler()
    {
        using CollisionGame game = CreateTwoServices();
        game.ServiceB.DeferredQueue.Enqueue(() => IGumService.Default = game.ServiceA);
        IGumService.Default = game.ServiceB;

        game.ServiceB.Update(FrameTime);

        IGumService.Default.ShouldBeSameAs(game.ServiceB, "the assignment inside Update is undone on return");
    }

    [Fact]
    public void Update_ShouldRestoreCorrectly_WhenAHandlerUpdatesAnotherService()
    {
        using CollisionGame game = CreateTwoServices();
        List<string> log = new();
        game.ServiceA.DeferredQueue.Enqueue(() => log.Add("inner sees " + Name(game, IGumService.Default)));
        game.ServiceB.DeferredQueue.Enqueue(() =>
        {
            log.Add("outer before " + Name(game, IGumService.Default));
            game.ServiceA.Update(FrameTime);
            log.Add("outer after " + Name(game, IGumService.Default));
        });
        IGumService.Default = game.ServiceA;

        game.ServiceB.Update(FrameTime);

        log.ShouldBe(new[] { "outer before B", "inner sees A", "outer after B" });
        IGumService.Default.ShouldBeSameAs(game.ServiceA);
    }

    [Fact]
    public void Update_ShouldRestoreTheDefault_WhenItThrows()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = game.ServiceA;

        Should.Throw<InvalidOperationException>(() => game.ServiceB.Update(FrameTime, ThrowingRoots()));

        IGumService.Default.ShouldBeSameAs(game.ServiceA);
    }

    [Fact]
    public void Draw_ShouldLeaveTheDefaultAsItWasBeforeTheCall()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = game.ServiceA;

        game.ServiceB.Draw();

        IGumService.Default.ShouldBeSameAs(game.ServiceA);
    }

    [Fact]
    public void Initialize_WhenNoDefaultExists_ShouldMakeTheServiceTheDefault()
    {
        using CollisionGame game = new CollisionGame();
        game.RunOneFrame();
        IGumService.Default = null;

        game.InitializeA(WriteProject("A"));

        IGumService.Default.ShouldBeSameAs(game.ServiceA);
    }

    [Fact]
    public void Initialize_WhenADefaultExists_ShouldLeaveItAlone()
    {
        using CollisionGame game = CreateTwoServices();

        IGumService.Default.ShouldBeSameAs(game.ServiceA, "the second Initialize never changes the default");
    }

    [Fact]
    public void Initialize_OfASecondService_ShouldRunAsThatService_NotAsTheDefault()
    {
        using CollisionGame game = new CollisionGame();
        game.RunOneFrame();
        GumProjectSave projectA = game.InitializeA(WriteProject("A"));

        GumProjectSave projectB = game.InitializeB(WriteProject("B"));

        // B's project was loaded while B was current, so it is B's, and A still has its own.
        IGumService.Default = game.ServiceA;
        Gum.Managers.ObjectFinder.Self.GumProjectSave.ShouldBeSameAs(projectA);
        IGumService.Default = game.ServiceB;
        Gum.Managers.ObjectFinder.Self.GumProjectSave.ShouldBeSameAs(projectB);
    }

    [Fact]
    public void Initialize_ThatFails_ShouldKeepTheExistingDefault()
    {
        using CollisionGame game = new CollisionGame();
        game.RunOneFrame();
        game.InitializeA(WriteProject("A"));
        string missing = Path.Combine(_root, "Missing", "Project.gumx");

        Should.Throw<Exception>(() => game.ServiceB.Initialize(game, missing));

        IGumService.Default.ShouldBeSameAs(game.ServiceA);
    }

    [Fact]
    public void Initialize_ThatFails_ShouldLeaveANullDefaultNull()
    {
        using CollisionGame game = new CollisionGame();
        game.RunOneFrame();
        IGumService.Default = null;
        string missing = Path.Combine(_root, "Missing", "Project.gumx");

        Should.Throw<Exception>(() => game.ServiceA.Initialize(game, missing));

        IGumService.Default.ShouldBeNull();
    }

    [Fact]
    public void Uninitialize_ShouldClearTheDefault_WhenItIsTheDefault()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = game.ServiceB;

        game.ServiceB.Uninitialize();

        IGumService.Default.ShouldBeNull();
        Gum.GumContext.Current.ShouldBeSameAs(game.ServiceA.Context, "with one service left and no default, it is the current one");
    }

    [Fact]
    public void Uninitialize_ShouldKeepAnotherDefault_WhenItIsNotTheDefault()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = game.ServiceA;

        game.ServiceB.Uninitialize();

        IGumService.Default.ShouldBeSameAs(game.ServiceA);
    }

    [Fact]
    public void AssigningTheDefault_ShouldMoveSystemManagersDefaultAndISystemManagersDefault()
    {
        using CollisionGame game = CreateTwoServices();

        IGumService.Default = game.ServiceB;
        SystemManagers.Default.ShouldBeSameAs(game.ServiceB.SystemManagers);
        ISystemManagers.Default.ShouldBeSameAs(game.ServiceB.SystemManagers);

        IGumService.Default = game.ServiceA;
        SystemManagers.Default.ShouldBeSameAs(game.ServiceA.SystemManagers);
        ISystemManagers.Default.ShouldBeSameAs(game.ServiceA.SystemManagers);
    }

    [Fact]
    public void Update_ShouldPutTheManagersDefaultsBack_WithThePreviousDefault()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = game.ServiceA;
        ISystemManagers? seenInside = null;
        game.ServiceB.DeferredQueue.Enqueue(() => seenInside = ISystemManagers.Default);

        game.ServiceB.Update(FrameTime);

        seenInside.ShouldBeSameAs(game.ServiceB.SystemManagers);
        ISystemManagers.Default.ShouldBeSameAs(game.ServiceA.SystemManagers);
        SystemManagers.Default.ShouldBeSameAs(game.ServiceA.SystemManagers);
    }

    [Fact]
    public void HandAssigningTheManagersDefaults_ShouldStillWork_UntilTheDefaultChanges()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = game.ServiceA;
        SystemManagers hostOwned = game.ServiceB.SystemManagers;

        SystemManagers.Default = hostOwned;
        ISystemManagers.Default = hostOwned;

        SystemManagers.Default.ShouldBeSameAs(hostOwned);
        ISystemManagers.Default.ShouldBeSameAs(hostOwned);
    }

    private static string Name(CollisionGame game, IGumService? service) =>
        ReferenceEquals(service, game.ServiceA) ? "A" : ReferenceEquals(service, game.ServiceB) ? "B" : "other";

    private static IEnumerable<GraphicalUiElement> ThrowingRoots()
    {
        throw new InvalidOperationException("roots failed");
#pragma warning disable CS0162 // iterator needs a yield
        yield break;
#pragma warning restore CS0162
    }
}
