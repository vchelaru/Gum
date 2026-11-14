using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Forms.Controls;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Setting a property a behavior declares (FormsProperty) on a Forms control in code must re-evaluate the rows that read it
/// (<c>Text = Ctl1.Minimum</c>), as setting <c>Width</c> on a visual does (#8029). The cases come from the shipped
/// behavior files, so a new FormsProperty, or a setter that forgets to report, goes red. A row that does not re-evaluate
/// shows the stale text until <c>RefreshReferences</c> reads the control again, which is what each case compares against.
/// </summary>
public class FormsPropertySetterReportsTests : BaseTestClass
{
    // A declared property no code can assign through a public setter has no setter to report from.
    private static readonly Dictionary<string, string> NoPublicSetter = new Dictionary<string, string>
    {
    };

    private sealed class Row
    {
        public string Behavior = "";
        public string Name = "";
    }

    private static string RepoRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (directory != null && !File.Exists(Path.Combine(directory, "GumFull.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }
        return directory ?? throw new InvalidOperationException("Repo root not found.");
    }

    private static string TemplatesFolder => Path.Combine(RepoRoot(), "Tools", "Gum.ProjectServices", "Templates");

    private static List<Row> Rows()
    {
        List<Row> rows = new List<Row>();
        foreach (string file in Directory.GetFiles(Path.Combine(TemplatesFolder, "FormsBehaviors"), "*.behx").OrderBy(f => f))
        {
            BehaviorSave behavior = GumFileSerializer.DeserializeBehaviorSave(File.ReadAllText(file), projectVersion: 2)!;
            // Toast and OnScreenKeyboard have no component the loader turns into a Forms control.
            if (behavior.Name is "ToastBehavior" or "OnScreenKeyboardBehavior")
            {
                continue;
            }
            foreach (VariableSave property in behavior.FormsProperties)
            {
                rows.Add(new Row { Behavior = behavior.Name, Name = property.Name });
            }
        }
        return rows;
    }

    public static IEnumerable<object[]> Inventory()
    {
        int index = 0;
        foreach (string theme in Themes)
        {
            foreach (Row row in Rows())
            {
                if (SweepScale.Includes(index++))
                {
                    yield return new object[] { theme, row.Behavior, row.Name };
                }
            }
        }
    }

    // The plain template and one shipped theme, so a difference between the two is caught.
    private static readonly string[] Themes = { "FormsTemplate", Path.Combine("FormsThemes", "DarkPro") };

    // A behavior a theme ships no component for has no control to set. Named here with the reason, and asserted below,
    // so a new behavior or a dropped component fails instead of passing without checking anything.
    private static readonly Dictionary<string, string> NoComponentInTheme = new Dictionary<string, string>
    {
    };

    private static readonly Dictionary<string, GumProjectSave> _projects = new Dictionary<string, GumProjectSave>();

    private static string ScreenName(string behavior, string name) => "Sweep_" + behavior + "_" + name;

    private static ComponentSave? ComponentOf(GumProjectSave project, string behavior) =>
        project.Components.FirstOrDefault(c => c.Behaviors.Any(b => b.BehaviorName == behavior));

    // One screen per row: a control instance and a Text whose row reads the property.
    private static GumProjectSave GetProject(string theme)
    {
        if (_projects.TryGetValue(theme, out GumProjectSave? existing))
        {
            return existing;
        }
        GumProjectSave project = GumProjectSave.Load(Path.Combine(TemplatesFolder, theme, "GumProject.gumx"), out _)!;
        project.Initialize();
        foreach (Row row in Rows())
        {
            ComponentSave? component = ComponentOf(project, row.Behavior);
            if (component == null)
            {
                continue;
            }
            ScreenSave screen = new ScreenSave { Name = ScreenName(row.Behavior, row.Name) };
            StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
            screen.States.Add(state);
            screen.Instances.Add(new InstanceSave { Name = "Ctl1", BaseType = component.Name, ParentContainer = screen });
            screen.Instances.Add(new InstanceSave { Name = "Probe", BaseType = "Text", ParentContainer = screen });
            VariableListSave<string> list = new VariableListSave<string> { Name = "Probe.VariableReferences", Type = "string" };
            list.Value.Add("Text = Ctl1." + row.Name);
            state.VariableLists.Add(list);
            project.Screens.Add(screen);
        }
        project.Initialize();
        _projects[theme] = project;
        return project;
    }

    private static void RegisterRuntimes()
    {
        StandardRuntimeRegistrations.Register();
        // A theme draws with shapes that need a rendering SystemManagers a headless test lacks; the references do not read them.
        foreach (string unrendered in new[] { "Circle", "Rectangle", "Polygon" })
        {
            ElementSaveExtensions.RegisterGueInstantiation(unrendered, () => new Gum.GueDeriving.ContainerRuntime());
        }
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static IEnumerable<object> Candidates(string name, Type propertyType, object? current)
    {
        Type type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (type == typeof(bool)) yield return !(current is bool b && b);
        else if (type == typeof(string) || type == typeof(object)) yield return "Edited " + name;
        else if (type == typeof(int)) { yield return 3; yield return 1; }
        else if (type == typeof(float)) { yield return 3f; yield return 4f; }
        else if (type == typeof(double)) { yield return 3.0; yield return 4.0; }
        else if (type.IsEnum)
        {
            foreach (object member in Enum.GetValues(type))
            {
                yield return member;
            }
        }
        else throw new NotSupportedException(type.Name);
    }

    [Theory]
    [MemberData(nameof(Inventory))]
    public void SettingTheProperty_ShouldReEvaluateTheRowsThatReadIt(string theme, string behavior, string name)
    {
        GumExpressionService.Initialize();
        GumProjectSave project = GetProject(theme);
        if (ComponentOf(project, behavior) == null)
        {
            NoComponentInTheme.ContainsKey(theme + ":" + behavior).ShouldBeTrue(
                theme + " ships no component for " + behavior + "; list it with the reason");
            return;
        }
        NoComponentInTheme.ContainsKey(theme + ":" + behavior).ShouldBeFalse(
            theme + ":" + behavior + " has a component now; drop it from the skip list");

        ObjectFinder.Self.GumProjectSave = project;
        RegisterRuntimes();
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        GraphicalUiElement screen = project.Screens.Single(s => s.Name == ScreenName(behavior, name))
            .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        FrameworkElement control = (FrameworkElement)((InteractiveGue)screen.GetGraphicalUiElementByName("Ctl1")!).FormsControlAsObject!;
        Gum.GueDeriving.TextRuntime probe = (Gum.GueDeriving.TextRuntime)screen.GetGraphicalUiElementByName("Probe")!;

        PropertyInfo? property = control.GetType().GetProperty(name);
        property.ShouldNotBeNull(behavior + "." + name + " is not a property of " + control.GetType().Name);
        if (property.GetSetMethod() == null)
        {
            NoPublicSetter.ContainsKey(behavior + "." + name).ShouldBeTrue(
                behavior + "." + name + " has no public setter; list it with the reason");
            return;
        }

        if (control is TabControl tabs)
        {
            // SelectedIndex only takes an index some tab holds.
            tabs.AddTab("One");
            tabs.AddTab("Two");
            tabs.SelectedIndex = -1;
        }

        object? initial = property.GetValue(control);
        string? initialText = probe.Text;
        bool changed = false;
        foreach (object candidate in Candidates(name, property.PropertyType, initial))
        {
            property.SetValue(control, candidate);
            if (!Equals(property.GetValue(control), initial))
            {
                changed = true;
                break;
            }
        }
        changed.ShouldBeTrue("no candidate value changed " + behavior + "." + name + " from " + initial);

        string? afterSetter = probe.Text;
        screen.RefreshReferences("Ctl1." + name);
        string? afterRefresh = probe.Text;

        afterRefresh.ShouldNotBe(initialText, "the control changed, so a read of it must differ");
        afterSetter.ShouldBe(afterRefresh, behavior + "." + name + " did not re-evaluate the row that reads it");
    }

    private static TabControl LoadTabControl(string theme, out GraphicalUiElement screen, out Gum.GueDeriving.TextRuntime probe)
    {
        GumExpressionService.Initialize();
        GumProjectSave project = GetProject(theme);
        ObjectFinder.Self.GumProjectSave = project;
        RegisterRuntimes();
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        screen = project.Screens.Single(s => s.Name == ScreenName("TabControlBehavior", "SelectedIndex"))
            .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        probe = (Gum.GueDeriving.TextRuntime)screen.GetGraphicalUiElementByName("Probe")!;
        return (TabControl)((InteractiveGue)screen.GetGraphicalUiElementByName("Ctl1")!).FormsControlAsObject!;
    }

    [Theory]
    [InlineData("FormsTemplate")]
    [InlineData("FormsThemes/DarkPro")]
    public void TabPushedByTheUser_ShouldReEvaluateTheRowsThatReadSelectedIndex(string theme)
    {
        TabControl tabs = LoadTabControl(theme, out GraphicalUiElement screen, out Gum.GueDeriving.TextRuntime probe);
        tabs.AddTab("One");
        TabItem second = tabs.AddTab("Two");
        tabs.SelectedIndex = 0;
        string? before = probe.Text;

        second.IsSelected = true;

        tabs.SelectedIndex.ShouldBe(1);
        probe.Text.ShouldNotBe(before);
        string? afterSelect = probe.Text;
        screen.RefreshReferences("Ctl1.SelectedIndex");
        probe.Text.ShouldBe(afterSelect);
    }

    [Theory]
    [InlineData("FormsTemplate")]
    [InlineData("FormsThemes/DarkPro")]
    public void DefaultSelectionOfTheFirstTab_ShouldReEvaluateTheRowsThatReadSelectedIndex(string theme)
    {
        TabControl tabs = LoadTabControl(theme, out GraphicalUiElement screen, out Gum.GueDeriving.TextRuntime probe);
        string? beforeTabs = probe.Text;

        tabs.AddTab("One");

        tabs.SelectedIndex.ShouldBe(0);
        string? afterAdd = probe.Text;
        screen.RefreshReferences("Ctl1.SelectedIndex");
        probe.Text.ShouldBe(afterAdd);
        afterAdd.ShouldNotBe(beforeTabs);
    }

    [Fact]
    public void LayoutDerivedLargeChange_ShouldReEvaluateTheRowsThatReadIt()
    {
        GumExpressionService.Initialize();
        GumProjectSave project = GetProject("FormsTemplate");
        ObjectFinder.Self.GumProjectSave = project;
        RegisterRuntimes();
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        GraphicalUiElement screen = project.Screens.Single(s => s.Name == ScreenName("ScrollBarBehavior", "LargeChange"))
            .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        Gum.Forms.Controls.ScrollBar bar = (Gum.Forms.Controls.ScrollBar)((InteractiveGue)screen.GetGraphicalUiElementByName("Ctl1")!).FormsControlAsObject!;
        Gum.GueDeriving.TextRuntime probe = (Gum.GueDeriving.TextRuntime)screen.GetGraphicalUiElementByName("Probe")!;

        bar.SetComputedLargeChange(123);

        bar.LargeChange.ShouldBe(123);
        string? afterComputed = probe.Text;
        screen.RefreshReferences("Ctl1.LargeChange");
        probe.Text.ShouldBe(afterComputed);
        afterComputed.ShouldBe("123");
    }
}
