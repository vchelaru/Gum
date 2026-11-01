using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum.Tests.Forms;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// Every property a Forms behavior declares (FormsProperty in Templates/FormsBehaviors/*.behx), edited on
/// an instance in a screen and on the component itself, then set back to its default: the control a hot
/// reload leaves behind must look and read exactly like a restart of the same files. The data is read
/// from the behavior files, so a new behavior property is covered when it is added. Runs on the plain
/// FormsTemplate and one theme.
/// </summary>
public class HotReloadBehaviorPropertyTests : FileThemeInputTestBase
{
    public enum Site { Instance, Component }

    private sealed class Row
    {
        public string Behavior = "";
        public string Name = "";
        public string Type = "";
    }

    private static string RepoRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (directory != null && !File.Exists(Path.Combine(directory, "GumFull.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }
        return directory!;
    }

    private static IEnumerable<Row> Rows()
    {
        string folder = Path.Combine(RepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsBehaviors");
                foreach (string file in Directory.GetFiles(folder, "*.behx").OrderBy(f => f))
        {
            System.Xml.Linq.XElement root = System.Xml.Linq.XDocument.Load(file).Root!;
            string behaviorName = root.Element("Name")!.Value;
            // Toast and OnScreenKeyboard have no component the loader turns into a Forms control: no control, no effect.
            if (behaviorName is "ToastBehavior" or "OnScreenKeyboardBehavior")
            {
                continue;
            }
            foreach (System.Xml.Linq.XElement property in root.Elements("FormsProperty"))
            {
                // The ListBoxItem templates have no Disabled state, so a restart and a live edit differ (#7495).
                if (behaviorName == "ListBoxItemBehavior" && (string)property.Attribute("Name")! == "IsEnabled")
                {
                    continue;
                }
                // ToolTip is a tooltip trigger, not a visual: it has no observable effect on the control's look.
                yield return new Row { Behavior = behaviorName, Name = (string)property.Attribute("Name")!, Type = (string)property.Attribute("Type")! };
            }
        }
    }

    // CI runs every row of both themes; a local run samples about a tenth of them (SweepScale).
    public static IEnumerable<object[]> Inventory()
    {
        int index = 0;
        foreach (string theme in new[] { "", "Bubblegum" })
        {
            foreach (Row row in Rows())
            {
                foreach (Site site in new[] { Site.Instance, Site.Component })
                {
                    if (SweepScale.Includes(index++))
                    {
                        yield return new object[] { theme, row.Behavior, row.Name, row.Type, site };
                    }
                }
            }
        }
    }

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static object NonDefault(string name, Type propertyType, object? current)
    {
        Type type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (type == typeof(bool)) return !(current is bool b && b);
        if (type == typeof(string) || type == typeof(object)) return "Edited " + name;
        if (type == typeof(int)) return 7;
        if (type == typeof(float)) return 6f;
        if (type == typeof(double))
        {
            return name switch
            {
                "Minimum" => 2.0, "Maximum" => 80.0, "Value" => 30.0, "SmallChange" => 3.0,
                "LargeChange" => 12.0, "TicksFrequency" => 4.0, _ => 9.0
            };
        }
        if (type.IsEnum)
        {
            return Enum.GetValues(type).Cast<object>().First(v => !v.Equals(current));
        }
        throw new NotSupportedException(type.Name);
    }

    private static string Describe(GraphicalUiElement element, int depth = 0)
    {
        StringBuilder text = new StringBuilder();
        text.Append(' ', depth * 2).Append(element.Name).Append(" vis=").Append(element.Visible);
        if (element is TextRuntime t)
        {
            text.Append(" text=").Append(t.Text).Append(" rgb=").Append(t.Red).Append(',').Append(t.Green).Append(',').Append(t.Blue);
        }
        else if (element is ColoredRectangleRuntime c)
        {
            text.Append(" rgb=").Append(c.Red).Append(',').Append(c.Green).Append(',').Append(c.Blue).Append(" a=").Append(c.Alpha);
        }
        // Sizes are not compared: a ScrollBar thumb or list container is laid out by frame updates, so a fresh load differs between runs.
        text.AppendLine();
        foreach (GraphicalUiElement child in element.ContainedElements.Concat(element.Children).Distinct())
        {
            text.Append(Describe(child, depth + 1));
        }
        return text.ToString();
    }

    private static string DescribeControl(FrameworkElement control, PropertyInfo property) =>
        "value=" + (property.GetValue(control) ?? "null") + "\n" + Describe(control.Visual);

    // Two loads per theme, shared by every row: "old" is what the running app loaded, "new" is the reloaded
    // files, which a row edits and puts back. Loading a theme project is the expensive part of a row.
    private sealed class Pair
    {
        public GumProjectSave Old = null!;
        public GumProjectSave New = null!;
        public GumProjectSave Back = null!;
    }

    private static readonly Dictionary<string, Pair> _pairs = new();

    private static string ScreenName(string behavior) => "PropScreen_" + behavior;

    private static ComponentSave ComponentOf(GumProjectSave project, string behavior) =>
        project.Components.First(c => c.Behaviors.Any(b => b.BehaviorName == behavior));

    // A theme that ships no component for a behavior has nothing to edit for it.
    private static bool HasComponent(GumProjectSave project, string behavior) =>
        project.Components.Any(c => c.Behaviors.Any(b => b.BehaviorName == behavior));

    // The shared theme cache hands every test the same project; a reload needs an old and a new one.
    private static GumProjectSave LoadOwnCopy(string theme)
    {
        string themeFolder = theme.Length == 0 ? "FormsTemplate" : Path.Combine("FormsThemes", theme);
        string gumx = Path.Combine(RepoRoot(), "Tools", "Gum.ProjectServices", "Templates", themeFolder, "GumProject.gumx");
        GumProjectSave loaded = GumProjectSave.Load(gumx, out _)!;
        loaded.Initialize();
        ObjectFinder.Self.GumProjectSave = loaded;
        loaded.ApplyAllVariableReferences();
        return loaded;
    }

    private static Pair GetPair(string theme)
    {
        lock (_pairs)
        {
            if (_pairs.TryGetValue(theme, out Pair? existing))
            {
                return existing;
            }
            Pair pair = new Pair { Old = LoadOwnCopy(theme), New = LoadOwnCopy(theme), Back = LoadOwnCopy(theme) };
            foreach (GumProjectSave project in new[] { pair.Old, pair.New, pair.Back })
            {
                foreach (string behavior in Rows().Select(r => r.Behavior).Distinct().Where(b => HasComponent(project, b)))
                {
                    ScreenSave screen = new ScreenSave { Name = ScreenName(behavior) };
                    screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
                    screen.Instances.Add(new InstanceSave
                    {
                        Name = "Ctl1", BaseType = ComponentOf(project, behavior).Name, ParentContainer = screen
                    });
                    project.Screens.Add(screen);
                }
                project.Initialize();
            }
            _pairs[theme] = pair;
            return pair;
        }
    }

    /// <summary>Writes the edit into the "new" project and returns the call that puts it back.</summary>
    private static Action Mutate(Pair pair, string behavior, string name, string type, Site site, object value)
    {
        StateSave target = site == Site.Instance
            ? pair.New.Screens.Single(s => s.Name == ScreenName(behavior)).DefaultState!
            : ComponentOf(pair.New, behavior).DefaultState!;
        // On the component the tool writes the inner variable an exposed name stands for (Placeholder -> PlaceholderTextInstance.Text).
        string componentVariable = ComponentOf(pair.New, behavior).DefaultState!.Variables
            .FirstOrDefault(v => v.ExposedAsName == name)?.Name ?? name;
        string variable = site == Site.Instance ? "Ctl1." + name : componentVariable;
        VariableSave? existing = target.Variables.FirstOrDefault(v => v.Name == variable);
        if (existing != null)
        {
            // The tool edits the saved variable, which keeps its exposed name and type.
            object? previousValue = existing.Value;
            bool previousSetsValue = existing.SetsValue;
            existing.Value = value;
            existing.SetsValue = true;
            return () =>
            {
                existing.Value = previousValue;
                existing.SetsValue = previousSetsValue;
            };
        }
        VariableSave added = new VariableSave { Name = variable, Type = type, Value = value, SetsValue = true };
        target.Variables.Add(added);
        return () => target.Variables.Remove(added);
    }

    private static FrameworkElement Load(GumProjectSave project, string behavior, out GraphicalUiElement screenGue)
    {
        ObjectFinder.Self.GumProjectSave = project;
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        StandardRuntimeRegistrations.Register();
        screenGue = project.Screens.Single(s => s.Name == ScreenName(behavior))
            .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        return (FrameworkElement)((InteractiveGue)screenGue.GetGraphicalUiElementByName("Ctl1")!).FormsControlAsObject!;
    }

    [Theory]
    [MemberData(nameof(Inventory))]
    public void HotReload_BehaviorPropertyEditedAndSetBack_MatchesARestart(
        string theme, string behavior, string name, string type, Site site)
    {
        Pair pair = GetPair(theme);
        if (!HasComponent(pair.Old, behavior))
        {
            return;
        }
        FrameworkElement probeControl = Load(pair.Old, behavior, out _);
        PropertyInfo? property = probeControl.GetType().GetProperty(name);
        if (property == null)
        {
            // Declared by the behavior but not a property of the control (nothing for the tool edit to reach).
            throw new InvalidOperationException(behavior + "." + name + " is not a property of " + probeControl.GetType().Name);
        }
        object value = NonDefault(name, property.PropertyType, property.GetValue(probeControl));
        string expectedDefault = DescribeControl(Load(pair.Old, behavior, out _), property);

        Action undo = Mutate(pair, behavior, name, type, site, value);
        try
        {
            // What a restart shows for the edited files.
            string expectedEdited = DescribeControl(Load(pair.New, behavior, out _), property);

            // The running app: loaded with the default, edited live, then set back.
            FrameworkElement live = Load(pair.Old, behavior, out GraphicalUiElement screen);
            ObjectFinder.Self.GumProjectSave = pair.New;
            GumHotReloadManager.ApplyDiff(new[] { screen }, pair.New, SystemManagers.Default);
            DescribeControl(live, property).ShouldBe(expectedEdited, "after editing " + name);

            // A reload always reads a new project object; reloading back to the one the tree was built from is not a real edit.
            ObjectFinder.Self.GumProjectSave = pair.Back;
            GumHotReloadManager.ApplyDiff(new[] { screen }, pair.Back, SystemManagers.Default);
            DescribeControl(live, property).ShouldBe(expectedDefault, "after setting " + name + " back");
        }
        finally
        {
            undo();
        }
    }
}
