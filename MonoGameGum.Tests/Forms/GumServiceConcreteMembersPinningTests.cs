using Gum.Forms;
using Gum.GueDeriving;
using Gum.Forms.Controls;
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
/// Pins what the concrete <c>GumService</c> members do today, before the same members become reachable
/// through <see cref="IGumService"/> (#6325). <see cref="IGumServiceParityTests"/> asserts the interface
/// reaches exactly this behavior.
/// </summary>
public class GumServiceConcreteMembersPinningTests : BaseTestClass
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

    private static Gum.GumService Service => Gum.GumService.Default;

    [Fact]
    public void PopupRootAndModalRoot_ReturnTheFormsRoots()
    {
        Service.PopupRoot.ShouldBeSameAs(FrameworkElement.PopupRoot);
        Service.ModalRoot.ShouldBeSameAs(FrameworkElement.ModalRoot);
    }

    [Fact]
    public void Gamepads_ReturnsTheFormsUtilitiesGamepads()
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
        Service.Root.Children.Add(inRoot.Visual);
        Service.PopupRoot.Children.Add(inPopupRoot.Visual);
        Service.ModalRoot.Children.Add(inModalRoot.Visual);
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
        Service.Root.Children.Add(target);
        Service.Root.Children.Add(elsewhere.Visual);
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

        refreshed.ShouldContain(Service.Root);
        refreshed.ShouldContain(Service.PopupRoot);
        refreshed.ShouldContain(Service.ModalRoot);
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
    public void Update_WithGameTime_RecordsTheGameTime()
    {
        GameTime gameTime = new GameTime(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0.5));

        Service.Update(gameTime);

        Service.GameTime.ShouldBeSameAs(gameTime);
        ((IGumService)Service).GameTime.ShouldBe(10f);
    }

    [Fact]
    public void Update_AnimatesRootsByTheElapsedGameTime()
    {
        ElapsedRecorder recorder = new ElapsedRecorder();
        InteractiveGue animated = new InteractiveGue { FormsControlAsObject = recorder };
        Service.Root.Children.Add(animated);

        Service.Update(new GameTime(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0.25)));

        recorder.Seconds.ShouldBe(new[] { 0.25 });
    }

    [Fact]
    public void UseSingleThreadedAsync_CreatesTheSynchronizationContextOnce()
    {
        System.Threading.SynchronizationContext? original = System.Threading.SynchronizationContext.Current;
        Gum.GumService service = new Gum.GumService();
        try
        {
            service.SynchronizationContext.ShouldBeNull();

            service.UseSingleThreadedAsync();
            Gum.Async.SingleThreadSynchronizationContext? created = service.SynchronizationContext;
            service.UseSingleThreadedAsync();

            created.ShouldNotBeNull();
            service.SynchronizationContext.ShouldBeSameAs(created);
        }
        finally
        {
            System.Threading.SynchronizationContext.SetSynchronizationContext(original);
        }
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

    // A control that records the elapsed seconds Update hands to the roots it animates.
    private class ElapsedRecorder : IUpdateEveryFrame
    {
        public List<double> Seconds { get; } = new List<double>();

        public void Activity(double secondDifference) => Seconds.Add(secondDifference);
    }
}
