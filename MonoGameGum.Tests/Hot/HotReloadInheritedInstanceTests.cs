#pragma warning disable CS0618
using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #7991: an edit to the instances a base element defines (add, remove, retype, reorder, rename) reaches a
/// shown element that inherits them, at any depth, and the tree equals a restart. Each project is written
/// the way the tool saves it: a derived element lists every inherited instance (DefinedByBase) in the base's
/// order ahead of its own, and the tool follows a base edit into those copies.
/// </summary>
public class HotReloadInheritedInstanceTests : BaseTestClass
{
    public enum BaseEdit { Add, Remove, Retype, Reorder, Rename }

    public enum Shown { ComponentAsRoot, ComponentInAScreen, DerivedScreen }

    public sealed record Case(
        BaseEdit Edit, int Depth, Shown Shown, bool DerivedOverrides, bool DerivedHasOwn, bool LeafEditedToo)
    {
        public override string ToString() =>
            $"{Edit} depth{Depth} {Shown}{(DerivedOverrides ? " override" : "")}{(DerivedHasOwn ? " own" : "")}{(LeafEditedToo ? " leafEdited" : "")}";
    }

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    public static IEnumerable<object[]> Cases()
    {
        int index = 0;
        foreach (BaseEdit edit in new[] { BaseEdit.Add, BaseEdit.Remove, BaseEdit.Retype, BaseEdit.Reorder, BaseEdit.Rename })
        foreach (int depth in new[] { 1, 2 })
        foreach (Shown shown in new[] { Shown.ComponentAsRoot, Shown.ComponentInAScreen, Shown.DerivedScreen })
        foreach (bool overrides in new[] { false, true })
        foreach (bool own in new[] { false, true })
        foreach (bool leafEdited in new[] { false, true })
        {
            if (SweepScale.Includes(index++))
            {
                yield return new object[] { new Case(edit, depth, shown, overrides, own, leafEdited) };
            }
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void BaseInstanceEdit_ReachesTheShownDerivedElement_LikeARestart(Case c)
    {
        (string hot, string fresh, HotReloadResult result) = Reload(c);

        hot.ShouldBe(fresh, c.ToString());
        result.MatchesRestart.ShouldBeTrue(c + ": " + string.Join(" | ", result.Issues.Select(i => i.Message)));
    }

    [Fact]
    public void BaseGainsAnInstance_DepthTwo_ComponentInAScreen_AddsItAheadOfTheDerivedOwnInstance()
    {
        (string hot, _, HotReloadResult result) = Reload(new Case(BaseEdit.Add, 2, Shown.ComponentInAScreen, DerivedOverrides: true, DerivedHasOwn: true, LeafEditedToo: false));

        hot.ShouldContain(
            "    Dot <ColoredRectangle> red=200\n    Added <ColoredRectangle> red=55\n    Tail <ColoredRectangle> red=66\n" +
            "    Card <Leaf>\n      Inner <ColoredRectangle> red=88\n    MidDot <ColoredRectangle> red=44\n    Own <ColoredRectangle> red=11\n");
        result.MatchesRestart.ShouldBeTrue();
    }

    [Fact]
    public void BaseLosesAnInstance_ComponentAsRoot_RemovesItAndKeepsTheOverride()
    {
        (string hot, _, HotReloadResult result) = Reload(new Case(BaseEdit.Remove, 1, Shown.ComponentAsRoot, DerivedOverrides: true, DerivedHasOwn: false, LeafEditedToo: false));

        hot.ShouldNotContain("Tail");
        hot.ShouldContain("Dot <ColoredRectangle> red=200");
        result.MatchesRestart.ShouldBeTrue();
    }

    // ---- The world ----

    private (string hot, string fresh, HotReloadResult result) Reload(Case c)
    {
        ElementSaveExtensions.RegisterGueInstantiation("ColoredRectangle", () => new ColoredRectangleRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Text", () => new TextRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Container", () => new ContainerRuntime());

        GumProjectSave before = Build(c, edited: false);
        ObjectFinder.Self.GumProjectSave = before;
        GraphicalUiElement root = ShownElement(before, c).ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        GumProjectSave after = Build(c, edited: true);
        HotReloadResult result = GumHotReloadManager.ApplyDiff(new[] { root }, after, SystemManagers.Default, before);

        ObjectFinder.Self.GumProjectSave = after;
        GraphicalUiElement restarted = ShownElement(after, c).ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        return (Describe(root), Describe(restarted), result);
    }

    private static ElementSave ShownElement(GumProjectSave project, Case c) => c.Shown switch
    {
        Shown.ComponentInAScreen => project.Screens.Single(s => s.Name == "Main"),
        Shown.DerivedScreen => project.Screens.Single(s => s.Name == "Derived"),
        _ => project.Components.Single(s => s.Name == "Derived"),
    };

    /// <summary>The design-time tree in child order, with the values a base edit could disturb.</summary>
    private static string Describe(GraphicalUiElement element, int depth = 0)
    {
        StringBuilder text = new StringBuilder();
        text.Append(' ', depth * 2).Append(element.Name).Append(" <").Append(element.ElementSave?.Name).Append('>');
        if (element is ColoredRectangleRuntime rectangle)
        {
            text.Append(" red=").Append(rectangle.Red);
        }
        if (element is TextRuntime label)
        {
            text.Append(" text=").Append(label.Text);
        }
        text.Append('\n');
        foreach (GraphicalUiElement child in element.Children.Where(child => child.Tag is InstanceSave))
        {
            text.Append(Describe(child, depth + 1));
        }
        return text.ToString();
    }

    private static GumProjectSave Build(Case c, bool edited)
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

        ComponentSave leaf = New<ComponentSave>("Leaf", "Container");
        AddInstance(leaf, "Inner", "ColoredRectangle");
        Set(leaf, "Inner.Red", 88);
        if (edited && c.LeafEditedToo)
        {
            AddInstance(leaf, "Inner2", "ColoredRectangle");
            Set(leaf, "Inner2.Red", 99);
        }
        project.Components.Add(leaf);

        bool screens = c.Shown == Shown.DerivedScreen;
        string BaseTypeOf(string layer) => layer == "Base" ? (screens ? "" : "Container") : layer == "Mid" ? "Base" : (c.Depth == 2 ? "Mid" : "Base");
        ElementSave NewLayer(string name)
        {
            ElementSave element = screens ? New<ScreenSave>(name, BaseTypeOf(name)) : New<ComponentSave>(name, BaseTypeOf(name));
            if (screens) project.Screens.Add((ScreenSave)element); else project.Components.Add((ComponentSave)element);
            return element;
        }

        // The base layer, before and after the edit.
        ElementSave baseLayer = NewLayer("Base");
        string tail = edited && c.Edit == BaseEdit.Rename ? "Tail2" : "Tail";
        List<(string name, string type)> baseInstances = new List<(string, string)>
        {
            ("Dot", "ColoredRectangle"),
            (tail, edited && c.Edit == BaseEdit.Retype ? "Text" : "ColoredRectangle"),
            ("Card", "Leaf"),
        };
        if (edited && c.Edit == BaseEdit.Add) baseInstances.Insert(1, ("Added", "ColoredRectangle"));
        if (edited && c.Edit == BaseEdit.Remove) baseInstances.RemoveAll(i => i.name == "Tail");
        if (edited && c.Edit == BaseEdit.Reorder) baseInstances.Reverse();
        foreach ((string name, string type) in baseInstances)
        {
            AddInstance(baseLayer, name, type);
            if (name == "Dot") Set(baseLayer, "Dot.Red", 77);
            if (name == "Added") Set(baseLayer, "Added.Red", 55);
            if (name == tail && type == "ColoredRectangle") Set(baseLayer, tail + ".Red", 66);
            if (name == tail && type == "Text") Set(baseLayer, tail + ".Text", "T", "string");
        }

        List<ElementSave> chain = new List<ElementSave> { baseLayer };
        if (c.Depth == 2)
        {
            ElementSave mid = NewLayer("Mid");
            InheritFrom(mid, chain);
            AddInstance(mid, "MidDot", "ColoredRectangle");
            Set(mid, "MidDot.Red", 44);
            chain.Add(mid);
        }

        ElementSave derived = NewLayer("Derived");
        InheritFrom(derived, chain);
        if (c.DerivedHasOwn)
        {
            AddInstance(derived, "Own", "ColoredRectangle");
            Set(derived, "Own.Red", 11);
        }
        if (c.DerivedOverrides)
        {
            Set(derived, "Dot.Red", 200);
        }

        if (c.Shown == Shown.ComponentInAScreen)
        {
            ScreenSave main = New<ScreenSave>("Main", "");
            AddInstance(main, "Shown", "Derived");
            project.Screens.Add(main);
        }

        project.Initialize();
        return project;
    }

    // As the tool saves a derived element: every inherited instance, in its owner's order, ahead of its own.
    private static void InheritFrom(ElementSave derived, List<ElementSave> chain)
    {
        foreach (InstanceSave inherited in chain.SelectMany(layer => layer.Instances.Where(i => !i.DefinedByBase)))
        {
            InstanceSave copy = inherited.Clone();
            copy.DefinedByBase = true;
            copy.ParentContainer = derived;
            derived.Instances.Add(copy);
        }
    }

    private static T New<T>(string name, string baseType) where T : ElementSave, new()
    {
        T element = new T { Name = name, BaseType = baseType };
        element.States.Add(new StateSave { Name = "Default", ParentContainer = element });
        return element;
    }

    private static void AddInstance(ElementSave owner, string name, string baseType) =>
        owner.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = owner });

    private static void Set(ElementSave owner, string name, object value, string type = "int")
    {
        owner.DefaultState!.Variables.RemoveAll(v => v.Name == name);
        owner.DefaultState!.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });
    }
}
