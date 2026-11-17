using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// A reload applies variable references only to the elements the new files can change and to what reads
/// them (<c>ReloadReferenceScope</c>). Each case compares the project a reload leaves with the same files
/// after the full <see cref="ElementSaveExtensions.ApplyAllVariableReferences"/> pass: every value must match.
/// </summary>
public class HotReloadScopedReferenceTests : BaseTestClass
{
    public enum Edit
    {
        Nothing,
        /// <summary>A text-editor edit of Styles alone: every reader still holds the old saved value.</summary>
        StylesChanged,
        /// <summary>A's row is removed; B reads A.</summary>
        ReaderRowRemoved,
        /// <summary>A's row reads another Styles variable.</summary>
        ReaderRetargeted,
        /// <summary>Item's own value changed; Holder reads it through its instance, not through a row path.</summary>
        InstanceTypeChanged,
        /// <summary>A's value changed; Derived inherits it and reads it.</summary>
        BaseTypeChanged,
        /// <summary>LoopA and LoopB read each other.</summary>
        LoopChanged,
        /// <summary>Styles is deleted; A still names it.</summary>
        SourceDeleted,
    }

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static ComponentSave Component(GumProjectSave project, string name, string baseType = "Container")
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        project.Components.Add(component);
        return component;
    }

    private static void Set(ElementSave element, string name, float value)
    {
        StateSave state = element.DefaultState!;
        state.Variables.RemoveAll(v => v.Name == name);
        state.Variables.Add(new VariableSave { Name = name, Type = "float", Value = value, SetsValue = true });
    }

    private static void Rows(ElementSave element, params string[] rows)
    {
        StateSave state = element.DefaultState!;
        state.VariableLists.RemoveAll(l => l.Name == "VariableReferences");
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        list.Value.AddRange(rows);
        state.VariableLists.Add(list);
    }

    private static ElementSave Get(GumProjectSave project, string name) =>
        project.Components.Single(c => c.Name == name);

    /// <summary>
    /// The project as saved: every stored value is what the rows computed when Styles.Width was 10.
    /// Holder comes before Item and LoopA before LoopB, so the pass order applies them first.
    /// </summary>
    private static GumProjectSave Build(Edit edit, float stylesWidth)
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = new GumProjectSave();
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container")!.Clone());
        container.DefaultState!.ParentContainer = container;
        project.StandardElements.Add(container);

        ComponentSave holder = Component(project, "Holder");
        holder.Instances.Add(new InstanceSave { Name = "Inner", BaseType = "Item", ParentContainer = holder });
        Rows(holder, "Height = Inner.Width", "Width = Inner.Height");
        Set(holder, "Height", 10);
        Set(holder, "Width", 10);

        ComponentSave item = Component(project, "Item");
        Set(item, "Width", 10);
        Rows(item, "Height = Components/Styles.Width");
        Set(item, "Height", 10);

        ComponentSave styles = Component(project, "Styles");
        Set(styles, "Width", 10);
        Set(styles, "Height", 30);

        ComponentSave a = Component(project, "A");
        Rows(a, "Width = Components/Styles.Width");
        Set(a, "Width", 10);

        ComponentSave b = Component(project, "B");
        Rows(b, "Width = Components/A.Width");
        Set(b, "Width", 10);

        ComponentSave derived = Component(project, "Derived", "A");
        Rows(derived, "Height = Width");
        Set(derived, "Height", 10);

        ComponentSave loopA = Component(project, "LoopA");
        Rows(loopA, "Width = Components/LoopB.Height");
        Set(loopA, "Width", 10);
        ComponentSave loopB = Component(project, "LoopB");
        Rows(loopB, "Height = Components/Styles.Width", "Width = Components/LoopA.Width");
        Set(loopB, "Height", 10);
        Set(loopB, "Width", 10);

        Component(project, "Unrelated");
        Set(Get(project, "Unrelated"), "Width", 5);

        Set(styles, "Width", stylesWidth);
        switch (edit)
        {
            case Edit.StylesChanged: Set(styles, "Width", stylesWidth + 20); break;
            case Edit.ReaderRowRemoved: Rows(a); break;
            case Edit.ReaderRetargeted: Rows(a, "Width = Components/Styles.Height"); break;
            case Edit.InstanceTypeChanged: Set(item, "Width", 44); break;
            case Edit.BaseTypeChanged: Rows(a); Set(a, "Width", 55); break;
            case Edit.LoopChanged: Set(loopB, "Height", 66); Rows(loopB, "Width = Components/LoopA.Width"); break;
            case Edit.SourceDeleted: project.Components.Remove(styles); break;
        }
        project.Initialize();
        return project;
    }

    private static string Describe(GumProjectSave project)
    {
        StringBuilder text = new StringBuilder();
        foreach (ElementSave element in project.AllElements)
        {
            foreach (StateSave state in element.AllStates)
            {
                foreach (VariableSave variable in state.Variables.OrderBy(v => v.Name))
                {
                    text.Append(element.Name).Append('.').Append(variable.Name).Append('=').Append(variable.Value).AppendLine();
                }
            }
        }
        return text.ToString();
    }

    private static string FullPass(Edit edit, float stylesWidth)
    {
        GumProjectSave project = Build(edit, stylesWidth);
        ObjectFinder.Self.GumProjectSave = project;
        project.ApplyAllVariableReferences();
        return Describe(project);
    }

    private static GumProjectSave Reload(GumProjectSave project, GumProjectSave? previous)
    {
        ObjectFinder.Self.GumProjectSave = previous;
        GumHotReloadManager.ApplyDiff(Array.Empty<GraphicalUiElement>(), project, SystemManagers.Default, previous);
        return project;
    }

    [Theory]
    [InlineData(Edit.Nothing)]
    [InlineData(Edit.StylesChanged)]
    [InlineData(Edit.ReaderRowRemoved)]
    [InlineData(Edit.ReaderRetargeted)]
    [InlineData(Edit.InstanceTypeChanged)]
    [InlineData(Edit.BaseTypeChanged)]
    [InlineData(Edit.LoopChanged)]
    [InlineData(Edit.SourceDeleted)]
    public void ApplyDiff_NewFiles_MatchesFullPass(Edit edit)
    {
        GumProjectSave running = Reload(Build(Edit.Nothing, 10), null);

        GumProjectSave reloaded = Reload(Build(edit, 10), running);
        Describe(reloaded).ShouldBe(FullPass(edit, 10));

        // A second reload compares with what the first one left.
        GumProjectSave second = Reload(Build(edit, 12), reloaded);
        Describe(second).ShouldBe(FullPass(edit, 12));
    }

    [Theory]
    [InlineData(Edit.StylesChanged)]
    [InlineData(Edit.ReaderRowRemoved)]
    [InlineData(Edit.InstanceTypeChanged)]
    [InlineData(Edit.BaseTypeChanged)]
    [InlineData(Edit.LoopChanged)]
    public void ApplyDiff_ProjectEditedInPlace_MatchesFullPass(Edit edit)
    {
        GumProjectSave project = Reload(Build(Edit.Nothing, 10), null);
        GumProjectSave edited = Build(edit, 10);

        // Copy the edited files' variables and rows into the running project object, the way a test edits one in place.
        foreach (ComponentSave component in project.Components.ToList())
        {
            ElementSave? source = edited.Components.FirstOrDefault(c => c.Name == component.Name);
            if (source == null)
            {
                project.Components.Remove(component);
                continue;
            }
            component.DefaultState!.Variables.Clear();
            component.DefaultState.Variables.AddRange(source.DefaultState!.Variables.Select(v => v.Clone()));
            component.DefaultState.VariableLists.Clear();
            component.DefaultState.VariableLists.AddRange(source.DefaultState.VariableLists.Select(l => l.Clone()));
        }

        Reload(project, project);
        Describe(project).ShouldBe(FullPass(edit, 10));
    }

    [Fact]
    public void ApplyDiff_OneSourceChanged_AppliesOnlyItAndItsReaders()
    {
        GumProjectSave running = Build(Edit.Nothing, 10);
        ObjectFinder.Self.GumProjectSave = running;
        ReloadReferenceScope.Apply(running, null);

        GumProjectSave reloaded = Build(Edit.StylesChanged, 10);
        ObjectFinder.Self.GumProjectSave = reloaded;
        int applied = ReloadReferenceScope.Apply(reloaded, running);

        // Styles and its readers: A, B, Derived (inherits A), LoopA, LoopB, Item and Holder (through Inner).
        // Not Container or Unrelated.
        applied.ShouldBe(8);
    }

    [Fact]
    public void ApplyDiff_SameProjectAgainAfterReaderAppliedFirst_MatchesSecondFullPass()
    {
        // Holder is applied before Item, whose row then changes what Holder read, so a second full pass
        // changes Holder again. The scoped pass must remember Holder was not settled.
        GumProjectSave project = Reload(Build(Edit.StylesChanged, 10), null);
        Reload(project, project);

        GumProjectSave twice = Build(Edit.StylesChanged, 10);
        ObjectFinder.Self.GumProjectSave = twice;
        twice.ApplyAllVariableReferences();
        twice.ApplyAllVariableReferences();
        Describe(project).ShouldBe(Describe(twice));
    }
}
