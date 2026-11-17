using Gum.DataTypes;
using Gum.Managers;
using GumRuntime;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms.Themed;

/// <summary>
/// A reload of a project nobody edited changes nothing on screen: every tool theme, a control placed alone on a
/// screen, the position and size of it and of everything inside it match before and after. A state the control
/// applied while it was built must not be replaced by a value from a component default (a standalone vertical
/// ScrollBar in DarkPro and Neon grew from 128 to the screen height). #8108
/// </summary>
public class ThemedNoOpHotReloadTests : FileThemeInputTestBase
{
    private ScreenSave? addedScreen;
    private GumProjectSave? addedTo;

    public override void Dispose()
    {
        addedTo?.Screens.Remove(addedScreen!);
        base.Dispose();
    }

    public static IEnumerable<object[]> ThemeAndControl() =>
        AllThemes().Select(row => (string)row[0]).SelectMany(theme =>
            new[] { "ScrollBar", "ScrollBarHorizontalInCode", "ScrollBarHorizontalAuthored", "ScrollBarHorizontalMoved", "ScrollViewerHorizontal", "ListBoxHorizontal", "Slider", "ComboBox", "TextBoxMultiLine", "ListBox", "ScrollViewer", "ItemsControl", "ScrollViewerBoth", "ScrollViewerVertical", "ScrollViewerHiddenThenShown", "ScrollViewerBothHiddenThenVertical", "ScrollViewerNested", "ListBoxNested", "ComboBoxNested", "TextBoxNested", "SliderNested", "TabControlNested", "ScrollViewerHorizontalTall", "ScrollViewerAutoWide", "ScrollViewerScrolled", "ItemsControlHorizontal", "ListBoxVertical", "ItemsControlItems", "TextBoxManyLines" }
                .Select(control => new object[] { theme, control }));

    private static void Snapshot(GraphicalUiElement element, string path, Dictionary<string, string> into)
    {
        if (!element.AbsoluteVisible) { return; } // a hidden element's layout is deferred until it shows
        into[path] = $"{element.AbsoluteLeft:0.##},{element.AbsoluteTop:0.##},{element.AbsoluteWidth:0.##},{element.AbsoluteHeight:0.##},{element.AbsoluteVisible}";
        foreach (GraphicalUiElement child in element.Children.OfType<GraphicalUiElement>())
        {
            Snapshot(child, path + "/" + (child.Name ?? "?"), into);
        }
    }

    public static IEnumerable<object[]> Themes() => AllThemes().Select(row => new object[] { row[0] });

    [Theory]
    [MemberData(nameof(Themes))]
    public void ScrollBar_HorizontalThenVertical_ShouldLayOutThumbLikeNeverSwitched(string theme)
    {
        GumProjectSave project = LoadTheme(theme);
        ScreenSave screen = new ScreenSave { Name = "AloneScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = ComponentName(project, "ScrollBar"), ParentContainer = screen });
        project.Screens.Add(screen);
        addedScreen = screen;
        addedTo = project;
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);
        ScrollBar box = (ScrollBar)((InteractiveGue)screenVisual.GetGraphicalUiElementByName("Box")!).FormsControlAsObject!;
        Dictionary<string, string> never = new Dictionary<string, string>();
        Snapshot(screenVisual, "", never);

        box.Orientation = Orientation.Horizontal;
        Driver.Step(2);
        box.Orientation = Orientation.Vertical;
        Driver.Step(2);

        Dictionary<string, string> roundTrip = new Dictionary<string, string>();
        Snapshot(screenVisual, "", roundTrip);
        roundTrip.Where(pair => never[pair.Key] != pair.Value).Select(pair => $"{pair.Key}: {never[pair.Key]} -> {pair.Value}")
            .ToArray().ShouldBeEmpty(theme);
    }

    /// <summary>
    /// Showing or hiding the horizontal bar resizes the clip container; the vertical bar's viewport and
    /// maximum follow it at once, so the thumb is already the right length before any reload. #8128
    /// </summary>
    [Theory]
    [MemberData(nameof(Themes))]
    public void ScrollViewer_HorizontalBarToggled_ShouldMeasureVerticalBarFromCurrentClip(string theme)
    {
        GumProjectSave project = LoadTheme(theme);
        ScreenSave screen = CreateScreen(project, "ScrollViewerHiddenThenShown");
        project.Screens.Add(screen);
        addedScreen = screen;
        addedTo = project;
        GraphicalUiElement screenVisual = BuildScreen(screen, "ScrollViewerHiddenThenShown");

        GraphicalUiElement box = screenVisual.GetGraphicalUiElementByName("Box")!;
        ScrollViewer viewer = (ScrollViewer)((InteractiveGue)box).FormsControlAsObject!;
        GraphicalUiElement clip = box.GetGraphicalUiElementByName("ClipContainerInstance")!;
        GraphicalUiElement panel = box.GetGraphicalUiElementByName("InnerPanelInstance")!;
        ScrollBar bar = viewer.VerticalScrollBar!;
        bar.ViewportSize.ShouldBe(clip.AbsoluteHeight, theme);
        bar.Maximum.ShouldBe(System.Math.Max(0, panel.AbsoluteHeight - clip.AbsoluteHeight), theme);
    }

