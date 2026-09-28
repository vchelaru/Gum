using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.StateAnimation;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StateAnimationPlugin.Timeline;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// The Animations tab, live on the head's real service graph, driven the way a user drives it:
/// pointer and key input into a headless window (nothing reaches the desktop), dialogs answered by
/// a script, and the results read back from the view, the view model and the saved sidecar file.
/// The tab is a plugin instance of this harness's own, over a temp project, so the head's singleton
/// plugin and its tab stay untouched for the main-window tests.
/// </summary>
internal sealed class AnimationEditorHarness : IDisposable
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    private readonly PluginManager _pluginManager;
    private readonly ToolProjectFixture _fixture;
    private readonly AvaloniaTabManager? _tabManager;
    private readonly AvaloniaPluginTab? _tab;
    private readonly PluginBase? _headPlugin;
    private readonly MenuItemModel? _menuItemAdded;
    private readonly HeadlessWindowDriver? _driver;
    private readonly string _sidecarExtension;
    private ToolExceptionWatch? _exceptions;

    /// <param name="jsonProject">True for a .gumj project, whose sidecars are .ganj files.</param>
    /// <param name="userDataFolder">
    /// Where the tool's per-user files (the plugin's settings) go for this run; a second harness given
    /// the same folder starts the way a restarted tool would. Defaults to a folder under the temp project.
    /// </param>
    public AnimationEditorHarness(bool jsonProject = false, string? userDataFolder = null)
    {
        _sidecarExtension = jsonProject ? "Animations.ganj" : "Animations.ganx";
        _pluginManager = Services.GetRequiredService<PluginManager>();
        _fixture = new ToolProjectFixture("GumAnimationEditor", jsonProject ? "AnimationEditor.gumj" : "AnimationEditor.gumx", userDataFolder);
        try
        {
            MenuModel menu = Services.GetRequiredService<MenuModel>();
            MenuItemModel viewMenu = menu.GetItem("View") ?? throw new InvalidOperationException("The head has no View menu.");
            List<MenuItemModel> viewItemsBefore = viewMenu.Items.ToList();
            Plugin = ActivatorUtilities.CreateInstance<TestAnimationPlugin>(Services);
            Plugin.GuiCommands = Services.GetRequiredService<IGuiCommands>();
            Plugin.FileCommands = Services.GetRequiredService<IFileCommands>();
            Plugin.TabManager = Services.GetRequiredService<ITabManager>();
            Plugin.DialogService = Dialogs;
            Plugin.Menu = menu;
            // Started through its container, which then knows StartUp ran, so turning the plugin
            // off and on does not run it again.
            PluginContainer container = new PluginContainer(Plugin);
            container.StartUpIfNeeded();
            _menuItemAdded = viewMenu.Items.Except(viewItemsBefore).SingleOrDefault();

            // Selection, rename, undo and the other plugin events reach this instance through the
            // manager in place of the head's own instance, which would otherwise handle every
            // event too (moving or copying the same sidecar first); the fixture puts the set back.
            _headPlugin = _pluginManager.InitializedPlugins.FirstOrDefault(plugin => plugin is AvaloniaStateAnimationPlugin);
            _pluginManager.Plugins = _pluginManager.InitializedPlugins.Where(plugin => plugin != _headPlugin).Append(Plugin).ToList();
            _pluginManager.PluginContainers[Plugin] = container;

            _tabManager = (AvaloniaTabManager)Services.GetRequiredService<ITabManager>();
            _tab = _tabManager.AllTabs.Last(tab => tab.Title == "Animations");
            _tab.Show();
            View = (AnimationsView)_tab.Content;
            _driver = new HeadlessWindowDriver(View, width: 1100, height: 640, framesFolderName: "GumAnimationEditor");
        }
        catch
        {
            // Nothing disposes a harness whose constructor threw.
            Dispose();
            throw;
        }
    }

    /// <summary>The plugin manager the tab's plugin is registered with, for events the tool raises.</summary>
    public PluginManager PluginManager => _pluginManager;

    /// <summary>The Animations tab this harness added to the tab manager.</summary>
    public AvaloniaPluginTab Tab => _tab!;

    /// <summary>The View menu entry this harness's plugin added ("View Animations" / "Hide Animations").</summary>
    public MenuItemModel ViewMenuItem => _menuItemAdded ?? throw new InvalidOperationException("The plugin added no View menu entry.");

    /// <summary>The temp project, for another tab's harness over the same project (the Variables tab).</summary>
    public ToolProjectFixture Fixture => _fixture;

    /// <summary>The scripted dialogs; queue an answer before the gesture that opens one.</summary>
    public ScriptedDialogService Dialogs => _fixture.Dialogs;

    /// <summary>This harness's plugin instance.</summary>
    public TestAnimationPlugin Plugin { get; } = null!;

    /// <summary>The tab's view, hosted in <see cref="Window"/>.</summary>
    public AnimationsView View { get; } = null!;

    /// <summary>Input, context menus and pixel reads for the tab's window.</summary>
    public HeadlessWindowDriver Input => _driver!;

    /// <summary>The headless window the view lives in; input goes through it.</summary>
    public Window Window => Input.Window;

    /// <summary>The temp project the tab edits.</summary>
    public GumProjectSave Project => _fixture.Project;

    /// <summary>The folder <see cref="Project"/> lives in; element files and sidecars resolve under it.</summary>
    public string ProjectFolder => _fixture.ProjectFolder;

    /// <summary>The project file's full path.</summary>
    public string ProjectFilePath => _fixture.ProjectFilePath;

    public ISelectedState SelectedState => _fixture.SelectedState;

    public IUndoManager UndoManager => _fixture.UndoManager;

    /// <summary>The view model the tab currently shows.</summary>
    public ElementAnimationsViewModel ViewModel => (ElementAnimationsViewModel)View.DataContext!;

    /// <summary>The animation list, left column.</summary>
    public ListBox AnimationList => Lists[0];

    /// <summary>The keyframe list, middle column.</summary>
    public ListBox KeyframeList => Lists[1];

    public TimelineView Timeline => Window.GetVisualDescendants().OfType<TimelineView>().Single();

    public KeyframeDetailView Detail => Window.GetVisualDescendants().OfType<KeyframeDetailView>().Single();

    /// <summary>The time box above the timeline.</summary>
    public TextBox TimelineTimeBox => Timeline.GetVisualDescendants().OfType<TextBox>().First();

    public Slider Scrubber => Timeline.GetVisualDescendants().OfType<Slider>().Single();

    public Thumb ScrubberThumb => Scrubber.GetVisualDescendants().OfType<Thumb>().Single();

    /// <summary>The splitter between the animation and keyframe columns.</summary>
    public GridSplitter ColumnSplitter => Window.GetVisualDescendants().OfType<GridSplitter>().First(splitter => splitter.ResizeDirection == GridResizeDirection.Columns);

    /// <summary>The animation column's width over the keyframe column's, as laid out.</summary>
    public double AnimationColumnRatio
    {
        get
        {
            Layout();
            Grid columns = (Grid)AnimationList.GetVisualParent()!.GetVisualParent()!;
            return columns.ColumnDefinitions[0].ActualWidth / columns.ColumnDefinitions[2].ActualWidth;
        }
    }

    /// <summary>The context menu a right-click opened, or null when none is open.</summary>
    public ContextMenu? OpenContextMenu => Input.OpenContextMenu;

    /// <summary>The text box inside the selected keyframe's editable state combo.</summary>
    public TextBox StateComboTextBox => DetailCombos[0].GetVisualDescendants().OfType<TextBox>().Single();

    /// <summary>The detail column's header text: "State", the event or sub-animation name, or "No Keyframe Selected".</summary>
    public string DetailTitle
    {
        get
        {
            Layout();
            DockPanel column = (DockPanel)Detail.GetVisualParent()!;
            return column.GetVisualDescendants().OfType<TextBlock>().First().Text ?? "";
        }
    }

    /// <summary>The selected keyframe's time box in the detail column.</summary>
    public TextBox DetailTimeBox => Detail.GetVisualDescendants().OfType<TextBox>().Single(box => box.FindAncestorOfType<ComboBox>() == null);

    /// <summary>The selected keyframe's state, interpolation and easing boxes, in that order.</summary>
    public List<ComboBox> DetailCombos => Detail.GetVisualDescendants().OfType<ComboBox>().ToList();

    public ToggleButton PlayButton => Window.GetVisualDescendants().OfType<ToggleButton>().First(button => button.Content is string text && text.EndsWith("Play") || button.Content is string stop && stop.EndsWith("Stop"));

    public Button AddAnimationButton => ButtonWithTip("Add Animation");

    public Button AddKeyframeButton => ButtonWithTip("Add a keyframe");

    private List<ListBox> Lists => Window.GetVisualDescendants().OfType<ListBox>().ToList();

    #region Project

    /// <summary>
    /// Adds a Container component with a Default state and one category holding
    /// <paramref name="states"/>; each state sets X so keyframes have something to interpolate.
    /// </summary>
    public ComponentSave AddComponent(string name, string category, params string[] states)
    {
        ComponentSave component = new ComponentSave();
        Services.GetRequiredService<ProjectCommands>().PrepareNewComponentSave(component, name, "Container");
        AddCategoryStateVariable(component, category);
        StateSaveCategory categorySave = new StateSaveCategory { Name = category };
        for (int i = 0; i < states.Length; i++)
        {
            StateSave state = new StateSave { Name = states[i], ParentContainer = component };
            state.SetValue("X", (float)(i * 100), "float");
            categorySave.States.Add(state);
        }
        component.Categories.Add(categorySave);
        Project.Components.Add(component);
        // The project file lists its elements by reference; without one a reload drops the element.
        Project.ComponentReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Component });
        return component;
    }

    /// <summary>Adds a screen with a Default state and one category holding <paramref name="states"/>.</summary>
    public ScreenSave AddScreen(string name, string category, params string[] states)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        screen.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Screen"));
        AddCategoryStateVariable(screen, category);
        StateSaveCategory categorySave = new StateSaveCategory { Name = category };
        foreach (string stateName in states)
        {
            categorySave.States.Add(new StateSave { Name = stateName, ParentContainer = screen });
        }
        screen.Categories.Add(categorySave);
        Project.Screens.Add(screen);
        Project.ScreenReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Screen });
        return screen;
    }

    // The Default state holds the category's state variable, as the tool's Add Category leaves it;
    // without it, loading the saved element adds one and a re-save changes the file.
    private static void AddCategoryStateVariable(ElementSave element, string category) =>
        element.GetDefaultStateOrThrow().Variables.Add(new VariableSave { Name = category + "State", Type = category, SetsValue = true });

    /// <summary>Adds an instance of <paramref name="type"/> named <paramref name="name"/> to <paramref name="owner"/>.</summary>
    public InstanceSave AddInstance(ElementSave owner, string name, ComponentSave type)
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = type.Name, ParentContainer = owner };
        owner.Instances.Add(instance);
        return instance;
    }

    /// <summary>Selects <paramref name="element"/> in the tool, which loads its animations into the tab.</summary>
    public void Select(ElementSave element)
    {
        SelectedState.SelectedElement = element;
        Layout();
        ThrowIfPluginFailed();
        if (SelectedState.SelectedElement != element || ViewModel.Element != element)
        {
            throw new InvalidOperationException($"Selecting {element.Name} did not stick: the tool selected {SelectedState.SelectedElement?.Name ?? "nothing"} and the tab shows {ViewModel.Element?.Name ?? "nothing"}.");
        }
    }

    /// <summary>The sidecar file the tab saves <paramref name="element"/>'s animations to.</summary>
    public string AnimationFilePath(ElementSave element)
    {
        string folder = element is ScreenSave ? "Screens" : "Components";
        return Path.Combine(ProjectFolder, folder, element.Name + _sidecarExtension);
    }

    /// <summary>Reads the saved sidecar back, or null when none has been written.</summary>
    public ElementAnimationsSave? ReadSavedAnimations(ElementSave element)
    {
        string path = AnimationFilePath(element);
        return File.Exists(path) ? ElementAnimationsSave.Load(path) : null;
    }

    #endregion

    #region Gestures

    /// <summary>
    /// Fails with the plugin's own exception when a tool event crashed it: the plugin manager
    /// disables a plugin that throws, after which the tab silently stops following the tool.
    /// </summary>
    public void ThrowIfPluginFailed()
    {
        PluginContainer container = _pluginManager.PluginContainers[Plugin];
        if (!container.IsEnabled)
        {
            throw new InvalidOperationException($"The Animations plugin was disabled: {container.FailureDetails}", container.FailureException);
        }
    }

    /// <summary>
    /// Lets real time pass while pumping the dispatcher, so playback timers tick; synchronous, since
    /// an awaiting test needs a nested dispatcher frame the headless session does not always allow.
    /// </summary>
    public void Wait(TimeSpan duration)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.Elapsed < duration)
        {
            Thread.Sleep(10);
            FireTimers();
            Dispatcher.UIThread.RunJobs();
        }
        Layout();
    }

    /// <summary>Raises a tick on every running playback timer, as the dispatcher would.</summary>
    public void FireTimers()
    {
        foreach (ManualUiTimer timer in Plugin.Timers.ToList())
        {
            timer.Fire();
        }
    }

    /// <summary>Pumps the dispatcher until <paramref name="condition"/> holds or <paramref name="timeout"/> passes.</summary>
    public bool WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && stopwatch.Elapsed < timeout)
        {
            Thread.Sleep(10);
            FireTimers();
            Dispatcher.UIThread.RunJobs();
        }
        Layout();
        return condition();
    }

    // The gestures scenarios use most, forwarded to Input so the scenarios read as the user's steps.

    /// <inheritdoc cref="HeadlessWindowDriver.Layout"/>
    public void Layout() => Input.Layout();

    public Point CenterOf(Control control) => Input.CenterOf(control);

    public void Click(Control control, RawInputModifiers modifiers = RawInputModifiers.None) => Input.Click(control, modifiers);

    public void ClickAt(Point point, RawInputModifiers modifiers = RawInputModifiers.None) => Input.ClickAt(point, modifiers);

    /// <inheritdoc cref="HeadlessWindowDriver.RightClick"/>
    public void RightClick(Control control) => Input.RightClick(control);

    /// <inheritdoc cref="HeadlessWindowDriver.PickContextMenuItem"/>
    public void PickContextMenuItem(string header) => Input.PickContextMenuItem(header);

    public void Hover(Point point) => Input.Hover(point);

    /// <inheritdoc cref="HeadlessWindowDriver.Drag"/>
    public void Drag(Point from, Point to) => Input.Drag(from, to);

    public void Press(Key key, PhysicalKey physicalKey, RawInputModifiers modifiers = RawInputModifiers.None) => Input.Press(key, physicalKey, modifiers);

    /// <inheritdoc cref="HeadlessWindowDriver.TypeAndEnter"/>
    public void TypeAndEnter(TextBox box, string text) => Input.TypeAndEnter(box, text);

    /// <summary>The list row showing <paramref name="item"/>.</summary>
    public Control RowFor(ListBox list, object item)
    {
        Layout();
        return list.ContainerFromItem(item) ?? throw new InvalidOperationException("The list has no row for the item.");
    }

    /// <summary>The height of a timeline row's track; markers are drawn 60% of it.</summary>
    public double TimelineTrackHeight => Timeline.GetVisualDescendants().OfType<TimelineTrack>().First().Bounds.Height;

    /// <summary>The tooltip set on the track that shows <paramref name="keyframe"/>.</summary>
    public string? TimelineTrackTipFor(AnimatedKeyframeViewModel keyframe) =>
        ToolTip.GetTip(Timeline.GetVisualDescendants().OfType<TimelineTrack>().First(track => track.Row.Items.Contains(keyframe))) as string;

    /// <summary>The middle of <paramref name="keyframe"/>'s marker on the timeline.</summary>
    public Point KeyframeMarkerCenter(AnimatedKeyframeViewModel keyframe)
    {
        Layout();
        TimelineTrack track = Timeline.GetVisualDescendants().OfType<TimelineTrack>()
            .FirstOrDefault(candidate => candidate.Row.Items.Contains(keyframe))
            ?? throw new InvalidOperationException($"No timeline row shows {keyframe.DisplayString}.");
        double length = Timeline.DrawnLength;
        double width = track.Bounds.Width;
        double height = track.Bounds.Height;
        double size = height * 0.6;
        Point local;
        if (!string.IsNullOrEmpty(keyframe.AnimationName))
        {
            double left = TimelineLayout.TimeToX(keyframe.Time, length, width);
            local = new Point(left + TimelineLayout.LengthToWidth(keyframe.Length, length, width) / 2, height / 2);
        }
        else
        {
            local = new Point(TimelineLayout.CenteredLeft(keyframe.Time, length, width, size) + size / 2, height / 2);
        }
        return track.TranslatePoint(local, Window)!.Value;
    }

    /// <summary>
    /// Opens the keyframe column's + menu and picks <paramref name="header"/> (State, Sub-Animation
    /// or Named Event); the action runs after the menu closes, as in the head.
    /// </summary>
    public void PickAddKeyframe(string header)
    {
        Click(AddKeyframeButton);
        MenuFlyout flyout = (MenuFlyout)AddKeyframeButton.Flyout!;
        MenuItem item = flyout.Items.OfType<MenuItem>().Single(candidate => (string)candidate.Header! == header);
        MenuItem? visual = Window.GetVisualDescendants().OfType<MenuItem>().FirstOrDefault(candidate => candidate == item);
        if (visual != null && visual.IsEffectivelyVisible)
        {
            Click(visual);
        }
        else
        {
            item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }
        flyout.Hide();
        Layout();
    }

    /// <summary>Clicks + above the animation list and fills the dialog in.</summary>
    public AnimationViewModel AddAnimation(string name, bool loops = false)
    {
        bool asked = false;
        Dialogs.AnswerNext<AddAnimationDialogViewModel>(dialog =>
        {
            asked = true;
            dialog.Name = name;
            dialog.Loops = loops;
            return true;
        });
        Click(AddAnimationButton);
        if (!asked)
        {
            throw new InvalidOperationException($"The click on the + button did not open the add-animation dialog: {DescribeClickTarget(AddAnimationButton)}");
        }
        return ViewModel.Animations.SingleOrDefault(animation => animation.Name == name)
            ?? throw new InvalidOperationException($"{name} was not added: the tab shows {ViewModel.Element?.Name ?? "no element"} with [{string.Join(", ", ViewModel.Animations.Select(animation => animation.Name))}] and the tool selected {SelectedState.SelectedElement?.Name ?? "nothing"}.");
    }

    /// <summary>What the window hit-tests where <paramref name="control"/> is, for a gesture that did not land.</summary>
    private string DescribeClickTarget(Control control)
    {
        Point point = CenterOf(control);
        Control? hit = Window.InputHitTest(point) as Control;
        return $"at {point} the window hit-tests {hit?.GetType().Name ?? "nothing"}{(hit == control ? " (the control)" : "")}; control bounds {control.Bounds}, visible {control.IsEffectivelyVisible}, enabled {control.IsEffectivelyEnabled}, DataContext {(View.DataContext == null ? "null" : "set")}";
    }

    /// <summary>Adds a state keyframe through the + menu, picking <paramref name="stateName"/> in the dialog.</summary>
    public AnimatedKeyframeViewModel AddStateKeyframe(string stateName)
    {
        Dialogs.AnswerNext<AddStateKeyframeDialog>(dialog =>
        {
            dialog.SelectedState = stateName;
            return true;
        });
        PickAddKeyframe("State");
        return ViewModel.SelectedAnimation!.SelectedKeyframe ?? throw new InvalidOperationException("No keyframe was added.");
    }

    #endregion

    #region End-to-end oracles

    /// <summary>
    /// Readies an end-to-end scenario after its setup: saves every file (setup edits the project
    /// directly), routes the main window's app-wide hotkeys (Ctrl+Z, Ctrl+Y) to the tab's window,
    /// starts watching for exceptions, and returns the files, for "undoing back to the start
    /// restores them". Call it with the element to edit already selected.
    /// </summary>
    public ProjectFileSnapshot StartScenario()
    {
        SaveAll();
        if (_exceptions == null)
        {
            TimelineEndToEndTests.RouteAppWideHotkeys(Window);
            _exceptions = new ToolExceptionWatch();
        }
        return SnapshotFiles();
    }

    /// <summary>Saves every file, as File > Save All does.</summary>
    public void SaveAll() => Services.GetRequiredService<IFileCommands>().ForceSaveProject(forceSaveContainedElements: true);

    /// <summary>The project's files right now, for a later byte comparison.</summary>
    public ProjectFileSnapshot SnapshotFiles() => ProjectFileSnapshot.Take(ProjectFolder);

    /// <summary>Ctrl+Z, handled app-wide as in the main window (after <see cref="StartScenario"/>).</summary>
    public void Undo()
    {
        Press(Key.Z, PhysicalKey.Z, RawInputModifiers.Control);
        ThrowIfPluginFailed();
    }

    /// <summary>Ctrl+Y, handled app-wide as in the main window (after <see cref="StartScenario"/>).</summary>
    public void Redo()
    {
        Press(Key.Y, PhysicalKey.Y, RawInputModifiers.Control);
        ThrowIfPluginFailed();
    }

    /// <summary>
    /// The shared end-of-scenario checks, for the element the tab shows: the saved sidecar holds
    /// exactly what the tab shows; the saved project passes <c>gumcli check</c> (keyframe
    /// references included), reloads cleanly and re-saves unchanged; the tab shows the same
    /// animations once the element is selected again in the reloaded project; and nothing logged an
    /// error or an exception meanwhile.
    /// </summary>
    public void AssertOracles()
    {
        if (_exceptions == null)
        {
            throw new InvalidOperationException("Call StartScenario before the gesture, so the oracles have an exception watch.");
        }
        Layout();
        ThrowIfPluginFailed();
        ElementSave element = ViewModel.Element ?? throw new InvalidOperationException("The tab shows no element to check.");
        bool isScreen = element is ScreenSave;
        string elementName = element.Name;
        AssertTabMatchesSavedSidecar(element, "before the reload");

        ProjectOracles.AssertSaveReloadAndCheckClean(_fixture);

        ElementSave reloaded = (isScreen
            ? Project.Screens.FirstOrDefault(screen => screen.Name == elementName)
            : (ElementSave?)Project.Components.FirstOrDefault(component => component.Name == elementName))
            ?? throw new InvalidOperationException($"The reloaded project has no {elementName}.");
        Select(reloaded);
        AssertTabMatchesSavedSidecar(reloaded, "after the reload");
        _exceptions.AssertClean();
    }

    /// <summary>The sidecar on disk holds, byte for byte, what the tab would save for <paramref name="element"/>.</summary>
    private void AssertTabMatchesSavedSidecar(ElementSave element, string when)
    {
        ElementAnimationsSave shown = ViewModel.ToSave();
        string sidecar = AnimationFilePath(element);
        if (!File.Exists(sidecar))
        {
            shown.Animations.ShouldBeEmpty($"the tab shows animations for {element.Name} {when}, but none were saved");
            return;
        }
        string shownPath = Path.Combine(Path.GetTempPath(), "GumAnimationEditor", Guid.NewGuid().ToString("N") + _sidecarExtension);
        Directory.CreateDirectory(Path.GetDirectoryName(shownPath)!);
        try
        {
            shown.Save(shownPath);
            File.ReadAllText(sidecar).ShouldBe(File.ReadAllText(shownPath), $"the saved sidecar differs from what the tab shows for {element.Name} {when}");
        }
        finally
        {
            File.Delete(shownPath);
        }
    }

    #endregion

    /// <summary>
    /// True when any pixel within <paramref name="radius"/> of <paramref name="center"/> is within
    /// <paramref name="tolerance"/> per channel of <paramref name="color"/>.
    /// </summary>
    public bool AnyPixelNear(Point center, int radius, Color color, int tolerance = 12) =>
        AnyPixelNear(center, radius, pixel =>
            Math.Abs(pixel.R - color.R) <= tolerance && Math.Abs(pixel.G - color.G) <= tolerance && Math.Abs(pixel.B - color.B) <= tolerance);

    /// <inheritdoc cref="HeadlessWindowDriver.AnyPixelNear"/>
    public bool AnyPixelNear(Point center, int radius, Func<Color, bool> matches) => Input.AnyPixelNear(center, radius, matches);

    /// <inheritdoc cref="HeadlessWindowDriver.SaveFrame"/>
    public string SaveFrame(string name) => Input.SaveFrame(name);

    private Button ButtonWithTip(string tip) =>
        Window.GetVisualDescendants().OfType<Button>().Single(button => ToolTip.GetTip(button) as string == tip);

    /// <summary>Takes the harness's plugin, tab and window out of the tool, then disposes the project fixture.</summary>
    public void Dispose()
    {
        try
        {
            if (View?.DataContext is ElementAnimationsViewModel viewModel)
            {
                viewModel.IsPlaying = false;
            }
            // Deselect while this plugin still handles the event, rather than the head's own.
            SelectedState.SelectedInstance = null;
            SelectedState.SelectedElement = null;
            if (Plugin != null)
            {
                _pluginManager.Plugins = _pluginManager.InitializedPlugins.Where(plugin => plugin != Plugin).ToList();
                _pluginManager.PluginContainers.Remove(Plugin);
            }
            if (_headPlugin is IAnimationUndoProvider headProvider)
            {
                Services.GetRequiredService<IAnimationUndoProviderRegistrar>().Register(headProvider);
            }
            if (_menuItemAdded != null)
            {
                Services.GetRequiredService<MenuModel>().GetItem("View")?.Items.Remove(_menuItemAdded);
            }
            _exceptions?.Dispose();
            _driver?.Dispose();
            if (_tab != null)
            {
                _tabManager!.RemoveTab(_tab);
            }
        }
        finally
        {
            // Restores the plugin set (the head's animation plugin included), the dialogs, the
            // per-user folder, and leaves no project pointing into the temp folder.
            _fixture.Dispose();
        }
    }
}
