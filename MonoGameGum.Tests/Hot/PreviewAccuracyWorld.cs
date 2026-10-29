// ColoredRectangle, Text and Container are the standards the project files in this sweep instantiate.
#pragma warning disable CS0618
using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MonoGameGum.Tests.Hot;

/// <summary>What one saved edit did to a running tree, measured against a fresh load of the same files.</summary>
internal sealed record PreviewVerdict(string Hot, string Fresh, HotReloadResult Result)
{
    public bool Differs => Hot != Fresh;

    public string Kinds => string.Join(",", Result.Issues.Select(issue => issue.Kind).Distinct());

    /// <summary>The tree differs from a restart and the result says nothing: the Preview or game stays stale unnoticed.</summary>
    public bool IsFalseNegative => Differs && Result.MatchesRestart;

    /// <summary>The tree equals a restart and the result asks for one anyway.</summary>
    public bool IsFalsePositive => !Differs && !Result.MatchesRestart;

    public string Explain() => "differs=" + Differs + " matchesRestart=" + Result.MatchesRestart + " issues=[" + Kinds + "]\n" + Difference();

    // Per node, the properties that differ; nodes only one side has are listed whole by name.
    private string Difference()
    {
        List<string> hot = Hot.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        List<string> fresh = Fresh.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.AppendLine("  hot nodes: " + string.Join(" / ", hot.Select(Key)));
        if (hot.Select(Key).SequenceEqual(fresh.Select(Key)))
        {
            for (int i = 0; i < hot.Count; i++)
            {
                HashSet<string> freshTokens = fresh[i].Split(' ').ToHashSet();
                string[] changed = hot[i].Split(' ').Where(token => !freshTokens.Contains(token)).ToArray();
                if (changed.Length > 0)
                {
                    text.Append("  ").Append(Key(hot[i])).Append(": hot ").Append(string.Join(' ', changed))
                        .Append(" | fresh ").Append(string.Join(' ', fresh[i].Split(' ').Where(token => !hot[i].Split(' ').Contains(token)))).AppendLine();
                }
            }
        }
        else
        {
            text.AppendLine("  nodes (in order) differ:");
            text.AppendLine("  hot:   " + string.Join(" / ", hot.Select(Key)));
            text.AppendLine("  fresh: " + string.Join(" / ", fresh.Select(Key)));
        }
        return text.ToString();
    }

    private static string Key(string line)
    {
        int end = line.IndexOf('>');
        int runtime = line.IndexOf(' ', end + 2);
        return line.Substring(0, runtime < 0 ? line.Length : runtime);
    }
}

/// <summary>
/// The disk side of the Preview accuracy sweep: a baseline project the tool saved, one edit applied
/// to it and saved the way the tool saves (the .gumx and every element file), and the real
/// <see cref="GumHotReloadManager"/> notify, debounce and Update flow with an injected clock.
/// The hot tree is compared with a fresh load through a reflection dump of every simple property.
/// </summary>
internal sealed class PreviewAccuracyWorld : IDisposable
{
    public string Dir { get; } = Path.Combine(Path.GetTempPath(), "PreviewAccuracy_" + Path.GetRandomFileName());
    public string Gumx => Path.Combine(Dir, "Proj.gumx");

    public DateTime Now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public GumHotReloadManager Manager = null!;
    public GraphicalUiElement Screen = null!;
    public readonly List<HotReloadResult> Results = new List<HotReloadResult>();
    public string ShownElement = "MainScreen";

    public PreviewAccuracyWorld()
    {
        Directory.CreateDirectory(Dir);
    }

    // ---- Baseline project ----

