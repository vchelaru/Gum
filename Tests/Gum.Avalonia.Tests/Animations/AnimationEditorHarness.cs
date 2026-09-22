using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.StateAnimation;
using Gum.Avalonia.Shell;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Gum.Undo;
using Microsoft.Extensions.DependencyInjection;
using StateAnimationPlugin.Timeline;
using StateAnimationPlugin.ViewModels;
using ToolsUtilities;

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
    private readonly AvaloniaTabManager _tabManager;
    private readonly AvaloniaPluginTab _tab;
    private readonly PluginBase? _headPlugin;
    private readonly MenuItemModel? _menuItemAdded;
    private readonly string? _originalUserDataOverride;
    private readonly string _framesFolder;
    private readonly string _sidecarExtension;

    /// <param name="jsonProject">True for a .gumj project, whose sidecars are .ganj files.</param>
    /// <param name="userDataFolder">
    /// Where the tool's per-user files (the plugin's settings) go for this run; a second harness given
    /// the same folder starts the way a restarted tool would. Defaults to a folder under the temp project.
    /// </param>
    public AnimationEditorHarness(bool jsonProject = false, string? userDataFolder = null)
    {
        _sidecarExtension = jsonProject ? "Animations.ganj" : "Animations.ganx";
        // Work another test left queued (a tree view syncing its selection, say) runs now, against
        // that test's state, not later against this harness's project and selection.
        Dispatcher.UIThread.RunJobs();
        _pluginManager = Services.GetRequiredService<PluginManager>();
        if (!_pluginManager.IsInitialized)
        {
            _pluginManager.Initialize();
        }
        Services.GetRequiredService<Gum.Reflection.ITypeManager>().Initialize();
        StandardElementsManager.Self.Initialize();
        Services.GetRequiredService<IStandardElementsManagerGumTool>().Initialize();

        ProjectFolder = Path.Combine(Path.GetTempPath(), "GumAnimationEditor", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ProjectFolder);
        _framesFolder = Environment.GetEnvironmentVariable("GUM_HEADLESS_FRAMES") is { Length: > 0 } folder
            ? folder
            : Path.Combine(Path.GetTempPath(), "GumAnimationEditor", "frames");

        // The plugin's settings file follows the tool's user-data folder; keep it out of the user's.
        _originalUserDataOverride = FileManager.UserApplicationDataFolderOverride;
        FileManager.UserApplicationDataFolderOverride = userDataFolder ?? Path.Combine(ProjectFolder, "UserData");
        try
        {
            SelectedState = Services.GetRequiredService<ISelectedState>();
            UndoManager = Services.GetRequiredService<IUndoManager>();
            IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
            projectManager.CreateNewProject();
            Project = projectManager.GumProjectSave!;
            Project.FullFileName = Path.Combine(ProjectFolder, jsonProject ? "AnimationEditor.gumj" : "AnimationEditor.gumx");

            Dialogs = new ScriptedDialogService();
            MenuModel menu = Services.GetRequiredService<MenuModel>();
            MenuItemModel viewMenu = menu.GetItem("View") ?? throw new InvalidOperationException("The head has no View menu.");
            List<MenuItemModel> viewItemsBefore = viewMenu.Items.ToList();
            Plugin = ActivatorUtilities.CreateInstance<TestAnimationPlugin>(Services);
            Plugin.GuiCommands = Services.GetRequiredService<IGuiCommands>();
            Plugin.FileCommands = Services.GetRequiredService<IFileCommands>();
            Plugin.TabManager = Services.GetRequiredService<ITabManager>();
            Plugin.DialogService = Dialogs;
            Plugin.Menu = menu;
            Plugin.StartUp();
            _menuItemAdded = viewMenu.Items.Except(viewItemsBefore).SingleOrDefault();

            // Selection, rename, undo and the other plugin events reach this instance through the
            // manager in place of the head's own instance, which would otherwise handle every
            // event too (moving or copying the same sidecar first); it is put back on dispose.
            _headPlugin = _pluginManager.Plugins.FirstOrDefault(plugin => plugin is AvaloniaStateAnimationPlugin);
            _pluginManager.Plugins = _pluginManager.Plugins.Where(plugin => plugin != _headPlugin).Append(Plugin).ToList();
            _pluginManager.PluginContainers[Plugin] = new PluginContainer(Plugin);

            _tabManager = (AvaloniaTabManager)Services.GetRequiredService<ITabManager>();
            _tab = _tabManager.AllTabs.Last(tab => tab.Title == "Animations");
            _tab.Show();
            View = (AnimationsView)_tab.Content;
            Window = new Window { Content = View, Width = 1100, Height = 640 };
            Window.Show();
            Layout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            if (Window.InputHitTest(new Point(2, 2)) == null)
            {
                // Every gesture would land on nothing; see this folder's README, "Gotchas". A
                // same-process retry here does not help - this is confirmed (not just suspected) to
                // be scoped to the whole test's own isolated dispatcher session, not to one Window:
                // recreating the Window 3x in place still failed 3/3 in local repro, so only a fresh
                // process (a new xunit test-assembly run) gets a genuinely independent roll.
                throw new InvalidOperationException("The tab's window hit-tests nothing after a render tick: this test's Avalonia session bound its compositor to a dispatcher that is not the current one.");
            }
        }
        catch
        {
            // Nothing disposes a harness whose constructor threw, and the override would otherwise
            // send every later test's per-user files into this temp folder.
            FileManager.UserApplicationDataFolderOverride = _originalUserDataOverride;
            throw;
        }
    }

    /// <summary>The plugin manager the tab's plugin is registered with, for events the tool raises.</summary>
    public PluginManager PluginManager => _pluginManager;

    /// <summary>The Animations tab this harness added to the tab manager.</summary>
    public AvaloniaPluginTab Tab => _tab;

    /// <summary>The View menu entry this harness's plugin added ("View Animations" / "Hide Animations").</summary>
    public MenuItemModel ViewMenuItem => _menuItemAdded ?? throw new InvalidOperationException("The plugin added no View menu entry.");

    /// <summary>The scripted dialogs; queue an answer before the gesture that opens one.</summary>
    public ScriptedDialogService Dialogs { get; }

    /// <summary>This harness's plugin instance.</summary>
    public TestAnimationPlugin Plugin { get; }

    /// <summary>The tab's view, hosted in <see cref="Window"/>.</summary>
    public AnimationsView View { get; }

    /// <summary>The headless window the view lives in; input goes through it.</summary>
    public Window Window { get; }

    /// <summary>The temp project the tab edits.</summary>
    public GumProjectSave Project { get; }

    /// <summary>The folder <see cref="Project"/> lives in; element files and sidecars resolve under it.</summary>
    public string ProjectFolder { get; }

    public ISelectedState SelectedState { get; }

    public IUndoManager UndoManager { get; }

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
    public ContextMenu? OpenContextMenu =>
        Window.GetSelfAndVisualDescendants().OfType<Control>().Select(control => control.ContextMenu).FirstOrDefault(menu => menu?.IsOpen == true);

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
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        StateSaveCategory categorySave = new StateSaveCategory { Name = category };
        for (int i = 0; i < states.Length; i++)
        {
            StateSave state = new StateSave { Name = states[i], ParentContainer = component };
            state.SetValue("X", (float)(i * 100), "float");
            categorySave.States.Add(state);
        }
        component.Categories.Add(categorySave);
        Project.Components.Add(component);
        return component;
    }

    /// <summary>Adds a screen with a Default state and one category holding <paramref name="states"/>.</summary>
    public ScreenSave AddScreen(string name, string category, params string[] states)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        StateSaveCategory categorySave = new StateSaveCategory { Name = category };
        foreach (string stateName in states)
        {
            categorySave.States.Add(new StateSave { Name = stateName, ParentContainer = screen });
        }
        screen.Categories.Add(categorySave);
        Project.Screens.Add(screen);
        return screen;
    }

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

    /// <summary>Runs pending dispatcher work and lays the window out, so the view reflects the model.</summary>
    public void Layout()
    {
        Dispatcher.UIThread.RunJobs();
        Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    public Point CenterOf(Control control)
    {
        Layout();
        return control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Window)
            ?? throw new InvalidOperationException($"{control.GetType().Name} is not in the window.");
    }

    public void Click(Control control, RawInputModifiers modifiers = RawInputModifiers.None) => ClickAt(CenterOf(control), modifiers);

    public void ClickAt(Point point, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        Window.MouseMove(point, modifiers);
        Window.MouseDown(point, MouseButton.Left, modifiers);
        Window.MouseUp(point, MouseButton.Left, modifiers);
        Layout();
    }

    /// <summary>Right-clicks <paramref name="control"/>, which opens its context menu.</summary>
    public void RightClick(Control control)
    {
        Point point = CenterOf(control);
        Window.MouseMove(point, RawInputModifiers.None);
        Window.MouseDown(point, MouseButton.Right, RawInputModifiers.None);
        Window.MouseUp(point, MouseButton.Right, RawInputModifiers.None);
        Layout();
    }

    /// <summary>
    /// Picks <paramref name="header"/> from the open context menu; a click when the popup laid the
    /// item out, else the item's own click event, as <see cref="PickAddKeyframe"/> does for the flyout.
    /// </summary>
    public void PickContextMenuItem(string header)
    {
        ContextMenu menu = OpenContextMenu ?? throw new InvalidOperationException("No context menu is open.");
        MenuItem item = menu.Items.OfType<MenuItem>().Single(candidate => (string)candidate.Header! == header);
        if (item.IsEffectivelyVisible && item.Bounds.Width > 0)
        {
            Click(item);
        }
        else
        {
            item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }
        menu.Close();
        Layout();
    }

    public void Hover(Point point)
    {
        Window.MouseMove(point, RawInputModifiers.None);
        Layout();
    }

    /// <summary>Presses the mouse at <paramref name="from"/>, moves it to <paramref name="to"/> and releases.</summary>
    public void Drag(Point from, Point to)
    {
        Window.MouseMove(from, RawInputModifiers.None);
        Window.MouseDown(from, MouseButton.Left, RawInputModifiers.None);
        Window.MouseMove(new Point((from.X + to.X) / 2, (from.Y + to.Y) / 2), RawInputModifiers.LeftMouseButton);
        Window.MouseMove(to, RawInputModifiers.LeftMouseButton);
        Window.MouseUp(to, MouseButton.Left, RawInputModifiers.None);
        Layout();
    }

    public void Press(Key key, PhysicalKey physicalKey, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        Window.KeyPress(key, modifiers, physicalKey, null);
        Window.KeyRelease(key, modifiers, physicalKey, null);
        Layout();
    }

    /// <summary>Focuses <paramref name="box"/>, replaces its text by typing, and presses Enter.</summary>
    public void TypeAndEnter(TextBox box, string text)
    {
        box.Focus();
        box.SelectAll();
        Window.KeyTextInput(text);
        Press(Key.Enter, PhysicalKey.Enter);
    }

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

    /// <summary>The rendered color at <paramref name="point"/> in the window.</summary>
    public Color PixelAt(Point point)
    {
        Layout();
        using WriteableBitmap frame = Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("The headless window rendered no frame.");
        return ReadPixel(frame, (int)point.X, (int)point.Y);
    }

    /// <summary>
    /// True when any pixel within <paramref name="radius"/> of <paramref name="center"/> is within
    /// <paramref name="tolerance"/> per channel of <paramref name="color"/>.
    /// </summary>
    public bool AnyPixelNear(Point center, int radius, Color color, int tolerance = 12) =>
        AnyPixelNear(center, radius, pixel =>
            Math.Abs(pixel.R - color.R) <= tolerance && Math.Abs(pixel.G - color.G) <= tolerance && Math.Abs(pixel.B - color.B) <= tolerance);

    /// <summary>True when any pixel within <paramref name="radius"/> of <paramref name="center"/> satisfies <paramref name="matches"/>.</summary>
    public bool AnyPixelNear(Point center, int radius, Func<Color, bool> matches)
    {
        Layout();
        using WriteableBitmap frame = Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("The headless window rendered no frame.");
        for (int y = (int)center.Y - radius; y <= (int)center.Y + radius; y++)
        {
            for (int x = (int)center.X - radius; x <= (int)center.X + radius; x++)
            {
                if (x < 0 || y < 0 || x >= frame.PixelSize.Width || y >= frame.PixelSize.Height)
                {
                    continue;
                }
                if (matches(ReadPixel(frame, x, y)))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static Color ReadPixel(WriteableBitmap frame, int x, int y)
    {
        using ILockedFramebuffer buffer = frame.Lock();
        int value = System.Runtime.InteropServices.Marshal.ReadInt32(buffer.Address, y * buffer.RowBytes + x * 4);
        byte b0 = (byte)value, b1 = (byte)(value >> 8), b2 = (byte)(value >> 16), b3 = (byte)(value >> 24);
        return buffer.Format == PixelFormat.Rgba8888
            ? Color.FromArgb(b3, b0, b1, b2)
            : Color.FromArgb(b3, b2, b1, b0);
    }

    /// <summary>Renders the window and saves it as a PNG for a person to look at; returns the path.</summary>
    public string SaveFrame(string name)
    {
        Layout();
        Directory.CreateDirectory(_framesFolder);
        string path = Path.Combine(_framesFolder, name + ".png");
        using WriteableBitmap frame = Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("The headless window rendered no frame.");
        frame.Save(path);
        return path;
    }

    private Button ButtonWithTip(string tip) =>
        Window.GetVisualDescendants().OfType<Button>().Single(button => ToolTip.GetTip(button) as string == tip);

    public void Dispose()
    {
        try
        {
            ViewModel.IsPlaying = false;
        }
        catch
        {
            // The view model may already be gone.
        }
        SelectedState.SelectedInstance = null;
        SelectedState.SelectedElement = null;
        _pluginManager.Plugins = _pluginManager.Plugins.Where(plugin => plugin != Plugin).ToList();
        _pluginManager.PluginContainers.Remove(Plugin);
        if (_headPlugin != null)
        {
            _pluginManager.Plugins = _pluginManager.Plugins.Append(_headPlugin).ToList();
        }
        if (_headPlugin is IAnimationUndoProvider headProvider)
        {
            Services.GetRequiredService<IAnimationUndoProviderRegistrar>().Register(headProvider);
        }
        if (_menuItemAdded != null)
        {
            Services.GetRequiredService<MenuModel>().GetItem("View")?.Items.Remove(_menuItemAdded);
        }
        Window.Content = null;
        Window.Close();
        _tabManager.RemoveTab(_tab);
        // The temp folder goes away below, so the tool must not keep a project that points into it
        // (the Code tab's setup check lists the project's folder).
        Services.GetRequiredService<IProjectManager>().CreateNewProject();
        ObjectFinder.Self.GumProjectSave = null;
        FileManager.UserApplicationDataFolderOverride = _originalUserDataOverride;
        try
        {
            Directory.Delete(ProjectFolder, recursive: true);
        }
        catch
        {
            // A file watcher may still hold the folder; the temp folder is cleaned up later.
        }
    }
}
