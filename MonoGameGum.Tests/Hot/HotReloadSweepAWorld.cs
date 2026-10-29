// ColoredRectangle is the standard the project files in this sweep instantiate.
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

namespace MonoGameGum.Tests.Hot;

/// <summary>One version of the saved project the sweep (#6878, part A) writes to disk.</summary>
internal sealed record SweepSpec
{
    public int BoxRed { get; init; } = 10;
    public float BoxX { get; init; } = 5;
    public string BoxType { get; init; } = "ColoredRectangle";
    public string[] Extras { get; init; } = Array.Empty<string>();
    public bool BoxReadsStyle { get; init; }
    public string StyleTarget { get; init; } = "Primary";
    public int Primary { get; init; } = 20;
    public int Accent { get; init; } = 220;
    public int WidgetRed { get; init; } = 30;
    public int WidgetHighlighted { get; init; } = 40;
    public bool WidgetHasHighlighted { get; init; } = true;
    public float BaseX { get; init; } = 7;
    public string LabelText { get; init; } = "Hello";
    public bool WithGadget { get; init; }
    public bool WithExtraBehavior { get; init; }
    public bool WithWidget { get; init; } = true;
    public bool OmitBaseX { get; init; }
    public bool OmitWidgetRed { get; init; }
    public string WidgetBase { get; init; } = "Widgets/Base";
    public bool BaseInstance { get; init; }
    public string? ExposedName { get; init; }
    public int ExposedValue { get; init; } = 150;
    public float? RectWidth { get; init; }
    public string? LabelParent { get; init; }
    public string? ScreenBase { get; init; }
    public bool BaseBLikeA { get; init; }
    public bool WithBaseScreens { get; init; }
    public string BaseName { get; init; } = "Widgets/Base";
    public bool OmitBaseComponent { get; init; }
    public bool WithMid { get; init; }
    public bool WithNewBase { get; init; }
}

/// <summary>
/// The disk side of the sweep: writes <see cref="SweepSpec"/> versions as the tool (every file) or a
/// text editor (one file) would, boots the running tree from the first version, and drives the real
/// <see cref="GumHotReloadManager"/> notify, debounce and <c>Update</c> flow with an injected clock.
/// </summary>
internal sealed class SweepWorld : IDisposable
{
    public string Dir { get; } = Path.Combine(Path.GetTempPath(), "HotReloadSweepA_" + Path.GetRandomFileName());
    public string Gumx => Path.Combine(Dir, "Proj.gumx");
    public string ScreenFile => Path.Combine(Dir, "Screens", "MainScreen.gusx");
    public string StylesFile => Path.Combine(Dir, "Components", "Styles.gucx");
    public string BaseFile => Path.Combine(Dir, "Components", "Widgets", "Base.gucx");
    public string WidgetFile => Path.Combine(Dir, "Components", "Widgets", "Widget.gucx");
    public string GadgetFile => Path.Combine(Dir, "Components", "Widgets", "Gadget.gucx");
    public string BehaviorFile => Path.Combine(Dir, "Behaviors", "ExtraBehavior.behx");

    public DateTime Now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public int Reloads;
    public GumHotReloadManager Manager = null!;
    public GraphicalUiElement Screen = null!;
    public List<string> Disposed { get; } = new List<string>();

    public SweepWorld()
    {
        Directory.CreateDirectory(Dir);
    }

    public IEnumerable<GraphicalUiElement> Roots => new[] { Screen };

    // ---- Building ----

