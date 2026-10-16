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
/// State that does not depend on the loaded project is shared by every service (#6066): the embedded default
/// font and the invalid-texture placeholder. They go away when the last service leaves, not before.
/// </summary>
public class GumServiceSharedDefaultsTests : BaseTestClass
{
    private readonly string _root;

    public GumServiceSharedDefaultsTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumSharedDefaults_" + Guid.NewGuid().ToString("N"));
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

    private static CollisionGame CreateGame()
    {
        CollisionGame game = new CollisionGame();
        game.RunOneFrame();
        return game;
    }

    [Fact]
    public void DefaultBitmapFont_WhenTheDefaultServiceSwitches_ShouldBeTheSameFont()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));

        IGumService.Default = game.ServiceA;
        BitmapFont? underA = Text.DefaultBitmapFont;
        IGumService.Default = game.ServiceB;
        BitmapFont? underB = Text.DefaultBitmapFont;

        underA.ShouldNotBeNull();
        underB.ShouldBeSameAs(underA);
    }

    [Fact]
    public void Uninitialize_FirstServiceWhileSecondIsLive_ShouldKeepDefaultFontAndInvalidTexture()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));
        IGumService.Default = game.ServiceA;
        Texture2D invalid = new Texture2D(game.GraphicsDevice, 1, 1);
        Sprite.InvalidTexture = invalid;

        game.ServiceA.Uninitialize();
        IGumService.Default = game.ServiceB;

        Text.DefaultBitmapFont.ShouldNotBeNull();
        Sprite.InvalidTexture.ShouldBeSameAs(invalid);
        invalid.IsDisposed.ShouldBeFalse();
    }

    [Fact]
    public void Uninitialize_LastService_ShouldClearDefaultFontAndInvalidTexture()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        Text.DefaultBitmapFont.ShouldNotBeNull("setup");
        Texture2D invalid = new Texture2D(game.GraphicsDevice, 1, 1);
        Sprite.InvalidTexture = invalid;

        game.ServiceA.Uninitialize();

        Text.DefaultBitmapFont.ShouldBeNull();
        Sprite.InvalidTexture.ShouldBeNull();
        invalid.IsDisposed.ShouldBeTrue();
    }
}
