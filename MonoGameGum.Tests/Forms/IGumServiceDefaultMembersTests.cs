using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Localization;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// What an <see cref="IGumService"/> written before the members of #6325 existed gets: the default
/// interface members. Moq's CallBase runs them, as a third-party implementer or a test double would.
/// </summary>
public class IGumServiceDefaultMembersTests : BaseTestClass
{
    private static Mock<IGumService> CreateMinimalService()
    {
        return new Mock<IGumService> { CallBase = true };
    }

    public override void Dispose()
    {
        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.Clear();
        base.Dispose();
    }

    [Fact]
    public void PopupRootAndModalRoot_ByDefault_ShouldBeTheFormsRoots()
    {
        IGumService service = CreateMinimalService().Object;

        service.PopupRoot.ShouldBeSameAs(FrameworkElement.PopupRoot);
        service.ModalRoot.ShouldBeSameAs(FrameworkElement.ModalRoot);
    }

    [Fact]
    public void Gamepads_ByDefault_ShouldBeEmpty()
    {
        IGumService service = CreateMinimalService().Object;

        service.Gamepads.ShouldNotBeNull().ShouldBeEmpty();
    }

    [Fact]
    public void UseKeyboardDefaults_ByDefault_ShouldRegisterTheServiceKeyboard()
    {
        Mock<IGumService> service = CreateMinimalService();
        IInputReceiverKeyboard keyboard = new Mock<IInputReceiverKeyboard>().Object;
        service.Setup(x => x.Keyboard).Returns(keyboard);

        service.Object.UseKeyboardDefaults();

        FrameworkElement.KeyboardsForUiControl.ShouldBe(new[] { keyboard });
    }

    [Fact]
    public void UseKeyboardDefaults_WithNoKeyboard_ShouldThrow()
    {
        IGumService service = CreateMinimalService().Object;

        Should.Throw<InvalidOperationException>(() => service.UseKeyboardDefaults());
    }

    [Fact]
    public void UseGamepadDefaults_ByDefault_ShouldRegisterTheServiceGamepads()
    {
        Mock<IGumService> service = CreateMinimalService();
        GamePad first = new GamePad();
        GamePad second = new GamePad();
        service.Setup(x => x.Gamepads).Returns(new[] { first, second });

        service.Object.UseGamepadDefaults();

        FrameworkElement.GamePadsForUiControl.ShouldBe(new IGamePad[] { first, second });
    }

    [Fact]
    public void RefreshStyles_ByDefault_ShouldReachRootPopupRootAndModalRoot()
    {
        Mock<IGumService> service = CreateMinimalService();
        Button inRoot = new Button();
        Button inPopupRoot = new Button();
        Button inModalRoot = new Button();
        service.Setup(x => x.Root).Returns(CreateRootHolding(inRoot));
        service.Setup(x => x.PopupRoot).Returns(CreateRootHolding(inPopupRoot));
        service.Setup(x => x.ModalRoot).Returns(CreateRootHolding(inModalRoot));
        List<object> saved = new List<object>();
        Action<object>? original = GraphicalUiElement.SaveFormsRuntimePropertiesAction;
        GraphicalUiElement.SaveFormsRuntimePropertiesAction = control => saved.Add(control);

        try
        {
            service.Object.RefreshStyles();
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
    public void RefreshStyles_WithTarget_ByDefault_ShouldReachOnlyThatSubtree()
    {
        IGumService service = CreateMinimalService().Object;
        Button inTarget = new Button();
        Button elsewhere = new Button();
        InteractiveGue target = CreateRootHolding(inTarget);
        List<object> saved = new List<object>();
        Action<object>? original = GraphicalUiElement.SaveFormsRuntimePropertiesAction;
        GraphicalUiElement.SaveFormsRuntimePropertiesAction = control => saved.Add(control);

        try
        {
            service.RefreshStyles(target);
        }
        finally
        {
            GraphicalUiElement.SaveFormsRuntimePropertiesAction = original;
        }

        saved.ShouldContain(inTarget);
        saved.ShouldNotContain(elsewhere);
    }

    [Fact]
    public void RefreshLocalization_ByDefault_ShouldReachRootPopupRootAndModalRoot()
    {
        Mock<IGumService> service = CreateMinimalService();
        ContainerRuntime root = new ContainerRuntime();
        ContainerRuntime popupRoot = new ContainerRuntime();
        ContainerRuntime modalRoot = new ContainerRuntime();
        service.Setup(x => x.Root).Returns(root);
        service.Setup(x => x.PopupRoot).Returns(popupRoot);
        service.Setup(x => x.ModalRoot).Returns(modalRoot);
        List<GraphicalUiElement> refreshed = new List<GraphicalUiElement>();
        Action<GraphicalUiElement>? original = GraphicalUiElement.RefreshLocalizationOnElementAction;
        GraphicalUiElement.RefreshLocalizationOnElementAction = element => refreshed.Add(element);

        try
        {
            service.Object.RefreshLocalization();
        }
        finally
        {
            GraphicalUiElement.RefreshLocalizationOnElementAction = original;
        }

        refreshed.ShouldBe(new GraphicalUiElement[] { root, popupRoot, modalRoot });
    }

    [Fact]
    public void LocalizationService_ByDefault_ShouldBeTheActiveService()
    {
        IGumService service = CreateMinimalService().Object;
        LocalizationService active = new LocalizationService();
        LocalizationRuntimeState.Current = active;

        try
        {
            service.LocalizationService.ShouldBeSameAs(active);
        }
        finally
        {
            LocalizationRuntimeState.Current = null;
        }
    }

    [Fact]
    public void ExportSnapshot_ByDefault_ShouldWriteTheRootToTheProjectFile()
    {
        Mock<IGumService> service = CreateMinimalService();
        service.Setup(x => x.Root).Returns(new ContainerRuntime());
        string directory = Path.Combine(Path.GetTempPath(), "GumSnapshot_" + Guid.NewGuid().ToString("N"));
        string filePath = Path.Combine(directory, "Snapshot.gumx");
        try
        {
            service.Object.ExportSnapshot(filePath);

            File.Exists(filePath).ShouldBeTrue();
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void LastLoadResultAndSynchronizationContext_ByDefault_ShouldBeNull()
    {
        IGumService service = CreateMinimalService().Object;

        service.LastLoadResult.ShouldBeNull();
        service.SynchronizationContext.ShouldBeNull();
    }

    [Fact]
    public void UpdateUseSingleThreadedAsyncAndUninitialize_ByDefault_ShouldDoNothing()
    {
        IGumService service = CreateMinimalService().Object;

        Should.NotThrow(() =>
        {
            service.Update(1.0);
            service.UseSingleThreadedAsync();
            service.Uninitialize();
        });
        service.SynchronizationContext.ShouldBeNull();
    }

    private static InteractiveGue CreateRootHolding(Button button)
    {
        InteractiveGue root = new ContainerRuntime();
        root.Children.Add(button.Visual);
        return root;
    }
}
