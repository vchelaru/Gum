using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using RenderingLibrary;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// A service's Cursor, Keyboard, Gamepads, PopupRoot and ModalRoot answer for that service whichever one is the
/// current default, and reading them never changes the default (#6066).
/// </summary>
public class GumServiceOwnInputPropertiesTests : BaseTestClass
{
    private readonly string _root;

    public GumServiceOwnInputPropertiesTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumOwnInput_" + Guid.NewGuid().ToString("N"));
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
    public void InstanceProperties_WhenAnotherServiceIsTheDefault_ShouldAnswerForTheirOwnService()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));

        IGumService.Default = game.ServiceA;
        Cursor? cursorA = FormsUtilities.Cursor as Cursor;
        Keyboard? keyboardA = FormsUtilities.Keyboard as Keyboard;
        GamePad[] gamepadsA = FormsUtilities.Gamepads;
        IGumService.Default = game.ServiceB;
        Cursor? cursorB = FormsUtilities.Cursor as Cursor;
        Keyboard? keyboardB = FormsUtilities.Keyboard as Keyboard;
        GamePad[] gamepadsB = FormsUtilities.Gamepads;
        cursorB.ShouldNotBeSameAs(cursorA, "setup");

        IGumService.Default = game.ServiceA;

        game.ServiceB.Cursor.ShouldBeSameAs(cursorB);
        game.ServiceB.Keyboard.ShouldBeSameAs(keyboardB);
        game.ServiceB.Gamepads.ShouldBeSameAs(gamepadsB);
        game.ServiceB.PopupRoot.ShouldNotBeSameAs(game.ServiceA.PopupRoot);
        game.ServiceB.ModalRoot.ShouldNotBeSameAs(game.ServiceA.ModalRoot);
        game.ServiceA.Cursor.ShouldBeSameAs(cursorA);
        game.ServiceA.Keyboard.ShouldBeSameAs(keyboardA);
        game.ServiceA.Gamepads.ShouldBeSameAs(gamepadsA);
        IGumService.Default.ShouldBeSameAs(game.ServiceA, "reading must not switch the current service");
        FrameworkElement.PopupRoot.ShouldBeSameAs(game.ServiceA.PopupRoot);
    }
}