    internal static GumProjectSave Baseline()
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = new GumProjectSave();
        foreach (string standard in new[] { "Container", "ColoredRectangle", "Text" })
        {
            StandardElementSave standardElement = new StandardElementSave { Name = standard };
            standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor(standard)!.Clone());
            standardElement.DefaultState!.ParentContainer = standardElement;
            project.StandardElements.Add(standardElement);
        }

        ComponentSave baseComponent = NewComponent("Widgets/Base");
        Set(baseComponent, "X", "float", 7f);
        project.Components.Add(baseComponent);

        ComponentSave widget = NewComponent("Widgets/Widget", "Widgets/Base");
        AddInstance(widget, "Fill", "ColoredRectangle");
        Std(widget, "Fill", "Red", 30);
        Std(widget, "Fill", "Green", 0);
        Std(widget, "Fill", "Blue", 0);
        AddInstance(widget, "Caption", "Text");
        Std(widget, "Caption", "Text", "Cap");
        StateSaveCategory look = new StateSaveCategory { Name = "Look" };
        look.States.Add(new StateSave { Name = "Normal", ParentContainer = widget });
        StateSave highlighted = new StateSave { Name = "Highlighted", ParentContainer = widget };
        Set(highlighted, "Fill.Red", "int", 40);
        look.States.Add(highlighted);
        widget.Categories.Add(look);
        project.Components.Add(widget);

        ComponentSave gadget = NewComponent("Widgets/Gadget", "Widgets/Widget");
        AddInstance(gadget, "Extra", "ColoredRectangle");
        Std(gadget, "Extra", "Red", 99);
        project.Components.Add(gadget);

        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        AddInstance(screen, "Box", "ColoredRectangle");
        Std(screen, "Box", "X", 5f);
        Std(screen, "Box", "Red", 10);
        AddInstance(screen, "Label", "Text");
        Std(screen, "Label", "Text", "Hello");
        AddInstance(screen, "Panel", "Container");
        Std(screen, "Panel", "Width", 200f);
        AddInstance(screen, "Inner", "ColoredRectangle");
        Set(screen, "Inner.Parent", "string", "Panel");
        Std(screen, "Inner", "Red", 5);
        AddInstance(screen, "Widget1", "Widgets/Widget");
        AddInstance(screen, "Gadget1", "Widgets/Gadget");
        AddInstance(screen, "Tail", "ColoredRectangle");
        project.Screens.Add(screen);

        ScreenSave derived = new ScreenSave { Name = "DerivedScreen", BaseType = "MainScreen" };
        derived.States.Add(new StateSave { Name = "Default", ParentContainer = derived });
        AddInstance(derived, "Own", "ColoredRectangle");
        project.Screens.Add(derived);

        ScreenSave second = new ScreenSave { Name = "OtherScreen" };
        second.States.Add(new StateSave { Name = "Default", ParentContainer = second });
        AddInstance(second, "Solo", "ColoredRectangle");
        project.Screens.Add(second);

        project.Initialize();
        return project;
    }

    // ---- Editing helpers the tests use ----

    internal static ComponentSave NewComponent(string name, string baseType = "Container")
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    internal static void AddInstance(ElementSave owner, string name, string baseType)
    {
        owner.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = owner });
    }

    internal static void Set(ElementSave owner, string name, string type, object? value)
    {
        StateSave state = owner.DefaultState!;
        state.Variables.RemoveAll(variable => variable.Name == name);
        state.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });
    }

    internal static void Set(StateSave state, string name, string type, object? value)
    {
        state.Variables.RemoveAll(variable => variable.Name == name);
        state.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });
    }

    /// <summary>Sets <c>instance.variable</c> typed as the standard element declares it. A string for an enum variable is parsed.</summary>
    internal static void Std(ElementSave owner, string instanceName, string variable, object value)
    {
        string baseType = owner.GetInstance(instanceName)!.BaseType!;
        VariableSave declared = StandardElementsManager.Self.GetDefaultStateFor(baseType)!.Variables.First(candidate => candidate.Name == variable);
        object converted = declared.Value != null && declared.Value.GetType().IsEnum && value is string text
            ? Enum.Parse(declared.Value.GetType(), text)
            : value;
        Set(owner, instanceName + "." + variable, declared.Type, converted);
    }

    /// <summary>Sets a variable of the element's own base standard, such as <c>Width</c> on a Container component.</summary>
    internal static void StdOwn(ElementSave owner, string standard, string variable, object value)
    {
        VariableSave declared = StandardElementsManager.Self.GetDefaultStateFor(standard)!.Variables.First(candidate => candidate.Name == variable);
        object converted = declared.Value != null && declared.Value.GetType().IsEnum && value is string text
            ? Enum.Parse(declared.Value.GetType(), text)
            : value;
        Set(owner, variable, declared.Type, converted);
    }

    internal static ScreenSave MainScreen(GumProjectSave project) => project.Screens.Single(screen => screen.Name == "MainScreen");

    internal static ComponentSave Component(GumProjectSave project, string name) => project.Components.Single(component => component.Name == name);

    // ---- Writing, as the tool does ----

    internal static void SaveAll(GumProjectSave project, string gumx)
    {
        project.ScreenReferences.Clear();
        project.ComponentReferences.Clear();
        project.StandardElementReferences.Clear();
        project.BehaviorReferences.Clear();
        foreach (ScreenSave screen in project.Screens)
        {
            project.ScreenReferences.Add(new ElementReference { Name = screen.Name, ElementType = ElementType.Screen });
        }
        foreach (ComponentSave component in project.Components)
        {
            project.ComponentReferences.Add(new ElementReference { Name = component.Name, ElementType = ElementType.Component });
        }
        foreach (StandardElementSave standard in project.StandardElements)
        {
            project.StandardElementReferences.Add(new ElementReference { Name = standard.Name, ElementType = ElementType.Standard });
        }
        foreach (BehaviorSave behavior in project.Behaviors)
        {
            project.BehaviorReferences.Add(new BehaviorReference { Name = behavior.Name });
        }
        project.Save(gumx, saveElements: true);
        foreach (BehaviorSave behavior in project.Behaviors)
        {
            string path = Path.Combine(Path.GetDirectoryName(gumx)!, "Behaviors", behavior.Name + ".behx");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            behavior.Save(path);
        }
    }

    // ---- Running side ----

    /// <summary>Saves the baseline, builds the running screen from it, and starts the real manager (the OS watcher is replaced by Notify).</summary>
    public void Boot(string shown = "MainScreen")
    {
        ShownElement = shown;
        SaveAll(Baseline(), Gumx);
        GumProjectSave loaded = GumProjectSave.Load(Gumx);
        loaded.Initialize();
        ObjectFinder.Self.GumProjectSave = loaded;
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        ElementSaveExtensions.RegisterGueInstantiation("ColoredRectangle", () => new ColoredRectangleRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Text", () => new TextRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Container", () => new ContainerRuntime());
        Screen = ObjectFinder.Self.GetElementSave(shown)!.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        Manager = new GumHotReloadManager(
            Gum.GumService.ApplyProjectTextureFilter,
            GumAnimationLoader.LoadAnimationsFromProvider,
            _ => { },
            () => Now);
        Manager.ReloadFinished += Results.Add;
        Manager.Start(Gumx);
        Manager.Stop();
    }

    /// <summary>Saves the edited baseline as the tool would, notifies every file it wrote, and runs the game's frames.</summary>
    public PreviewVerdict Save(Action<GumProjectSave> edit)
    {
        GumProjectSave edited = Baseline();
        edit(edited);
        // The tool refreshes the instances a derived component inherits before it saves.
        foreach (ElementSave element in edited.AllElements)
        {
            element.Instances.RemoveAll(instance => instance.DefinedByBase);
        }
        edited.Initialize();
        SaveAll(edited, Gumx);
        return Reload();
    }

    /// <summary>Notifies every file on disk, waits out the debounce and runs Update; the verdict is for that reload.</summary>
    public PreviewVerdict Reload()
    {
        int before = Results.Count;
        foreach (string file in Directory.EnumerateFiles(Dir, "*.*", SearchOption.AllDirectories))
        {
            Manager.NotifyFileChanged(file);
        }
        Now = Now.AddMilliseconds(300);
        Manager.Update(new[] { Screen });
        HotReloadResult result = Results.Count > before
            ? Results[Results.Count - 1]
            : throw new InvalidOperationException("the reload never finished");
        return new PreviewVerdict(Describe(Screen), DescribeAFreshLoad(), result);
    }

    // ---- The oracle ----

    private static readonly HashSet<string> _skipped = new HashSet<string>
    {
        "Name", "Tag", "AbsoluteWidth", "AbsoluteHeight", "AbsoluteLeft", "AbsoluteTop", "AbsoluteRight", "AbsoluteBottom", "AbsoluteX", "AbsoluteY",
        "WorldX", "WorldY", "ChildrenCount", "ContainedElementsCount", "EffectiveParentCount", "Z", "NameInHierarchy",
    };

    private static bool IsSimple(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
        || type == typeof(Microsoft.Xna.Framework.Color) || type == typeof(Microsoft.Xna.Framework.Vector2);

    /// <summary>
    /// The design-time part of a tree: the element, every simple property of the visual, and the
    /// children in draw order. Instances nothing parents sort by name.
    /// </summary>
    public static string Describe(GraphicalUiElement element, int depth = 0)
    {
        StringBuilder text = new StringBuilder();
        text.Append(' ', depth * 2).Append(element.Name).Append(" <").Append(element.ElementSave?.Name).Append("> ").Append(element.GetType().Name);
        foreach (PropertyInfo property in element.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(candidate => candidate.GetIndexParameters().Length == 0 && candidate.CanRead && IsSimple(candidate.PropertyType)
                         && !_skipped.Contains(candidate.Name))
                     .OrderBy(candidate => candidate.Name, StringComparer.Ordinal))
        {
            object? value;
            try
            {
                value = property.GetValue(element);
            }
            catch (Exception)
            {
                continue;
            }
            text.Append(' ').Append(property.Name).Append('=').Append(value);
        }
        if (element is InteractiveGue interactive && interactive.FormsControlAsObject != null)
        {
            text.Append(" forms=").Append(interactive.FormsControlAsObject.GetType().Name);
        }
        text.AppendLine();

        List<GraphicalUiElement> inOrder = element.Children.Where(child => child.Tag is InstanceSave).ToList();
        IEnumerable<GraphicalUiElement> rest = element.ContainedElements.Where(child => child.Tag is InstanceSave && !inOrder.Contains(child))
            .OrderBy(child => child.Name, StringComparer.Ordinal);
        foreach (GraphicalUiElement child in inOrder.Concat(rest))
        {
            text.Append(Describe(child, depth + 1));
        }
        return text.ToString();
    }

    public string DescribeAFreshLoad()
    {
        GumProjectSave fresh = GumProjectSave.Load(Gumx);
        fresh.Initialize();
        GumProjectSave? running = ObjectFinder.Self.GumProjectSave;
        ObjectFinder.Self.GumProjectSave = fresh;
        try
        {
            ElementSave? element = fresh.AllElements.FirstOrDefault(candidate => candidate.Name == Screen.ElementSave!.Name);
            return element == null ? "<gone>" : Describe(element.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false));
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = running;
        }
    }

    public GraphicalUiElement? Find(string name) => Screen.GetGraphicalUiElementByName(name);

    public void Dispose()
    {
        try { Directory.Delete(Dir, recursive: true); } catch { /* best-effort */ }
    }
}
