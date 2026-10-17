using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
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
[Collection(SharedGame.CollectionName)]
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

    [Fact]
    public void AdditionalPopupRootPairs_RegisteredUnderOneService_ShouldBeVisibleUnderTheOther()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));
        InteractiveGue popup = new InteractiveGue();
        InteractiveGue modal = new InteractiveGue();

        try
        {
            IGumService.Default = game.ServiceA;
            FrameworkElement.AdditionalPopupRootPairs.Add((popup, modal));
            IGumService.Default = game.ServiceB;

            FrameworkElement.AdditionalPopupRootPairs.ShouldContain((popup, modal));
        }
        finally
        {
            FrameworkElement.AdditionalPopupRootPairs.Clear();
        }
    }

#pragma warning disable CS0618 // DefaultFormsComponents is obsolete but still supported
    [Fact]
    public void DefaultFormsComponents_RegisteredUnderOneService_ShouldBeVisibleUnderTheOther()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));

        IGumService.Default = game.ServiceA;
        FrameworkElement.DefaultFormsComponents[typeof(Button)] = typeof(ContainerRuntime);
        IGumService.Default = game.ServiceB;

        FrameworkElement.DefaultFormsComponents.ShouldContainKey(typeof(Button));
    }

    [Fact]
    public void Uninitialize_FirstServiceWhileSecondIsLive_ShouldKeepDefaultFormsComponents()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));
        IGumService.Default = game.ServiceA;
        FrameworkElement.DefaultFormsComponents[typeof(Button)] = typeof(ContainerRuntime);

        game.ServiceA.Uninitialize();
        IGumService.Default = game.ServiceB;

        FrameworkElement.DefaultFormsComponents.ShouldContainKey(typeof(Button));

        game.ServiceB.Uninitialize();

        FrameworkElement.DefaultFormsComponents.ShouldBeEmpty("the last service out clears them");
    }
#pragma warning restore CS0618
}
