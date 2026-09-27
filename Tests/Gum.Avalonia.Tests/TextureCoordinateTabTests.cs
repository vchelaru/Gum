using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Plugins.TextureCoordinates;
using Gum.Avalonia.Tests.Harness;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;
using TextureCoordinateSelectionPlugin.Logic;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Texture Coordinates tab on a real canvas and graphics device, driven with real pointer
/// input. Needs a display and a GL driver, like <see cref="CanvasHostTests"/>.
/// </summary>
public class TextureCoordinateTabTests
{
    private const string SkipReason = "needs a display and a GL driver, and not macOS; set GUM_RUN_CANVAS_DEVICE_TESTS=1 to run on CI";

    private static bool HasDisplay => TestEnvironment.CanCreateDeviceInProcess("GUM_RUN_CANVAS_DEVICE_TESTS");

    private static void OnUiThread(Action action) => DeviceTestThread.Run(action);

    [SkippableFact]
    public void SelectingASpriteWithNoTexture_LeavesNoRegionToDrag()
    {
        // #5101: the region selector of the previously selected sprite stayed live when the next
        // sprite had no texture, so dragging where it was wrote texture coordinates to that sprite.
        Skip.IfNot(HasDisplay, SkipReason);

        OnUiThread(() =>
        {
            TextureCoordinateView view = new TextureCoordinateView(new CanvasRedrawScheduler(TimeProvider.System));
            using HeadlessWindowDriver driver = new HeadlessWindowDriver(view, 600, 500, "GumTextureCoordinateTabTests");
            // Disposing it releases its reference on the shared device, which CanvasHostTests counts.
            using ImageRegionCanvasControl canvasControl = view.GetVisualDescendants().OfType<ImageRegionCanvasControl>().Single();
            Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
            using TextureCoordinateDisplayController controller = CreateController(selectedState.Object);
            controller.CreateControl(view, new object(), new List<int> { 100 });
            using Texture2D texture = new Texture2D(view.Canvas.SystemManagers.Renderer.GraphicsDevice!, 64, 64);

            ComponentSave textured = SpriteComponent("Textured");
            StateSave texturedState = textured.DefaultState!;
            texturedState.SetValue("TextureAddress", TextureAddress.Custom, nameof(TextureAddress));
            texturedState.SetValue("TextureLeft", 16, "int");
            texturedState.SetValue("TextureTop", 16, "int");
            texturedState.SetValue("TextureWidth", 32, "int");
            texturedState.SetValue("TextureHeight", 32, "int");
            Select(selectedState, textured, new GraphicalUiElement(new Sprite(texture)));
            controller.Refresh();
            controller.RefreshSelector(RefreshType.Force);
            Dispatcher.UIThread.RunJobs();
            view.Canvas.RectangleSelectors.Count.ShouldBe(1, "the textured sprite's region is shown");

            ComponentSave untextured = SpriteComponent("Untextured");
            untextured.DefaultState!.SetValue("TextureAddress", TextureAddress.Custom, nameof(TextureAddress));
            Select(selectedState, untextured, new GraphicalUiElement(new Sprite(null)));
            controller.Refresh();
            controller.RefreshSelector(RefreshType.Force);
            Dispatcher.UIThread.RunJobs();

            // Drag from the middle of the canvas, where the camera centered the old region.
            Point center = driver.CenterOf(canvasControl);
            Frame(view, () => driver.Window.MouseMove(center, RawInputModifiers.None));
            Frame(view, () => driver.Window.MouseDown(center, MouseButton.Left, RawInputModifiers.None));
            Frame(view, () => driver.Window.MouseMove(center + new Point(40, 40), RawInputModifiers.LeftMouseButton));
            Frame(view, () => driver.Window.MouseUp(center + new Point(40, 40), MouseButton.Left, RawInputModifiers.None));

            untextured.DefaultState.GetVariableSave("TextureLeft").ShouldBeNull("the drag wrote to the sprite with no texture");
            view.Canvas.RectangleSelectors.Count.ShouldBe(0);

            // A behavior has no element: the same must hold when one is selected after the sprite.
            Select(selectedState, textured, new GraphicalUiElement(new Sprite(texture)));
            controller.Refresh();
            controller.RefreshSelector(RefreshType.Force);
            view.Canvas.RectangleSelectors.Count.ShouldBe(1);
            selectedState.SetupGet(s => s.SelectedElement).Returns((ElementSave?)null);
            controller.RefreshSelector(RefreshType.Force);
            view.Canvas.RectangleSelectors.Count.ShouldBe(0, "a selected behavior leaves no region to drag");
        });
    }

