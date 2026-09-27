using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Panels;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins.Behaviors;
using Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;
using Gum.Plugins.Errors;
using Gum.Plugins.FileWatchPlugin;
using Gum.Plugins.InternalPlugins.Undos;
using Gum.Plugins.Undos;
using Gum.SelectionHistory;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Settings;
using Microsoft.Extensions.DependencyInjection;
using PerformanceMeasurementPlugin.ViewModels;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the tool's other tabs and the View menu (inventory areas TAB and VIEW,
/// and the KEY items that reach them): each hosts the head's own singleton tab view in a headless
/// window, clicks its buttons and rows, and checks the project, the saved files and the shell.
/// Tabs, menus, theme and font size live for the whole test process, so each scenario puts back
/// what it changed (<see cref="ProjectTreeHarness"/> restores tab visibility).
/// </summary>
[Trait("Category", "EndToEnd")]
public class TabViewScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    private static AvaloniaTabManager Tabs => (AvaloniaTabManager)Services.GetRequiredService<ITabManager>();

    #region Output, Errors and History

    [AvaloniaFact]
    [Trait("Feature", "TAB-001")]
    [Trait("Feature", "TAB-002")]
    [Trait("Feature", "TAB-003")]
    [Trait("Feature", "TAB-004")]
    [Trait("Feature", "TAB-005")]
    [Trait("Feature", "TAB-006")]
    [Trait("Feature", "TAB-007")]
    public void AnEditAndItsUndo_ShowInTheOutputErrorsAndHistoryTabs()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        InstanceSave icon = tree.Project.AddInstance(card, "Icon", "Sprite");
        tree.Click(tree.NodeFor(icon));
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        // Output: Clear empties it, and the next save writes a line.
        using (HeadlessWindowDriver output = HostTab("Output"))
        {
            MainOutputViewModel outputViewModel = (MainOutputViewModel)((Control)Tab("Output").Content).DataContext!;
            TextBox text = output.Window.GetVisualDescendants().OfType<TextBox>().Single();
            output.Click(ButtonWithTip(output, "Clear Output"));
            outputViewModel.OutputText.ShouldBeEmpty();
            (text.Text ?? "").ShouldBeEmpty();

            grid.TypeAndLeave("Width", "64");
            output.Layout();
            text.Text.ShouldNotBeNull().ShouldContain("Saved Card");
        }

        // Errors: a missing source file is listed and counted, and copies from the row.
        grid.Input.TypeAndEnter(grid.Row("SourceFile").GetVisualDescendants().OfType<TextBox>().First(), "Missing.png");
        tree.ThrowIfCrashed();
        AllErrorsViewModel errors = ErrorsViewModel();
        RecordingClipboardService clipboard = (RecordingClipboardService)Services.GetRequiredService<IClipboardService>();
        clipboard.Clear();
        using (HeadlessWindowDriver errorsWindow = HostTab("Errors"))
        {
            ErrorViewModel missing = errors.Errors.ShouldHaveSingleItem();
            missing.Code.ShouldBe("GUM0006");
            missing.Message.ShouldContain("Missing.png");
            errors.CountDescription.ShouldBe("1 Error");
            ErrorsView list = (ErrorsView)Tab("Errors").Content;
            list.GetRealizedContainers().Count().ShouldBe(1);

            errorsWindow.Click(list.GetRealizedContainers().Single());
            errors.SelectedItem.ShouldBeSameAs(missing);

            errorsWindow.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
            clipboard.LastText.ShouldBe(missing.ClipboardText);
            clipboard.Clear();
            errorsWindow.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control | RawInputModifiers.Shift);
            clipboard.LastText.ShouldBe(missing.ClipboardText);

            errorsWindow.RightClick(list.GetRealizedContainers().Single());
            errorsWindow.ContextMenuHeaders().ShouldBe(new[] { "_Copy", "Copy _All Errors" });
            clipboard.Clear();
            errorsWindow.PickContextMenuItem("Copy _All Errors");
            clipboard.LastText.ShouldBe(missing.ClipboardText);
        }

        // History: adding the instance, then both edits, the newest current; a click on an entry
        // does not move the undo position.
        using (HeadlessWindowDriver historyWindow = HostTab("History"))
        {
            UndosView history = (UndosView)Tab("History").Content;
            UndosViewModel historyViewModel = (UndosViewModel)history.DataContext!;
            historyViewModel.HistoryItems.Select(item => item.Display).ShouldBe(new[]
            {
                "Add instances: Icon",
                "Variables in Default: Icon.Width=64",
                "Variables in Default: Icon.SourceFile=Missing.png",
            });
            history.SelectedIndex.ShouldBe(2);
            ProjectFileSnapshot beforeClick = tree.SnapshotFiles();

            historyWindow.Click(history.GetRealizedContainers().First());

            history.SelectedIndex.ShouldBe(2, "History is read-only; a click only scrolls");
            historyViewModel.UndoIndex.ShouldBe(2);
            tree.SnapshotFiles().ShouldMatch(beforeClick, "clicking a History entry is not an undo");

            tree.Undo();
            historyWindow.Layout();
            history.SelectedIndex.ShouldBe(1);
            historyViewModel.HistoryItems.Select(item => item.UndoOrRedo).ShouldBe(new[] { UndoOrRedo.Undo, UndoOrRedo.Undo, UndoOrRedo.Redo });
            errors.Errors.ShouldBeEmpty("undoing the missing file clears its error");
            errors.CountDescription.ShouldBe("0 Errors");

            tree.Undo();
            tree.SnapshotFiles().ShouldMatch(start, "undoing both edits restores the files");
            tree.Redo();
            historyWindow.Layout();
            history.SelectedIndex.ShouldBe(1);
        }

        VariableGridHarness.StoredValue(card, "Icon.Width").ShouldBe(64f);
        VariableGridHarness.StoredValue(card, "Icon.SourceFile").ShouldBeNull();
        tree.AssertOracles();
    }

    #endregion

    #region Alignment and Behaviors

    [AvaloniaFact]
    [Trait("Feature", "TAB-008")]
    [Trait("Feature", "TAB-009")]
    [Trait("Feature", "TAB-010")]
    public void AlignmentButtons_AnchorDockAndSizeToChildren_SaveEachLayout_AndUndoBack()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        InstanceSave panel = tree.Project.AddInstance(card, "Panel", "Container");
        tree.Click(tree.NodeFor(panel));
        ProjectFileSnapshot start = tree.SnapshotFiles();
        using HeadlessWindowDriver alignment = HostTab("Alignment");
        TextBox margin = alignment.Window.GetVisualDescendants().OfType<TextBox>().Single();

        // The tab's view model outlives the test, margin included.
        AlignmentViewModel alignmentViewModel = (AlignmentViewModel)margin.DataContext!;
        string originalMargin = alignmentViewModel.DockMarginText;
        try
        {
            alignment.TypeAndEnter(margin, "4");
            alignment.Click(ButtonWithTip(alignment, "Anchor Bottom Right"));
            tree.ThrowIfCrashed();

            ComponentSave saved = tree.Grid.ReadSaved(card);
            VariableGridHarness.StoredValue(saved, "Panel.X").ShouldBe(-4f);
            SavedEnum(saved, "Panel.XOrigin").ShouldBe((int)global::RenderingLibrary.Graphics.HorizontalAlignment.Right);
            SavedEnum(saved, "Panel.XUnits").ShouldBe((int)PositionUnitType.PixelsFromRight);
            VariableGridHarness.StoredValue(saved, "Panel.Y").ShouldBe(-4f);
            SavedEnum(saved, "Panel.YUnits").ShouldBe((int)PositionUnitType.PixelsFromBottom);

            alignment.Click(ButtonWithTip(alignment, "Fill"));
            tree.ThrowIfCrashed();
            saved = tree.Grid.ReadSaved(card);
            VariableGridHarness.StoredValue(saved, "Panel.Width").ShouldBe(-8f);
            SavedEnum(saved, "Panel.WidthUnits").ShouldBe((int)DimensionUnitType.RelativeToParent);
            SavedEnum(saved, "Panel.XUnits").ShouldBe((int)PositionUnitType.PixelsFromCenterX);
            tree.Grid.Settle();
            tree.Grid.FieldText("Width").ShouldBe("-8", "the Variables tab shows the new size");

            alignment.Click(ButtonWithTip(alignment, "Size to Children"));
            tree.ThrowIfCrashed();
            saved = tree.Grid.ReadSaved(card);
            VariableGridHarness.StoredValue(saved, "Panel.Width").ShouldBe(8f);
            SavedEnum(saved, "Panel.WidthUnits").ShouldBe((int)DimensionUnitType.RelativeToChildren);
            SavedEnum(saved, "Panel.HeightUnits").ShouldBe((int)DimensionUnitType.RelativeToChildren);
            tree.Grid.Settle();
            tree.Grid.FieldText("Width").ShouldBe("8", "the Variables tab shows the new size");

            tree.Undo();
            VariableGridHarness.StoredValue(card, "Panel.WidthUnits").ShouldBe(DimensionUnitType.RelativeToParent);
            tree.Undo();
            tree.Undo();
            tree.SnapshotFiles().ShouldMatch(start, "undoing each button restores the files");
            tree.Redo();
            VariableGridHarness.StoredValue(card, "Panel.XUnits").ShouldBe(PositionUnitType.PixelsFromRight);
        }
        finally
        {
            alignmentViewModel.DockMarginText = originalMargin;
        }

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TAB-011")]
    [Trait("Feature", "VIEW-008")]
    public void BehaviorsTab_ShowsForAComponent_AndEditChecksABehaviorOnAndOff()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.Dialogs.AnswerNextUserString("Clickable");
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        AvaloniaPluginTab behaviorsTab = Tab("Behaviors");
        behaviorsTab.IsVisible.ShouldBeFalse("a behavior is selected, not a component");

        tree.Click(tree.NodeFor(card));
        behaviorsTab.IsVisible.ShouldBeTrue("selecting a component shows its behaviors");
        ProjectFileSnapshot start = tree.SnapshotFiles();
        using (HeadlessWindowDriver behaviors = HostTab("Behaviors"))
        {
            behaviors.Click(ButtonWithContent(behaviors, "Edit"));
            CheckBox clickable = behaviors.Window.GetVisualDescendants().OfType<CheckBox>()
                .Single(box => (box.DataContext as CheckListBehaviorItem)?.Name == "Clickable");
            clickable.IsChecked.ShouldBe(false);
            behaviors.Click(clickable);
            behaviors.Click(ButtonWithContent(behaviors, "OK"));
            tree.ThrowIfCrashed();

            card.Behaviors.Select(behavior => behavior.BehaviorName).ShouldBe(new[] { "Clickable" });
            tree.Grid.ReadSaved(card).Behaviors.Select(behavior => behavior.BehaviorName).ShouldBe(new[] { "Clickable" });
            ((BehaviorsViewModel)((Control)behaviorsTab.Content).DataContext!).AddedBehaviors
                .Select(item => item.Name).ShouldBe(new[] { "Clickable" });

            tree.Undo();
            card.Behaviors.ShouldBeEmpty();
            tree.SnapshotFiles().ShouldMatch(start, "undoing the behavior edit restores the files");
            tree.Redo();
            card.Behaviors.Select(behavior => behavior.BehaviorName).ShouldBe(new[] { "Clickable" });
        }

        tree.Click(tree.RootNode("Behaviors").Nodes.Single());
        behaviorsTab.IsVisible.ShouldBeFalse("selecting something else hides the tab again");
        tree.AssertOracles();
    }

    #endregion

    #region View menu tabs

    [AvaloniaFact]
    [Trait("Feature", "VIEW-004")]
    [Trait("Feature", "VIEW-005")]
    [Trait("Feature", "VIEW-006")]
    [Trait("Feature", "TAB-012")]
    [Trait("Feature", "TAB-013")]
    [Trait("Feature", "KEY-018")]
    [Trait("Feature", "KEY-019")]
    public void ViewMenu_ShowsAndHidesTheHotkeysFileWatchAndAnimationsTabs_AndCtrlQuestionShowsHotkeys()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        // Loading the project starts the file watch that the File Watch tab reports on.
        tree.Project.SaveAndReload();
        ProjectFileSnapshot start = tree.SnapshotFiles();
        MenuItemModel view = Services.GetRequiredService<MenuModel>().GetItem("View")!;
        AvaloniaPluginTab hotkeys = Tab("Hotkeys");
        AvaloniaPluginTab fileWatch = Tab("File Watch");
        AvaloniaPluginTab animations = Tab("Animations");
        hotkeys.IsVisible = false;
        fileWatch.IsVisible = false;
        animations.IsVisible = false;
        Dispatcher.UIThread.RunJobs();

        // View Hotkeys shows the tab in front, and the item then reads Hide Hotkeys.
        tree.PickMainMenu("View", "View Hotkeys");
        hotkeys.IsVisible.ShouldBeTrue();
        hotkeys.IsSelected.ShouldBeTrue();
        Tabs.CenterBottom.ShouldContain(hotkeys);
        view.Items.Select(item => item.Header).ShouldContain("Hide Hotkeys");
        using (HeadlessWindowDriver hotkeysWindow = HostTab("Hotkeys", height: 1200))
        {
            List<string> rows = ((HotkeyView)hotkeys.Content).GetRealizedContainers().OfType<ListBoxItem>()
                .Select(row => row.GetVisualDescendants().OfType<TextBlock>().First().Text ?? "").ToList();
            IHotkeyManager hotkeyManager = Services.GetRequiredService<IHotkeyManager>();
            IKeyCombinationFormatter formatter = Services.GetRequiredService<IKeyCombinationFormatter>();
            rows.ShouldContain("Undo: " + formatter.Format(hotkeyManager.Undo));
            rows.ShouldContain("Show Hotkeys: " + formatter.Format(hotkeyManager.ShowHotkeys));
        }
        tree.PickMainMenu("View", "Hide Hotkeys");
        hotkeys.IsVisible.ShouldBeFalse();
        Dispatcher.UIThread.RunJobs();
        Tabs.CenterBottom.ShouldNotContain(hotkeys);

        // Ctrl+? brings it back from anywhere in the window.
        tree.Click(tree.RootNode("Components"));
        tree.Press(Key.Oem2, PhysicalKey.Slash, RawInputModifiers.Control);
        hotkeys.IsVisible.ShouldBeTrue("Ctrl+? shows the Hotkeys tab");
        hotkeys.IsSelected.ShouldBeTrue();

        // Show File Watch: the watched folders, and the print option reaches the watcher.
        IFileWatchManager watcher = Services.GetRequiredService<IFileWatchManager>();
        tree.PickMainMenu("View", "Show File Watch");
        try
        {
            fileWatch.IsVisible.ShouldBeTrue();
            view.Items.Select(item => item.Header).ShouldContain("Hide File Watch");
            FileWatchViewModel fileWatchViewModel = (FileWatchViewModel)((Control)fileWatch.Content).DataContext!;
            using HeadlessWindowDriver fileWatchWindow = HostTab("File Watch");
            // The tab refreshes on a timer while it shows.
            string projectFolderName = Path.GetFileName(tree.Project.ProjectFolder.TrimEnd('/', '\\'));
            tree.WaitUntil(() => fileWatchViewModel.WatchFolderInformation?.Contains(projectFolderName, StringComparison.OrdinalIgnoreCase) == true,
                TimeSpan.FromSeconds(10), $"the File Watch tab to list the project folder {projectFolderName} (watching [{string.Join(", ", watcher.CurrentFilePathsWatching)}], showing \"{fileWatchViewModel.WatchFolderInformation}\")");
            CheckBox print = fileWatchWindow.Window.GetVisualDescendants().OfType<CheckBox>().Single();
            fileWatchWindow.Click(print);
            watcher.PrintFileChangesToOutput.ShouldBeTrue();
            fileWatchWindow.Click(print);
            watcher.PrintFileChangesToOutput.ShouldBeFalse();
        }
        finally
        {
            watcher.PrintFileChangesToOutput = false;
            if (fileWatch.IsVisible)
            {
                tree.PickMainMenu("View", "Hide File Watch");
            }
        }
        fileWatch.IsVisible.ShouldBeFalse();
        view.Items.Select(item => item.Header).ShouldContain("Show File Watch");

        tree.PickMainMenu("View", "View Animations");
        animations.IsVisible.ShouldBeTrue();
        animations.IsSelected.ShouldBeTrue();
        tree.PickMainMenu("View", "Hide Animations");
        animations.IsVisible.ShouldBeFalse();

        tree.SnapshotFiles().ShouldMatch(start, "showing and hiding tabs is not a project edit");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TAB-014")]
    public void PerformanceTab_SwitchesTheSiblingOrderAndTheCull_AndSwitchesThemBack()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        PerformanceViewModel viewModel = (PerformanceViewModel)((Control)Tab("Performance").Content).DataContext!;
        bool sortByBatch = viewModel.SortByBatchKey;
        bool cull = viewModel.CullOffscreenWhenClipped;
        try
        {
            using HeadlessWindowDriver performance = HostTab("Performance");
            RadioButton depthFirst = performance.Window.GetVisualDescendants().OfType<RadioButton>().Single(button => (string?)button.Content == "Depth-first (hierarchical)");
            RadioButton byBatch = performance.Window.GetVisualDescendants().OfType<RadioButton>().Single(button => (string?)button.Content == "Sort by batch");
            CheckBox culling = performance.Window.GetVisualDescendants().OfType<CheckBox>().Single();

            performance.Click(byBatch);
            viewModel.SortByBatchKey.ShouldBeTrue();
            depthFirst.IsChecked.ShouldBe(false);
            performance.Click(depthFirst);
            viewModel.SortByBatchKey.ShouldBeFalse();
            byBatch.IsChecked.ShouldBe(false);

            bool cullBefore = viewModel.CullOffscreenWhenClipped;
            performance.Click(culling);
            viewModel.CullOffscreenWhenClipped.ShouldBe(!cullBefore);
            culling.IsChecked.ShouldBe(!cullBefore);
            performance.Click(culling);
            viewModel.CullOffscreenWhenClipped.ShouldBe(cullBefore);
        }
        finally
        {
            viewModel.SortByBatchKey = sortByBatch;
            viewModel.CullOffscreenWhenClipped = cull;
        }
        tree.AssertOracles();
    }

    #endregion

    #region Shell

    [AvaloniaFact]
    [Trait("Feature", "VIEW-001")]
    [Trait("Feature", "VIEW-002")]
    [Trait("Feature", "VIEW-003")]
    [Trait("Feature", "VIEW-011")]
    [Trait("Feature", "VIEW-012")]
    [Trait("Feature", "VIEW-013")]
    public void ViewMenu_ToolsPaletteThemeAndFontSize_ChangeTheShell_WithoutTouchingTheProject()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        ProjectFileSnapshot start = tree.SnapshotFiles();
        MenuItemModel view = Services.GetRequiredService<MenuModel>().GetItem("View")!;
        IThemingService theming = Services.GetRequiredService<IThemingService>();
        IUiSettingsService uiSettings = Services.GetRequiredService<IUiSettingsService>();
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        ThemeMode? originalMode = theming.Mode;
        System.Drawing.Color? originalAccent = theming.Accent;
        ThemeVariant? originalVariant = Application.Current!.RequestedThemeVariant;
        double originalFontSize = uiSettings.BaseFontSize;
        bool? originalPalette = projectManager.UseStandardsPalette;
        bool originalEffectivePalette = projectManager.EffectiveUseStandardsPalette;
        MenuItemModel palette = view.Items.Single(item => item.Header == "Standards palette");
        IDisposable fontSize = AppWideWindowInput.FollowBaseFontSize(tree.Input.Window, Services.GetRequiredService<IAppScaleProvider>());
        try
        {
            // Hide Tools / Show Tools.
            tree.PickMainMenu("View", "Hide Tools");
            Tabs.IsToolsVisible.ShouldBeFalse();
            view.Items.Select(item => item.Header).ShouldContain("Show Tools");
            tree.PickMainMenu("View", "Show Tools");
            Tabs.IsToolsVisible.ShouldBeTrue();
            view.Items.Select(item => item.Header).ShouldContain("Hide Tools");

            // Standards palette off puts the Standard folder in the tree; on takes it out again.
            if (!palette.IsChecked)
            {
                tree.PickMainMenu("View", "Standards palette");
            }
            tree.View.Nodes.Select(node => node.Text).ShouldNotContain("Standard");
            tree.PickMainMenu("View", "Standards palette");
            palette.IsChecked.ShouldBeFalse();
            projectManager.EffectiveUseStandardsPalette.ShouldBeFalse();
            tree.Input.Layout();
            tree.RootNode("Standard").Nodes.Select(node => node.Text).ShouldContain("Sprite");
            tree.PickMainMenu("View", "Standards palette");
            palette.IsChecked.ShouldBeTrue();
            tree.Input.Layout();
            tree.View.Nodes.Select(node => node.Text).ShouldNotContain("Standard");

            // Theming: dark mode and an accent reach the app's resources.
            System.Drawing.Color accent = System.Drawing.Color.FromArgb(255, 0x20, 0x90, 0xd0);
            tree.Dialogs.AnswerNext<ThemingDialogViewModel>(dialog =>
            {
                dialog.Mode = ThemeMode.Dark;
                dialog.AccentColor = accent;
                return true;
            });
            tree.PickMainMenu("View", "Theming");
            tree.Input.Layout();
            tree.Input.Window.ActualThemeVariant.ShouldBe(ThemeVariant.Dark);
            ((ISolidColorBrush)Application.Current.Resources["Frb.Brushes.Primary"]!).Color.ShouldBe(Color.FromRgb(0x20, 0x90, 0xd0));
            tree.Dialogs.AnswerNext<ThemingDialogViewModel>(dialog =>
            {
                dialog.Mode = ThemeMode.Light;
                return true;
            });
            tree.PickMainMenu("View", "Theming");
            tree.Input.Layout();
            tree.Input.Window.ActualThemeVariant.ShouldBe(ThemeVariant.Light);

            // Ctrl+Plus and Ctrl+Minus scale the UI font, app-wide.
            tree.Click(tree.RootNode("Components"));
            tree.Press(Key.OemPlus, PhysicalKey.Equal, RawInputModifiers.Control);
            uiSettings.BaseFontSize.ShouldBeGreaterThan(originalFontSize);
            tree.Input.Window.FontSize.ShouldBe(uiSettings.BaseFontSize);
            tree.Press(Key.OemMinus, PhysicalKey.Minus, RawInputModifiers.Control);
            uiSettings.BaseFontSize.ShouldBe(originalFontSize);
            tree.Input.Window.FontSize.ShouldBe(originalFontSize);
        }
        finally
        {
            fontSize.Dispose();
            uiSettings.BaseFontSize = originalFontSize;
            theming.Mode = originalMode;
            theming.Accent = originalAccent;
            Application.Current.RequestedThemeVariant = originalVariant;
            Tabs.IsToolsVisible = true;
            if (palette.IsChecked != originalEffectivePalette)
            {
                tree.PickMainMenu("View", "Standards palette");
            }
            projectManager.UseStandardsPalette = originalPalette;
            projectManager.SaveGeneralSettings();
        }

        tree.SnapshotFiles().ShouldMatch(start, "the View menu changes tool settings, not project data");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "VIEW-015")]
    [Trait("Feature", "KEY-009")]
    [Trait("Feature", "KEY-017")]
    public void GoToDefinition_ThenMouseSideButtonsAndAltArrows_StepThroughTheSelectionHistory()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        ScreenSave title = tree.Project.AddScreen("Title");
        InstanceSave ok = tree.Project.AddInstance(title, "OkButton", "Button");
        AppWideWindowGestures.RouteSelectionHistoryButtons(tree.Input.Window, Services.GetRequiredService<ISelectionHistory>());
        tree.Click(tree.NodeFor(title));
        tree.Click(tree.NodeFor(ok));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Press(Key.F12, PhysicalKey.F12);
        tree.SelectedState.SelectedElement.ShouldBeSameAs(button, "F12 goes to the instance's component");

        SideButton(tree, MouseButton.XButton1);
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(ok, "the back button returns to the instance");
        SideButton(tree, MouseButton.XButton1);
        tree.SelectedState.SelectedElement.ShouldBeSameAs(title);
        tree.SelectedState.SelectedInstance.ShouldBeNull();
        SideButton(tree, MouseButton.XButton2);
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(ok, "the forward button steps forward again");

        tree.Press(Key.Right, PhysicalKey.ArrowRight, RawInputModifiers.Alt);
        tree.SelectedState.SelectedElement.ShouldBeSameAs(button, "Alt+Right steps forward");
        tree.Press(Key.Left, PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(ok, "Alt+Left steps back");
        tree.TreeManager.SelectedNode.ShouldBeSameAs(tree.NodeFor(ok), "the tree follows the history");

        tree.SnapshotFiles().ShouldMatch(start, "moving through the selection history is not an edit");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "VIEW-010")]
    [Trait("Feature", "VIEW-014")]
    public void TitleFollowsTheLoadedProject_AndTheMacMenusRunTheSameItems()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        ShellViewModel shell = Services.GetRequiredService<ShellViewModel>();
        shell.Title.ShouldBe(Path.GetFileNameWithoutExtension(tree.Project.ProjectFilePath));

        string otherFolder = Path.Combine(Path.GetTempPath(), "GumTabViewScenarios", Guid.NewGuid().ToString("N"));
        string otherProject = Path.Combine(otherFolder, "Other.gumx");
        CopyProjectFiles(tree.Project.ProjectFolder, otherFolder, Path.GetFileName(tree.Project.ProjectFilePath), "Other.gumx");
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        tree.Dialogs.AnswerNextOpenFile(otherProject);
        tree.PickMainMenu("File", "Load Project...");
        tree.WaitUntil(() => projectManager.GumProjectSave?.FullFileName?.EndsWith("Other.gumx", StringComparison.OrdinalIgnoreCase) == true,
            TimeSpan.FromSeconds(60), "the other project to load");
        shell.Title.ShouldBe("Other");

        // The macOS menu bar: the app menu's About Gum, and a View item built from the same model.
        NativeMenu appMenu = AvaloniaNativeMenuBuilder.BuildAppMenu(() => Services.GetRequiredService<StandardMenuModelBuilder>().ShowAbout());
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        ClickNative(appMenu, "About Gum");
        tree.Dialogs.Messages.Last().ShouldStartWith("Gum version");

        AvaloniaPluginTab hotkeys = Tab("Hotkeys");
        hotkeys.IsVisible = false;
        NativeMenu menuBar = AvaloniaNativeMenuBuilder.Build(Services.GetRequiredService<MenuModel>());
        ClickNative(menuBar, "View", "View Hotkeys");
        hotkeys.IsVisible.ShouldBeTrue("the menu-bar item runs the same action");
        NativeMenuItem viewHotkeys = NativeItem(menuBar, "View").Menu!.Items.OfType<NativeMenuItem>().Single(item => item.Header is "Hide Hotkeys" or "View Hotkeys");
        viewHotkeys.Header.ShouldBe("Hide Hotkeys", "the menu-bar item follows the model's header");

        // Back to the harness's project, which its oracles check.
        tree.Dialogs.AnswerNextOpenFile(tree.Project.ProjectFilePath);
        tree.PickMainMenu("File", "Load Project...");
        tree.WaitUntil(() => projectManager.GumProjectSave?.FullFileName is { } loaded
            && Path.GetFullPath(loaded) == Path.GetFullPath(tree.Project.ProjectFilePath), TimeSpan.FromSeconds(60), "the harness project to load");
        shell.Title.ShouldBe(Path.GetFileNameWithoutExtension(tree.Project.ProjectFilePath));
        TryDeleteFolder(otherFolder);

        tree.AssertOracles();
    }

    #endregion

    #region Helpers

    private static AvaloniaPluginTab Tab(string title) => Tabs.AllTabs.Single(tab => tab.Title == title);

    /// <summary>The head's own view of the tab titled <paramref name="title"/>, in a headless window.</summary>
    private static HeadlessWindowDriver HostTab(string title, double width = 500, double height = 700) =>
        new HeadlessWindowDriver((Control)Tab(title).Content, width, height, framesFolderName: "GumTabViewScenarios", contentOutlivesTest: true);

    /// <summary>An enum variable as the saved file holds it, whether it loaded as the enum or its number.</summary>
    private static int SavedEnum(ElementSave saved, string variableName) =>
        Convert.ToInt32(VariableGridHarness.StoredValue(saved, variableName) ?? throw new InvalidOperationException($"{variableName} was not saved."));

    private static AllErrorsViewModel ErrorsViewModel() => (AllErrorsViewModel)((Control)Tab("Errors").Content).DataContext!;

    private static Button ButtonWithTip(HeadlessWindowDriver window, string tip) =>
        window.Window.GetVisualDescendants().OfType<Button>().SingleOrDefault(button => ToolTip.GetTip(button) as string == tip)
        ?? throw new InvalidOperationException($"No button has the tip \"{tip}\".");

    private static Button ButtonWithContent(HeadlessWindowDriver window, string content) =>
        window.Window.GetVisualDescendants().OfType<Button>().SingleOrDefault(button => button.IsEffectivelyVisible && button.Content as string == content)
        ?? throw new InvalidOperationException($"No visible button reads \"{content}\".");

    private static void SideButton(ProjectTreeHarness tree, MouseButton button)
    {
        Point point = new Point(200, 450);
        tree.Input.Window.MouseDown(point, button);
        tree.Input.Window.MouseUp(point, button);
        tree.Input.Layout();
        tree.ThrowIfCrashed();
    }

    private static NativeMenuItem NativeItem(NativeMenu menu, string header) =>
        menu.Items.OfType<NativeMenuItem>().SingleOrDefault(item => item.Header == header)
        ?? throw new InvalidOperationException($"The menu has no \"{header}\"; it has [{string.Join(", ", menu.Items.OfType<NativeMenuItem>().Select(item => item.Header))}].");

    private static void ClickNative(NativeMenu menu, params string[] path)
    {
        NativeMenuItem item = NativeItem(menu, path[0]);
        foreach (string header in path.Skip(1))
        {
            item = NativeItem(item.Menu!, header);
        }
        ((INativeMenuItemExporterEventsImplBridge)item).RaiseClicked();
        Dispatcher.UIThread.RunJobs();
    }

    private static void CopyProjectFiles(string from, string to, string projectFileName, string newProjectFileName)
    {
        foreach (string file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(from, file);
            if (relative.StartsWith("UserData", StringComparison.Ordinal))
            {
                continue;
            }
            if (relative == projectFileName)
            {
                relative = newProjectFileName;
            }
            string target = Path.Combine(to, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    private static void TryDeleteFolder(string folder)
    {
        try
        {
            Directory.Delete(folder, recursive: true);
        }
        catch (IOException)
        {
            // A file watcher may still hold it; the temp folder is cleaned up later.
        }
    }

    #endregion
}
