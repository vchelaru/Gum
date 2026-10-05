using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Shell;
using Gum.Wireframe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Color = Avalonia.Media.Color;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the Editor canvas's right-click menu, its toolbar (grid snap, canvas
/// size, font scale), the resize edge cases and what the canvas shows around a gesture (hover
/// highlight, dimension display, the checkerboard, redraws). Same conventions as
/// <see cref="CanvasScenarioTests"/>.
/// </summary>
[Trait("Category", "EndToEnd")]
public class CanvasMenuAndToolbarScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    #region Right-click menu

    [SkippableFact]
    [Trait("Feature", "CANV-015")]
    [Trait("Feature", "CANV-031")]
    [Trait("Feature", "CANV-032")]
    [Trait("Feature", "CANV-033")]
    [Trait("Feature", "CANV-034")]
    [Trait("Feature", "CANV-035")]
    [Trait("Feature", "CANV-038")]
    [Trait("Feature", "CANV-039")]
    public void RightClickMenu_ReordersAndLocksTheInstanceUnderThePointer_AndOpensOnlyOverAnInstance()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.AddInstance(button, "A", "Rectangle", x: 20, y: 20, width: 40, height: 40);
            canvas.AddInstance(button, "B", "Rectangle", x: 100, y: 20, width: 40, height: 40);
            canvas.AddInstance(button, "C", "Rectangle", x: 180, y: 20, width: 40, height: 40);
            Point overA = canvas.WindowPointOf(40, 40);
            Point overB = canvas.WindowPointOf(120, 40);
            canvas.Tree.Click(canvas.Tree.NodeFor(button));

            // With nothing selected and nothing under the pointer there is no menu.
            canvas.RightClick(canvas.WindowPointOf(400, 300));
            canvas.ContextMenu.IsOpen.ShouldBeFalse();

            // Right-clicking an instance selects it, and the menu is for that instance.
            canvas.Click(overB);
            ProjectFileSnapshot start = canvas.Tree.SnapshotFiles();
            canvas.RightClick(overA);
            canvas.Project.SelectedState.SelectedInstance.ShouldNotBeNull(canvas.Describe()).Name.ShouldBe("A", canvas.Describe());
            canvas.ContextMenu.IsOpen.ShouldBeTrue();
            canvas.MenuHeaders().ShouldBe(new[] { "Bring to Front", "Move Forward", "Move In Front Of", "Move Backward", "Send to Back", "Add child object to 'A'", "Lock A" });

            canvas.PickMenu("Bring to Front");
            SavedOrder(canvas, button).ShouldBe(new[] { "B", "C", "A" });
            canvas.Undo();
            canvas.Tree.SnapshotFiles().ShouldMatch(start, "undoing Bring to Front should restore the files");
            canvas.Redo();
            SavedOrder(canvas, button).ShouldBe(new[] { "B", "C", "A" });

            canvas.RightClick(overA);
            canvas.PickMenu("Send to Back");
            SavedOrder(canvas, button).ShouldBe(new[] { "A", "B", "C" });

            canvas.RightClick(overA);
            canvas.PickMenu("Move Forward");
            SavedOrder(canvas, button).ShouldBe(new[] { "B", "A", "C" });

            canvas.RightClick(overA);
            canvas.PickMenu("Move Backward");
            SavedOrder(canvas, button).ShouldBe(new[] { "A", "B", "C" });

            canvas.RightClick(overA);
            canvas.PickMenu("Move In Front Of", "C");
            SavedOrder(canvas, button).ShouldBe(new[] { "B", "C", "A" });

            // Locked, A can't be dragged, and a click on it doesn't select it.
            canvas.RightClick(overA);
            canvas.PickMenu("Lock A");
            canvas.SavedElement(button).GetInstance("A")!.Locked.ShouldBeTrue();
            canvas.Drag(overA, overA + new Point(30, 30));
            canvas.SavedValue(button, "A.X").ShouldBe(20f);
            canvas.SavedValue(button, "A.Y").ShouldBe(20f);
            canvas.Click(overB);
            canvas.Click(overA);
            (canvas.Project.SelectedState.SelectedInstance?.Name).ShouldNotBe("A");

            // Selected from the tree, it unlocks from the menu and moves again.
            canvas.Tree.Click(canvas.Tree.NodeFor(button.GetInstance("A")!));
            canvas.RightClick(overA);
            canvas.MenuHeaders().Last().ShouldBe("Unlock A");
            canvas.PickMenu("Unlock A");
            canvas.SavedElement(button).GetInstance("A")!.Locked.ShouldBeFalse();
            canvas.Drag(overA, overA + new Point(30, 30));
            canvas.SavedValue(button, "A.X").ShouldBe(50f);

            canvas.AssertOracles();
        });
    }

    [SkippableFact]
    [Trait("Feature", "CANV-036")]
    [Trait("Feature", "CANV-037")]
    public void RightClickMenu_AddsAStandardChildOrAFavoritedComponent_ToTheInstance()
    {
        OnCanvas(canvas =>
        {
            ComponentSave icon = canvas.Project.AddComponent("Icon");
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.AddInstance(button, "Panel", "Container", x: 100, y: 100, width: 200, height: 100);
            Point overPanel = canvas.WindowPointOf(200, 150);

            canvas.Tree.RightClick(canvas.Tree.NodeFor(icon));
            canvas.Tree.PickMenu("Add to Favorites");
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            canvas.Click(overPanel);
            ProjectFileSnapshot start = canvas.Tree.SnapshotFiles();

            canvas.RightClick(overPanel);
            canvas.PickMenu("Add child object to 'Panel'", "Rectangle");

            InstanceSave rectangle = button.Instances.Single(instance => instance.BaseType == "Rectangle");
            canvas.SavedValue(button, $"{rectangle.Name}.Parent").ShouldBe("Panel");
            canvas.Undo();
            canvas.Tree.SnapshotFiles().ShouldMatch(start, "undoing the added child should restore the files");

            canvas.Tree.Click(canvas.Tree.NodeFor(button.GetInstance("Panel")!));
            canvas.RightClick(overPanel);
            canvas.PickMenu("Add child object to 'Panel'", "Favorited Components", "Icon");

            InstanceSave added = button.Instances.Single(instance => instance.BaseType == "Icon");
            canvas.SavedValue(button, $"{added.Name}.Parent").ShouldBe("Panel");

            canvas.AssertOracles();
        });
    }

    private static List<string> SavedOrder(CanvasHarness canvas, ElementSave element) =>
        canvas.SavedElement(element).Instances.Select(instance => instance.Name).ToList();

    #endregion

    #region Toolbar

    [SkippableFact]
    public void GridSize_PickingAPresetFromTheToolbar_SavesThatGridSize()
    {
        OnCanvas(canvas =>
        {
            canvas.Frame();

            canvas.GridSizeComboBox.SelectedItem = 16;
            canvas.Frame();

            SavedProject(canvas).GridSize.ShouldBe(16);
        });
    }

    [SkippableFact]
    [Trait("Feature", "CANV-017")]
    [Trait("Feature", "CANV-018")]
    [Trait("Feature", "CANV-019")]
    public void SnapToGrid_FromTheToolbar_ShowsTheGrid_AndMovesLandOnTheGridSize()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.AddInstance(button, "Box", "Rectangle", x: 40, y: 40, width: 60, height: 40);
            canvas.Tree.Click(canvas.Tree.NodeFor(button.GetInstance("Box")!));
            // A strip of empty canvas, crossing where lines of a 20-unit grid fall.
            Point strip = canvas.WindowPointOf(200, 305);
            List<Color> withoutGrid = canvas.PixelsAlong(strip, 60);

            canvas.Input.Click(canvas.SnapToGridCheckBox);
            canvas.Input.TypeAndEnter(canvas.GridSizeBox, "20");
            canvas.Frame();

            GumProjectSave saved = SavedProject(canvas);
            saved.SnapToGrid.ShouldBeTrue();
            saved.GridSize.ShouldBe(20);
            canvas.PixelsAlong(strip, 60).ShouldNotBe(withoutGrid, "the grid overlay should draw over the empty canvas");

            // 40 + 27 and 40 + 13: both land on the 20-unit grid.
            canvas.Drag(canvas.WindowPointOf(70, 60), canvas.WindowPointOf(97, 73));
            float x = (float)canvas.SavedValue(button, "Box.X")!;
            float y = (float)canvas.SavedValue(button, "Box.Y")!;
            (x % 20).ShouldBe(0f, $"X {x} should be on the grid");
            (y % 20).ShouldBe(0f, $"Y {y} should be on the grid");
            x.ShouldBe(67f, tolerance: 20f);
            y.ShouldBe(53f, tolerance: 20f);

            canvas.Input.Click(canvas.SnapToGridCheckBox);
            canvas.Frame();
            SavedProject(canvas).SnapToGrid.ShouldBeFalse();
            canvas.PixelsAlong(strip, 60).ShouldBe(withoutGrid, "the grid overlay should go with snapping");

            canvas.AssertOracles();
        });
    }

    [SkippableFact]
    public void SnapToGridNote_FollowsTheUnitsAsTheyAreEdited_NotTheEditBefore()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.AddInstance(button, "Box", "Rectangle", x: 40, y: 40, width: 60, height: 40);
            InstanceSave box = button.GetInstance("Box")!;
            canvas.Tree.Click(canvas.Tree.NodeFor(box));
            canvas.Input.Click(canvas.SnapToGridCheckBox);
            canvas.Frame();
            canvas.Editor.HasGridSnapWarning.ShouldBeFalse();

            ISetVariableLogic setVariableLogic = Services.GetRequiredService<ISetVariableLogic>();
            StateSave state = button.DefaultState!;

            object? oldUnits = state.GetValue("Box.XUnits");
            state.SetValue("Box.XUnits", PositionUnitType.PercentageWidth, "PositionUnitType");
            setVariableLogic.PropertyValueChanged("XUnits", oldUnits, box, state);
            canvas.Editor.HasGridSnapWarning.ShouldBeTrue("percent X Units won't snap, so the note shows right away");

            oldUnits = state.GetValue("Box.XUnits");
            state.SetValue("Box.XUnits", PositionUnitType.PixelsFromLeft, "PositionUnitType");
            setVariableLogic.PropertyValueChanged("XUnits", oldUnits, box, state);
            canvas.Editor.HasGridSnapWarning.ShouldBeFalse("back to pixels, so the note goes away right away");
        });
    }

    [SkippableFact]
    [Trait("Feature", "CANV-024")]
    [Trait("Feature", "CANV-025")]
    [Trait("Feature", "CANV-030")]
    [Trait("Feature", "CANV-040")]
    public void CanvasSizeFontScaleCheckerboardAndZoomToFit_ChangeTheViewButNoFile()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.AddInstance(button, "Label", "Text", x: 20, y: 20);
            using (canvas.Project.UndoManager.RequestLock(button))
            {
                button.DefaultState!.SetValue("Label.WidthUnits", DimensionUnitType.RelativeToChildren, "DimensionUnitType");
                button.DefaultState.SetValue("Label.Width", 0f, "float");
            }
            canvas.Tree.SaveAll();
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            ProjectFileSnapshot start = canvas.Tree.SnapshotFiles();
            float canvasWidth = GraphicalUiElement.CanvasWidth;
            float canvasHeight = GraphicalUiElement.CanvasHeight;
            try
            {
                // The canvas size combo shows Project Default and steps with the arrow keys past
                // 480p to 720p: the canvas the layout uses resizes.
                ComboBox sizes = canvas.CanvasSizeComboBox;
                canvas.Editor.SelectedCustomCanvasSize.FriendlyName.ShouldBe("Project Default");
                sizes.SelectedItem.ShouldBeSameAs(canvas.Editor.SelectedCustomCanvasSize);
                sizes.Focus();
                canvas.Input.Press(Key.Down, PhysicalKey.ArrowDown);
                canvas.Editor.SelectedCustomCanvasSize.FriendlyName.ShouldBe("480p");
                canvas.Input.Press(Key.Down, PhysicalKey.ArrowDown);
                canvas.Frame();
                canvas.Editor.SelectedCustomCanvasSize.FriendlyName.ShouldBe("720p");
                (GraphicalUiElement.CanvasWidth, GraphicalUiElement.CanvasHeight).ShouldBe((1280f, 720f));

                // Font scale "+" makes text wider.
                float labelWidth = LabelWidth(canvas);
                canvas.Input.Click(canvas.FontScaleIncreaseButton);
                canvas.Frame();
                canvas.Editor.GlobalFontScale.ShouldBe(1.25f);
                LabelWidth(canvas).ShouldBeGreaterThan(labelWidth);
                canvas.Input.Click(canvas.FontScaleDecreaseButton);
                canvas.Frame();
                canvas.Editor.GlobalFontScale.ShouldBe(1f);
                LabelWidth(canvas).ShouldBe(labelWidth, tolerance: 0.01f);

                // The checkerboard behind the canvas alternates two colors; off, one is left.
                Point empty = canvas.WindowPointOf(400, 300);
                canvas.PixelsAlong(empty, 64).Distinct().Count().ShouldBeGreaterThan(1, "the checkerboard should show");
                ProjectPropertiesViewModel properties = Properties();
                properties.ShowCheckerBackground.ShouldBeTrue();
                properties.ShowCheckerBackground = false;
                canvas.Frame();
                canvas.PixelsAlong(empty, 64).Distinct().Count().ShouldBe(1, "without the checkerboard the empty canvas is one color");
                properties.ShowCheckerBackground = true;
                canvas.Frame();
                start = canvas.Tree.SnapshotFiles();

                // Zoom to fit, as --zoom-to-fit asks for it: the element fills the view.
                canvas.Editor.PercentZoom = 25;
                canvas.Frame();
                CanvasZoomToFitReport report = Services.GetRequiredService<IMessenger>().Send(new ZoomCanvasToFitSelectionMessage()).Response
                    ?? throw new InvalidOperationException("Zoom to fit found nothing to frame.");
                canvas.Frame();
                canvas.Editor.PercentZoomLevel.Value.ShouldBe(report.ZoomPercent);
                report.ZoomPercent.ShouldBeGreaterThan(25);
                canvas.Camera.Zoom.ShouldBe(report.ZoomPercent / 100f);

                canvas.Tree.SnapshotFiles().ShouldMatch(start, "view settings are not saved in element files");
            }
            finally
            {
                canvas.Editor.GlobalFontScale = 1;
                canvas.Editor.SelectedCustomCanvasSize = canvas.Editor.CustomCanvasSizes[0];
                GraphicalUiElement.CanvasWidth = canvasWidth;
                GraphicalUiElement.CanvasHeight = canvasHeight;
                canvas.ResetCamera();
            }

            canvas.AssertOracles();
        });
    }

    private static float LabelWidth(CanvasHarness canvas) =>
        canvas.Wireframe.AllIpsos.Single(ipso => ipso.Name == "Label").AbsoluteWidth;

    private static ProjectPropertiesViewModel Properties()
    {
        AvaloniaTabManager tabs = (AvaloniaTabManager)Services.GetRequiredService<ITabManager>();
        AvaloniaPluginTab tab = tabs.AllTabs.Single(candidate => candidate.Title == "Project Properties");
        return (ProjectPropertiesViewModel)((Control)tab.Content).DataContext!;
    }

    private static GumProjectSave SavedProject(CanvasHarness canvas) =>
        GumProjectSave.Load(canvas.Project.ProjectFilePath, out _) ?? throw new InvalidOperationException("The project file did not load.");

    #endregion

    #region Resizing

    [SkippableFact]
    [Trait("Feature", "CANV-010")]
    [Trait("Feature", "CANV-011")]
    [Trait("Feature", "CANV-016")]
    [Trait("Feature", "CANV-029")]
    public void Resize_PastZeroFlips_PercentWidthResizesInPercent_AndHoverShowsHighlightAndSize()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            using (canvas.Project.UndoManager.RequestLock(button))
            {
                button.DefaultState!.SetValue("Width", 400f, "float");
                button.DefaultState.SetValue("Height", 400f, "float");
            }
            canvas.AddInstance(button, "Box", "Rectangle", x: 100, y: 100, width: 80, height: 40);
            canvas.AddInstance(button, "Other", "Rectangle", x: 300, y: 100, width: 40, height: 40);
            canvas.AddInstance(button, "Bar", "Rectangle", x: 100, y: 300, width: 10, height: 20);
            using (canvas.Project.UndoManager.RequestLock(button))
            {
                button.DefaultState.SetValue("Bar.WidthUnits", DimensionUnitType.PercentageOfParent, "DimensionUnitType");
            }
            canvas.Tree.SaveAll();
            canvas.Wireframe.RefreshAll(forceLayout: true);
            canvas.Tree.Click(canvas.Tree.NodeFor(button.GetInstance("Box")!));

            // Hovering an instance that isn't selected highlights it; empty canvas clears it.
            canvas.MoveTo(canvas.WindowPointOf(320, 120));
            canvas.Plugin.CanvasSelectionManager.HighlightedIpso.ShouldNotBeNull(canvas.Describe()).Name.ShouldBe("Other", canvas.Describe());
            canvas.MoveTo(canvas.WindowPointOf(600, 500));
            canvas.Plugin.CanvasSelectionManager.HighlightedIpso.ShouldBeNull();

            // Hovering the right edge handle (just right of (180, 120)) shows the width.
            canvas.MoveTo(canvas.WindowPointOf(186, 120));
            string.Join(" | ", ShownDimensions()).ShouldBe("80.0", canvas.Describe());
            canvas.MoveTo(canvas.WindowPointOf(250, 250));
            ShownDimensions().ShouldBeEmpty();

            // Dragged 120 left, the right edge crosses the left one at 100: the box flips to 60..100.
            ProjectFileSnapshot start = canvas.Tree.SnapshotFiles();
            canvas.Drag(canvas.WindowPointOf(186, 120), canvas.WindowPointOf(66, 120));
            canvas.SavedValue(button, "Box.X").ShouldBe(60f, canvas.Describe());
            canvas.SavedValue(button, "Box.Width").ShouldBe(40f);
            canvas.Undo();
            canvas.Tree.SnapshotFiles().ShouldMatch(start, "undoing the flip should restore the files");

            // Bar is 10% of the 400-wide button (40 pixels); 40 pixels more is 20%.
            canvas.Tree.Click(canvas.Tree.NodeFor(button.GetInstance("Bar")!));
            canvas.Drag(canvas.WindowPointOf(146, 310), canvas.WindowPointOf(186, 310));
            canvas.SavedValue(button, "Bar.Width").ShouldBe(20f, canvas.Describe());
            Convert.ToInt32(canvas.SavedValue(button, "Bar.WidthUnits")).ShouldBe((int)DimensionUnitType.PercentageOfParent);

            canvas.AssertOracles();
        });
    }

    private static List<string> ShownDimensions() =>
        SystemManagers.Default.TextManager.Texts
            .Where(text => text.Name == "Dimension display text" && text.Visible)
            .Select(text => text.RawText ?? "")
            .ToList();

    #endregion

    #region What the canvas shows

    [SkippableFact]
    [Trait("Feature", "CANV-004")]
    [Trait("Feature", "CANV-041")]
    [Trait("Feature", "CANV-042")]
    public void ComponentInstances_SelectAsAWhole_RedrawAfterAGridEdit_AndSkiaShapesDraw()
    {
        OnCanvas(canvas =>
        {
            ComponentSave card = canvas.Project.AddComponent("Card");
            canvas.AddInstance(card, "Fill", "Rectangle", x: 0, y: 0, width: 100, height: 60);
            Fill(canvas, card, "Fill");
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.AddInstance(button, "CardInstance", "Card", x: 100, y: 100);
            canvas.Tree.Click(canvas.Tree.NodeFor(button));

            // A click on the card's own child selects the card instance, not the child inside it.
            canvas.Click(canvas.WindowPointOf(150, 130));
            canvas.Project.SelectedState.SelectedInstance.ShouldNotBeNull(canvas.Describe()).Name.ShouldBe("CardInstance", canvas.Describe());

            // An edit in the Variables tab redraws the canvas on its own, with no canvas input. The
            // app's input hook (installed at startup) is what asks for the frame.
            ICanvasRedrawScheduler redraws = Services.GetRequiredService<ICanvasRedrawScheduler>();
            using IDisposable inputHook = CanvasInputRedrawHook.Install(redraws);
            PumpUntil(canvas, () => !redraws.IsRedrawNeeded, "the canvas to settle");
            Point newCenter = canvas.WindowPointOf(350, 130);
            IsWhite(canvas.Input.PixelAt(newCenter)).ShouldBeFalse();
            canvas.Tree.Grid.TypeAndEnter("X", "300");
            redraws.IsRedrawNeeded.ShouldBeTrue("the edit should ask the canvas to redraw");
            PumpUntil(canvas, () => IsWhite(canvas.Input.PixelAt(newCenter)), "the moved card to show");

            // A Skia shape, once the Skia standards are in the project, draws on the canvas.
            canvas.Tree.PickMainMenu("Plugins", "Add Skia Standard Elements");
            canvas.Project.Project.StandardElements.Select(element => element.Name).ShouldContain("Line");
            Point strokeArea = canvas.WindowPointOf(140, 320);
            canvas.Input.AnyPixelNear(strokeArea, 30, IsWhite).ShouldBeFalse();
            canvas.AddInstance(button, "Stroke", "Line", x: 100, y: 300, width: 80, height: 40);
            using (canvas.Project.UndoManager.RequestLock(button))
            {
                button.DefaultState!.SetValue("Stroke.StrokeWidth", 10f, "float");
            }
            canvas.Tree.SaveAll();
            // Selecting the element moves the resize handles away from the line.
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            canvas.Wireframe.RefreshAll(forceLayout: true);
            canvas.Frame();
            canvas.Input.AnyPixelNear(strokeArea, 30, IsWhite).ShouldBeTrue("the Skia line should draw");

            canvas.AssertOracles();
        });
    }

    [SkippableFact]
    public void MissingSinglePixelTextureFile_FallsBackToTheWhitePixel_SoFillsStillDraw()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.AddInstance(button, "Fill", "Rectangle", x: 100, y: 100, width: 100, height: 60);
            Fill(canvas, button, "Fill");
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            Point fillCenter = canvas.WindowPointOf(150, 130);
            canvas.Frame();
            IsWhite(canvas.Input.PixelAt(fillCenter)).ShouldBeTrue(canvas.Describe());

            ProjectPropertiesViewModel properties = Properties();
            (string File, int? Left, int? Top, int? Right, int? Bottom) original = (properties.SinglePixelTextureFile,
                properties.SinglePixelTextureLeft, properties.SinglePixelTextureTop,
                properties.SinglePixelTextureRight, properties.SinglePixelTextureBottom);
            try
            {
                properties.SinglePixelTextureLeft = 0;
                properties.SinglePixelTextureTop = 0;
                properties.SinglePixelTextureRight = 1;
                properties.SinglePixelTextureBottom = 1;
                Renderer renderer = Renderer.Self;
                Texture2D? before = renderer.TryGetSinglePixelTexture();
                properties.SinglePixelTextureFile = "MissingPixel.png";

                Texture2D? texture = renderer.TryGetSinglePixelTexture();
                texture.ShouldNotBeNull("a missing file should fall back to the white pixel");
                texture.ShouldNotBeSameAs(before, "setting the file should refresh the texture");
                (texture.Width, texture.Height).ShouldBe((1, 1));
                renderer.SinglePixelSourceRectangle.ShouldBeNull();
                canvas.Frame();
                IsWhite(canvas.Input.PixelAt(fillCenter)).ShouldBeTrue("the fill should still draw");
            }
            finally
            {
                properties.SinglePixelTextureFile = original.File;
                properties.SinglePixelTextureLeft = original.Left;
                properties.SinglePixelTextureTop = original.Top;
                properties.SinglePixelTextureRight = original.Right;
                properties.SinglePixelTextureBottom = original.Bottom;
            }

            canvas.AssertOracles();
        });
    }

    // A white fill, which the gray checkerboard behind the canvas never shows.
    private static void Fill(CanvasHarness canvas, ElementSave owner, string instanceName)
    {
        using (canvas.Project.UndoManager.RequestLock(owner))
        {
            owner.DefaultState!.SetValue($"{instanceName}.IsFilled", true, "bool");
        }
        canvas.Tree.SaveAll();
        canvas.Wireframe.RefreshAll(forceLayout: true);
    }

    private static bool IsWhite(Color color) => color.R > 240 && color.G > 240 && color.B > 240;

    // Lets the canvas's own frame timer run, as between user actions, without the harness drawing a frame.
    private static void PumpUntil(CanvasHarness canvas, Func<bool> condition, string what)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > TimeSpan.FromSeconds(5))
            {
                throw new TimeoutException($"Waited 5 s for {what}. {canvas.Describe()}");
            }
            Thread.Sleep(10);
            canvas.Input.Layout();
        }
    }

    #endregion

    #region Preview

    [SkippableFact]
    [Trait("Feature", "CANV-026")]
    public void PreviewButton_LaunchesThePreviewOnce_ThenFollowsTheSelection()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            ScreenSave mainMenu = canvas.Project.AddScreen("MainMenu");
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            PreviewLauncher launcher = canvas.Plugin.PreviewLauncher;
            IPreviewProcessStarter originalStarter = launcher.ProcessStarter;
            StandInPreview preview = new StandInPreview();
            launcher.ProcessStarter = preview;
            try
            {
                canvas.Input.Click(canvas.PreviewButton);

                preview.Started.Count.ShouldBe(1);
                string projectFile = canvas.Project.Project.FullFileName.ShouldNotBeNull();
                preview.Argument(0, "--project").ShouldBe(projectFile);
                preview.Argument(0, "--element").ShouldBe("Button");
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(preview.Argument(0, "--content-root")))
                    .ShouldBe(Path.GetDirectoryName(Path.GetFullPath(projectFile)));
                PreviewSelectionMessage launched = preview.Selection(0);
                launched.ElementName.ShouldBe("Button");
                launched.Activate.ShouldBeFalse();

                // Selecting in the tree follows along without raising the preview window.
                canvas.Tree.Click(canvas.Tree.NodeFor(mainMenu));
                preview.Selection(0).ElementName.ShouldBe("MainMenu");
                preview.Selection(0).Activate.ShouldBeFalse();

                // A second click raises the running preview instead of starting another.
                canvas.Input.Click(canvas.PreviewButton);
                preview.Started.Count.ShouldBe(1);
                preview.Selection(0).Activate.ShouldBeTrue();

                // Once the preview has closed, the button starts a new one.
                preview.HasExited = true;
                canvas.Input.Click(canvas.PreviewButton);
                preview.Started.Count.ShouldBe(2);
                preview.Argument(1, "--element").ShouldBe("MainMenu");
                canvas.Tree.OutputWritten.ShouldContain("Launched preview for MainMenu.");
            }
            finally
            {
                preview.HasExited = true;
                launcher.ProcessStarter = originalStarter;
                preview.DeleteSelectionFiles();
            }

            canvas.AssertOracles();
        });
    }

    // Stands in for GumPreview: records each launch and reports itself running until told otherwise.
    private sealed class StandInPreview : IPreviewProcessStarter, IPreviewProcess
    {
        public List<ProcessStartInfo> Started { get; } = new List<ProcessStartInfo>();

        public bool HasExited { get; set; }

        public ResolvedPreviewExecutable? Resolve() => new ResolvedPreviewExecutable("/Preview/GumPreview", IsNativeAot: false);

        public IPreviewProcess? Start(ProcessStartInfo startInfo)
        {
            Started.Add(startInfo);
            HasExited = false;
            return this;
        }

        public string Argument(int launch, string name)
        {
            List<string> arguments = Started[launch].ArgumentList.ToList();
            return arguments[arguments.IndexOf(name) + 1];
        }

        public PreviewSelectionMessage Selection(int launch) =>
            PreviewSelectionMessage.TryParse(File.ReadAllLines(Argument(launch, "--selection-file")))
            ?? throw new InvalidOperationException("The selection file holds no selection.");

        public void DeleteSelectionFiles()
        {
            for (int launch = 0; launch < Started.Count; launch++)
            {
                File.Delete(Argument(launch, "--selection-file"));
            }
        }
    }

    #endregion

    private static void OnCanvas(Action<CanvasHarness> scenario)
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            scenario(canvas);
        });
    }
}