    [Theory]
    [MemberData(nameof(ThemeAndControl))]
    public void ReloadWithNoEdit_ShouldNotMoveOrResizeAnything(string theme, string control)
    {
        GumProjectSave project = LoadTheme(theme);
        ScreenSave screen = CreateScreen(project, control);
        project.Screens.Add(screen);
        addedScreen = screen;
        addedTo = project;
        GraphicalUiElement screenVisual = BuildScreen(screen, control);
        Dictionary<string, string> before = new Dictionary<string, string>();
        Snapshot(screenVisual, "", before);

        Gum.GumHotReloadManager.ApplyDiff(new[] { screenVisual }, project, RenderingLibrary.SystemManagers.Default);
        Driver.Step(2);
        AssertUnchanged(before, screenVisual, theme, control);
    }

    /// <summary>
    /// The same reload as a running game does it: the new project is a separate object read from the same files,
    /// not the project the visuals were built from. #8125
    /// </summary>
    [Theory]
    [MemberData(nameof(ThemeAndControl))]
    public void ReloadFromFreshProject_ShouldNotMoveOrResizeAnything(string theme, string control)
    {
        GumProjectSave oldProject = LoadFresh(theme, isNew: false);
        ScreenSave screen = CreateScreen(oldProject, control);
        oldProject.Screens.Add(screen);
        GraphicalUiElement screenVisual = BuildScreen(screen, control);
        Dictionary<string, string> before = new Dictionary<string, string>();
        Snapshot(screenVisual, "", before);

        GumProjectSave newProject = LoadFresh(theme, isNew: true);
        newProject.Screens.Add(CreateScreen(newProject, control));
        Gum.GumHotReloadManager.ApplyDiff(new[] { screenVisual }, newProject, RenderingLibrary.SystemManagers.Default, oldProject);
        Driver.Step(2);
        AssertUnchanged(before, screenVisual, theme, control);
    }

    // Two projects per theme read from the same files, each loaded once: the old one the visuals are built from and
    // the new one the reload reads. Loading both from disk for every case was most of this class's run time.
    private static readonly Dictionary<(string Theme, bool IsNew), (GumProjectSave Project, int ScreenCount)> Fresh =
        new Dictionary<(string Theme, bool IsNew), (GumProjectSave Project, int ScreenCount)>();

    private static GumProjectSave LoadFresh(string theme, bool isNew)
    {
        if (!Fresh.TryGetValue((theme, isNew), out (GumProjectSave Project, int ScreenCount) entry))
        {
            string themeFolder = theme.Length == 0 ? "FormsTemplate" : System.IO.Path.Combine("FormsThemes", theme);
            string gumx = System.IO.Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", themeFolder, "GumProject.gumx");
            GumProjectSave loaded = GumProjectSave.Load(gumx, out _)!;
            loaded.Initialize();
            ObjectFinder.Self.GumProjectSave = loaded;
            loaded.ApplyAllVariableReferences();
            entry = (loaded, loaded.Screens.Count);
            Fresh[(theme, isNew)] = entry;
        }
        GumProjectSave project = entry.Project;
        while (project.Screens.Count > entry.ScreenCount) { project.Screens.RemoveAt(project.Screens.Count - 1); }
        ObjectFinder.Self.GumProjectSave = project;
        StandardRuntimeRegistrations.Register();
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        return project;
    }

