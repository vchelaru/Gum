using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace SkiaHostTests;

/// <summary>
/// What a game does through the host's concrete <c>GumService</c> is reachable through
/// <see cref="IGumService"/> with the same result (#6325). The members are shared by every Skia host, so
/// a host's test project derives from this class and supplies nothing. The concrete side of each
/// comparison is <see cref="GumServiceSkiaBase"/>, so a pass pins both.
/// </summary>
public abstract class IGumServiceParityTestsBase : IDisposable
{
    private static GumServiceSkiaBase Concrete => (GumServiceSkiaBase)IGumService.Default!;

    private static IGumService Service => IGumService.Default!;

    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;
    private readonly ICursor? _originalCursor = FormsUtilities.Cursor;

    public void Dispose()
    {
        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.Clear();
        Concrete.Root.Children.Clear();
        Concrete.PopupRoot.Children.Clear();
        Concrete.ModalRoot.Children.Clear();
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }
        if (_originalCursor != null)
        {
            FormsUtilities.SetCursor(_originalCursor);
        }
    }

    [Fact]
    public void PopupRootAndModalRoot_ReturnTheConcreteRoots()
    {
        Service.PopupRoot.ShouldBeSameAs(Concrete.PopupRoot);
        Service.ModalRoot.ShouldBeSameAs(Concrete.ModalRoot);
    }

    [Fact]
    public void Gamepads_ReturnsTheConcreteGamepads()
    {
        Service.Gamepads.ShouldBeSameAs(Concrete.Gamepads);
        Service.Gamepads.ShouldNotBeEmpty();
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
    public void UseGamepadDefaults_RegistersEveryGamepad()
    {
        Service.UseGamepadDefaults();

        FrameworkElement.GamePadsForUiControl.ShouldBe(Concrete.Gamepads);
    }

    [Fact]
    public void RefreshStyles_ReachesRootPopupRootAndModalRoot()
    {
        Button inRoot = new Button();
        Button inPopupRoot = new Button();
        Button inModalRoot = new Button();
        Concrete.Root.Children.Add(inRoot.Visual);
        Concrete.PopupRoot.Children.Add(inPopupRoot.Visual);
        Concrete.ModalRoot.Children.Add(inModalRoot.Visual);
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
        Concrete.Root.Children.Add(target);
        Concrete.Root.Children.Add(elsewhere.Visual);
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

        refreshed.ShouldContain(Concrete.Root);
        refreshed.ShouldContain(Concrete.PopupRoot);
        refreshed.ShouldContain(Concrete.ModalRoot);
    }

    [Fact]
    public void LocalizationService_ReturnsTheConcreteService()
    {
        Service.LocalizationService.ShouldBeSameAs(Concrete.LocalizationService);
    }

    [Fact]
    public void LastLoadResult_ReturnsTheConcreteResult()
    {
        Service.LastLoadResult.ShouldBeSameAs(Concrete.LastLoadResult);
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
    public void SynchronizationContext_ReturnsTheConcreteContext()
    {
        Service.SynchronizationContext.ShouldBeSameAs(Concrete.SynchronizationContext);
    }

    [Fact]
    public void Update_WithTotalSeconds_RecordsTheClockAndTicksTheInstalledInput()
    {
        Mock<ICursor> cursor = new Mock<ICursor>();
        FormsUtilities.SetCursor(cursor.Object);

        Service.Update(7.0);

        Service.GameTime.ShouldBe(7f);
        cursor.Verify(c => c.Activity(7.0), Times.Once());
    }

    [Fact]
    public void AddLayerAndRemoveLayer_ThroughTheRendererInterface_ChangeTheLayers()
    {
        IRenderer renderer = Service.Renderer;

        Layer layer = renderer.AddLayer();

        renderer.Layers.ShouldContain(layer);
        renderer.RemoveLayer(layer);
        renderer.Layers.ShouldNotContain(layer);
    }
}
