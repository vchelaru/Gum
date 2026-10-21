using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;

namespace RaylibGum.Tests;

/// <summary>
/// What a game does through the concrete raylib <c>GumService</c> is reachable through
/// <see cref="IGumService"/> with the same result (#6325). The service shares its source with the
/// MonoGame one, so this mirrors the headless cases in MonoGameGum.Tests (<c>IGumServiceParityTests</c>).
/// </summary>
public class IGumServiceParityTests : BaseTestClass
{
    private readonly ICursor? _originalCursor = FormsUtilities.Cursor;
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;

    public override void Dispose()
    {
        FrameworkElement.GamePadsForUiControl.Clear();
        if (_originalCursor != null)
        {
            FormsUtilities.SetCursor(_originalCursor);
        }
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }
        base.Dispose();
    }

    private static Gum.GumService Concrete => Gum.GumService.Default;

    private static IGumService Service => Gum.GumService.Default;

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
        IRenderer renderer = SystemManagers.Default.Renderer;

        Layer layer = renderer.AddLayer();

        renderer.Layers.ShouldContain(layer);
        renderer.RemoveLayer(layer);
        renderer.Layers.ShouldNotContain(layer);
    }
}
