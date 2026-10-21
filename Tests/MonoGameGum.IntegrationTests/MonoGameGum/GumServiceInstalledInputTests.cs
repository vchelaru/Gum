using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Moq;
using RenderingLibrary;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Pins which cursor and keyboard each accessor returns after a real <c>Initialize</c>, after
/// <see cref="FormsUtilities.SetCursor"/> / <see cref="FormsUtilities.SetKeyboard"/> installs a custom one,
/// and across several services (#6322). The first service keeps its input in the process statics, a later
/// service in its own context, and a service that is not initialized has none.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class GumServiceInstalledInputTests : BaseTestClass
{
    private readonly string _root;

    public GumServiceInstalledInputTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumInstalledInput_" + Guid.NewGuid().ToString("N"));
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

    private CollisionGame CreateGame()
    {
        CollisionGame game = new CollisionGame();
        game.RunOneFrame();
        IGumService.Default = null;
        return game;
    }

    private CollisionGame CreateOneService()
    {
        CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        return game;
    }

    private CollisionGame CreateTwoServices()
    {
        CollisionGame game = CreateOneService();
        game.InitializeB(WriteProject("B"));
        return game;
    }

    private static readonly GameTime FrameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60.0));

    #region One service, default install

    [Fact]
    public void Initialize_InstallsBuiltInCursorAndKeyboard_ReturnedByEveryAccessor()
    {
        using CollisionGame game = CreateOneService();
        Gum.GumService service = game.ServiceA;

        Cursor cursor = service.Cursor.ShouldNotBeNull();
        Keyboard keyboard = service.Keyboard.ShouldNotBeNull();
        ((IGumService)service).Cursor.ShouldBeSameAs(cursor);
        FormsUtilities.Cursor.ShouldBeSameAs(cursor);
        FormsUtilities.Keyboard.ShouldBeSameAs(keyboard);
        FrameworkElement.MainCursor.ShouldBeSameAs(cursor);
        FrameworkElement.MainKeyboard.ShouldBeSameAs(keyboard);
    }

    [Fact]
    public void SetCursor_WithCustomCursor_ConcreteAccessorIsNullAndFormsUtilitiesReturnsIt()
    {
        using CollisionGame game = CreateOneService();
        ICursor custom = new Mock<ICursor>().Object;

        FormsUtilities.SetCursor(custom);

        game.ServiceA.Cursor.ShouldBeNull();
        FormsUtilities.Cursor.ShouldBeSameAs(custom);
        FrameworkElement.MainCursor.ShouldBeSameAs(custom);
    }

    [Fact]
    public void SetKeyboard_WithCustomKeyboard_ConcreteAccessorIsNullAndFormsUtilitiesReturnsIt()
    {
        using CollisionGame game = CreateOneService();
        IInputReceiverKeyboard custom = new Mock<IInputReceiverKeyboard>().Object;

        FormsUtilities.SetKeyboard(custom);

        game.ServiceA.Keyboard.ShouldBeNull();
        FormsUtilities.Keyboard.ShouldBeSameAs(custom);
        FrameworkElement.MainKeyboard.ShouldBeSameAs(custom);
    }

    [Fact]
    public void SetCursor_WithCustomCursor_InterfaceAccessorIsNullToday()
    {
        // Pins today's behavior: IGumService.Cursor forwards to the concrete property (#6322 flips it).
        using CollisionGame game = CreateOneService();

        FormsUtilities.SetCursor(new Mock<ICursor>().Object);

        ((IGumService)game.ServiceA).Cursor.ShouldBeNull();
    }

    [Fact]
    public void SetCursor_ReplacingCustomWithTheOriginalBuiltIn_RestoresEveryAccessor()
    {
        using CollisionGame game = CreateOneService();
        Cursor original = game.ServiceA.Cursor;
        FormsUtilities.SetCursor(new Mock<ICursor>().Object);

        FormsUtilities.SetCursor(original);

        game.ServiceA.Cursor.ShouldBeSameAs(original);
        ((IGumService)game.ServiceA).Cursor.ShouldBeSameAs(original);
        FormsUtilities.Cursor.ShouldBeSameAs(original);
    }

    [Fact]
    public void SetKeyboard_ReplacingCustomWithTheOriginalBuiltIn_RestoresEveryAccessor()
    {
        using CollisionGame game = CreateOneService();
        Keyboard original = game.ServiceA.Keyboard;
        FormsUtilities.SetKeyboard(new Mock<IInputReceiverKeyboard>().Object);

        FormsUtilities.SetKeyboard(original);

        game.ServiceA.Keyboard.ShouldBeSameAs(original);
        FormsUtilities.Keyboard.ShouldBeSameAs(original);
    }

    [Fact]
    public void SetCursor_ReplacingOneBuiltInWithAnother_ConcreteAccessorReturnsTheNewOne()
    {
        using CollisionGame game = CreateOneService();
        Cursor replacement = new Cursor(null);

        FormsUtilities.SetCursor(replacement);

        game.ServiceA.Cursor.ShouldBeSameAs(replacement);
        ((IGumService)game.ServiceA).Cursor.ShouldBeSameAs(replacement);
        FormsUtilities.Cursor.ShouldBeSameAs(replacement);
    }

    [Fact]
    public void Update_WithCustomCursorAndKeyboard_TicksThemAndNotTheBuiltIns()
    {
        using CollisionGame game = CreateOneService();
        Mock<ICursor> cursor = new Mock<ICursor>();
        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        FormsUtilities.SetCursor(cursor.Object);
        FormsUtilities.SetKeyboard(keyboard.Object);

        game.ServiceA.Update(FrameTime);

        cursor.Verify(c => c.Activity(It.IsAny<double>()), Times.Once());
        keyboard.Verify(k => k.Activity(It.IsAny<double>()), Times.Once());
    }

    #endregion

    #region One service, lifecycle

    [Fact]
    public void Uninitialize_ClearsEveryAccessor()
    {
        using CollisionGame game = CreateOneService();

        game.ServiceA.Uninitialize();

        game.ServiceA.Cursor.ShouldBeNull();
        game.ServiceA.Keyboard.ShouldBeNull();
        ((IGumService)game.ServiceA).Cursor.ShouldBeNull();
        FormsUtilities.Cursor.ShouldBeNull();
        FormsUtilities.Keyboard.ShouldBeNull();
    }

    [Fact]
    public void Uninitialize_WithCustomInputInstalled_ClearsEveryAccessor()
    {
        using CollisionGame game = CreateOneService();
        FormsUtilities.SetCursor(new Mock<ICursor>().Object);
        FormsUtilities.SetKeyboard(new Mock<IInputReceiverKeyboard>().Object);

        game.ServiceA.Uninitialize();

        game.ServiceA.Cursor.ShouldBeNull();
        game.ServiceA.Keyboard.ShouldBeNull();
        FormsUtilities.Cursor.ShouldBeNull();
        FormsUtilities.Keyboard.ShouldBeNull();
    }

    [Fact]
    public void Initialize_AfterUninitialize_InstallsFreshBuiltInInputAndDropsTheCustomOne()
    {
        using CollisionGame game = CreateOneService();
        Cursor firstCursor = game.ServiceA.Cursor;
        Keyboard firstKeyboard = game.ServiceA.Keyboard;
        FormsUtilities.SetCursor(new Mock<ICursor>().Object);
        FormsUtilities.SetKeyboard(new Mock<IInputReceiverKeyboard>().Object);
        game.ServiceA.Uninitialize();

        game.ServiceA.Initialize(game, WriteProject("A2"));

        Cursor secondCursor = game.ServiceA.Cursor.ShouldNotBeNull();
        Keyboard secondKeyboard = game.ServiceA.Keyboard.ShouldNotBeNull();
        secondCursor.ShouldNotBeSameAs(firstCursor);
        secondKeyboard.ShouldNotBeSameAs(firstKeyboard);
        ((IGumService)game.ServiceA).Cursor.ShouldBeSameAs(secondCursor);
        FormsUtilities.Cursor.ShouldBeSameAs(secondCursor);
        FormsUtilities.Keyboard.ShouldBeSameAs(secondKeyboard);
    }

    [Fact]
    public void NewService_WithNoServiceLive_ReadsTheProcessStatics()
    {
        // With no live service, an uninitialized service has nothing of its own and reads the process-wide slots.
        using CollisionGame game = CreateGame();
        Cursor staticCursor = new Cursor(null);
        Keyboard staticKeyboard = new Keyboard();
        FrameworkElement.MainCursor = staticCursor;
        FrameworkElement.MainKeyboard = staticKeyboard;

        game.ServiceA.Cursor.ShouldBeSameAs(staticCursor);
        game.ServiceA.Keyboard.ShouldBeSameAs(staticKeyboard);
        ((IGumService)game.ServiceA).Cursor.ShouldBeSameAs(staticCursor);
    }

    #endregion

    #region Several services

    [Fact]
    public void TwoServices_EachReturnsItsOwnBuiltInInput_WhicheverIsTheDefault()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = game.ServiceA;
        Cursor cursorA = game.ServiceA.Cursor.ShouldNotBeNull();
        Keyboard keyboardA = game.ServiceA.Keyboard.ShouldNotBeNull();
        IGumService.Default = game.ServiceB;
        Cursor cursorB = game.ServiceB.Cursor.ShouldNotBeNull();
        Keyboard keyboardB = game.ServiceB.Keyboard.ShouldNotBeNull();
        cursorB.ShouldNotBeSameAs(cursorA, "setup");
        keyboardB.ShouldNotBeSameAs(keyboardA, "setup");

        foreach (IGumService defaultService in new IGumService[] { game.ServiceA, game.ServiceB })
        {
            IGumService.Default = defaultService;

            game.ServiceA.Cursor.ShouldBeSameAs(cursorA);
            game.ServiceA.Keyboard.ShouldBeSameAs(keyboardA);
            game.ServiceB.Cursor.ShouldBeSameAs(cursorB);
            game.ServiceB.Keyboard.ShouldBeSameAs(keyboardB);
            ((IGumService)game.ServiceA).Cursor.ShouldBeSameAs(cursorA);
            ((IGumService)game.ServiceB).Cursor.ShouldBeSameAs(cursorB);
            FormsUtilities.Cursor.ShouldBeSameAs(defaultService == game.ServiceA ? cursorA : cursorB);
            FormsUtilities.Keyboard.ShouldBeSameAs(defaultService == game.ServiceA ? keyboardA : keyboardB);
        }
    }

    [Fact]
    public void TwoServices_ReadingEitherAccessor_DoesNotSwitchTheDefault()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = game.ServiceA;

        _ = game.ServiceB.Cursor;
        _ = game.ServiceB.Keyboard;
        _ = ((IGumService)game.ServiceB).Cursor;

        IGumService.Default.ShouldBeSameAs(game.ServiceA);
    }

    [Fact]
    public void TwoServices_CustomCursorOnTheDefaultSecondService_AffectsOnlyThatService()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = game.ServiceA;
        Cursor cursorA = game.ServiceA.Cursor;
        IGumService.Default = game.ServiceB;
        ICursor custom = new Mock<ICursor>().Object;

        FormsUtilities.SetCursor(custom);

        game.ServiceB.Cursor.ShouldBeNull();
        FormsUtilities.Cursor.ShouldBeSameAs(custom);
        game.ServiceA.Cursor.ShouldBeSameAs(cursorA);
        IGumService.Default = game.ServiceA;
        FormsUtilities.Cursor.ShouldBeSameAs(cursorA);
        game.ServiceB.Cursor.ShouldBeNull("B still has the custom cursor while A is the default");
        IGumService.Default = game.ServiceB;
        FormsUtilities.Cursor.ShouldBeSameAs(custom);
    }

    [Fact]
    public void TwoServices_CustomKeyboardOnTheDefaultSecondService_AffectsOnlyThatService()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = game.ServiceA;
        Keyboard keyboardA = game.ServiceA.Keyboard;
        IGumService.Default = game.ServiceB;
        IInputReceiverKeyboard custom = new Mock<IInputReceiverKeyboard>().Object;

        FormsUtilities.SetKeyboard(custom);

        game.ServiceB.Keyboard.ShouldBeNull();
        FormsUtilities.Keyboard.ShouldBeSameAs(custom);
        game.ServiceA.Keyboard.ShouldBeSameAs(keyboardA);
        IGumService.Default = game.ServiceA;
        FormsUtilities.Keyboard.ShouldBeSameAs(keyboardA);
    }

    [Fact]
    public void TwoServices_CustomCursorOnTheFirstService_AffectsOnlyThatService()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = game.ServiceB;
        Cursor cursorB = game.ServiceB.Cursor;
        IGumService.Default = game.ServiceA;
        ICursor custom = new Mock<ICursor>().Object;

        FormsUtilities.SetCursor(custom);

        game.ServiceA.Cursor.ShouldBeNull();
        FormsUtilities.Cursor.ShouldBeSameAs(custom);
        game.ServiceB.Cursor.ShouldBeSameAs(cursorB);
        IGumService.Default = game.ServiceB;
        FormsUtilities.Cursor.ShouldBeSameAs(cursorB);
        game.ServiceA.Cursor.ShouldBeNull("A still has the custom cursor while B is the default");
    }

    [Fact]
    public void TwoServices_CustomCursorOnBoth_UpdateTicksOnlyTheUpdatedServicesCursor()
    {
        using CollisionGame game = CreateTwoServices();
        Mock<ICursor> cursorA = new Mock<ICursor>();
        Mock<ICursor> cursorB = new Mock<ICursor>();
        IGumService.Default = game.ServiceA;
        FormsUtilities.SetCursor(cursorA.Object);
        IGumService.Default = game.ServiceB;
        FormsUtilities.SetCursor(cursorB.Object);

        game.ServiceA.Update(FrameTime);

        cursorA.Verify(c => c.Activity(It.IsAny<double>()), Times.Once());
        cursorB.Verify(c => c.Activity(It.IsAny<double>()), Times.Never());
        IGumService.Default.ShouldBeSameAs(game.ServiceB, "Update puts the previous default back");
    }

    [Fact]
    public void TwoServices_AnUninitializedThirdService_HasNoCursorOrKeyboard()
    {
        using CollisionGame game = CreateTwoServices();
        Gum.GumService third = new Gum.GumService();

        third.Cursor.ShouldBeNull();
        third.Keyboard.ShouldBeNull();
        ((IGumService)third).Cursor.ShouldBeNull();

        IGumService.Default = game.ServiceA;
        FormsUtilities.SetCursor(new Mock<ICursor>().Object);

        third.Cursor.ShouldBeNull();
        ((IGumService)third).Cursor.ShouldBeNull();
        IGumService.Default.ShouldBeSameAs(game.ServiceA);
    }

    [Fact]
    public void TwoServices_UninitializingTheSecond_LeavesTheFirstServicesInputAlone()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = game.ServiceA;
        Cursor cursorA = game.ServiceA.Cursor;
        Keyboard keyboardA = game.ServiceA.Keyboard;

        game.ServiceB.Uninitialize();

        game.ServiceA.Cursor.ShouldBeSameAs(cursorA);
        game.ServiceA.Keyboard.ShouldBeSameAs(keyboardA);
        ((IGumService)game.ServiceA).Cursor.ShouldBeSameAs(cursorA);
        FormsUtilities.Cursor.ShouldBeSameAs(cursorA);
        game.ServiceB.Cursor.ShouldBeNull();
        game.ServiceB.Keyboard.ShouldBeNull();
        ((IGumService)game.ServiceB).Cursor.ShouldBeNull();
    }

    [Fact]
    public void TwoServices_UninitializingTheFirst_LeavesTheSecondServicesInputAlone()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = game.ServiceB;
        Cursor cursorB = game.ServiceB.Cursor;
        Keyboard keyboardB = game.ServiceB.Keyboard;

        game.ServiceA.Uninitialize();

        game.ServiceA.Cursor.ShouldBeNull();
        game.ServiceA.Keyboard.ShouldBeNull();
        ((IGumService)game.ServiceA).Cursor.ShouldBeNull();
        IGumService.Default = game.ServiceB;
        game.ServiceB.Cursor.ShouldBeSameAs(cursorB);
        game.ServiceB.Keyboard.ShouldBeSameAs(keyboardB);
        ((IGumService)game.ServiceB).Cursor.ShouldBeSameAs(cursorB);
        FormsUtilities.Cursor.ShouldBeSameAs(cursorB);
    }

    [Fact]
    public void TwoServices_DefaultSetToNull_ServiceAccessorsStillAnswerButFormsUtilitiesThrows()
    {
        using CollisionGame game = CreateTwoServices();
        IGumService.Default = game.ServiceA;
        Cursor cursorA = game.ServiceA.Cursor;
        Cursor cursorB = game.ServiceB.Cursor;

        IGumService.Default = null;

        game.ServiceA.Cursor.ShouldBeSameAs(cursorA);
        game.ServiceB.Cursor.ShouldBeSameAs(cursorB);
        ((IGumService)game.ServiceB).Cursor.ShouldBeSameAs(cursorB);
        Should.Throw<InvalidOperationException>(() => FormsUtilities.Cursor);
    }

    #endregion
}
