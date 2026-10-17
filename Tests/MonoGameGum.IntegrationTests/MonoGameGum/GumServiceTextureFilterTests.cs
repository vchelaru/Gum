using Gum.DataTypes;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// The project's texture filter (#6066): the first service sets the process-wide <see cref="Renderer.TextureFilter"/>
/// as before; a later service keeps its own on its renderer, so loading it never changes what the first draws with.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class GumServiceTextureFilterTests : BaseTestClass
{
    private readonly string _root;

    public GumServiceTextureFilterTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumTextureFilter_" + Guid.NewGuid().ToString("N"));
    }

    public override void Dispose()
    {
        base.Dispose();
        Renderer.TextureFilter = TextureFilter.Point;
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private string WriteProject(string name, string textureFilter)
    {
        GumProjectFolder folder = new GumProjectFolder(Path.Combine(_root, name));
        folder.TextureFilter = textureFilter;
        folder.Add(new GumElement("MainScreen", isScreen: true));
        return folder.Write();
    }

    private static CollisionGame CreateGame()
    {
        CollisionGame game = new CollisionGame();
        game.RunOneFrame();
        return game;
    }

    [Fact]
    public void Initialize_SecondServiceWithPointFilter_ShouldNotChangeTheProcessWideFilter()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A", "Linear"));
        game.InitializeB(WriteProject("B", "Point"));

        IGumService.Default = game.ServiceB;

        Renderer.TextureFilter.ShouldBe(TextureFilter.Linear);
    }

    [Fact]
    public void Initialize_TwoServices_ShouldGiveEachRendererItsOwnProjectFilter()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A", "Linear"));
        game.InitializeB(WriteProject("B", "Point"));

        game.ServiceA.Renderer.EffectiveTextureFilter.ShouldBe(TextureFilter.Linear);
        game.ServiceB.Renderer.EffectiveTextureFilter.ShouldBe(TextureFilter.Point);
    }

    [Fact]
    public void Initialize_SingleService_ShouldSetTheProcessWideFilterAndNoOverride()
    {
        using CollisionGame game = CreateGame();

        game.InitializeA(WriteProject("A", "Linear"));

        Renderer.TextureFilter.ShouldBe(TextureFilter.Linear);
        game.ServiceA.Renderer.TextureFilterOverride.ShouldBeNull();
    }
}
