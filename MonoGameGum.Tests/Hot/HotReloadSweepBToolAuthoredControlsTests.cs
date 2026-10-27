using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #6878 sweep B: TextBox, ListBox and ComboBox that are project instances (tool-authored), so a
/// reload reaches them through ApplyDiffRecursive and the Forms runtime-property save and restore.
/// </summary>
public class HotReloadSweepBToolAuthoredControlsTests : BaseTestClass
{
    private static readonly HotReloadFormsProjects.Look First = HotReloadFormsProjects.First;
    private static readonly HotReloadFormsProjects.Look Second = HotReloadFormsProjects.Second;

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static ComponentSave NewComponent(string name, string behavior)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behavior });
        return component;
    }

    private static void AddInstance(ComponentSave component, string name, string baseType) =>
        component.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = component });

    private static void SetRed(ComponentSave component, string instance, int red)
    {
        foreach ((string channel, int value) in new[] { ("Red", red), ("Green", 0), ("Blue", 0) })
        {
            component.DefaultState!.Variables.Add(new VariableSave
            {
                Name = instance + "." + channel, Type = "int", Value = value, SetsValue = true
            });
        }
    }

    private static void SetText(ComponentSave component, string instance, string text) =>
        component.DefaultState!.Variables.Add(new VariableSave
        {
            Name = instance + ".Text", Type = "string", Value = text, SetsValue = true
        });

    /// <summary>The standard fixture plus a TextBox, a ListBox and a ComboBox instance in the screen.</summary>
    private static GumProjectSave Build(HotReloadFormsProjects.Look look)
    {
        GumProjectSave project = HotReloadFormsProjects.Build(look);
        foreach (string behavior in new[]
        {
            StandardFormsBehaviorNames.TextBoxBehaviorName, StandardFormsBehaviorNames.ListBoxBehaviorName,
            StandardFormsBehaviorNames.ComboBoxBehaviorName
        })
        {
            project.Behaviors.Add(new BehaviorSave { Name = behavior });
        }

        ComponentSave textBox = NewComponent("Controls/TextBoxStandard", StandardFormsBehaviorNames.TextBoxBehaviorName);
        AddInstance(textBox, "Background", "ColoredRectangle");
        AddInstance(textBox, "TextInstance", "Text");
        AddInstance(textBox, "CaretInstance", "ColoredRectangle");
        SetRed(textBox, "Background", look.Default);
        SetText(textBox, "TextInstance", "");
        project.Components.Add(textBox);

        ComponentSave listBox = NewComponent("Controls/ListBoxStandard", StandardFormsBehaviorNames.ListBoxBehaviorName);
        AddInstance(listBox, "Background", "ColoredRectangle");
        AddInstance(listBox, "ClipContainerInstance", "Container");
        AddInstance(listBox, "InnerPanelInstance", "Container");
        listBox.DefaultState!.Variables.Add(new VariableSave
        {
            Name = "InnerPanelInstance.Parent", Type = "string", Value = "ClipContainerInstance", SetsValue = true
        });
        SetRed(listBox, "Background", look.Default);
        project.Components.Add(listBox);

        ComponentSave comboBox = NewComponent("Controls/ComboBoxStandard", StandardFormsBehaviorNames.ComboBoxBehaviorName);
        AddInstance(comboBox, "Background", "ColoredRectangle");
        AddInstance(comboBox, "TextInstance", "Text");
        SetRed(comboBox, "Background", look.Default);
        SetText(comboBox, "TextInstance", "Pick");
        project.Components.Add(comboBox);

        ScreenSave screen = project.Screens.Single();
        screen.Instances.Add(new InstanceSave { Name = "Text1", BaseType = textBox.Name, ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "List1", BaseType = listBox.Name, ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Combo1", BaseType = comboBox.Name, ParentContainer = screen });
        project.Initialize();
        return project;
    }

    private static GraphicalUiElement LoadScreen() => HotReloadFormsProjects.Load(Build(First));

    private static void Reload(GraphicalUiElement screen, HotReloadFormsProjects.Look look) =>
        HotReloadFormsProjects.Reload(screen, Build(look));

    private static T Control<T>(GraphicalUiElement screen, string name) where T : FrameworkElement =>
        HotReloadFormsProjects.Control<T>(HotReloadFormsProjects.Child(screen, name));

    // ---- TextBox ----

    [Fact]
    public void ApplyDiff_ToolAuthoredTextBox_TextCaretAndSelectionSurvive()
    {
        GraphicalUiElement screen = LoadScreen();
        TextBox textBox = Control<TextBox>(screen, "Text1");
        textBox.Text = "hello world";
        textBox.IsFocused = true;
        textBox.CaretIndex = 6;
        textBox.SelectionLength = 3;

        Reload(screen, Second);

        textBox.Text.ShouldBe("hello world");
        textBox.CaretIndex.ShouldBe(6);
        textBox.SelectionLength.ShouldBe(3);
    }

    [Fact]
    public void ApplyDiff_ToolAuthoredTextBoxTypedIntoAfterReload_InsertsAtTheSavedCaret()
    {
        GraphicalUiElement screen = LoadScreen();
        TextBox textBox = Control<TextBox>(screen, "Text1");
        textBox.Text = "abcd";
        textBox.IsFocused = true;
        textBox.CaretIndex = 2;

        Reload(screen, Second);
        textBox.HandleCharEntered('X');

        textBox.Text.ShouldBe("abXcd");
        textBox.CaretIndex.ShouldBe(3);
    }

    [Fact]
    public void ApplyDiff_ToolAuthoredTextBoxReloadedTwice_KeepsTextCaretAndFocus()
    {
        GraphicalUiElement screen = LoadScreen();
        TextBox textBox = Control<TextBox>(screen, "Text1");
        textBox.Text = "twice";
        textBox.IsFocused = true;
        textBox.CaretIndex = 3;

        Reload(screen, Second);
        Reload(screen, First);

        textBox.Text.ShouldBe("twice");
        textBox.CaretIndex.ShouldBe(3);
        textBox.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void ApplyDiff_ToolAuthoredTextBoxAuthoredBackgroundEdited_BackgroundShowsNewValueAndTextStays()
    {
        GraphicalUiElement screen = LoadScreen();
        TextBox textBox = Control<TextBox>(screen, "Text1");
        textBox.Text = "keep";

        Reload(screen, Second);

        HotReloadFormsProjects.Red(HotReloadFormsProjects.Child(HotReloadFormsProjects.Child(screen, "Text1"), "Background"))
            .ShouldBe(Second.Default);
        textBox.Text.ShouldBe("keep");
    }

    // ---- ListBox ----

    [Fact]
    public void ApplyDiff_ToolAuthoredListBoxWithItems_ItemsAreNotDuplicatedOrLostAndSelectionStays()
    {
        GraphicalUiElement screen = LoadScreen();
        ListBox listBox = Control<ListBox>(screen, "List1");
        for (int i = 0; i < 4; i++)
        {
            listBox.Items!.Add("Item" + i);
        }
        listBox.SelectedIndex = 2;
        int visualCount = listBox.InnerPanel.Children.Count;

        Reload(screen, Second);

        listBox.Items!.Count.ShouldBe(4);
        listBox.InnerPanel.Children.Count.ShouldBe(visualCount);
        listBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void ApplyDiff_ToolAuthoredListBoxThenItemAddedAndSelectedAfterReload_Works()
    {
        GraphicalUiElement screen = LoadScreen();
        ListBox listBox = Control<ListBox>(screen, "List1");
        listBox.Items!.Add("A");
        listBox.Items.Add("B");
        int before = listBox.InnerPanel.Children.Count;

        Reload(screen, Second);
        listBox.Items.Add("Late");
        listBox.SelectedIndex = 2;

        listBox.InnerPanel.Children.Count.ShouldBe(before + 1);
        listBox.SelectedObject.ShouldBe("Late");
    }

    // ---- ComboBox ----

    [Fact]
    public void ApplyDiff_ToolAuthoredComboBoxWithSelection_KeepsItemsAndSelection()
    {
        GraphicalUiElement screen = LoadScreen();
        ComboBox combo = Control<ComboBox>(screen, "Combo1");
        combo.Items!.Add("A");
        combo.Items.Add("B");
        combo.SelectedIndex = 1;

        Reload(screen, Second);

        combo.Items.Count.ShouldBe(2);
        combo.SelectedIndex.ShouldBe(1);
        combo.SelectedObject.ShouldBe("B");
    }
}
