using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Managers;
using Gum.Wireframe;
using Moq;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A from-file ComboBox with no ListBoxInstance child builds its drop-down lazily from the project's
/// ListBoxBehavior component, the first time the drop-down opens (#6728).
/// </summary>
public class ComboBoxLazyDropDownTests : BaseTestClass
{
    private int _templateCalls;

    public override void Dispose()
    {
        ComboBox.FromFileDropDownTemplate = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private void RegisterCountingTemplate()
    {
        ComboBox.FromFileDropDownTemplate = new VisualTemplate((_, _) =>
        {
            _templateCalls++;
            return new ListBoxVisual(tryCreateFormsObject: false);
        });
    }

    // A ComboBox visual like a from-file project's whose component has no ListBoxInstance child.
    private static ComboBox CreateComboBoxWithoutListBoxInstance(params string[] items)
    {
        ComboBoxVisual visual = new ComboBoxVisual(tryCreateFormsObject: false);
        visual.Children.Remove(visual.GetGraphicalUiElementByName("ListBoxInstance")!);
        ComboBox comboBox = new ComboBox(visual);
        foreach (string item in items)
        {
            comboBox.Items!.Add(item);
        }
        comboBox.AddToRoot();
        return comboBox;
    }

    [Fact]
    public void ListBoxInstanceChild_IsUsedEvenWhenALazyTemplateIsRegistered()
    {
        RegisterCountingTemplate();

        ComboBox comboBox = new ComboBox();
        comboBox.IsDropDownOpen = true;

        _templateCalls.ShouldBe(0);
        comboBox.ListBox.ShouldNotBeNull();
    }

    [Fact]
    public void ListBoxInstanceChild_ListBoxExistsBeforeTheFirstOpen()
    {
        ComboBox comboBox = new ComboBox();

        comboBox.ListBox.ShouldNotBeNull();
        comboBox.ListBox.IsVisible.ShouldBeFalse();
    }

    [Fact]
    public void NoListBoxInstance_WithoutALazyTemplate_Throws()
    {
        ComboBoxVisual visual = new ComboBoxVisual(tryCreateFormsObject: false);
        visual.Children.Remove(visual.GetGraphicalUiElementByName("ListBoxInstance")!);

        Should.Throw<System.Exception>(() => new ComboBox(visual));
    }

    [Fact]
    public void Lazy_ItemsAndSelectionBeforeOpen_DoNotBuildTheListBox()
    {
        RegisterCountingTemplate();
        ComboBox comboBox = CreateComboBoxWithoutListBoxInstance("Apple", "Banana", "Cherry");

        comboBox.SelectedIndex = 1;

        _templateCalls.ShouldBe(0);
        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.SelectedObject.ShouldBe("Banana");
        comboBox.Text.ShouldBe("Banana");
        comboBox.Items!.Count.ShouldBe(3);
    }

    [Fact]
    public void Lazy_SelectedObjectBeforeOpen_RaisesSelectionChangedAndUpdatesText()
    {
        RegisterCountingTemplate();
        ComboBox comboBox = CreateComboBoxWithoutListBoxInstance("Apple", "Banana", "Cherry");
        List<object?> added = new();
        comboBox.SelectionChanged += (_, args) => added.Add(args.AddedItems.Cast<object?>().FirstOrDefault());

        comboBox.SelectedObject = "Cherry";

        added.ShouldBe(new object?[] { "Cherry" });
        comboBox.SelectedIndex.ShouldBe(2);
        comboBox.Text.ShouldBe("Cherry");
        _templateCalls.ShouldBe(0);
    }

    [Fact]
    public void Lazy_FirstOpen_BuildsTheListBoxOnceWithTheItemsAndSelection()
    {
        RegisterCountingTemplate();
        ComboBox comboBox = CreateComboBoxWithoutListBoxInstance("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 2;

        comboBox.IsDropDownOpen = true;

        _templateCalls.ShouldBe(1);
        comboBox.IsDropDownOpen.ShouldBeTrue();
        comboBox.ListBox.Items.ShouldBeSameAs(comboBox.Items);
        comboBox.ListBox.ListBoxItems.Count.ShouldBe(3);
        comboBox.ListBox.SelectedIndex.ShouldBe(2);

        comboBox.IsDropDownOpen = false;
        comboBox.IsDropDownOpen = true;
        _templateCalls.ShouldBe(1);
    }

    [Fact]
    public void Lazy_SelectingARowAfterOpen_UpdatesTextAndRaisesSelectionChanged()
    {
        RegisterCountingTemplate();
        ComboBox comboBox = CreateComboBoxWithoutListBoxInstance("Apple", "Banana", "Cherry");
        int raised = 0;
        comboBox.SelectionChanged += (_, _) => raised++;
        comboBox.IsDropDownOpen = true;

        comboBox.ListBox.SelectedIndex = 0;

        raised.ShouldBe(1);
        comboBox.SelectedObject.ShouldBe("Apple");
        comboBox.Text.ShouldBe("Apple");
    }

    [Fact]
    public void Lazy_FirstOpen_PlacesTheDropDownBelowTheComboBoxAtItsWidth()
    {
        RegisterCountingTemplate();
        ComboBox comboBox = CreateComboBoxWithoutListBoxInstance("Apple");

        comboBox.IsDropDownOpen = true;

        comboBox.ListBox.Visual.AbsoluteLeft.ShouldBe(comboBox.Visual.AbsoluteLeft);
        comboBox.ListBox.Visual.AbsoluteTop.ShouldBe(comboBox.Visual.AbsoluteBottom);
        comboBox.ListBox.Visual.AbsoluteWidth.ShouldBe(comboBox.Visual.AbsoluteWidth);
    }

    [Fact]
    public void Lazy_EnterPressed_OpensAndFocusesTheItems()
    {
        RegisterCountingTemplate();
        ComboBox comboBox = CreateComboBoxWithoutListBoxInstance("Apple", "Banana");
        comboBox.SelectedIndex = 1;
        Mock<Gum.Input.IInputReceiverKeyboardMonoGame> keyboard = new Mock<Gum.Input.IInputReceiverKeyboardMonoGame>();
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeyPushed(Gum.Forms.Input.Keys.Enter)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeysTyped).Returns(new List<Gum.Forms.Input.Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        comboBox.OnFocusUpdate();

        comboBox.IsDropDownOpen.ShouldBeTrue();
        comboBox.ListBox.DoListItemsHaveFocus.ShouldBeTrue();
        comboBox.ListBox.ListBoxItems[1].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Lazy_ItemsReplacedBeforeOpen_ClearsASelectionNotInTheNewItems()
    {
        RegisterCountingTemplate();
        ComboBox comboBox = CreateComboBoxWithoutListBoxInstance("Apple", "Banana");
        comboBox.SelectedIndex = 1;

        comboBox.Items = new ObservableCollection<object> { "Cherry" };

        comboBox.SelectedIndex.ShouldBe(-1);
        comboBox.SelectedObject.ShouldBeNull();
        _templateCalls.ShouldBe(0);
    }

    private static ComponentSave AddListBoxComponent(GumProjectSave project, string name)
    {
        if (!project.StandardElements.Any(standard => standard.Name == "Container"))
        {
            StandardElementSave standard = new StandardElementSave { Name = "Container" };
            standard.States.Add(new StateSave { Name = "Default", ParentContainer = standard });
            project.StandardElements.Add(standard);
        }
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = StandardFormsBehaviorNames.ListBoxBehaviorName });
        project.Components.Add(component);
        return component;
    }

    [Fact]
    public void Register_ProjectWithListBoxComponents_UsesTheBehaviorsDefaultImplementationLazily()
    {
        GumProjectSave project = new GumProjectSave();
        AddListBoxComponent(project, "Controls/ListBox");
        ComponentSave darkListBox = AddListBoxComponent(project, "Controls/ListBoxDark");
        project.Behaviors.Add(new BehaviorSave
        {
            Name = StandardFormsBehaviorNames.ListBoxBehaviorName,
            DefaultImplementation = "Controls/ListBoxDark",
        });
        ObjectFinder.Self.GumProjectSave = project;

        FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        ComboBox.FromFileDropDownTemplate.ShouldNotBeNull();
        ComboBox.FromFileDropDownTemplate.CreateContent(null).ShouldBeOfType<InteractiveGue>()
            .ElementSave.ShouldBeSameAs(darkListBox);
    }

    [Fact]
    public void Register_ProjectWithNoListBoxComponent_LeavesNoLazyTemplate()
    {
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();

        FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        ComboBox.FromFileDropDownTemplate.ShouldBeNull();
    }

    #region Parity with the ListBoxInstance path while closed

    private static string Snapshot(ComboBox comboBox) =>
        $"idx={comboBox.SelectedIndex} obj={comboBox.SelectedObject ?? "null"} text={(string.IsNullOrEmpty(comboBox.Text) ? "null" : comboBox.Text)}";

    // Runs the same steps on a ListBoxInstance-child ComboBox and on a closed lazy one, both with
    // ["Apple","Banana","Cherry"] and Banana selected, and returns what each ends up showing.
    private (string child, string lazy) RunOnBoth(System.Func<ComboBox, string> steps)
    {
        RegisterCountingTemplate();
        List<string> results = new();
        foreach (bool lazy in new[] { false, true })
        {
            ComboBox comboBox = lazy ? CreateComboBoxWithoutListBoxInstance("Apple", "Banana", "Cherry") : new ComboBox();
            if (!lazy)
            {
                foreach (string item in new[] { "Apple", "Banana", "Cherry" })
                {
                    comboBox.Items!.Add(item);
                }
                comboBox.AddToRoot();
            }
            comboBox.SelectedIndex = 1;
            List<string> events = new();
            comboBox.SelectionChanged += (_, args) =>
                events.Add($"+{string.Join(",", args.AddedItems.Cast<object>())}-{string.Join(",", args.RemovedItems.Cast<object>())}");
            string extra = steps(comboBox);
            results.Add($"{Snapshot(comboBox)} events=[{string.Join(" ", events)}] {extra}");
        }
        return (results[0], results[1]);
    }

    private void ShouldMatchTheChildPath(System.Func<ComboBox, string> steps)
    {
        (string child, string lazy) = RunOnBoth(steps);
        lazy.ShouldBe(child);
    }

    [Fact]
    public void Lazy_RemovingTheSelectedItem_MatchesTheChildPath() =>
        ShouldMatchTheChildPath(c => { c.Items!.RemoveAt(1); return ""; });

    [Fact]
    public void Lazy_ClearingItems_MatchesTheChildPath() =>
        ShouldMatchTheChildPath(c => { c.Items!.Clear(); return ""; });

    [Fact]
    public void Lazy_ReplacingTheSelectedItem_MatchesTheChildPath() =>
        ShouldMatchTheChildPath(c => { c.Items![1] = "Blueberry"; return ""; });

    [Fact]
    public void Lazy_InsertingBeforeTheSelectedItem_MatchesTheChildPath() =>
        ShouldMatchTheChildPath(c => { c.Items!.Insert(0, "Zero"); return ""; });

    [Fact]
    public void Lazy_RemovingAnItemBeforeTheSelectedItem_MatchesTheChildPath() =>
        ShouldMatchTheChildPath(c => { c.Items!.RemoveAt(0); return ""; });

    [Fact]
    public void Lazy_SwappingItemsForOnesThatContainTheSelection_MatchesTheChildPath() =>
        ShouldMatchTheChildPath(c => { c.Items = new ObservableCollection<object> { "Banana", "Kiwi" }; return ""; });

    [Fact]
    public void Lazy_RemovingTheSelectedItemThenOpening_MatchesTheChildPath() =>
        ShouldMatchTheChildPath(c => { c.Items!.RemoveAt(1); c.IsDropDownOpen = true; return $"listBox={c.ListBox.SelectedIndex}"; });

    [Fact]
    public void Lazy_ClearingItemsThenOpening_MatchesTheChildPath() =>
        ShouldMatchTheChildPath(c => { c.Items!.Clear(); c.IsDropDownOpen = true; return $"listBox={c.ListBox.SelectedIndex}"; });

    [Fact]
    public void Lazy_ReplacingTheSelectedItemThenOpening_MatchesTheChildPath() =>
        ShouldMatchTheChildPath(c => { c.Items![1] = "Blueberry"; c.IsDropDownOpen = true; return $"listBox={c.ListBox.SelectedIndex}/{c.ListBox.SelectedObject ?? "null"}"; });

    [Fact]
    public void Lazy_ItemsAddedToASwappedCollection_AreShownOnOpen()
    {
        RegisterCountingTemplate();
        ComboBox comboBox = CreateComboBoxWithoutListBoxInstance("Apple");
        ObservableCollection<object> swapped = new() { "Kiwi" };
        comboBox.Items = swapped;
        swapped.Add("Lime");
        comboBox.SelectedIndex = 1;

        comboBox.IsDropDownOpen = true;

        comboBox.ListBox.ListBoxItems.Count.ShouldBe(2);
        comboBox.ListBox.SelectedIndex.ShouldBe(1);
        comboBox.Text.ShouldBe("Lime");
    }

    #endregion
}
