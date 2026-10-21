using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Localization;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Moq;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// What a game does through the concrete <c>GumService</c> is reachable through <see cref="IGumService"/>
/// with the same result (#6325). <see cref="GumServiceConcreteMembersPinningTests"/> pins the concrete side.
/// </summary>
public class IGumServiceParityTests : BaseTestClass
{
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;
    private readonly GamePad[] _originalGamepads = FormsUtilities._gamepads;

    public override void Dispose()
    {
        FormsUtilities._gamepads = _originalGamepads;
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }
        base.Dispose();
    }

    private static IGumService Service => Gum.GumService.Default;

    [Fact]
    public void PopupRootAndModalRoot_ReturnTheConcreteRoots()
    {
        Service.PopupRoot.ShouldBeSameAs(Gum.GumService.Default.PopupRoot);
        Service.ModalRoot.ShouldBeSameAs(Gum.GumService.Default.ModalRoot);
    }

    [Fact]
    public void Gamepads_ReturnsTheConcreteGamepads()
    {
        GamePad[] gamepads = new GamePad[] { new GamePad(), new GamePad() };
        FormsUtilities._gamepads = gamepads;

        Service.Gamepads.ShouldBeSameAs(gamepads);
    }

    [Fact]
    public void UseKeyboardDefaults_RegistersTheInstalledKeyboard()
    {
        IInputReceiverKeyboard keyboard = new Mock<IInputReceiverKeyboard>().Object;
        FormsUtilities.SetKeyboard(keyboard);

        Service.UseKeyboardDefaults();

        FrameworkElement.KeyboardsForUiControl.ShouldBe(new[] { keyboard });
    }

    [Fact]
    public void UseKeyboardDefaults_WithNoKeyboard_Throws()
    {
        FrameworkElement.MainKeyboard = null!;

        Should.Throw<InvalidOperationException>(() => Service.UseKeyboardDefaults());
    }

    [Fact]
    public void UseGamepadDefaults_RegistersEveryGamepad()
    {
        GamePad first = new GamePad();
        GamePad second = new GamePad();
        FormsUtilities._gamepads = new GamePad[] { first, second };

        Service.UseGamepadDefaults();

        FrameworkElement.GamePadsForUiControl.ShouldBe(new IGamePad[] { first, second });
    }

    [Fact]
    public void RefreshStyles_ReachesRootPopupRootAndModalRoot()
    {
        Button inRoot = new Button();
        Button inPopupRoot = new Button();
        Button inModalRoot = new Button();
        Gum.GumService.Default.Root.Children.Add(inRoot.Visual);
        Gum.GumService.Default.PopupRoot.Children.Add(inPopupRoot.Visual);
        Gum.GumService.Default.ModalRoot.Children.Add(inModalRoot.Visual);
        List<object> saved = new List<object>();
        Action<object>? original = GraphicalUiElement.SaveFormsRuntimePropertiesAction;
        GraphicalUiElement.SaveFormsRuntimePropertiesAction = control => saved.Add(control);

        try
        {
            Service.RefreshStyles();
        }
        finally
        {
            GraphicalUiElement.SaveFormsRuntimePropertiesAction = original;
        }

        saved.ShouldContain(inRoot);
        saved.ShouldContain(inPopupRoot);
        saved.ShouldContain(inModalRoot);
    }

    [Fact]
    public void RefreshStyles_WithTarget_ReachesOnlyThatSubtree()
    {
        Button inTarget = new Button();
        Button elsewhere = new Button();
        ContainerRuntime target = new ContainerRuntime();
        target.Children.Add(inTarget.Visual);
        Gum.GumService.Default.Root.Children.Add(target);
        Gum.GumService.Default.Root.Children.Add(elsewhere.Visual);
        List<object> saved = new List<object>();
        Action<object>? original = GraphicalUiElement.SaveFormsRuntimePropertiesAction;
        GraphicalUiElement.SaveFormsRuntimePropertiesAction = control => saved.Add(control);

        try
        {
            Service.RefreshStyles(target);
        }
        finally
        {
            GraphicalUiElement.SaveFormsRuntimePropertiesAction = original;
        }

        saved.ShouldContain(inTarget);
        saved.ShouldNotContain(elsewhere);
    }

    [Fact]
    public void RefreshLocalization_ReachesRootPopupRootAndModalRoot()
    {
        List<GraphicalUiElement> refreshed = new List<GraphicalUiElement>();
        Action<GraphicalUiElement>? original = GraphicalUiElement.RefreshLocalizationOnElementAction;
        GraphicalUiElement.RefreshLocalizationOnElementAction = element => refreshed.Add(element);

        try
        {
            Service.RefreshLocalization();
        }
        finally
        {
            GraphicalUiElement.RefreshLocalizationOnElementAction = original;
        }

        refreshed.ShouldContain(Gum.GumService.Default.Root);
        refreshed.ShouldContain(Gum.GumService.Default.PopupRoot);
        refreshed.ShouldContain(Gum.GumService.Default.ModalRoot);
    }

    [Fact]
    public void LocalizationService_ReturnsTheActiveService()
    {
        LocalizationService active = new LocalizationService();
        CustomSetPropertyOnRenderable.LocalizationService = active;

        Service.LocalizationService.ShouldBeSameAs(active);
    }

    [Fact]
    public void LastLoadResult_WithNoProjectLoaded_IsNull()
    {
        Service.LastLoadResult.ShouldBeNull();
    }

    [Fact]
    public void Update_WithTotalSeconds_RecordsTheGameTime()
    {
        Service.Update(10.0);

        Gum.GumService.Default.GameTime.ShouldNotBeNull().TotalGameTime.ShouldBe(TimeSpan.FromSeconds(10));
        Service.GameTime.ShouldBe(10f);
    }

    [Fact]
    public void Update_WithTotalSeconds_AnimatesRootsByTheSecondsSincePreviousUpdate()
    {
        ElapsedRecorder recorder = new ElapsedRecorder();
        InteractiveGue animated = new InteractiveGue { FormsControlAsObject = recorder };
        Gum.GumService.Default.Root.Children.Add(animated);
        Service.Update(20.0);
        recorder.Seconds.Clear();

        Service.Update(20.25);

        recorder.Seconds.ShouldBe(new[] { 0.25 });
    }

    [Fact]
    public void Update_WithTotalSeconds_OnAServiceThatHasNotUpdated_AdvancesByZero()
    {
        Gum.GumService fresh = new Gum.GumService();
        ElapsedRecorder recorder = new ElapsedRecorder();
        fresh.Root.Children.Add(new InteractiveGue { FormsControlAsObject = recorder });

        ((IGumService)fresh).Update(5.0);

        recorder.Seconds.ShouldBe(new[] { 0.0 });
    }

    [Fact]
    public void Update_WithTotalSeconds_ThenGameTime_AgreeOnTheClock()
    {
        Service.Update(30.0);
        Gum.GumService.Default.Update(new GameTime(TimeSpan.FromSeconds(30.5), TimeSpan.FromSeconds(0.5)));

        Service.GameTime.ShouldBe(30.5f);
    }

    [Fact]
    public void ExportSnapshot_WritesTheProjectFile()
    {
        string directory = Path.Combine(Path.GetTempPath(), "GumSnapshot_" + Guid.NewGuid().ToString("N"));
        string filePath = Path.Combine(directory, "Snapshot.gumx");
        try
        {
            Service.ExportSnapshot(filePath);

            File.Exists(filePath).ShouldBeTrue();
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void UseSingleThreadedAsync_CreatesTheSynchronizationContext()
    {
        System.Threading.SynchronizationContext? original = System.Threading.SynchronizationContext.Current;
        Gum.GumService service = new Gum.GumService();
        try
        {
            IGumService asInterface = service;
            asInterface.SynchronizationContext.ShouldBeNull();

            asInterface.UseSingleThreadedAsync();

            asInterface.SynchronizationContext.ShouldNotBeNull();
            asInterface.SynchronizationContext.ShouldBeSameAs(service.SynchronizationContext);
        }
        finally
        {
            System.Threading.SynchronizationContext.SetSynchronizationContext(original);
        }
    }

    private class ElapsedRecorder : IUpdateEveryFrame
    {
        public List<double> Seconds { get; } = new List<double>();

        public void Activity(double secondDifference) => Seconds.Add(secondDifference);
    }
}