    private static void Set(StateSave state, string name, string type, object? value)
    {
        state.Variables.RemoveAll(v => v.Name == name);
        state.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });
    }

    private static void SetRed(StateSave state, string owner, int red)
    {
        string prefix = owner.Length == 0 ? "" : owner + ".";
        Set(state, prefix + "Red", "int", red);
        Set(state, prefix + "Green", "int", 0);
        Set(state, prefix + "Blue", "int", 0);
    }

    private static ComponentSave NewComponent(string name, string baseType = "Container")
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    private static void AddInstance(ElementSave owner, string name, string baseType)
    {
        owner.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = owner });
    }

    public static GumProjectSave Build(SweepSpec spec)
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = new GumProjectSave();
        foreach (string standard in new[] { "Container", "ColoredRectangle", "Text" })
        {
            StandardElementSave standardElement = new StandardElementSave { Name = standard };
            standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor(standard)!.Clone());
            standardElement.DefaultState!.ParentContainer = standardElement;
            if (standard == "ColoredRectangle" && spec.RectWidth != null)
            {
                Set(standardElement.DefaultState!, "Width", "float", spec.RectWidth.Value);
            }
            project.StandardElements.Add(standardElement);
        }

        ComponentSave styles = NewComponent("Styles");
        AddInstance(styles, "Primary", "ColoredRectangle");
        SetRed(styles.DefaultState!, "Primary", spec.Primary);
        AddInstance(styles, "Accent", "ColoredRectangle");
        SetRed(styles.DefaultState!, "Accent", spec.Accent);
        project.Components.Add(styles);

        ComponentSave baseComponent = NewComponent(spec.BaseName);
        if (!spec.OmitBaseX)
        {
            Set(baseComponent.DefaultState!, "X", "float", spec.BaseX);
        }
        if (spec.BaseInstance)
        {
            AddInstance(baseComponent, "BaseDot", "ColoredRectangle");
            SetRed(baseComponent.DefaultState!, "BaseDot", 77);
        }
        if (!spec.OmitBaseComponent)
        {
            project.Components.Add(baseComponent);
        }
        if (spec.WithMid)
        {
            ComponentSave mid = NewComponent("Widgets/Mid", spec.BaseName);
            Set(mid.DefaultState!, "Y", "float", 4f);
            project.Components.Add(mid);
        }
        if (spec.WithNewBase)
        {
            ComponentSave newBase = NewComponent("Widgets/NewBase");
            Set(newBase.DefaultState!, "Y", "float", 3f);
            project.Components.Add(newBase);
        }

        if (spec.WithWidget)
        {
            ComponentSave widget = NewComponent("Widgets/Widget", spec.WidgetBase);
            AddInstance(widget, "Fill", "ColoredRectangle");
            if (!spec.OmitWidgetRed)
            {
                SetRed(widget.DefaultState!, "Fill", spec.WidgetRed);
            }
            if (spec.ExposedName != null)
            {
                widget.DefaultState!.Variables.First(v => v.Name == "Fill.Red").ExposedAsName = spec.ExposedName;
            }
            StateSaveCategory look = new StateSaveCategory { Name = "Look" };
            look.States.Add(new StateSave { Name = "Normal", ParentContainer = widget });
            if (spec.WidgetHasHighlighted)
            {
                StateSave highlighted = new StateSave { Name = "Highlighted", ParentContainer = widget };
                SetRed(highlighted, "Fill", spec.WidgetHighlighted);
                look.States.Add(highlighted);
            }
            widget.Categories.Add(look);
            project.Components.Add(widget);
        }

        if (spec.WithGadget)
        {
            ComponentSave gadget = NewComponent("Widgets/Gadget");
            AddInstance(gadget, "Fill", "ColoredRectangle");
            SetRed(gadget.DefaultState!, "Fill", 99);
            project.Components.Add(gadget);
        }

        if (spec.WithExtraBehavior)
        {
            project.Behaviors.Add(new BehaviorSave { Name = "ExtraBehavior" });
        }

        // Two screens a shown screen can derive from. B differs from A unless BaseBLikeA.
        foreach ((string baseName, string instance, float x) in spec.WithBaseScreens
                     ? new[]
                     {
                         ("BaseA", "FromA", 10f),
                         ("BaseB", spec.BaseBLikeA ? "FromA" : "FromB", spec.BaseBLikeA ? 10f : 20f)
                     }
                     : Array.Empty<(string, string, float)>())
        {
            ScreenSave baseScreen = new ScreenSave { Name = baseName };
            StateSave baseDefault = new StateSave { Name = "Default", ParentContainer = baseScreen };
            baseScreen.States.Add(baseDefault);
            AddInstance(baseScreen, instance, "ColoredRectangle");
            Set(baseDefault, instance + ".X", "float", x);
            Set(baseDefault, "X", "float", x);
            project.Screens.Add(baseScreen);
        }

        ScreenSave screen = new ScreenSave { Name = "MainScreen", BaseType = spec.ScreenBase };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        AddInstance(screen, "Box", spec.BoxType);
        Set(screenDefault, "Box.X", "float", spec.BoxX);
        if (spec.BoxType == "ColoredRectangle")
        {
            SetRed(screenDefault, "Box", spec.BoxRed);
            if (spec.BoxReadsStyle)
            {
                VariableListSave<string> rows = new VariableListSave<string> { Type = "string", Name = "Box.VariableReferences" };
                rows.Value.Add("Color = Components/Styles." + spec.StyleTarget + ".Color");
                screenDefault.VariableLists.Add(rows);
            }
        }
        AddInstance(screen, "Label", "Text");
        Set(screenDefault, "Label.Text", "string", spec.LabelText);
        if (spec.LabelParent != null)
        {
            Set(screenDefault, "Label.Parent", "string", spec.LabelParent);
        }
        if (spec.WithWidget)
        {
            AddInstance(screen, "Widget1", "Widgets/Widget");
            if (spec.ExposedName != null)
            {
                Set(screenDefault, "Widget1." + spec.ExposedName, "int", spec.ExposedValue);
            }
        }
        if (spec.WithGadget)
        {
            AddInstance(screen, "Gadget1", "Widgets/Gadget");
        }
        foreach (string extra in spec.Extras)
        {
            AddInstance(screen, extra, "ColoredRectangle");
        }
        project.Screens.Add(screen);

        project.Initialize();
        return project;
    }

    // ---- Writing, as the tool or an editor would ----

    /// <summary>The tool's save: the .gumx and every element file.</summary>
    public void SaveAll(SweepSpec spec)
    {
        GumProjectSave project = Build(spec);
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
        project.Save(Gumx, saveElements: true);
        foreach (BehaviorSave behavior in project.Behaviors)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BehaviorFile)!);
            behavior.Save(BehaviorFile);
        }
    }

    /// <summary>Serializes just one element of the spec to a string, as a text editor would hold it.</summary>
    public string ElementText(SweepSpec spec, string elementName)
    {
        string temp = Path.Combine(Dir, "serialize_" + Path.GetRandomFileName());
        try
        {
            ElementSave element = Build(spec).AllElements.Single(e => e.Name == elementName);
            element.Save(temp, true);
            return File.ReadAllText(temp);
        }
        finally
        {
            File.Delete(temp);
        }
    }

    /// <summary>A text editor overwriting the file in place.</summary>
    public void EditInPlace(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    /// <summary>Write a temp file next to it, then replace: the watcher sees a delete and a create/rename.</summary>
    public void EditAtomic(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + ".tmp";
        File.WriteAllText(temp, text);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        File.Move(temp, path);
    }

    public void Edit(bool atomic, string path, string text)
    {
        if (atomic) EditAtomic(path, text); else EditInPlace(path, text);
    }

    // ---- Running side ----

    public void Boot(SweepSpec first)
    {
        SaveAll(first);
        GumProjectSave loaded = GumProjectSave.Load(Gumx);
        loaded.Initialize();
        ObjectFinder.Self.GumProjectSave = loaded;
        ElementSaveExtensions.RegisterGueInstantiation("ColoredRectangle", () => new ColoredRectangleRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Text", () => new TextRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Container", () => new ContainerRuntime());
        Screen = loaded.Screens.Single(s => s.Name == "MainScreen")
            .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        Manager = new GumHotReloadManager(
            Gum.GumService.ApplyProjectTextureFilter,
            GumAnimationLoader.LoadAnimationsFromProvider,
            path => Disposed.Add(path),
            () => Now);
        Manager.ReloadCompleted += () => Reloads++;
        Manager.Start(Gumx);
        // Start records the source path; the OS watcher is replaced by NotifyFileChanged.
        Manager.Stop();
    }

    public void Notify(string path) => Manager.NotifyFileChanged(path);

    public void Advance(int milliseconds) => Now = Now.AddMilliseconds(milliseconds);

    /// <summary>Runs one frame of the game's Update.</summary>
    public void Update() => Manager.Update(Roots);

    /// <summary>Notifies each path, waits out the debounce, and runs Update.</summary>
    public void NotifyAndSettle(params string[] paths)
    {
        foreach (string path in paths)
        {
            Notify(path);
        }
        Advance(300);
        Update();
    }

    /// <summary>
    /// The design-time part of a tree as text: name, element, layout, visibility and the Red and Text
    /// the runtime holds. Siblings are sorted by name, since rebuilt instances land in another order
    /// in <c>ContainedElements</c> than a fresh load gives them.
    /// </summary>
    public static string Describe(GraphicalUiElement element, int depth = 0)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append(' ', depth * 2).Append(element.Name).Append(" <").Append(element.ElementSave?.Name).Append("> ")
            .Append(element.X).Append(',').Append(element.Y).Append(' ').Append(element.Width).Append('x').Append(element.Height)
            .Append(element.Visible ? "" : " hidden");
        if (element is ColoredRectangleRuntime rectangle)
        {
            text.Append(" red=").Append(rectangle.Red);
        }
        if (element is TextRuntime label)
        {
            text.Append(" text=").Append(label.Text);
        }
        text.AppendLine();
        foreach (GraphicalUiElement child in element.ContainedElements.Concat(element.Children).Distinct()
                     .Where(child => child.Tag is InstanceSave).OrderBy(child => child.Name, StringComparer.Ordinal))
        {
            text.Append(Describe(child, depth + 1));
        }
        return text.ToString();
    }

    /// <summary>The tree a game that started now, from the files on disk, would show.</summary>
    public string DescribeAFreshLoad()
    {
        GumProjectSave fresh = GumProjectSave.Load(Gumx);
        fresh.Initialize();
        GumProjectSave? running = ObjectFinder.Self.GumProjectSave;
        ObjectFinder.Self.GumProjectSave = fresh;
        try
        {
            return Describe(fresh.Screens.Single(s => s.Name == "MainScreen")
                .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false));
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = running;
        }
    }

    public GraphicalUiElement? Find(string name) => Screen.GetGraphicalUiElementByName(name);

    public int Red(string name) => ((ColoredRectangleRuntime)Find(name)!).Red;

    public int WidgetFillRed() => ((ColoredRectangleRuntime)Find("Widget1")!.GetGraphicalUiElementByName("Fill")!).Red;

    public void Dispose()
    {
        try { Directory.Delete(Dir, recursive: true); } catch { /* best-effort */ }
    }
}
