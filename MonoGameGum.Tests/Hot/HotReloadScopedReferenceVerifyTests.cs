using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System;
using System.Linq;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// Inputs the scoped reload pass (ReloadReferenceScope) could resolve differently from the evaluator, each
/// compared with the full pass on the same files.
/// </summary>
public class HotReloadScopedReferenceVerifyTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static T Add<T>(GumProjectSave project, string name) where T : ElementSave, new()
    {
        T element = new T { Name = name, BaseType = "Container" };
        element.States.Add(new StateSave { Name = "Default", ParentContainer = element });
        if (element is ScreenSave screen) project.Screens.Add(screen);
        else project.Components.Add((ComponentSave)(ElementSave)element);
        return element;
    }

    private static void Set(ElementSave element, string name, float value)
    {
        element.DefaultState!.Variables.RemoveAll(v => v.Name == name);
        element.DefaultState.Variables.Add(new VariableSave { Name = name, Type = "float", Value = value, SetsValue = true });
    }

    private static void Rows(ElementSave element, params string[] rows)
    {
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        list.Value.AddRange(rows);
        element.DefaultState!.VariableLists.Add(list);
    }

    private static GumProjectSave Start()
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = new GumProjectSave();
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container")!.Clone());
        container.DefaultState!.ParentContainer = container;
        project.StandardElements.Add(container);
        return project;
    }

    private static string Describe(GumProjectSave project)
    {
        StringBuilder text = new StringBuilder();
        foreach (ElementSave element in project.AllElements)
            foreach (StateSave state in element.AllStates) foreach (VariableSave variable in state.Variables.OrderBy(v => v.Name))
                text.Append(element.Name).Append(state.Name).Append('.').Append(variable.Name).Append('=').Append(variable.Value).AppendLine();
        return text.ToString();
    }

    private static void Reload(GumProjectSave project, GumProjectSave? previous)
    {
        ObjectFinder.Self.GumProjectSave = previous;
        GumHotReloadManager.ApplyDiff(Array.Empty<GraphicalUiElement>(), project, SystemManagers.Default, previous);
    }

    private static string FullPass(Func<GumProjectSave> build)
    {
        GumProjectSave project = build();
        ObjectFinder.Self.GumProjectSave = project;
        project.ApplyAllVariableReferences();
        return Describe(project);
    }

    private static void AssertMatchesFullPass(Func<float, GumProjectSave> build)
    {
        GumProjectSave running = build(10);
        Reload(running, null);
        GumProjectSave reloaded = build(20);
        Reload(reloaded, running);
        Describe(reloaded).ShouldBe(FullPass(() => build(20)));
    }

    [Fact]
    public void ScreenAndComponentSameName_ReaderOfComponentUpdates()
    {
        AssertMatchesFullPass(width =>
        {
            GumProjectSave project = Start();
            ComponentSave styles = Add<ComponentSave>(project, "Styles");
            Set(styles, "Width", width);
            ScreenSave screenStyles = Add<ScreenSave>(project, "Styles");
            Set(screenStyles, "Width", 1);
            ComponentSave reader = Add<ComponentSave>(project, "Reader");
            Rows(reader, "Width = Components/Styles.Width");
            Set(reader, "Width", 10);
            project.Initialize();
            return project;
        });
    }

    [Fact]
    public void ReferenceWithDifferentCase_ReaderUpdates()
    {
        AssertMatchesFullPass(width =>
        {
            GumProjectSave project = Start();
            ComponentSave styles = Add<ComponentSave>(project, "Styles");
            Set(styles, "Width", width);
            ComponentSave reader = Add<ComponentSave>(project, "Reader");
            Rows(reader, "Width = Components/styles.Width");
            Set(reader, "Width", 10);
            project.Initialize();
            return project;
        });
    }

    [Fact]
    public void ChainThroughNestedInstanceBase_OnlyLastChanges_ReaderUpdates()
    {
        // Reader -> instance of Mid -> Mid's base Leaf; only Leaf's own value changes.
        AssertMatchesFullPass(width =>
        {
            GumProjectSave project = Start();
            ComponentSave reader = Add<ComponentSave>(project, "Reader");
            reader.Instances.Add(new InstanceSave { Name = "M", BaseType = "Mid", ParentContainer = reader });
            Rows(reader, "Width = M.Width");
            Set(reader, "Width", 10);
            ComponentSave mid = Add<ComponentSave>(project, "Mid");
            mid.BaseType = "Leaf";
            ComponentSave leaf = Add<ComponentSave>(project, "Leaf");
            Set(leaf, "Width", width);
            project.Initialize();
            return project;
        });
    }

    [Fact]
    public void ScreenReadsComponentInNonDefaultCategoryState_Updates()
    {
        AssertMatchesFullPass(width =>
        {
            GumProjectSave project = Start();
            ComponentSave styles = Add<ComponentSave>(project, "Styles");
            Set(styles, "Width", width);
            ScreenSave screen = Add<ScreenSave>(project, "Main");
            StateSaveCategory category = new StateSaveCategory { Name = "Size" };
            StateSave big = new StateSave { Name = "Big", ParentContainer = screen };
            big.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 10f, SetsValue = true });
            VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
            list.Value.Add("Width = Components/Styles.Width");
            big.VariableLists.Add(list);
            category.States.Add(big);
            screen.Categories.Add(category);
            project.Initialize();
            return project;
        });
    }

    [Fact]
    public void BehaviorDefaultChanged_ReaderOfFormsPropertyUpdates()
    {
        // Only the behavior file changed; the reader reads the property's declared default through an instance.
        AssertMatchesFullPass(width =>
        {
            GumProjectSave project = Start();
            BehaviorSave behavior = new BehaviorSave { Name = "AmountBehavior" };
            behavior.FormsProperties.Add(new VariableSave { Name = "Amount", Type = "float", Value = width });
            project.Behaviors.Add(behavior);
            ComponentSave control = Add<ComponentSave>(project, "Control");
            control.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "AmountBehavior" });
            ComponentSave reader = Add<ComponentSave>(project, "Reader");
            reader.Instances.Add(new InstanceSave { Name = "C", BaseType = "Control", ParentContainer = reader });
            Rows(reader, "Width = C.Amount");
            Set(reader, "Width", 10);
            project.Initialize();
            return project;
        });
    }

    [Fact]
    public void StandardElementDefaultChanged_ReaderOfStandardInstanceUpdates()
    {
        // Only the Container standard's default Width changes; the reader's instance of it sets nothing.
        AssertMatchesFullPass(width =>
        {
            GumProjectSave project = Start();
            project.StandardElements[0].DefaultState!.SetValue("Width", width, "float");
            ComponentSave reader = Add<ComponentSave>(project, "Reader");
            reader.Instances.Add(new InstanceSave { Name = "R", BaseType = "Container", ParentContainer = reader });
            Rows(reader, "Height = R.Width");
            Set(reader, "Height", 10);
            project.Initialize();
            return project;
        });
    }

    [Fact]
    public void BaseElementVariableChanged_ReaderOfDerivedPathUpdates()
    {
        // Reader names Derived; the value lives on Derived's base, the only file that changes.
        AssertMatchesFullPass(width =>
        {
            GumProjectSave project = Start();
            ComponentSave reader = Add<ComponentSave>(project, "Reader");
            Rows(reader, "Width = Components/Derived.Width");
            Set(reader, "Width", 10);
            ComponentSave derived = Add<ComponentSave>(project, "Derived");
            derived.BaseType = "Base";
            ComponentSave baseElement = Add<ComponentSave>(project, "Base");
            Set(baseElement, "Width", width);
            project.Initialize();
            return project;
        });
    }

    [Fact]
    public void BaseInstanceVariableChanged_ReaderOfInheritedInstanceUpdates()
    {
        // Inner is defined in Base; only Base's Inner.Width changes, and Reader reads it through Derived.
        AssertMatchesFullPass(width =>
        {
            GumProjectSave project = Start();
            ComponentSave reader = Add<ComponentSave>(project, "Reader");
            Rows(reader, "Width = Components/Derived.Inner.Width");
            Set(reader, "Width", 10);
            ComponentSave derived = Add<ComponentSave>(project, "Derived");
            derived.BaseType = "Base";
            derived.Instances.Add(new InstanceSave { Name = "Inner", BaseType = "Container", DefinedByBase = true, ParentContainer = derived });
            ComponentSave baseElement = Add<ComponentSave>(project, "Base");
            baseElement.Instances.Add(new InstanceSave { Name = "Inner", BaseType = "Container", ParentContainer = baseElement });
            Set(baseElement, "Inner.Width", width);
            project.Initialize();
            return project;
        });
    }
}