    private static ScreenSave CreateScreen(GumProjectSave project, string control)
    {
        string component = control.EndsWith("Nested") ? "ScrollViewer" : control.StartsWith("ScrollBar") ? "ScrollBar" : control.StartsWith("TextBox") ? "TextBox" : control.StartsWith("ScrollViewer") ? "ScrollViewer" : control.StartsWith("ListBox") ? "ListBox" : control.StartsWith("ItemsControl") ? "ItemsControl" : control;
        ScreenSave screen = new ScreenSave { Name = "AloneScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = ComponentName(project, component), ParentContainer = screen });
        if (control == "ScrollBarHorizontalAuthored")
        {
            screen.DefaultState.Variables.Add(new VariableSave { Name = "Box.OrientationCategoryState", Type = "OrientationCategory", Value = "Horizontal", SetsValue = true });
        }
        return screen;
    }

    private GraphicalUiElement BuildScreen(ScreenSave screen, string control)
    {
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);
        FrameworkElement box = (FrameworkElement)((InteractiveGue)screenVisual.GetGraphicalUiElementByName("Box")!).FormsControlAsObject!;
        if (control == "ScrollBarHorizontalInCode") { ((ScrollBar)box).Orientation = Orientation.Horizontal; }
        if (control == "ScrollBarHorizontalMoved")
        {
            ScrollBar bar = (ScrollBar)box;
            bar.Orientation = Orientation.Horizontal;
            ContainerRuntime holder = new ContainerRuntime { Name = "Holder", Width = 200, Height = 60, WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute, HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute };
            screenVisual.Children.Add(holder);
            bar.Visual.Parent = holder;
        }
        if (control is "ScrollViewerHorizontal" or "ListBoxHorizontal")
        {
            ScrollViewer viewer = (ScrollViewer)box;
            viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            if (box is ListBox list) { for (int i = 0; i < 40; i++) { list.Items.Add("an item with fairly long text number " + i); } }
            else { for (int i = 0; i < 10; i++) { viewer.AddChild(new Button { Width = 400, Text = "wide " + i }); } }
        }
        if (control is "ScrollViewerBoth" or "ScrollViewerVertical" or "ScrollViewerHiddenThenShown" or "ScrollViewerBothHiddenThenVertical" or "ScrollViewerHorizontalTall")
        {
            ScrollViewer viewer = (ScrollViewer)box;
            for (int i = 0; i < 30; i++) { viewer.AddChild(new Button { Width = 400, Text = "item " + i }); }
            if (control == "ScrollViewerBoth") { viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible; viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible; }
            if (control == "ScrollViewerHorizontalTall") { viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible; }
            if (control == "ScrollViewerVertical") { viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible; }
            if (control == "ScrollViewerHiddenThenShown")
            {
                viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible; Driver.Step(2);
                viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden; Driver.Step(2);
                viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            }
            if (control == "ScrollViewerBothHiddenThenVertical")
            {
                viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible; viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible; Driver.Step(2);
                viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
        }
        if (control == "ScrollViewerNested")
        {
            // A ScrollViewer made in code and added to another one's content: a runtime child, not a project instance. #8130
            ScrollViewer inner = new ScrollViewer { Width = 150, Height = 80 };
            for (int i = 0; i < 20; i++) { inner.AddChild(new Button { Width = 400, Text = "inner " + i }); }
            ((ScrollViewer)box).AddChild(inner);
        }
        if (control is "ListBoxNested" or "ComboBoxNested" or "TextBoxNested" or "SliderNested" or "TabControlNested")
        {
            // Controls made in code inside a tool-authored ScrollViewer: runtime children, not project instances.
            FrameworkElement made = control switch
            {
                "ListBoxNested" => new ListBox { Width = 150, Height = 80 },
                "ComboBoxNested" => new ComboBox(),
                "TextBoxNested" => new TextBox { Width = 150, Height = 60, TextWrapping = Gum.Forms.TextWrapping.Wrap, AcceptsReturn = true, Text = "a b c d e f g" },
                "SliderNested" => new Slider { Minimum = 0, Maximum = 10, Value = 4 },
                _ => new TabControl { Width = 200, Height = 100 },
            };
            if (made is ItemsControl items) { for (int i = 0; i < 12; i++) { items.Items!.Add("entry " + i); } }
            if (made is TabControl tabs) { tabs.AddTab("One", new Label { Text = "first" }); tabs.AddTab("Two", new Label { Text = "second" }); }
            ((ScrollViewer)box).AddChild(made);
        }
        if (control == "ScrollViewerAutoWide")
        {
            ScrollViewer viewer = (ScrollViewer)box;
            for (int i = 0; i < 30; i++) { viewer.AddChild(new Button { Width = 400, Text = "item " + i }); }
        }
        if (control == "ScrollViewerScrolled")
        {
            ScrollViewer viewer = (ScrollViewer)box;
            for (int i = 0; i < 30; i++) { viewer.AddChild(new Button { Width = 400, Text = "item " + i }); }
            viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible; viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible; Driver.Step(2);
            viewer.VerticalScrollBarValue = 150; viewer.HorizontalScrollBar!.Value = 40;
        }
        if (control == "ItemsControlHorizontal")
        {
            ItemsControl items = (ItemsControl)box;
            items.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            for (int i = 0; i < 40; i++) { items.Items.Add("an item with fairly long text number " + i); }
        }
        if (control == "ListBoxVertical") { ListBox list = (ListBox)box; for (int i = 0; i < 40; i++) { list.Items.Add("item " + i); } }
        if (control == "ItemsControlItems") { ItemsControl items = (ItemsControl)box; for (int i = 0; i < 40; i++) { items.Items.Add("item " + i); } }
        if (control == "TextBoxManyLines") { TextBox text = (TextBox)box; text.TextWrapping = Gum.Forms.TextWrapping.Wrap; text.AcceptsReturn = true; text.Text = string.Join("\n", Enumerable.Range(0, 30)); }
        if (control == "TextBoxMultiLine") { ((TextBox)box).TextWrapping = Gum.Forms.TextWrapping.Wrap; ((TextBox)box).AcceptsReturn = true; ((TextBox)box).Text = "a\nb\nc"; }
        Driver.Step(2);
        return screenVisual;
    }

    private static void AssertUnchanged(Dictionary<string, string> before, GraphicalUiElement screenVisual, string theme, string control)
    {
        Dictionary<string, string> after = new Dictionary<string, string>();
        Snapshot(screenVisual, "", after);
        string[] changed = before.Where(pair => !after.TryGetValue(pair.Key, out string? value) || value != pair.Value)
            .Select(pair => $"{pair.Key}: {pair.Value} -> {(after.TryGetValue(pair.Key, out string? now) ? now : "gone")}").ToArray();
        changed.ShouldBeEmpty($"{theme} {control}");
    }
}