    [SkippableFact]
    public void ARegionEventThatRemovesTheSelector_LeavesTheFrameDrawing()
    {
        // A region event reaches the display controller, whose refresh can remove the selectors
        // while the canvas is still walking them; the frame threw "Collection was modified".
        Skip.IfNot(HasDisplay, SkipReason);

        OnUiThread(() =>
        {
            TextureCoordinateView view = new TextureCoordinateView(new CanvasRedrawScheduler(TimeProvider.System));
            using HeadlessWindowDriver driver = new HeadlessWindowDriver(view, 600, 500, "GumTextureCoordinateTabTests");
            using ImageRegionCanvasControl canvasControl = view.GetVisualDescendants().OfType<ImageRegionCanvasControl>().Single();
            using Texture2D texture = new Texture2D(view.Canvas.SystemManagers.Renderer.GraphicsDevice!, 64, 64);
            view.Canvas.CurrentTexture = texture;
            view.Canvas.DesiredSelectorCount = 1;
            TextureCoordinateSelectionPlugin.RegionSelection.RectangleSelector selector = view.Canvas.RectangleSelectors[0];
            selector.Left = 16;
            selector.Top = 16;
            selector.Width = 32;
            selector.Height = 32;
            selector.Visible = true;
            selector.ShowHandles = true;
            view.Canvas.EndRegionChanged += (_, _) => view.Canvas.DesiredSelectorCount = 0;

            Point from = WindowPointOf(view, canvasControl, driver, 32, 32);
            Point to = WindowPointOf(view, canvasControl, driver, 42, 32);
            Frame(view, () => driver.Window.MouseMove(from, RawInputModifiers.None));
            Frame(view, () => driver.Window.MouseDown(from, MouseButton.Left, RawInputModifiers.None));
            Frame(view, () => driver.Window.MouseMove(to, RawInputModifiers.LeftMouseButton));
            Frame(view, () => driver.Window.MouseUp(to, MouseButton.Left, RawInputModifiers.None));

            view.Canvas.RectangleSelectors.Count.ShouldBe(0);
        });
    }

    private static Point WindowPointOf(TextureCoordinateView view, ImageRegionCanvasControl canvasControl, HeadlessWindowDriver driver, float x, float y)
    {
        view.Canvas.SystemManagers.Renderer.Camera.WorldToScreen(x, y, out float screenX, out float screenY);
        return canvasControl.TranslatePoint(new Point(screenX, screenY), driver.Window)
            ?? throw new InvalidOperationException("The canvas is not in the window.");
    }

    private static void Frame(TextureCoordinateView view, Action input)
    {
        input();
        // The canvas polls its input once per frame, in Draw.
        view.Canvas.Draw();
    }

    private static ComponentSave SpriteComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Sprite" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    private static void Select(Mock<ISelectedState> selectedState, ComponentSave component, GraphicalUiElement visual)
    {
        selectedState.SetupGet(s => s.SelectedElement).Returns(component);
        selectedState.SetupGet(s => s.SelectedStateSave).Returns(component.DefaultState);
        selectedState.SetupGet(s => s.SelectedIpso).Returns(visual);
        selectedState.SetupGet(s => s.SelectedInstance).Returns((InstanceSave?)null);
    }

    private static TextureCoordinateDisplayController CreateController(ISelectedState selectedState)
    {
        Mock<ITabManager> tabManager = new Mock<ITabManager>();
        tabManager.Setup(t => t.AddControl(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<TabLocation>()))
            .Returns(new Mock<IPluginTab>().Object);
        return new TextureCoordinateDisplayController(
            selectedState,
            new Mock<IUndoManager>().Object,
            new Mock<IGuiCommands>().Object,
            new Mock<IFileCommands>().Object,
            new Mock<ISetVariableLogic>().Object,
            tabManager.Object,
            new Mock<IHotkeyManager>().Object,
            new CameraScrollBarBinder(new ScrollBarLogic()),
            TestAppBuilder.Services.GetRequiredService<IMessenger>(),
            TestAppBuilder.Services.GetRequiredService<IThemingService>());
    }
}
